using Analysis.Logging;
using Analysis;
using Dependencies;
using GaussianSplatting.Runtime;
using System;
using Timing;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SplatLoader : MonoBehaviour
{
	[SerializeField]
	private string _mainSplatName = "";
	[SerializeField]
	private AssetReferenceT<GaussianSplatAsset> _splatAsset;
	[SerializeField]
	private GaussianSplatRenderer _renderer;

	public bool IsLoaded { get; private set; }

#if UNITY_EDITOR
	private void OnValidate()
	{
		_renderer = GetComponent<GaussianSplatRenderer>();
		_renderer.enabled = false;
		if(_splatAsset != null && string.IsNullOrEmpty(_mainSplatName))
		{
			_mainSplatName = _splatAsset.editorAsset.name;
		}
	}
#endif
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void Awake()
	{
		var operation = _splatAsset.LoadAssetAsync();
		//var analysisLogger = DependencyService.GetService<IPerformanceReporter>();
		var loadingMonitor = LoadingMonitor.Instance;

		//scene loading operation
		var sceneLoadEvent = loadingMonitor.FindActiveOperation((ev) => ev.EventType == LoadEvent.LoadEventType.LoadScene );

		var splatLoadEvent = loadingMonitor.MonitorAsyncOperation(operation, LoadEvent.LoadEventType.LoadSplat, _mainSplatName);

		if(sceneLoadEvent != null)
		{
			sceneLoadEvent.ChildLoadingEvent = splatLoadEvent;
		}


		operation.Completed += SplatLoader_Completed;
		
	}

	private void SplatLoader_Completed(AsyncOperationHandle<GaussianSplatAsset> obj)
	{
		_renderer.m_Asset = obj.Result;
		_renderer.enabled = true;
		IsLoaded = true;
	}

	public void OnDestroy()
	{
		if (_renderer != null)
		{
			_renderer.enabled = false;
			_renderer.m_Asset = null;
		}
		_splatAsset.ReleaseAsset();
	}
	// Update is called once per frame
	void Update()
	{

	}
}
