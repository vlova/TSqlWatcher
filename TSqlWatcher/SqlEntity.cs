using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace TSqlWatcher
{
    struct SqlEntity
    {
    	public SqlEntityType Type { get; set; }
    
    	public string Name { get; set; }

		public string Content { get; set; }

		public string Path { get; set; }
    
    	public override string ToString()
    	{
    		return Type + " " + Name;
    	}
    }
}
