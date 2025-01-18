namespace Analysis.Logging
{
	public interface IAnalysisLogger
	{
		void ReportLoadEventStarted(string loadDataName, long timestamp);
		void ReportLoadEventFinished(string loadDataName, long timestamp, int durationMS);
		void ReportFPS(float currentFrameTime, float averageFPS);
		void ReportCompleted();

	}
}