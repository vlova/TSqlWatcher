using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TSqlWatcher
{
    internal class UndefinedVariableAnalyzer
    {
    	private static Regex VariableRegex = new Regex(@"\$\((.*?)\)", RegexOptions.Compiled);
    
    
    	internal static void Anaylyze(SqlProjectInfo projectInfo)
    	{
    		var undefinedVariables = projectInfo.FileToEntityMapping.Values
    			.Select(entity => new
    			{
    				entity,
    				matches = VariableRegex
    				.Matches(entity.Content)
    				.OfType<Match>()
    				.Select(m => m.Groups[1].Value)
    			})
    			.SelectMany(e => e.matches.Select(match => new { e.entity, match }))
    			.GroupBy(e => e.match)
    			.Select(g => new
    			{
    				variable = g.Key,
    				entities = g.Take(5).Select(e => e.entity),
    				total = g.Count()
    			})
    			.ToList();
    
    		if (!undefinedVariables.Any())
    			return;
    
    		Console.WriteLine();
    
    		foreach (var varInfo in undefinedVariables)
    		{
    			Formatter.WriteLine(
    				"^[Red]warning^[Gray]: variable ^[Yellow]{0} ^[Gray]is undefined and is used in ^[White]{1} ^[Gray]files",
    				varInfo.variable,
    				varInfo.total);
    
    			Console.WriteLine();
    
    			foreach (var entity in varInfo.entities)
    			{
    				var lines = entity.Content
    					.Replace("\r\n", "\n")
    					.Split('\n');
    
    				var badLine = lines
    					.Select((line, index) => new { text = line, index })
    					.Where(_ => VariableRegex.IsMatch(_.text))
    					.First();
    
    				Formatter.WriteLine("\tin file ^[DarkYellow]{0}^[Gray] (entity {1}) at line {2}:",
    					entity.Path,
    					entity.Name,
    					badLine.index + 1);
    
    				Formatter.WriteLine(string.Format("\t\t{0}", GetHighligtedVariable(badLine.text.Trim())));
    
    				Console.WriteLine();
    			}
    		}
    	}
    
    	private static string GetHighligtedVariable(string text)
    	{
    		var match = VariableRegex.Match(text);
    		text = text.Insert(match.Index + match.Value.Length + 1, "^[Gray]");
    		text = text.Insert(match.Index - 1, "^[Red]");
    		return text;
    	}
    
    }
}
