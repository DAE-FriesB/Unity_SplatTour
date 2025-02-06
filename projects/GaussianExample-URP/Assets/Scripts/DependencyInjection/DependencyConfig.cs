using Analysis;
using Analysis.Logging;
using Dependencies;
using Timing;
using UnityEngine;



[CreateAssetMenu(menuName ="DependencyConfig")]
public class DependencyConfig : ScriptableObject
{
	[SerializeField]
	private bool _logEditorFPS, _logEditorLoading;

	[SerializeField]
	private bool _benchmarking = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void Build()
	{
		DependencyService.Clear();
#if UNITY_EDITOR
		DependencyService.RegisterService<IPerformanceReporter>(() =>
		{
			return new EditorPerformanceReporter(_logEditorFPS, _logEditorLoading);
		});
#else

		DependencyService.RegisterService<IAnalysisLogger>(() =>
		{
			return new WebGLAnalysisLogger();
		});
#endif

		DependencyService.RegisterService<ITimeService>(() =>
		{
			return _benchmarking ? new BenchmarkTimeService() : new DefaultTimeService();
		});
	}
}
