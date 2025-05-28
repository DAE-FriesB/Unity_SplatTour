// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using static GaussianSplatting.Runtime.GaussianSplatAsset;

namespace GaussianSplatting.Runtime
{
	public class GaussianSplatRenderer : MonoBehaviour
	{
		public class CombinedSplatData
		{
			private readonly ColorFormat _colorFormat;

			public CombinedSplatData(int splatCount, ColorFormat colorFormat)
			{
				PosData = new Vector3[splatCount];
				ColorData = new NativeArray<float4>(splatCount, Allocator.Persistent);
				this._colorFormat = colorFormat;
			}
			public Vector3[] PosData { get; }
				
			public NativeArray<float4> ColorData { get; set; }

			public void CopyColorData(int startIdx, int splatCount, NativeArray<float4> colorData)
			{
				var slice = new NativeSlice<float4>(ColorData, startIdx, splatCount);
				slice.CopyFrom(colorData);
			}

			public void Dispose()
			{
				ColorData.Dispose();
			}
		}

		public enum RenderMode
		{
			Splats,
			DebugPoints,
			DebugPointIndices,
			DebugBoxes,
			DebugChunkBounds,
		}

		[Range(0.1f, 2.0f)]
		[Tooltip("Additional scaling factor for the splats")]
		public float m_SplatScale = 1.0f;
		[Range(0.05f, 20.0f)]
		[Tooltip("Additional scaling factor for opacity")]
		public float m_OpacityScale = 1.0f;
		[Range(0, 3)]
		[Tooltip("Spherical Harmonics order to use")]
		public int m_SHOrder = 3;
		[Tooltip("Show only Spherical Harmonics contribution, using gray color")]
		public bool m_SHOnly;
		[Range(1, 30)]
		[Tooltip("Sort splats only every N frames")]
		public int m_SortNthFrame = 100;

		public RenderMode m_RenderMode = RenderMode.Splats;
		[Range(1.0f, 15.0f)] public float m_PointDisplaySize = 3.0f;

		public GaussianCutout[] m_Cutouts;

		public Shader m_ShaderSplats;
		public Shader m_ShaderComposite;
		public Shader m_ShaderDebugPoints;
		public Shader m_ShaderDebugBoxes;
		[Tooltip("Gaussian splatting compute shader")]
		public ComputeShader m_CSSplatUtilities;

		int m_ReservedSplatCount;
		int m_RegisteredSplatCount;
		uint[] m_GpuSortDistances;
		uint[] m_GpuSortIndices;

		internal GraphicsBuffer m_GpuSortKeys;

		GraphicsBuffer m_GpuPosData;
		GraphicsBuffer m_GpuOtherData;
		GraphicsBuffer m_GpuSHData;
		GraphicsBuffer m_GpuColorData;
		internal GraphicsBuffer m_GpuChunks;
		internal bool m_GpuChunksValid;
		internal GraphicsBuffer m_GpuView;
		internal GraphicsBuffer m_GpuIndexBuffer;

		CPUSorting m_Sorter;
		CPUDistanceCalculator m_distCalculator;

		internal Material m_MatSplats;
		internal Material m_MatComposite;
		internal Material m_MatDebugPoints;
		internal Material m_MatDebugBoxes;

		internal int m_FrameCounter;
		Hash128 m_PrevHash;

		private CombinedSplatData _combinedData = null;

		static readonly ProfilerMarker s_ProfSort = new(ProfilerCategory.Render, "GaussianSplat.Sort", MarkerFlags.SampleGPU);

		private GaussianSplatAsset.VectorFormat _posFormat = GaussianSplatAsset.VectorFormat.Float32;
		private GaussianSplatAsset.VectorFormat _scaleFormat = GaussianSplatAsset.VectorFormat.Float32;
		private GaussianSplatAsset.ColorFormat _colorFormat = GaussianSplatAsset.ColorFormat.Float32x4;
		private GaussianSplatAsset.SHFormat _shFormat = GaussianSplatAsset.SHFormat.Float32;

