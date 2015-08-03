using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace TSqlWatcher
{
	class CreateCommand : ICommand
	{
		public SqlEntity Entity { get; private set; }

		public CreateCommand(SqlEntity entity)
		{
			Entity = entity;
		}

		public void Perform(SqlTransaction transaction, SqlProjectInfo project, CommandAdder commandAdder)
		{
			var criticalError = false;
			try
			{
				using (var command = transaction.Connection.CreateCommand())
				{
					command.CommandText = Entity.Content;
					command.Transaction = transaction;
					command.ExecuteNonQuery();
				}

				Logger.Log("created {0}", Entity);
			}
			catch (Exception ex)
			{
				criticalError = true;
				Logger.Log(ex);
				transaction.Rollback();
			}
			finally
			{
				if (!criticalError)
				{
					// command to update memory data must be performed only after sql commands
					commandAdder(new LambdaCommand((_, proj, _a) => UpdateProjectInfo(proj)));
				}
			}
		}

		private void UpdateProjectInfo(SqlProjectInfo project)
		{
			project.FileToEntityMapping[Entity.Path] = Entity;

			foreach (var entityName in project.FileToEntityMapping.Values.Select(e => e.Name))
			{
				if (Entity.Content.Contains(entityName) && Entity.Name != entityName)
				{
					var dependants = project.DependentEntities.TryGet(entityName) ?? new List<SqlEntity>();

					if (!dependants.Contains(Entity))
					{
						dependants.Add(Entity);
					}

					project.DependentEntities[entityName] = dependants;
				}
			}
		}
	}
}
