using Analysis;
using System;
using UnityEngine;

namespace Analysis.Logging
{
	public class EditorAnalysisLogger : IAnalysisLogger
	{
		private readonly bool _logFPS;
		private readonly bool _logLoading;

		public EditorAnalysisLogger(bool logFPS, bool logLoading)
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

			DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
			Debug.Log($"[{ts}] Finished [{loadDataName}] ({durationMS} ms)");
		}

		public void ReportLoadEventStarted(string loadDataName, long timestamp)
		{
			if (!_logLoading) return;

			DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
			Debug.Log($"[{ts}] Started [{loadDataName}]");
		}
	}
}