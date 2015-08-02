using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace TSqlWatcher
{
	internal class SqlProjectInfo
	{
		/// <summary>
		/// file path -> (file type, entity name) 
		/// </summary>
		public Dictionary<string, SqlEntity> FileToEntityMapping { get; set; }

		public IEnumerable<SqlEntity> SchemaBoundEntities
		{
			get
			{
				return FileToEntityMapping.Values.Where(e => e.IsSchemaBound);
			}
		}

		/// <summary>
		/// entity name -> list[dependent entities]
		/// </summary>
		public Dictionary<string, List<SqlEntity>> DependentEntities { get; set; }

		public static SqlProjectInfo Create(Settings settings)
		{
			var project = new SqlProjectInfo();
			project.FileToEntityMapping = GetFileToEntityMapping(settings.Path);
			project.DependentEntities = GetDependentEntities(project.FileToEntityMapping);
			return project;
		}

		private static Dictionary<string, SqlEntity> GetFileToEntityMapping(string path)
		{
			return Directory
				.EnumerateFiles(path, "*.sql", SearchOption.AllDirectories)
				.Select(filePath => new { path = filePath, content = GetContent(filePath) })
				.Select(e => SqlEntity.Create(e.path, e.content))
				.Where(e => e.Type != SqlEntityType.Unknown)
				.ToDictionary(e => e.Path, e => e);
		}

		private static Dictionary<string, List<SqlEntity>> GetDependentEntities(Dictionary<string, SqlEntity> fileToEntityMapping)
		{
			var entityNames = fileToEntityMapping.Where(p => p.Value.IsSchemaBound).Select(p => p.Value.Name).ToList();

			return entityNames
				.Select(name => new
				{
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

		public static string GetContent(string path)
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
					Logger.Log(ex);
					return null;
				}
				catch (IOException ex)
				{
					Thread.Sleep(100);
					Logger.Log(ex);
				}

				tries--;
			}

			return null;
		}

		internal IEnumerable<SqlEntity> GetDependantsByPath(string path, bool reversed = false)
		{
			if (path == null) return Enumerable.Empty<SqlEntity>().ToArray();

			var entity = FileToEntityMapping.TryGet(path);
			if (entity == null) return Enumerable.Empty<SqlEntity>().ToArray();
			return GetDependantsByEntity(entity, reversed)
				.DistinctSameOrder()
				.ToArray();
		}

		private IEnumerable<SqlEntity> GetDependantsByEntity(SqlEntity entity, bool reversed)
		{
			var dependants = DependentEntities.TryGet(entity.Name).EmptyIfNull();
			foreach (var dependant in dependants)
			{
				if (!reversed)
				{
					foreach (var grandDependant in GetDependantsByEntity(dependant, reversed))
					{
						yield return grandDependant;
					}
					yield return dependant;
				}
				else
				{
					yield return dependant;
					foreach (var grandDependant in GetDependantsByEntity(dependant, reversed))
					{
						yield return grandDependant;
					}
				}
			}
		}
	}
}
