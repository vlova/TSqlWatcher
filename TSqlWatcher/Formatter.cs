using System;
using System.Linq;

namespace TSqlWatcher
{
    static class Formatter
    {
    	public static void WriteLine(string format, params object[] args)
    	{
    		Write(format, args);
    		Console.WriteLine();
    	}
    
    	public static void Write(string format, params object[] args) {
    		var splitted = format
    			.Split('^')
    			.Select(text => new
    			{
    				text, 
    				colorDescriptionEnd = text.IndexOf(']')
    			})
    			.Select(s => new {
    				color = s.colorDescriptionEnd == -1 
    					? null 
    					: s.text.Substring(1, s.colorDescriptionEnd - 1),
    				text = s.colorDescriptionEnd == -1
    					? s.text 
    					: s.text.Substring(s.colorDescriptionEnd + 1) 
    			});
    
    		var color = Console.ForegroundColor;
    		foreach (var item in splitted)
    		{
    			if (item.color != null)
    			{
    				Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), item.color, ignoreCase: true);
    			}
    			Console.Write(item.text, args);
    			Console.ForegroundColor = color;
    		}
    	}
    }
}