		private SplatPartition[] _partitions;
		private int[] _partitionsOrder;
		private int _numRegisteredPartitions = 0;

		internal static class Props
		{
			public static readonly int SplatPos = Shader.PropertyToID("_SplatPos");
			public static readonly int SplatOther = Shader.PropertyToID("_SplatOther");
			public static readonly int SplatSH = Shader.PropertyToID("_SplatSH");
			public static readonly int SplatColor = Shader.PropertyToID("_SplatColor");
			public static readonly int SplatFormat = Shader.PropertyToID("_SplatFormat");
			public static readonly int SplatChunks = Shader.PropertyToID("_SplatChunks");
			public static readonly int SplatChunkCount = Shader.PropertyToID("_SplatChunkCount");
			public static readonly int SplatViewData = Shader.PropertyToID("_SplatViewData");
			public static readonly int OrderBuffer = Shader.PropertyToID("_OrderBuffer");
			public static readonly int SplatScale = Shader.PropertyToID("_SplatScale");
			public static readonly int SplatOpacityScale = Shader.PropertyToID("_SplatOpacityScale");
			public static readonly int SplatSize = Shader.PropertyToID("_SplatSize");
			public static readonly int SplatCount = Shader.PropertyToID("_SplatCount");
			public static readonly int SHOrder = Shader.PropertyToID("_SHOrder");
			public static readonly int SHOnly = Shader.PropertyToID("_SHOnly");
			public static readonly int DisplayIndex = Shader.PropertyToID("_DisplayIndex");
			public static readonly int DisplayChunks = Shader.PropertyToID("_DisplayChunks");
			public static readonly int GaussianSplatRT = Shader.PropertyToID("_GaussianSplatRT");
			public static readonly int SplatSortKeys = Shader.PropertyToID("_SplatSortKeys");
			public static readonly int SplatSortDistances = Shader.PropertyToID("_SplatSortDistances");
			public static readonly int MatrixMV = Shader.PropertyToID("_MatrixMV");
			public static readonly int MatrixObjectToWorld = Shader.PropertyToID("_MatrixObjectToWorld");
			public static readonly int MatrixWorldToObject = Shader.PropertyToID("_MatrixWorldToObject");
			public static readonly int VecScreenParams = Shader.PropertyToID("_VecScreenParams");
			public static readonly int VecWorldSpaceCameraPos = Shader.PropertyToID("_VecWorldSpaceCameraPos");

		}

		[field: NonSerialized] public bool editModified { get; private set; }
		[field: NonSerialized] public uint editSelectedSplats { get; private set; }
		[field: NonSerialized] public uint editDeletedSplats { get; private set; }
		[field: NonSerialized] public uint editCutSplats { get; private set; }
		[field: NonSerialized] public Bounds editSelectedBounds { get; private set; }

		public int splatCount => m_RegisteredSplatCount;

		enum KernelIndices
		{
			SetIndices,
			CalcViewData
		}

		public bool HasValidRenderSetup => m_GpuPosData != null && m_GpuOtherData != null && m_GpuChunks != null;


		const int kGpuViewDataSize = 40;
		public void RegisterPartition(SplatPartition partition)
		{
			if (partition.StartIndex >= 0) return; //already registered
			if (!partition.HasValidAsset)
			{
				Debug.Log("No valid asset for partition " + partition.PartitionIndex);
				return;
			}


			partition.StartIndex = m_RegisteredSplatCount;
			_partitionsOrder[_numRegisteredPartitions] = partition.PartitionIndex;
			Debug.Log("Merging partition data");
			MergePartitionData(partition);

			Debug.Log("Partition data merged");

			m_RegisteredSplatCount += partition.SplatCount;
			++_numRegisteredPartitions;
		}

		public void UnregisterPartition(SplatPartition partition)
		{
			
		}

