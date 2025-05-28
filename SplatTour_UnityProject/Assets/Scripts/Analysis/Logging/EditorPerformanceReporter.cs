using Analysis;
using System;
using UnityEngine;

namespace Analysis.Logging
{
	public class EditorPerformanceReporter : IPerformanceReporter
	{
		private readonly bool _logFPS;
		private readonly bool _logLoading;

		public EditorPerformanceReporter(bool logFPS, bool logLoading)
        {
			_logFPS = logFPS;
			_logLoading = logLoading;
		}

		public void ReportCompleted()
		{
			Debug.Log("Finished benchmark");

#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#endif
		}

		public void ReportFPS(float currentFrameTime, float averageFPS)
		{
			if (!_logFPS) return;

			Debug.Log($"FPS: {averageFPS} ({currentFrameTime} s)");
		}

		public void ReportLoadEventFinished(string loadDataName, long timestamp, int durationMS)
		{
			if (!_logLoading) return;

			//DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
			Debug.Log($"[{timestamp}] Finished [{loadDataName}] (duration = {durationMS} ms)");
		}

		public void ReportLoadEventStarted(string loadDataName, long timestamp)
		{
			if (!_logLoading) return;

			//DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
			Debug.Log($"[{timestamp}] Started [{loadDataName}]");
		}
	}
}