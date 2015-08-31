namespace TSqlWatcher
{
	internal interface ISqlChangeHandler
	{
		void Prepare();
		void Handle(string oldPath, string newPath);
	}
}