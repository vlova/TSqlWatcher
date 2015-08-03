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

		private object locker = new object();
		private SqlConnection connection;
		private SqlTransaction transaction;
		private SqlProjectInfo project;

		public SqlChangeHandler(Settings settings)
		{
			this.settings = settings;
		}

		public void Prepare()
		{
			Logger.Log("started analyzing project");

			var watch = Stopwatch.StartNew();
			project = SqlProjectInfo.Create(settings);

			Logger.Log("finished analyzing project for {0}", watch.Elapsed);
			Console.WriteLine();
			Console.WriteLine();
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
							if (transaction.Connection != null)
							{
								transaction.Commit();
							}
							else
							{
								Logger.Log("auto rollback due errors");
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
					Logger.Log(ex);
				}

				Console.WriteLine(); Console.WriteLine();
			}
		}

		public void HandleInternal(string oldPath, string path)
		{
			var watch = Stopwatch.StartNew();
			Logger.Log("started handling update of file {0}", path.Substring(settings.Path.Length));

			var actions = new List<ICommand>();

			var dependants = project.GetDependantsByPath(oldPath ?? path);
			var reversedDependants = project.GetDependantsByPath(oldPath ?? path, reversed: true);
			actions.AddRange(dependants.Select(e => new DropCommand(e)));

			var oldEntity = project.FileToEntityMapping.TryGet(oldPath ?? path);
			if (oldEntity != null)
			{
				actions.Add(new DropCommand(oldEntity));
			}


			if (File.Exists(path))
			{
				var content = SqlProjectInfo.GetContent(path);
				if (content == null)
				{
					Logger.Log("failed to get content for file {0}", path);
				}
				else
				{
					if (oldEntity.Maybe(_ => _.Content) == content)
					{
						Logger.Log("fake update of {0}", path);
						return; // short circuit return to prevent fake updates
					}

					var entity = SqlEntity.Create(path, content);
					if (oldEntity.Maybe(e => e.Name) != entity.Name)
					{
						actions.Add(new DropCommand(entity));
					}

					actions.Add(new CreateCommand(entity));
				}
			}
			else
			{
				if (dependants.Any())
				{
					Logger.Log("you have removed {0}, but some entities depends on it: {1}",
						project.FileToEntityMapping.TryGet(path).Maybe(e => e.Name) ?? path,
						string.Join(";", dependants));

					return;
				}
			}

			actions.AddRange(reversedDependants.Select(e => new CreateCommand(e)));

			while (transaction.Connection != null && actions.Any())
			{
				foreach (var action in actions.ToList())
				{
					if (transaction.Connection == null)
					{
						break;
					}

					action.Perform(transaction, project, actions.Add);
					actions.Remove(action);
				}
			}

			Logger.Log("finished handling update of file {0} for {1}", path.Substring(settings.Path.Length), watch.Elapsed);
		}
	}
}
