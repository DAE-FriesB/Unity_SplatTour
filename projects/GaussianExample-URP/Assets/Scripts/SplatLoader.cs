using Analysis.Logging;
using Analysis;
using Dependencies;
using GaussianSplatting.Runtime;
using System;
using Timing;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;

[System.Serializable]
public class SplatAssetInfo
{
	public bool ShouldLoad = false;
	public bool StartedLoading { get; set; } = false;

	public string Name;



	public AssetReferenceT<GaussianSplatAsset> Asset;

	public int NumSplats;
	public GaussianSplatAsset.ColorFormat ColorFormat;

	public SplatPartition Partition { get; set; }
}
public class SplatLoader : MonoBehaviour
{

	[SerializeField]
	private SplatAssetInfo _defaultSplatAsset;

	[SerializeField]
	private SplatAssetInfo[] _splatAssets;

	public bool IsLoaded { get; private set; }
	private SplatSplitter _splitter;

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (_defaultSplatAsset?.Asset?.editorAsset != null)
		{
			_defaultSplatAsset.Name = _defaultSplatAsset.Asset.editorAsset.name;
			_defaultSplatAsset.NumSplats = _defaultSplatAsset.Asset.editorAsset.splatCount;
		}

		if (_splatAssets.Any())
		{
			for (int i = 0; i < _splatAssets.Length; i++)
			{
				SplatAssetInfo splatAsset = _splatAssets[i];
				if (splatAsset?.Asset?.editorAsset == null) continue;
				splatAsset.Name = splatAsset.Asset.editorAsset.name;
				splatAsset.NumSplats = splatAsset.Asset.editorAsset.splatCount;
				splatAsset.ColorFormat = splatAsset.Asset.editorAsset.colorFormat;
			}
		}
	}
#endif
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void Awake()
	{
		_splitter = GetComponent<SplatSplitter>();

		int mainPartitionIndex = _splitter.MainChunkIndex;

		CreatePartitionForSplatAsset(-1, _defaultSplatAsset);
	
		//Prepare splat partitions
		for(int idx = 0; idx < _splatAssets.Length; ++idx)
		{
			var splatAsset = _splatAssets[idx];
			CreatePartitionForSplatAsset(idx, splatAsset);

		}

		_splitter.InitializeRenderer();
		//scene loading operation
		var loadingMonitor = LoadingMonitor.Instance;

		//var sceneLoadEvent = loadingMonitor.FindActiveOperation((ev) => ev.EventType == LoadEvent.LoadEventType.LoadScene);
		//if (sceneLoadEvent != null)
		//{
		//	sceneLoadEvent.ChildLoadingEvent = LoadSplat(_splatAssets[mainPartitionIndex], mainPartitionIndex);
		//	sceneLoadEvent.Completed += (s, e) => IsLoaded = true;
		//}
	}

	private void CreatePartitionForSplatAsset(int partitionIdx, SplatAssetInfo splatAsset)
	{
		SplatPartition p = _splitter.CreatePartition(partitionIdx);
		p.SplatCount = splatAsset.NumSplats;
		splatAsset.Partition = p;
	}

	void CreatePartitionForSplatAsset()
	{

	}


	LoadEvent LoadSplat(SplatAssetInfo splatAssetInfo, int partitionIndex)
	{
		if (splatAssetInfo.StartedLoading || !splatAssetInfo.ShouldLoad) return null;

		splatAssetInfo.StartedLoading = true;

		var operation = splatAssetInfo.Asset.LoadAssetAsync();
		//var analysisLogger = DependencyService.GetService<IPerformanceReporter>();
		var loadingMonitor = LoadingMonitor.Instance;
		var splatLoadEvent = loadingMonitor.MonitorAsyncOperation(operation, LoadEvent.LoadEventType.LoadSplat, splatAssetInfo.Name);
		
		operation.Completed += (task) => _splitter.SplatLoaded(partitionIndex, task.Result);
		
		return splatLoadEvent;
	}

	private void Update()
	{
		//if (Input.GetKeyDown(KeyCode.Space))
		//{
		//	LoadAdditionalSplats(false);
		//}
	}

	private void Start()
	{
		LoadSplat(_splatAssets[7], 7);
	}

	//void LoadAdditionalSplats(bool mainOnly = false)
	//{
	//	LoadSplat(_defaultSplatAsset, -1);
	//	if (mainOnly) return;
	//	for (int idx = 0; idx < _splatAssets.Length; idx++)
	//	{
	//		SplatAssetInfo asset = _splatAssets[idx];
	//		LoadSplat(asset, idx);
	//	}
	//}



	public void OnDestroy()
	{
		_defaultSplatAsset.Asset.ReleaseAsset();

		foreach (var asset in _splatAssets)
		{
			if (asset.StartedLoading)
			{
				asset.Asset.ReleaseAsset();
			}
		}
	}

}
