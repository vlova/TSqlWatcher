using System;
using System.IO;
using System.Linq;

namespace TSqlWatcher
{
	class Program
	{
		static void Main(string[] args)
		{
			var settings = new Settings(args);
			if (!args.Any())
			{
				PrintHelp();
				return;
			}
			if (settings.Errors.Any())
			{
				settings.Errors.ForEach(Console.WriteLine);
				return;
			}


			var handler = new SqlChangeHandler(settings);

			CreateWatcher(settings,
				(sender, e) =>
				{
					if (e.ChangeType.HasFlag(WatcherChangeTypes.Changed))
					{
						handler.Handle(e.FullPath);
					}
				},
				(sender, e) =>
				{
					if (e.ChangeType.HasFlag(WatcherChangeTypes.Changed))
					{
						handler.Handle(e.FullPath);
					}
				});

			Console.ReadKey();
		}

		private static void PrintHelp()
		{
			Console.WriteLine("usage: --path \"D:\\project\" --connectionString \"..\"");
		}

		private static void CreateWatcher(
			Settings settings,
			FileSystemEventHandler handler,
			RenamedEventHandler renamedHandler)
		{
			var watcher = new FileSystemWatcher(settings.Path);

			watcher.IncludeSubdirectories = true;
			watcher.NotifyFilter =
				NotifyFilters.Attributes
				| NotifyFilters.CreationTime
				| NotifyFilters.DirectoryName
				| NotifyFilters.FileName
				| NotifyFilters.LastAccess
				| NotifyFilters.LastWrite
				| NotifyFilters.Security
				| NotifyFilters.Size;

			watcher.Filter = "*.sql";
			watcher.Changed += handler;
			watcher.Created += handler;
			watcher.Deleted += handler;
			watcher.Renamed += renamedHandler;
			watcher.EnableRaisingEvents = true;
		}

	}
}
