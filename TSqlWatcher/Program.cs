using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TSqlWatcher
{
	class Program
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int GetLongPathName(
			[MarshalAs(UnmanagedType.LPTStr)] string path,
			[MarshalAs(UnmanagedType.LPTStr)] StringBuilder longPath,
			int longPathLength
		);

		public static string NormalizePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) 
				return null;
			
			StringBuilder longPath = new StringBuilder(2048);
			GetLongPathName(path, longPath, longPath.Capacity);
			var longPathString = longPath.ToString();
			if (string.IsNullOrWhiteSpace(longPathString))
			{
				return null;
			}

			return longPathString;
		}
		
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
			handler.Prepare();

			CreateWatcher(settings,
				(sender, e) =>
				{
					if (e.ChangeType.HasFlag(WatcherChangeTypes.Changed))
					{
						handler.Handle(oldPath: null, newPath: NormalizePath(e.FullPath));
					}
				},
				(sender, e) =>
				{
					handler.Handle(oldPath: NormalizePath(e.OldFullPath), newPath: NormalizePath(e.FullPath));
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
				NotifyFilters.DirectoryName
				| NotifyFilters.FileName
				| NotifyFilters.LastWrite;

			watcher.Filter = "*.sql";
			watcher.Changed += handler;
			watcher.Created += handler;
			watcher.Deleted += handler;
			watcher.Renamed += renamedHandler;
			watcher.EnableRaisingEvents = true;
		}

	}
}
