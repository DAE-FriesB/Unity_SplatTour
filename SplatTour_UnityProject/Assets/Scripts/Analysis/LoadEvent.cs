using System;
using UnityEngine;

namespace Analysis
{
	public static class DateTimeExtensions
	{
		public static long ToUnixTimeMS(this DateTime d) { return new DateTimeOffset(d).ToUnixTimeMilliseconds(); }
	}

	public class LoadEvent<T> : LoadEvent
	{
		public T Operation { get; }
		public LoadEvent(T operation, LoadEventType type, string dataName, DateTime dateTime) : base(type, dataName, dateTime)
		{
			Operation = operation;
		}
	}

	public class TimestampEventArgs : EventArgs
	{
		public TimestampEventArgs(DateTime endTime)
		{
			TimeStamp = endTime;
		}

		public DateTime TimeStamp { get; }
	}
	public abstract class LoadEvent
	{
		public event EventHandler<TimestampEventArgs> Completed;
		public LoadEvent ChildLoadingEvent
		{
			get => _childLoadingEvent; set
			{
				if(_childLoadingEvent != null)
				{
					throw new InvalidOperationException("Cannot set child loading event multiple times");
				}
				_childLoadingEvent = value;

				if (_childLoadingEvent != null)
				{
					_childLoadingEvent.Completed += _childLoadingEvent_Completed;
				}
			}
		}

		private void _childLoadingEvent_Completed(object sender, TimestampEventArgs e)
		{
			if (_childLoadingEvent != null)
			{
				_childLoadingEvent.Completed -= _childLoadingEvent_Completed;
				_childLoadingEvent = null;
			}
			OperationFinished(e.TimeStamp);
		}

		public AsyncOperation LoadOperation { get; }
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public TimeSpan Duration => End - Start;

		public long StartTimeEpoch => Start.ToUnixTimeMS();
		public long EndTimeEpoch => End.ToUnixTimeMS();
		public int DurationMilliseconds => (int)(Math.Round(Duration.TotalMilliseconds));

		public bool IsCompleted = false;
		private LoadEvent _childLoadingEvent = null;

		public LoadEventType EventType { get; }
		public enum LoadEventType
		{
			LoadScene = 0,
			UnloadScene = 1,
			LoadSplat = 2,
			UnloadSplat = 3,
		}
		private readonly string _dataName;
		public LoadEvent(LoadEventType type, string dataName, DateTime dateTime)
		{
			Start = dateTime;
			EventType = type;
			_dataName = dataName;
		}


		public void OperationFinished(DateTime dateTime)
		{
			if (ChildLoadingEvent != null) return;

			End = dateTime;
			IsCompleted = true;
			Completed?.Invoke(this, new TimestampEventArgs(dateTime));
		}

		public override string ToString()
		{
			return $"{EventType}_{_dataName}";
		}
	}
}