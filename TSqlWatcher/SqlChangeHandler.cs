using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace TSqlWatcher
{
	internal class SqlChangeHandler
	{
		private readonly Settings settings;
		/// <summary>
		/// file path -> (file type, entity name) 
		/// </summary>
		private Dictionary<string, SqlEntity> fileToEntityMapping;

		/// <summary>
		/// entity name -> list[dependent entities]
		/// </summary>
		private Dictionary<string, List<SqlEntity>> dependentEntities;

		private object locker = new object();
		private SqlConnection connection;
		private SqlTransaction transaction;

		public SqlChangeHandler(Settings settings)
		{
			this.settings = settings;
		}

		public void Prepare()
		{
			Log("started analyzing project");

			var watch = Stopwatch.StartNew(); 
			fileToEntityMapping = GetFileToEntityMapping();
			dependentEntities = GetDependentEntities();
			CleanupMemory();

			Log("finished analyzing project for {0}", watch.Elapsed);
		}

		private Dictionary<string, SqlEntity> GetFileToEntityMapping()
		{
			return Directory
				.EnumerateFiles(settings.Path, "*.sql", SearchOption.AllDirectories)
				.Select(path => new { path, content = GetContent(path) })
				.Select(e => SqlEntity.Create(e.path, e.content))
				.Where(e => e.Type != SqlEntityType.Unknown)
				.ToDictionary(e => e.Path, e => e);
		}

		private Dictionary<string, List<SqlEntity>> GetDependentEntities()
		{
			var entityNames = fileToEntityMapping.Where(p => p.Value.IsSchemaBound).Select(p => p.Value.Name).ToList();

			return entityNames
				.Select(name => new { 
					name, 
					dependent = fileToEntityMapping
						.Select(_ => _.Value)
						.Where(e => e.Name != name)
						.Where(e => e.Content.ContainsInsensetive(name))
						.ToList()
				})
				.Where(d => d.dependent.Any())
				.ToDictionary(d => d.name, d => d.dependent);
		}

		private void CleanupMemory()
		{
			foreach (var entity in fileToEntityMapping.Select(e => e.Value))
			{
				entity.Content = null; // cleanup memory
			}
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

			var entity = SqlEntity.Create(path, content);

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
	}
}
