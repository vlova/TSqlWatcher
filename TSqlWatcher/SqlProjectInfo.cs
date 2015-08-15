using System;
using System.Collections.Generic;
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

		/// <summary>
		/// entity name -> list[dependent entities]
		/// </summary>
		public Dictionary<string, List<SqlEntity>> DependentEntities { get; set; }

		public static SqlProjectInfo Create(Settings settings)
		{
			using (Profiling.Profile())
			{
				var project = new SqlProjectInfo();
				project.FileToEntityMapping = Profiling.Profile("GetFileToEntityMapping", () => GetFileToEntityMapping(settings));
				project.DependentEntities = Profiling.Profile("GetDependentEntities", () => GetDependentEntities(project.FileToEntityMapping));
				//project.VerifyNoCycles();
				//project.PrintCommonWords();
				return project;
			}
		}

		#region developer usage area

		private void PrintCommonWords()
		{
			var groupedWords = FileToEntityMapping.Values
				.SelectMany(v => v.Words)
				.GroupBy(v => v, StringComparer.InvariantCultureIgnoreCase)
				.OrderBy(g => g.Count()).ToList();

			Console.WriteLine("common words: \n{0}", 
				string.Join(
					"\n", 
					groupedWords
						.Select(v => string.Format("{0} {1}", v.Key, v.Count()))
						.Take(100)));
		}

		private SqlEntity GetByName(string name)
		{
			return FileToEntityMapping.Values.FirstOrDefault(e => e.Name == name);
		}

		private void VerifyNoCycles()
		{
			foreach (var name in DependentEntities.Keys)
			{
				VerifyNoCycles(name, new HashSet<string>());
			}
		}

		private void VerifyNoCycles(string name, HashSet<string> parents)
		{
			if (parents.Contains(name)) {
				Console.WriteLine("Cycle is here {0} {1}", parents.Count, string.Join("; ", parents));
				return;
			}

			parents.Add(name);
			var dependants = DependentEntities.TryGet(name).EmptyIfNull();
			foreach (var entity in dependants)
			{
				VerifyNoCycles(entity.Name, new HashSet<string>(parents));
			}
		}

		#endregion

		private static Dictionary<string, SqlEntity> GetFileToEntityMapping(Settings settings)
		{
			return Directory
				.EnumerateFiles(settings.Path, "*.sql", SearchOption.AllDirectories)
				.AsParallel().WithDegreeOfParallelism(4).WithExecutionMode(ParallelExecutionMode.ForceParallelism)
				.Select(filePath => new { path = filePath, content = GetContent(filePath) })
				.Select(e => SqlEntity.Create(e.path, e.content, settings))
				.Where(e => e.Type != SqlEntityType.Unknown)
				.AsSequential()
				.ToDictionary(e => e.Path, e => e);
		}

		private static Dictionary<string, List<SqlEntity>> GetDependentEntities(Dictionary<string, SqlEntity> fileToEntityMapping)
		{
			var entityNames = new HashSet<string>(fileToEntityMapping.Select(p => p.Value.Name));

			var nameToEntity = fileToEntityMapping.Values.ToDictionary(s => s.Name, StringComparer.InvariantCultureIgnoreCase);

			return fileToEntityMapping.Values
				.Select(entity => new
				{
					dependsOn = entityNames.Intersect(entity.Words),
					name = entity.Name
				})
				.SelectMany(relation => relation.dependsOn.Select(who => new { dependent = relation.name, name = who }))
				.Where(r => !string.Equals(r.name, r.dependent, StringComparison.InvariantCultureIgnoreCase))
				.GroupBy(r => r.name, StringComparer.InvariantCultureIgnoreCase)
				.ToDictionary(g => g.Key, g => g.Select(i => nameToEntity[i.dependent]).ToList());
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
