using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace TSqlWatcher
{
	internal class SqlChangeHandler
	{
		enum FileType
		{
			Unknown,
			Function,
			Procedure,
			View
		}

		private static RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase;

		private static Regex functionRegex
			= new Regex(@"create\s+function\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);
		private static Regex storedProcedureRegex
			= new Regex(@"create\s+procedure\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);
		private static Regex viewRegex
			= new Regex(@"create\s+view\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}(.*)\]{0,1}", regexOptions);

		private readonly Settings settings;

		public SqlChangeHandler(Settings settings)
		{
			this.settings = settings;
		}

		public void Handle(string path)
		{
			if (!File.Exists(path))
			{
				return; // TODO: add special handling to remove old code
			}

			var content = GetContent(path);
			if (content == null)
			{
				return;
			}

			var fileType = GetFileType(content);
			switch (fileType)
			{
				case FileType.Function: 
					RecreateEntity(content, functionRegex, "function"); break;
				
				// TODO: add special handling if view is schema bound
				case FileType.View:
					RecreateEntity(content, functionRegex, "view");
					break;
				
				case FileType.Procedure:
					RecreateEntity(content, storedProcedureRegex, "procedure"); 
					break;
				
				default: 
					break;
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


		private void RecreateEntity(string content, Regex regex, string kind)
		{
			var match = regex.Match(content);
			var name = match.Groups[1].Value;
			try
			{
				using (var connection = new SqlConnection(settings.ConnectionString))
				{
					connection.Open();
					using (var transaction = connection.BeginTransaction())
					{
						DropEntity(kind, name, transaction);
						CreateEntity(content, transaction);

						if (transaction.Connection != null)
						{
							transaction.Commit();
						}
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

			Console.WriteLine("{0} | updated {1} {2}", DateTime.Now.TimeOfDay, kind, name);
		}

		private void DropEntity(string kind, string name, SqlTransaction transaction)
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

		private void CreateEntity(string content, SqlTransaction transaction)
		{
			try
			{
				using (var command = transaction.Connection.CreateCommand())
				{
					command.CommandText = content;
					command.Transaction = transaction;
					command.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				Log(ex);
				transaction.Rollback();
			}
		}

		private static void Log(Exception ex)
		{
			Console.WriteLine("{0} | exception of type {1} happened. Message: {2}", DateTime.Now.TimeOfDay, ex.GetType().Name, ex.Message);
		}

		private FileType GetFileType(string content)
		{
			if (functionRegex.IsMatch(content)) return FileType.Function;
			if (storedProcedureRegex.IsMatch(content)) return FileType.Procedure;
			if (viewRegex.IsMatch(content)) return FileType.View;
			return FileType.Unknown;
		}
	}
}
