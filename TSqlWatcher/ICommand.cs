using System.Data.SqlClient;

namespace TSqlWatcher
{
	delegate void CommandAdder(ICommand command);

    interface ICommand
    {
    	void Perform(SqlTransaction transaction, SqlProjectInfo project, CommandAdder commandAdder);
    
    }
}
