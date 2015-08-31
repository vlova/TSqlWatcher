using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;

namespace TSqlWatcher
{
    [DebuggerNonUserCode, DebuggerStepThrough]
    public static class Profiling
    {
    	private class WatchData : IDisposable
    	{
    		public Stopwatch Watch { private get; set; }
    		public string Description { private get; set; }
    
    		[DebuggerStepperBoundary]
    		public WatchData()
    		{
    		}
    
    		[DebuggerStepperBoundary]
    		public void Dispose()
    		{
    			Watch.Stop();
    			CallContext.SetData("profilertabs", GetTabsCount() - 1);
    			Debug.WriteLine("{0}executed {1} for {2} {3}", GetTabs(), Description, Watch.Elapsed, Watch.ElapsedMilliseconds);
    		}
    	}
    
    	[DebuggerStepperBoundary]
    	private static string GetTabs()
    	{
    		var tabs = string.Join("", Enumerable.Range(0, GetTabsCount()).Select(i => "\t"));
    		return tabs;
    	}
    
    	[DebuggerStepperBoundary]
    	private static int GetTabsCount()
    	{
    
    		var tabsCount = (int?)CallContext.GetData("profilertabs") ?? 0;
    		return tabsCount;
    	}
    
    	[DebuggerStepperBoundary]
    	public static IDisposable Profile([CallerMemberName]string description = null)
    	{
    		return Profile(description, new object[0]);
    	}
    
    	[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), DebuggerStepperBoundary]
    	public static IDisposable Profile(string description, params object[] args)
    	{
    		description = args.Any() ? string.Format(description, args) : description;
    		Debug.WriteLine("{0}started {1}", GetTabs(), description);
    		CallContext.SetData("profilertabs", GetTabsCount() + 1);
    		return new WatchData
    		{
    			Description = description,
    			Watch = Stopwatch.StartNew()
    		};
    	}
    
    	[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), DebuggerStepperBoundary]
    	public static IDisposable ProfileExecOnly(string description, params object[] args)
    	{
    		description = args.Any() ? string.Format(description, args) : description;
    		CallContext.SetData("profilertabs", GetTabsCount() + 1);
    		return new WatchData
    		{
    			Description = description,
    			Watch = Stopwatch.StartNew()
    		};
    	}
    
    	[DebuggerStepperBoundary]
    	public static TRes Profile<TRes>(string description, Func<TRes> expr)
    	{
    		using (Profile(description))
    		{
    			return expr();
    		}
    	}
    
    	[DebuggerStepperBoundary]
    	public static TRes Profile<TRes>(Expression<Func<TRes>> expr)
    	{
    		var description = expr.ToString();
    		var compiled = expr.Compile();
    		using (Profile(description))
    		{
    			return compiled();
    		}
    	}
    }
}