		private void MergePartitionData(SplatPartition partition)
		{

			NativeArray<float> posData = partition.Asset.posData.GetData<float>();
			NativeArray<float4> colorData = partition.Asset.colorData.GetData<float4>();
			NativeArray<uint> shData = partition.Asset.shData.GetData<uint>();
			NativeArray<uint> otherData = partition.Asset.otherData.GetData<uint>();

			//Merge Position data
			m_distCalculator.LoadSplatPositions(posData, partition.StartIndex, partition.SplatCount, partition.transform, this.transform);
			
			m_GpuPosData.SetData(_combinedData.PosData, partition.StartIndex, partition.StartIndex, partition.SplatCount);

			//Merge Color data
			_combinedData.CopyColorData(partition.StartIndex, partition.SplatCount, colorData);
			m_GpuColorData.SetData(_combinedData.ColorData, partition.StartIndex, partition.StartIndex, partition.SplatCount);

			//Merge SH data
			int shSize = UnsafeUtility.SizeOf<SHTableItemFloat32>() / 4;

			m_GpuSHData.SetData(shData, 0, shSize * partition.StartIndex, partition.SplatCount * shSize);

			//Merge other data
			int otherDataSize = 4;

			m_GpuOtherData.SetData(otherData, 0, otherDataSize * partition.StartIndex, otherDataSize * partition.SplatCount);

			posData.Dispose();
			colorData.Dispose();
			shData.Dispose();
			otherData.Dispose();
		}

		public void ReserveResources(SplatPartition[] partitions)
		{
			_partitions = partitions;
			_partitionsOrder = new int[partitions.Length];


			m_ReservedSplatCount = partitions.Sum(p => p.SplatCount);

			_combinedData = new CombinedSplatData(m_ReservedSplatCount, GaussianSplatAsset.ColorFormat.Float32x4);
			Debug.Log($"CreateResourcesForAsset - #01");
			m_GpuPosData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_ReservedSplatCount * 4, 4) { name = "GaussianPosData" };

			Debug.Log($"CreateResourcesForAsset -  #02");

			m_distCalculator = new CPUDistanceCalculator(_combinedData.PosData);
			// null, 0, GaussianSplatAsset.GetVectorSize(GaussianSplatAsset.VectorFormat.Float32), m_RegisteredSplatCount
			Debug.Log($"CreateResourcesForAsset -  #03");

			m_GpuOtherData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_ReservedSplatCount * 4, 4) { name = "GaussianOtherData" };

