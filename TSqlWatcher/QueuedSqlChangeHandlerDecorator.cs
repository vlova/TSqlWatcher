using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace TSqlWatcher
{
	internal class QueuedSqlChangeHandlerDecorator : ISqlChangeHandler
	{
		struct FileChange
		{
			public string OldPath { get; set; }
			public string NewPath { get; set; }
		}

		private static readonly TimeSpan minDelay = TimeSpan.FromMilliseconds(100);
		private static readonly TimeSpan maxDelay = TimeSpan.FromSeconds(2);
		private static readonly TimeSpan stepOfDelay = TimeSpan.FromMilliseconds(50);

		private readonly ISqlChangeHandler handler;

		private readonly List<FileChange> pendingChanges = new List<FileChange>();
		private readonly Stopwatch watch = new Stopwatch();
		private readonly object locker = new object();
		private readonly Timer timer;

		public QueuedSqlChangeHandlerDecorator(ISqlChangeHandler handler)
		{
			this.handler = handler;
			this.timer = new Timer(Step, null, TimeSpan.Zero, period: TimeSpan.FromMilliseconds(minDelay.Milliseconds / 2));
		}

		public void Prepare()
		{
			handler.Prepare();
		}

		public void Handle(string oldPath, string newPath)
		{
			lock (locker)
			{
				var change = new FileChange
				{
					NewPath = newPath,
					OldPath = oldPath
				};

				if (pendingChanges.Contains(change))
				{
					return;
				}

				pendingChanges.Add(change);

				if (watch.IsRunning)
				{
					watch.Stop();
				}

				watch.Start();

				Logger.Log("delaying for {0}ms", CurrentDelay.Milliseconds);
			}
		}

		public void Step(object state)
		{
			lock (locker)
			{
				if (!watch.IsRunning || watch.Elapsed <= CurrentDelay) return;

				var distinctPendingChanges = pendingChanges.DistinctSameOrder().ToList();
				distinctPendingChanges.ForEach(a => handler.Handle(a.OldPath, a.NewPath));
				pendingChanges.Clear();
				watch.Stop();
			}
		}

		public TimeSpan CurrentDelay
		{
			get
			{
				var delay = minDelay + TimeSpan.FromTicks(stepOfDelay.Ticks * pendingChanges.Count);
				return TimeSpan.FromTicks(Math.Min(delay.Ticks, maxDelay.Ticks));
			}
		}
	}
}
