using System;
using System.Data.SqlClient;

namespace TSqlWatcher
{
    class LambdaCommand : ICommand
    {
    	private readonly Action<SqlTransaction, SqlProjectInfo, CommandAdder> lambda;
    	public LambdaCommand(Action<SqlTransaction, SqlProjectInfo, CommandAdder> lambda)
    	{
    		this.lambda = lambda;
    	}
    
    	public void Perform(SqlTransaction transaction, SqlProjectInfo project, CommandAdder commandAdder)
    	{
    		lambda(transaction, project, commandAdder);
    	}
    }
}
