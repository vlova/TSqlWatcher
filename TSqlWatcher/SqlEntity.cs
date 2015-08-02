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
			return Type + " " + Name;
		}

		private static Regex functionRegex = GetEntityRegex("function");
		private static Regex storedProcedureRegex = GetEntityRegex("procedure");
		private static Regex viewRegex = GetEntityRegex("view");
		private static Regex customTypeRegex = GetEntityRegex("type");

		private static Regex GetEntityRegex(string entityType)
		{
			return new Regex(@"create\s+" + entityType + @"\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}((?:\w|\d|-)*)\]{0,1}",
				RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
		}

		public static SqlEntity Create(string path, string content)
		{
			var type = GetEntityType(content);
			return new SqlEntity
			{
				Path = path,
				Type = type,
				Name = GetEntityName(content),
				IsSchemaBound = content.ContainsInsensetive("schemabinding") || type == SqlEntityType.CustomType,
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
			if (customTypeRegex.IsMatch(content)) return SqlEntityType.CustomType;
			return SqlEntityType.Unknown;
		}
	}
}
