using Analysis;
using Analysis.Logging;
using Dependencies;
using UnityEngine;
public class DependencyBuilder : MonoBehaviour
{
	[SerializeField]
	private bool _logEditorFPS, _logEditorLoading;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Awake()
	{
		DependencyService.Clear();
#if UNITY_EDITOR
		DependencyService.RegisterService<IAnalysisLogger>(() =>
		{
			return new EditorAnalysisLogger(_logEditorFPS, _logEditorLoading);
		});
#else

		DependencyService.RegisterService<IAnalysisLogger>(() =>
		{
			return new WebGLAnalysisLogger();
		});
#endif
	}
}
