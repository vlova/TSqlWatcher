using System;
using System.Data.SqlClient;
using System.Linq;

namespace TSqlWatcher
{
	class DropCommand : ICommand
	{
		public SqlEntity Entity { get; private set; }

		public DropCommand(SqlEntity entity)
		{
			this.Entity = entity;
		}

		public void Perform(SqlTransaction transaction, SqlProjectInfo project, CommandAdder commandAdder)
		{
			var criticalError = false;
			try
			{
				using (var command = transaction.Connection.CreateCommand())
				{
					command.CommandText = "drop " + Entity.Type.ToString().ToLower() + " dbo." + Entity.Name;
					command.Transaction = transaction;
					command.ExecuteNonQuery();
				}

				Logger.Log("dropped {0}", Entity);


			}
			catch (Exception ex)
			{
				if (ex.Message.Contains(" it does not exist "))
				{
					// ignore
				}
				else
				{
					criticalError = true;
					Logger.Log(ex);
				}
			}
			finally
			{
				if (!criticalError)
				{
					// command to update memory data must be performed only after sql commands
					commandAdder(new LambdaCommand((_, proj, _1) => UpdateProjectInfo(proj)));
				}
			}
		}

		private void UpdateProjectInfo(SqlProjectInfo project)
		{
			project.FileToEntityMapping.Remove(Entity.Path);

			foreach (var pair in project.DependentEntities.ToList())
			{
				var keyName = pair.Key;
				var coll = pair.Value;

				if (coll.Contains(Entity))
				{
					coll.Remove(Entity);
				}

				if (pair.Key == Entity.Name)
				{
					if (coll.Any())
					{
						Logger.Log("impossible");
					}

					project.DependentEntities.Remove(pair.Key);
				}
			}
		}

	}
}
