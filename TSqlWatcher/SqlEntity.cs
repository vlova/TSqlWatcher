using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TSqlWatcher
{
	class SqlEntity
	{
		private string _content;

		public HashSet<string> Words { get; set; }

		public SqlEntityType Type { get; set; }

		public string Name { get; set; }

		public string Content
		{
			get
			{
				return _content;
			}
			set
			{
				_content = value;
				Words = new HashSet<string>(
					Content
						.Split('\n','\r')
						.Select(RemoveComment)
						.SelectMany(s => s.Split(Constants.Delimiters))
						.Except(Constants.CommonWords)
						.Where(s => !string.IsNullOrWhiteSpace(s))
						.Where(s => !s.All(char.IsDigit)) // remove integers
						.Where(s => !s.StartsWith("@")), // remove variables, 
					StringComparer.InvariantCultureIgnoreCase);
			}
		}

		private string RemoveComment(string s)
		{
			// TODO: handle /* */ multiline comments
			var index = s.IndexOf("--", StringComparison.Ordinal);
			if (index == -1) return s;
			return s.Substring(0, index);
		}

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

		private static readonly Regex functionRegex = GetEntityRegex("function");
		private static readonly Regex storedProcedureRegex = GetEntityRegex(@"(?:procedure|proc)");
		private static readonly Regex viewRegex = GetEntityRegex("view");
		private static readonly Regex customTypeRegex = GetEntityRegex("type");

		private static Regex GetEntityRegex(string type)
		{
			return new Regex(@"create\s+" + type + @"\s+(?:\[{0,1}dbo\]{0,1}\.){0,1}\[{0,1}((?:\w|\d|-)*)\]{0,1}",
				RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
		}

		public static SqlEntity Create(string path, string content, Settings settings)
		{
			var type = content.Maybe(GetEntityType, SqlEntityType.Unknown);
			var entity = new SqlEntity
			{
				Path = path,
				Type = type,
				Name = content.Maybe(GetEntityName),
				IsSchemaBound = content.Maybe(c => c.ContainsSql("schemabinding"), defaultValue: false)
					|| type == SqlEntityType.Type,
				Content = ReplaceVariables(content, settings)
			};

			return entity;
		}

		private static string ReplaceVariables(string content, Settings settings)
		{
			return settings.UserVariables.Aggregate(
				content, 
				(current, variable) => current
					.Replace(
						string.Format("$({0})", variable.Key),
						variable.Value));
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
