using System;
using System.Collections.Generic;

namespace TSqlWatcher
{
    class Constants
    {
    	public static HashSet<string> CommonWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
    		"create", "as", "dbo",
    		"end", "begin", "from",
    		"int", "select", "int",
    		"procedure", "where", "on",
    		"varchar", "join", "and",
    		"return", "declare", "set",
    		"null", "*", "function",
    		"returns", "nolock",
    		"readonly", "max",
    		"insert", "into",
    		"if", "is", "not", "or",
    		"table", "bit", "in", "nocount",
    		"exists", "inner", "left", "else", "by",
    		"values", "when", "update", "then", "top",
    		"case", "order", "datetime2", "throw",
    		"datetime", "delete", "distinct", "exec",
    		"type", "view", "fetch", "like", 
    		"cast", "rowlock", "next", "scope_identity",
    		"offset", "rows", "uniqueidentifier",
    		"group", "output", "isnull",
    		"asc", "desc", "with", "only",
    		"nvarchar", "count", "getdate",
    		"go", "convert", "inserted", "while",
    		"datediff", "schemabinding", "proc",
    		"view", "using", "decimal",
    		"row_number", "over", "matched", "union",
    		"cursor", "open", "iif", "references",
    		"object_id", "to", "min", 
    		"target", "source", "index",
    		"outer", "apply"
    	};
    
    	public static char[] Delimiters = new [] {
    		'[', ']',  '(', ')', '\t', 
    		'+', ' ', '.', '!',
    		';', ',', '-', '\'',
    		'<', '>', '%', '=', '$'
    	};
    }
}
