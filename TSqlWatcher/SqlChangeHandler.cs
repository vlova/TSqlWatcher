using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace TSqlWatcher
{
	internal class SqlChangeHandler
	{
		private static RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase;

		private static Regex functionRegex
			= new Regex(@"create\s+function\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);
		private static Regex storedProcedureRegex
			= new Regex(@"create\s+procedure\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);
		private static Regex viewRegex
			= new Regex(@"create\s+view\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);

		private readonly Settings settings;
		// file path -> (file type, entity name) 
		private Dictionary<string, SqlEntity> fileToEntityMapping;

		private object locker = new object();
		private SqlConnection connection;
		private SqlTransaction transaction;

		public SqlChangeHandler(Settings settings)
		{
			this.settings = settings;
		}

		public void Prepare()
		{
			fileToEntityMapping = Directory
				.EnumerateFiles(settings.Path, "*.sql", SearchOption.AllDirectories)
				.Select(path => new { path, content = GetContent(path) })
				.Select(e => new SqlEntity
				{
					Path = e.path,
					Type = GetEntityType(e.content),
					Name = GetEntityName(e.content)
				})
				.Where(e => e.Type != SqlEntityType.Unknown)
				.ToDictionary(e => e.Path, e => e);
		}

		public void Handle(string oldPath, string newPath)
		{
			lock (locker)
			{
				try
				{
					using (connection = new SqlConnection(settings.ConnectionString))
					{
						connection.Open();
						using (transaction = connection.BeginTransaction())
						{
							HandleInternal(oldPath, newPath);
						}

						if (transaction.Connection != null)
						{
							transaction.Commit();
						}

						if (connection.State == System.Data.ConnectionState.Open)
						{
							connection.Close();
						}
					}
				}
				catch (Exception ex)
				{
					Log(ex);
				}
			}
		}

		public void HandleInternal(string oldPath, string path)
		{
			if (oldPath != null)
			{
				DropEntityByPath(oldPath);
			}

			if (!File.Exists(path))
			{
				return;
			}

			var content = GetContent(path);
			if (content == null)
			{
				Log("failed to get content for file {0}", path);
				return;
			}

			var entity = new SqlEntity
			{

				Path = path,
				Content = content,
				Name = GetEntityName(content),
				Type = GetEntityType(content)
			};

			switch (entity.Type)
			{
				case SqlEntityType.Function:
					RecreateEntity(entity);
					break;

				// TODO: add special handling if view is schema bound
				case SqlEntityType.View:
					RecreateEntity(entity);
					break;

				case SqlEntityType.Procedure:
					RecreateEntity(entity);
					break;

				default:
					break;
			}
		}

		private void DropEntityByPath(string path)
		{
			if (fileToEntityMapping.ContainsKey(path))
			{
				var entity = fileToEntityMapping[path];
				try
				{
					DropEntity(entity.Name, kind: entity.Type.ToString().ToLower());
					Log("dropped {0} {1}", entity.Type.ToString().ToLower(), entity.Name);
					fileToEntityMapping.Remove(path);
				}
				catch (Exception ex)
				{
					Log(ex);
				}
			}
		}

		private static string GetContent(string path)
		{
			var tries = 5;
			while (tries > 0)
			{
				try
				{
					return File.ReadAllText(path);
				}
				catch (FileNotFoundException ex)
				{
					Log(ex);
					return null;
				}
				catch (IOException ex)
				{
					Thread.Sleep(100);
					Log(ex);
				}

				tries--;
			}

			return null;
		}


		private void RecreateEntity(SqlEntity entity)
		{
			try
			{
				DropEntity(entity);
				CreateEntity(entity);
			}
			catch (Exception ex)
			{
				Log(ex);
			}

			Log("updated {0} {1}", entity.Type, entity.Name);
		}

		private void DropEntity(SqlEntity entity)
		{
			var oldName = entity.Name;
			if (fileToEntityMapping.ContainsKey(entity.Path))
			{
				oldName = fileToEntityMapping[entity.Path].Name;
			}

			DropEntity(oldName, kind: entity.Type.ToString().ToLower());
			DropEntity(entity.Name, kind: entity.Type.ToString().ToLower());


			if (entity.Name != oldName)
			{
				Log("dropped {0} {1}", entity.Type.ToString().ToLower(), oldName);
				fileToEntityMapping.Remove(entity.Path);
			}
		}

		private void DropEntity(string name, string kind)
		{
			try
			{
				using (var command = transaction.Connection.CreateCommand())
				{
					command.CommandText = "drop " + kind + " dbo." + name;
					command.Transaction = transaction;
					command.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains(" it does not exist "))
				{
					// ignore
				}
				else
				{
					Log(ex);
				}
			}
		}

		private void CreateEntity(SqlEntity entity)
		{
			try
			{
				using (var command = transaction.Connection.CreateCommand())
				{
					command.CommandText = entity.Content;
					command.Transaction = transaction;
					command.ExecuteNonQuery();
				}

				entity.Content = null;
				fileToEntityMapping[entity.Path] = entity;
			}
			catch (Exception ex)
			{
				Log(ex);
				transaction.Rollback();
			}
		}

		private static void Log(Exception ex)
		{
			Log("exception of type {0} happened. Message: {1}", ex.GetType().Name, ex.Message);
		}

		private static void Log(string message, params object[] args)
		{
			var originalColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.Write(DateTime.Now.TimeOfDay);
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(" | ");
			Console.ForegroundColor = originalColor;
			Console.WriteLine(message, args);
		}

		private SqlEntityType GetEntityType(string content)
		{
			if (functionRegex.IsMatch(content)) return SqlEntityType.Function;
			if (storedProcedureRegex.IsMatch(content)) return SqlEntityType.Procedure;
			if (viewRegex.IsMatch(content)) return SqlEntityType.View;
			return SqlEntityType.Unknown;
		}

		private string GetEntityName(string content)
		{
			return GetEntityName(functionRegex, content)
				?? GetEntityName(viewRegex, content)
				?? GetEntityName(storedProcedureRegex, content);
		}

		private string GetEntityName(Regex regex, string content)
		{
			var match = regex.Match(content);
			if (match.Success)
			{
				return match.Groups[1].Value;
			}
			else
			{
				return null;
			}
		}
	}
}
