using System.Data.SqlClient;

namespace TSqlWatcher
{
    interface ICommand
    {
    	void Perform(SqlTransaction transaction, SqlProjectInfo project);
    
    }
}
