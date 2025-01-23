using Analysis.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Analysis
{
	public class LoadingMonitor
	{
		private List<LoadEvent> _activeOperations = new List<LoadEvent>(4);

		private readonly IPerformanceReporter _reporter;

		public LoadingMonitor(IPerformanceReporter reporter)
		{
			_reporter = reporter;
		}
		public void MonitorAsyncOperation(AsyncOperation operation, LoadEvent.LoadEventType loadEventType, string dataName)
		{
			LoadEvent loadEvent = new LoadEvent(operation, loadEventType, dataName, DateTime.UtcNow);

			operation.completed += Operation_completed;
			_reporter.ReportLoadEventStarted(loadEvent.ToString(), loadEvent.StartTimeEpoch);
			_activeOperations.Add(loadEvent);
		}

		private void Operation_completed(AsyncOperation obj)
		{
			obj.completed -= Operation_completed;

			int index = _activeOperations.FindIndex(ev => ev.LoadOperation == obj);
			LoadEvent loadEvent = _activeOperations[index];
			_activeOperations.RemoveAt(index);
			loadEvent.CompletedLoading(DateTime.UtcNow);
			_reporter.ReportLoadEventFinished(loadEvent.ToString(), loadEvent.EndTimeEpoch, loadEvent.DurationMilliseconds);
		}
	}
}