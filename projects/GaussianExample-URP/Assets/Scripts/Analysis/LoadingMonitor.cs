using Analysis.Logging;
using Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Analysis
{
	public class LoadingMonitor
	{
		private static LoadingMonitor _instance;
		public static LoadingMonitor Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new LoadingMonitor(DependencyService.GetService<IPerformanceReporter>());
				}
				return _instance;
			}
		}

		private List<LoadEvent> _activeOperations = new List<LoadEvent>(4);

		private readonly IPerformanceReporter _reporter;

		private LoadingMonitor(IPerformanceReporter reporter)
		{
			_reporter = reporter;
		}

		public LoadEvent FindActiveOperation(Func<LoadEvent,bool> predicate)
		{
			return _activeOperations.FirstOrDefault(predicate);
		}

		public LoadEvent MonitorAsyncOperation(AsyncOperationHandle handle, LoadEvent.LoadEventType loadEventType, string dataName)
		{
			LoadEvent loadEvent = new LoadEvent<AsyncOperationHandle>(handle, loadEventType, dataName, DateTime.UtcNow);

			handle.Completed += (op) => LoadOperationFinished(loadEvent);
			loadEvent.Completed += LoadEvent_Completed;
			_reporter.ReportLoadEventStarted(loadEvent.ToString(), loadEvent.StartTimeEpoch);
			_activeOperations.Add(loadEvent);
			return loadEvent;
		}
		public LoadEvent MonitorAsyncOperation(AsyncOperation operation, LoadEvent.LoadEventType loadEventType, string dataName)
		{
			LoadEvent loadEvent = new LoadEvent<AsyncOperation>(operation, loadEventType, dataName, DateTime.UtcNow);

			operation.completed += (op) => LoadOperationFinished(loadEvent) ;

			loadEvent.Completed += LoadEvent_Completed;
			_reporter.ReportLoadEventStarted(loadEvent.ToString(), loadEvent.StartTimeEpoch);
			_activeOperations.Add(loadEvent);
			return loadEvent;
		}

		private void LoadEvent_Completed(object sender, EventArgs e)
		{
			if (sender is not LoadEvent loadEvent) return;
			int index = _activeOperations.IndexOf(loadEvent);
			_activeOperations.RemoveAt(index);

			_reporter.ReportLoadEventFinished(loadEvent.ToString(), loadEvent.EndTimeEpoch, loadEvent.DurationMilliseconds);
		}

		private void LoadOperationFinished(LoadEvent loadEvent)
		{
			loadEvent.OperationFinished(DateTime.UtcNow);

		}

		//private void Operation_completed(AsyncOperationHandle obj)
		//{
		//	obj.completed -= Operation_completed;

		//	int index = _activeOperations.FindIndex(ev => (ev is LoadEvent<AsyncOperationHandle> loadevent) && loadevent.Operation.Equals(obj));
		//	LoadEvent loadEvent = _activeOperations[index];
		//	_activeOperations.RemoveAt(index);
		//	loadEvent.CompletedLoading(DateTime.UtcNow);
		//	_reporter.ReportLoadEventFinished(loadEvent.ToString(), loadEvent.EndTimeEpoch, loadEvent.DurationMilliseconds);
		//}
		//private void Operation_completed(AsyncOperation obj)
		//{
		//	obj.completed -= Operation_completed;

		//	int index = _activeOperations.FindIndex(ev =>  ev.LoadOperation == obj);
		//	LoadEvent loadEvent = _activeOperations[index];
		//	_activeOperations.RemoveAt(index);
		//	loadEvent.CompletedLoading(DateTime.UtcNow);
		//	_reporter.ReportLoadEventFinished(loadEvent.ToString(), loadEvent.EndTimeEpoch, loadEvent.DurationMilliseconds);
		//}


	}
}