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
	private bool _buildBenchmark = false, _editorBenchmark = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void Build()
	{
		DependencyService.Clear();
#if UNITY_EDITOR
		DependencyService.RegisterService<IPerformanceReporter>(() =>
		{
			return new EditorPerformanceReporter(_logEditorFPS, _logEditorLoading);
		});
		DependencyService.RegisterService<ITimeService>(() =>
		{
			return _editorBenchmark ? new BenchmarkTimeService() : new DefaultTimeService();
		});
#else

		DependencyService.RegisterService<IPerformanceReporter>(() =>
		{
			return new WebGLPerformanceReporter();
		});
		DependencyService.RegisterService<ITimeService>(() =>
		{
			return _buildBenchmark ? new BenchmarkTimeService() : new DefaultTimeService();
		});
#endif


	}
}
