using System;
using UnityEngine;

namespace Analysis
{
	public static class DateTimeExtensions
	{
		public static long ToUnixTimeMS(this DateTime d) { return new DateTimeOffset(d).ToUnixTimeMilliseconds(); }
	}
	public class LoadEvent
	{
		public AsyncOperation LoadOperation { get; }
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public TimeSpan Duration => End - Start;

		public long StartTimeEpoch => Start.ToUnixTimeMS();
		public long EndTimeEpoch => End.ToUnixTimeMS();
		public int DurationMilliseconds => (int)(Math.Round(Duration.TotalMilliseconds));

		public LoadEventType EventType { get; }
		public enum LoadEventType
		{
			LoadScene = 0, 
			UnloadScene = 1,
			LoadSplat = 2,
			UnloadSplat = 3,
		}
		private readonly string _dataName;
        public LoadEvent(AsyncOperation operation, LoadEventType type, string dataName, DateTime dateTime)
        {
			LoadOperation = operation;
			Start = dateTime;
			EventType = type;
			_dataName = dataName;
		}
 

		public void CompletedLoading(DateTime dateTime)
		{
			End = dateTime;
		}

		public override string ToString()
		{
			return $"{EventType}_{_dataName}";
		}
	}
}