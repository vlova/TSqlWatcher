using System;

namespace TSqlWatcher
{
    static class Logger
    {
    	public static void Log(Exception ex)
    	{
    		Log("exception of type {0} happened. Message: {1}", ex.GetType().Name, ex.Message);
    	}
    
    	public static void Log(string message, params object[] args)
    	{
    		var originalColor = Console.ForegroundColor;
    		Console.ForegroundColor = ConsoleColor.DarkGreen;
    		Console.Write(DateTime.Now.TimeOfDay);
    		Console.ForegroundColor = ConsoleColor.Gray;
    		Console.Write(" | ");
    		Console.ForegroundColor = originalColor;
    		Console.WriteLine(message, args);
    	}
    }
}