			int dataLen = (int)GaussianSplatAsset.CalcSHDataSize(m_ReservedSplatCount, _shFormat);
			m_GpuSHData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)(dataLen / 4), 4) { name = "GaussianSHData" };

			//var (texWidth, texHeight) = GaussianSplatAsset.CalcTextureSize(m_ReservedSplatCount);
			//var texFormat = GaussianSplatAsset.ColorFormatToGraphics(GaussianSplatAsset.ColorFormat.Float32x4);
			////var tex = new Texture2D(texWidth, texHeight, texFormat, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.IgnoreMipmapLimit | TextureCreationFlags.DontUploadUponCreate) { name = "GaussianColorData" };


			Debug.Log($"CreateResourcesForAsset - #04");
			m_GpuColorData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, m_ReservedSplatCount * 16, 16) ;

			// just a dummy chunk buffer
			m_GpuChunks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1,
				UnsafeUtility.SizeOf<GaussianSplatAsset.ChunkInfo>())
			{ name = "GaussianChunkData" };
			m_GpuChunksValid = false;
			//}

			m_GpuView = new GraphicsBuffer(GraphicsBuffer.Target.Structured, m_ReservedSplatCount, kGpuViewDataSize);
			m_GpuIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 36, 2);
			// cube indices, most often we use only the first quad
			m_GpuIndexBuffer.SetData(new ushort[]
			{
				0, 1, 2, 1, 3, 2,
				4, 6, 5, 5, 6, 7,
				0, 2, 4, 4, 2, 6,
				1, 5, 3, 5, 7, 3,
				0, 4, 1, 4, 5, 1,
				2, 3, 6, 3, 7, 6
			});
			Debug.Log($"CreateResourcesForAsset - #05");
			InitSortBuffers(m_ReservedSplatCount);

			Debug.Log($"CreateResourcesForAsset -  #06");
		}

		void InitSortBuffers(int count)
		{

			m_GpuSortDistances = new uint[count];
			m_GpuSortIndices = new uint[count];
			m_GpuSortKeys = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, 4) { name = "GaussianSplatSortIndices" };

			// init keys buffer to splat indices
			////m_CSSplatUtilities.SetBuffer((int)KernelIndices.SetIndices, Props.SplatSortKeys, m_GpuSortKeys);
			//m_CSSplatUtilities.SetInt(Props.SplatCount, count);
			//m_CSSplatUtilities.GetKernelThreadGroupSizes((int)KernelIndices.SetIndices, out uint gsX, out _, out _);
			//m_CSSplatUtilities.Dispatch((int)KernelIndices.SetIndices, (count + (int)gsX - 1) / (int)gsX, 1, 1);
			m_Sorter = new CPUSorting((uint)count);
			m_Sorter.InitializeIndices(ref m_GpuSortIndices);
		}

		public bool GatherPartitionsForCamera(Camera cam)
		{
			if (!_partitions.Any(p => p.ShouldRender))
			{
				return false;
			}

			foreach(var p in _partitions.Where(pa=> !pa.ShouldRender))
			{
				p.RenderOrder = 10000;
			}

			//TODO: sort partitions by render order
			Array.Sort(_partitions, (a, b) =>
			{
				return a.RenderOrder.CompareTo(b.RenderOrder);
			});
			//fixRenderOrder
			UpdateRenderOrder();


			return true;
		}

		//private void OnGUI()
		//{
		//	GUI.Label(new Rect(10,10,Screen.width-20,30), "Partitions to render: " + string.Join(", ", _partitions.Where(p => p.ShouldRender).Select(p => p.PartitionIndex)));

		//}

		void UpdateRenderOrder()
		{
			bool isCorrectOrder = true;
			for (int idx = 0; idx < _partitions.Length; ++idx)
			{
				int expectedPartition = _partitions[idx].PartitionIndex;
				int currentPartitionIdx = _partitionsOrder[idx];
				if (expectedPartition != currentPartitionIdx)
				{
					isCorrectOrder = false;
					break;
				}
			}
			if (isCorrectOrder) return;

			int[] correctedOrder = _partitions.Select(p => p.PartitionIndex).ToArray();
			m_Sorter.ReorderPartitions(_partitions, ref _partitionsOrder, ref m_GpuSortDistances, ref m_GpuSortIndices);

		}


		public void Awake()
		{
			Debug.Log("GaussianSplatRenderer - Start #01");
			m_FrameCounter = 0;
			if (m_ShaderSplats == null || m_ShaderComposite == null || m_ShaderDebugPoints == null || m_ShaderDebugBoxes == null || m_CSSplatUtilities == null)
				return;
			if (!SystemInfo.supportsComputeShaders)
				return;

			string assetName = "combined";
			m_MatSplats = new Material(m_ShaderSplats) { name = "GS_" + assetName };
			m_MatComposite = new Material(m_ShaderComposite) { name = "GClearDstA_" + assetName };
			m_MatDebugPoints = new Material(m_ShaderDebugPoints) { name = "GDebugPoints_" + assetName };
			m_MatDebugBoxes = new Material(m_ShaderDebugBoxes) { name = "GDebugBox_" + assetName };
			Debug.Log("GaussianSplatRenderer - Start #02");
			GaussianSplatRenderSystem.instance.RegisterSplatRenderer(this);

			Debug.Log("GaussianSplatRenderer - Start #03");

		}

		void SetAssetDataOnCS(CommandBuffer cmb, KernelIndices kernel)
		{
			ComputeShader cs = m_CSSplatUtilities;
			int kernelIndex = (int)kernel;
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatPos, m_GpuPosData);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatChunks, m_GpuChunks);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatOther, m_GpuOtherData);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatSH, m_GpuSHData);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatColor, m_GpuColorData);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.SplatViewData, m_GpuView);
			cmb.SetComputeBufferParam(cs, kernelIndex, Props.OrderBuffer, m_GpuSortKeys);

			uint format = (uint)_posFormat | ((uint)_scaleFormat << 8) | ((uint)_shFormat << 16);
			cmb.SetComputeIntParam(cs, Props.SplatFormat, (int)format);
			cmb.SetComputeIntParam(cs, Props.SplatCount, m_RegisteredSplatCount);
			cmb.SetComputeIntParam(cs, Props.SplatChunkCount, m_GpuChunksValid ? m_GpuChunks.count : 0);

			//UpdateCutoutsBuffer();
			//cmb.SetComputeIntParam(cs, Props.SplatCutoutsCount, m_Cutouts?.Length ?? 0);
		}

		internal void SetAssetDataOnMaterial(MaterialPropertyBlock mat)
		{
			mat.SetBuffer(Props.SplatPos, m_GpuPosData);
			mat.SetBuffer(Props.SplatOther, m_GpuOtherData);
			mat.SetBuffer(Props.SplatSH, m_GpuSHData);
			mat.SetBuffer(Props.SplatColor, m_GpuColorData);
			uint format = (uint)_posFormat | ((uint)_scaleFormat << 8) | ((uint)_shFormat << 16);
			mat.SetInteger(Props.SplatFormat, (int)format);
			mat.SetInteger(Props.SplatCount, m_RegisteredSplatCount);
			mat.SetInteger(Props.SplatChunkCount, m_GpuChunksValid ? m_GpuChunks.count : 0);
		}

		static void DisposeBuffer(ref GraphicsBuffer buf)
		{
			buf?.Dispose();
			buf = null;
		}

		void DisposeResourcesForAsset()
		{
			DisposeBuffer(ref m_GpuColorData);

			DisposeBuffer(ref m_GpuPosData);
			DisposeBuffer(ref m_GpuOtherData);
			DisposeBuffer(ref m_GpuSHData);
			DisposeBuffer(ref m_GpuChunks);

			DisposeBuffer(ref m_GpuView);
			DisposeBuffer(ref m_GpuIndexBuffer);
			//DisposeBuffer(ref m_GpuSortDistances);
			DisposeBuffer(ref m_GpuSortKeys);

			//m_SorterArgs.resources.Dispose();

			m_RegisteredSplatCount = 0;
			m_GpuChunksValid = false;

			editSelectedSplats = 0;
			editDeletedSplats = 0;
			editCutSplats = 0;
			editModified = false;
			editSelectedBounds = default;

			_combinedData.Dispose();
		}

		public void OnDestroy()
		{
			DisposeResourcesForAsset();
			GaussianSplatRenderSystem.instance.UnRegisterSplatRenderer(this);

			DestroyImmediate(m_MatSplats);
			DestroyImmediate(m_MatComposite);
			DestroyImmediate(m_MatDebugPoints);
			DestroyImmediate(m_MatDebugBoxes);
		}

		internal void CalcViewData(CommandBuffer cmb, Camera cam, Matrix4x4 matrix)
		{
			if (cam.cameraType == CameraType.Preview)
				return;

			var tr = transform;

			Matrix4x4 matView = cam.worldToCameraMatrix;
			Matrix4x4 matProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
			Matrix4x4 matO2W = tr.localToWorldMatrix;
			Matrix4x4 matW2O = tr.worldToLocalMatrix;
			int screenW = cam.pixelWidth, screenH = cam.pixelHeight;
			int eyeW = XRSettings.eyeTextureWidth, eyeH = XRSettings.eyeTextureHeight;
			Vector4 screenPar = new Vector4(eyeW != 0 ? eyeW : screenW, eyeH != 0 ? eyeH : screenH, 0, 0);
			Vector4 camPos = cam.transform.position;

			// calculate view dependent data for each splat
			SetAssetDataOnCS(cmb, KernelIndices.CalcViewData);

			cmb.SetComputeMatrixParam(m_CSSplatUtilities, Props.MatrixMV, matView * matO2W);
			cmb.SetComputeMatrixParam(m_CSSplatUtilities, Props.MatrixObjectToWorld, matO2W);
			cmb.SetComputeMatrixParam(m_CSSplatUtilities, Props.MatrixWorldToObject, matW2O);

			cmb.SetComputeVectorParam(m_CSSplatUtilities, Props.VecScreenParams, screenPar);
			cmb.SetComputeVectorParam(m_CSSplatUtilities, Props.VecWorldSpaceCameraPos, camPos);
			cmb.SetComputeFloatParam(m_CSSplatUtilities, Props.SplatScale, m_SplatScale);
			cmb.SetComputeFloatParam(m_CSSplatUtilities, Props.SplatOpacityScale, m_OpacityScale);
			cmb.SetComputeIntParam(m_CSSplatUtilities, Props.SHOrder, m_SHOrder);
			cmb.SetComputeIntParam(m_CSSplatUtilities, Props.SHOnly, m_SHOnly ? 1 : 0);

			m_CSSplatUtilities.GetKernelThreadGroupSizes((int)KernelIndices.CalcViewData, out uint gsX, out _, out _);
			cmb.DispatchCompute(m_CSSplatUtilities, (int)KernelIndices.CalcViewData, (m_GpuView.count + (int)gsX - 1) / (int)gsX, 1, 1);
		}

		internal void SortPoints(CommandBuffer cmd, Camera cam, Matrix4x4 matrix)
		{
			if (cam.cameraType == CameraType.Preview)
				return;

			Matrix4x4 worldToCamMatrix = cam.worldToCameraMatrix;
			worldToCamMatrix.m20 *= -1;
			worldToCamMatrix.m21 *= -1;
			worldToCamMatrix.m22 *= -1;

			Vector3 camPosInv = worldToCamMatrix.inverse.MultiplyPoint(cam.transform.position);
			float camScale = 1 / cam.farClipPlane;
			camScale *= camScale;
			//// calculate distance to the camera for each splat
			//cmd.BeginSample(s_ProfSort);
			//cmd.SetComputeBufferParam(m_CSSplatUtilities, (int)KernelIndices.CalcDistances, Props.SplatSortDistances, m_GpuSortDistances);
			//cmd.SetComputeBufferParam(m_CSSplatUtilities, (int)KernelIndices.CalcDistances, Props.SplatSortKeys, m_GpuSortKeys);
			//cmd.SetComputeBufferParam(m_CSSplatUtilities, (int)KernelIndices.CalcDistances, Props.SplatChunks, m_GpuChunks);
			//cmd.SetComputeBufferParam(m_CSSplatUtilities, (int)KernelIndices.CalcDistances, Props.SplatPos, m_GpuPosData);
			//cmd.SetComputeIntParam(m_CSSplatUtilities, Props.SplatFormat, (int)m_Asset.posFormat);
			//cmd.SetComputeMatrixParam(m_CSSplatUtilities, Props.MatrixMV, worldToCamMatrix * matrix);
			//cmd.SetComputeIntParam(m_CSSplatUtilities, Props.SplatCount, m_SplatCount);
			//cmd.SetComputeIntParam(m_CSSplatUtilities, Props.SplatChunkCount, m_GpuChunksValid ? m_GpuChunks.count : 0);
			//m_CSSplatUtilities.GetKernelThreadGroupSizes((int)KernelIndices.CalcDistances, out uint gsX, out _, out _);
			//cmd.DispatchCompute(m_CSSplatUtilities, (int)KernelIndices.CalcDistances, (m_GpuSortDistances.count + (int)gsX - 1)/(int)gsX, 1, 1);
			m_distCalculator.CalcDistances(worldToCamMatrix * matrix, ref m_GpuSortIndices, ref m_GpuSortDistances, m_RegisteredSplatCount);
			//m_distCalculator.CalcDistances(camPosInv, camScale, ref m_GpuSortIndices, ref m_GpuSortDistances);
			//// sort the splats

#if SORT_NONE
#elif SORT_CPU

			for (int idx = 0; idx < _partitions.Length; ++idx)
			{
				if (!_partitions[idx].ShouldRender) continue;
				m_Sorter.Sort(ref m_GpuSortDistances, ref m_GpuSortIndices, _partitions[idx].StartIndex, _partitions[idx].SplatCount);
			}
#endif
			//m_Sorter.Dispatch(cmd, m_SorterArgs);
			//cmd.EndSample(s_ProfSort);


			//Copy sorted indices to the buffer
			m_GpuSortKeys.SetData(m_GpuSortIndices, 0, 0, m_RegisteredSplatCount);
		}

		//		public void ActivateCamera(int index)
		//		{
		//			Camera mainCam = Camera.main;
		//			if (!mainCam)
		//				return;
		//			if (!m_Asset || m_Asset.cameras == null)
		//				return;

		//			var selfTr = transform;
		//			var camTr = mainCam.transform;
		//			var prevParent = camTr.parent;
		//			var cam = m_Asset.cameras[index];
		//			camTr.parent = selfTr;
		//			camTr.localPosition = cam.pos;
		//			camTr.localRotation = Quaternion.LookRotation(cam.axisZ, cam.axisY);
		//			camTr.parent = prevParent;
		//			camTr.localScale = Vector3.one;
		//#if UNITY_EDITOR
		//			UnityEditor.EditorUtility.SetDirty(camTr);
		//#endif
		//		}

		void ClearGraphicsBuffer(GraphicsBuffer buf)
		{
		}

		void UnionGraphicsBuffers(GraphicsBuffer dst, GraphicsBuffer src)
		{
		}

		public void UpdateEditCountsAndBounds()
		{

		}

		void UpdateCutoutsBuffer()
		{

		}

		bool EnsureEditingBuffers()
		{
			return false;
		}

		public void EditStoreSelectionMouseDown()
		{

		}

		public void EditStorePosMouseDown()
		{

		}
		public void EditStoreOtherMouseDown()
		{

		}

		public void EditUpdateSelection(Vector2 rectMin, Vector2 rectMax, Camera cam, bool subtract)
		{

		}

		public void EditTranslateSelection(Vector3 localSpacePosDelta)
		{

		}

		public void EditRotateSelection(Vector3 localSpaceCenter, Matrix4x4 localToWorld, Matrix4x4 worldToLocal, Quaternion rotation)
		{

		}


		public void EditScaleSelection(Vector3 localSpaceCenter, Matrix4x4 localToWorld, Matrix4x4 worldToLocal, Vector3 scale)
		{

		}

		public void EditDeleteSelected()
		{

		}

		public void EditSelectAll()
		{

		}

		public void EditDeselectAll()
		{

		}

		public void EditInvertSelection()
		{

		}

		public bool EditExportData(GraphicsBuffer dstData, bool bakeTransform)
		{
			return false;
		}

		public void EditSetSplatCount(int newSplatCount)
		{

		}

		public void EditCopySplatsInto(GaussianSplatRenderer dst, int copySrcStartIndex, int copyDstStartIndex, int copyCount)
		{

		}

		public void EditCopySplats(
			Transform dstTransform,
			GraphicsBuffer dstPos, GraphicsBuffer dstOther, GraphicsBuffer dstSH, Texture dstColor,
			GraphicsBuffer dstEditDeleted,
			int dstSize,
			int copySrcStartIndex, int copyDstStartIndex, int copyCount)
		{

		}

		void DispatchUtilsAndExecute(CommandBuffer cmb, KernelIndices kernel, int count)
		{
			m_CSSplatUtilities.GetKernelThreadGroupSizes((int)kernel, out uint gsX, out _, out _);
			cmb.DispatchCompute(m_CSSplatUtilities, (int)kernel, (int)((count + gsX - 1) / gsX), 1, 1);
			Graphics.ExecuteCommandBuffer(cmb);
		}

	}
}