using System.Runtime.InteropServices;
using UnityEngine;

namespace Analysis.Logging
{
	public class WebGLAnalysisLogger : IAnalysisLogger
	{
		[DllImport("__Internal")]
		private static extern void EngineLoaded();
		[DllImport("__Internal")]
		private static extern void BenchmarkComplete();

		[DllImport("__Internal")]
		private static extern void FPSEvent(float currentFrameTimeMS, float averageFPS);
		[DllImport("__Internal")]
		private static extern void StartLoadEvent(string loadDataName, int timestampLow, int timestampHigh);
		[DllImport("__Internal")]
		private static extern void FinishedLoadEvent(string loadDataName, int timestampLow, int timestampHigh, int durationMS);

		public WebGLAnalysisLogger()
		{
			EngineLoaded();
		}

		public void ReportFPS(float currentFrameTime, float averageFPS)
		{
			FPSEvent(currentFrameTime * 1000f, averageFPS);
		}
		public void ReportLoadEventStarted(string loadDataName, long timestamp)
		{
			(int low, int high) = Marshall(timestamp);
			StartLoadEvent(loadDataName, low, high);
		}
		public void ReportLoadEventFinished(string loadDataName, long timestamp, int durationMS)
		{
			(int low, int high) = Marshall(timestamp);
			FinishedLoadEvent(loadDataName, low, high, durationMS);
		}

		static (int low, int high) Marshall(long value)
		{
			int low = (int)(value & 0xFFFFFFFF);
			int high = (int)(value >> 32);
			return (low, high);
		}

		public void ReportCompleted()
		{
			BenchmarkComplete();
		}
	}
}