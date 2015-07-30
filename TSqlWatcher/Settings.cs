using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSqlWatcher
{
    class Settings
    {
    	public Settings(string[] args)
    		: this(args
    			.OfType<string>()
    			.IntoGroups(count: 2)
    			.ToDictionary(p => p[0].TrimStart('-', ' '), p => p[1], StringComparer.InvariantCultureIgnoreCase))
    	{ 
    	}
    	public Settings(IDictionary<string, string> settings)
    	{
    		this.Errors = new List<string>();
    		
    		this.Path = settings.TryGet("path") ?? string.Empty;
    		if (string.IsNullOrEmpty(this.Path) || !Directory.Exists(this.Path))
    		{
    			this.Errors.Add(string.Format("Directory {0} doesn't exists", this.Path));
			}

			this.ConnectionString = settings.TryGet("ConnectionString") ?? string.Empty;
    	}
    
    	public string Path { get; set; }
    
    	public List<string> Errors { get; private set; }

		public string ConnectionString { get; set; }
	}
}
