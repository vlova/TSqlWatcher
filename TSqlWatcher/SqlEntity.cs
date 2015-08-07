using System.Text.RegularExpressions;

namespace TSqlWatcher
{
	class SqlEntity
	{
		public SqlEntityType Type { get; set; }

		public string Name { get; set; }

		public string Content { get; set; }

		public string Path { get; set; }

		public bool IsSchemaBound { get; set; }

		public override string ToString()
		{
			return Type.ToString().ToLower() + " " + Name;
		}

		public override bool Equals(object obj)
		{
			var another = obj as SqlEntity;
			if (another != null)
			{
				return this.Path == another.Path
					|| this.Name == another.Name; 
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() | Path.GetHashCode();
		}

		private static Regex functionRegex = GetEntityRegex("function");
		private static Regex storedProcedureRegex = GetEntityRegex(@"(?:procedure|proc)");
		private static Regex viewRegex = GetEntityRegex("view");
		private static Regex customTypeRegex = GetEntityRegex("type");

		private static Regex GetEntityRegex(string type)
		{
			return new Regex(@"create\s+" + type + @"\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}((?:\w|\d|-)*)\]{0,1}",
				RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
		}

		public static SqlEntity Create(string path, string content)
		{
			var type = content.Maybe(GetEntityType, SqlEntityType.Unknown);
			return new SqlEntity
			{
				Path = path,
				Type = type,
				Name = content.Maybe(GetEntityName),
				IsSchemaBound = content.Maybe(c => c.ContainsCall("schemabinding"), defaultValue: false)
					|| type == SqlEntityType.Type,
				Content = content
			};

		}

		private static string GetEntityName(string content)
		{
			return GetEntityName(functionRegex, content)
				?? GetEntityName(viewRegex, content)
				?? GetEntityName(storedProcedureRegex, content)
				?? GetEntityName(customTypeRegex, content);
		}

		private static string GetEntityName(Regex regex, string content)
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

		private static SqlEntityType GetEntityType(string content)
		{
			if (functionRegex.IsMatch(content)) return SqlEntityType.Function;
			if (storedProcedureRegex.IsMatch(content)) return SqlEntityType.Procedure;
			if (viewRegex.IsMatch(content)) return SqlEntityType.View;
			if (customTypeRegex.IsMatch(content)) return SqlEntityType.Type;
			return SqlEntityType.Unknown;
		}
	}
}
