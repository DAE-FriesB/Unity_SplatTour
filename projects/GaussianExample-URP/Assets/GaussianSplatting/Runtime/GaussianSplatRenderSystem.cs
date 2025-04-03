// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace GaussianSplatting.Runtime
{
	class GaussianSplatRenderSystem
	{
		// ReSharper disable MemberCanBePrivate.Global - used by HDRP/URP features that are not always compiled
		internal static readonly ProfilerMarker s_ProfDraw = new(ProfilerCategory.Render, "GaussianSplat.Draw", MarkerFlags.SampleGPU);
		internal static readonly ProfilerMarker s_ProfCompose = new(ProfilerCategory.Render, "GaussianSplat.Compose", MarkerFlags.SampleGPU);
		internal static readonly ProfilerMarker s_ProfCalcView = new(ProfilerCategory.Render, "GaussianSplat.CalcView", MarkerFlags.SampleGPU);
		// ReSharper restore MemberCanBePrivate.Global

		public static GaussianSplatRenderSystem instance => ms_Instance ??= new GaussianSplatRenderSystem();
		static GaussianSplatRenderSystem ms_Instance;

		private GaussianSplatRenderer _renderer;
		private MaterialPropertyBlock _propertyBlock;

		readonly HashSet<Camera> m_CameraCommandBuffersDone = new();

		CommandBuffer m_CommandBuffer;

		public void AddPartition(SplatPartition partition)
		{

		}
		public void RegisterSplatRenderer(GaussianSplatRenderer r)
		{
			if (_renderer != null) return;


			if (GraphicsSettings.currentRenderPipeline == null)
				Camera.onPreCull += OnPreCullCamera;

			_renderer = r;
			_propertyBlock = new MaterialPropertyBlock();
		}

		public void SetSplatActive(SplatPartition p, bool active)
		{
			p.IsActive = active;
			//var instance = m_Partitions.FirstOrDefault(s => s.Renderer == r);
			//if (instance != null) instance.IsActive = active;
		}

		public void UnRegisterSplatRenderer(GaussianSplatRenderer r)
		{
			if (_renderer == null)
				return;

			if (m_CameraCommandBuffersDone != null)
			{
				if (m_CommandBuffer != null)
				{
					foreach (var cam in m_CameraCommandBuffersDone)
					{
						if (cam)
							cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, m_CommandBuffer);
					}
				}
				m_CameraCommandBuffersDone.Clear();
			}

			m_CommandBuffer?.Dispose();
			m_CommandBuffer = null;
			Camera.onPreCull -= OnPreCullCamera;

			_renderer = null;
		}

		// ReSharper disable once MemberCanBePrivate.Global - used by HDRP/URP features that are not always compiled
		public bool GatherSplatsForCamera(Camera cam)
		{
			if (_renderer == null)
				return false;
			if (cam.cameraType == CameraType.Preview)
				return false;
			if (_renderer.GatherAndSortSplatsForCamera(cam))
			{
				return true;
			}
			

			return true;
		}

		// ReSharper disable once MemberCanBePrivate.Global - used by HDRP/URP features that are not always compiled
		public Material SortAndRenderSplats(Camera cam, CommandBuffer cmb)
		{
			if (_renderer == null) return null;
			var gs = _renderer;
			Material matComposite = gs.m_MatComposite;

			var mpb = _propertyBlock;

			// sort
			var matrix = gs.transform.localToWorldMatrix;
			if (gs.m_FrameCounter % gs.m_SortNthFrame == 0)
			{
				gs.SortPoints(cmb, cam, matrix);
			}
			++gs.m_FrameCounter;

			// cache view
			_propertyBlock.Clear();
			Material displayMat = gs.m_RenderMode switch
			{
				GaussianSplatRenderer.RenderMode.DebugPoints => gs.m_MatDebugPoints,
				GaussianSplatRenderer.RenderMode.DebugPointIndices => gs.m_MatDebugPoints,
				GaussianSplatRenderer.RenderMode.DebugBoxes => gs.m_MatDebugBoxes,
				GaussianSplatRenderer.RenderMode.DebugChunkBounds => gs.m_MatDebugBoxes,
				_ => gs.m_MatSplats
			};

			gs.SetAssetDataOnMaterial(mpb);
			mpb.SetBuffer(GaussianSplatRenderer.Props.SplatChunks, gs.m_GpuChunks);

			mpb.SetBuffer(GaussianSplatRenderer.Props.SplatViewData, gs.m_GpuView);
			mpb.SetBuffer(GaussianSplatRenderer.Props.OrderBuffer, gs.m_GpuSortKeys);
			mpb.SetFloat(GaussianSplatRenderer.Props.SplatScale, gs.m_SplatScale);
			mpb.SetFloat(GaussianSplatRenderer.Props.SplatOpacityScale, gs.m_OpacityScale);
			mpb.SetFloat(GaussianSplatRenderer.Props.SplatSize, gs.m_PointDisplaySize);
			mpb.SetInteger(GaussianSplatRenderer.Props.SHOrder, gs.m_SHOrder);
			mpb.SetInteger(GaussianSplatRenderer.Props.SHOnly, gs.m_SHOnly ? 1 : 0);
			mpb.SetInteger(GaussianSplatRenderer.Props.DisplayIndex, gs.m_RenderMode == GaussianSplatRenderer.RenderMode.DebugPointIndices ? 1 : 0);
			mpb.SetInteger(GaussianSplatRenderer.Props.DisplayChunks, gs.m_RenderMode == GaussianSplatRenderer.RenderMode.DebugChunkBounds ? 1 : 0);

			cmb.BeginSample(s_ProfCalcView);
			gs.CalcViewData(cmb, cam, matrix);
			cmb.EndSample(s_ProfCalcView);

			// draw
			int indexCount = 6;
			int instanceCount = gs.splatCount;
			MeshTopology topology = MeshTopology.Triangles;
			if (gs.m_RenderMode is GaussianSplatRenderer.RenderMode.DebugBoxes or GaussianSplatRenderer.RenderMode.DebugChunkBounds)
				indexCount = 36;
			if (gs.m_RenderMode == GaussianSplatRenderer.RenderMode.DebugChunkBounds)
				instanceCount = gs.m_GpuChunksValid ? gs.m_GpuChunks.count : 0;

			cmb.BeginSample(s_ProfDraw);
			cmb.DrawProcedural(gs.m_GpuIndexBuffer, matrix, displayMat, 0, topology, indexCount, instanceCount, mpb);
			cmb.EndSample(s_ProfDraw);

			return matComposite;
		}

		// ReSharper disable once MemberCanBePrivate.Global - used by HDRP/URP features that are not always compiled
		// ReSharper disable once UnusedMethodReturnValue.Global - used by HDRP/URP features that are not always compiled
		public CommandBuffer InitialClearCmdBuffer(Camera cam)
		{
			m_CommandBuffer ??= new CommandBuffer { name = "RenderGaussianSplats" };
			if (GraphicsSettings.currentRenderPipeline == null && cam != null && !m_CameraCommandBuffersDone.Contains(cam))
			{
				cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, m_CommandBuffer);
				m_CameraCommandBuffersDone.Add(cam);
			}

			// get render target for all splats
			m_CommandBuffer.Clear();
			return m_CommandBuffer;
		}

		void OnPreCullCamera(Camera cam)
		{
			if (!GatherSplatsForCamera(cam))
				return;

			InitialClearCmdBuffer(cam);

			m_CommandBuffer.GetTemporaryRT(GaussianSplatRenderer.Props.GaussianSplatRT, -1, -1, 0, FilterMode.Point, GraphicsFormat.R16G16B16A16_SFloat);
			m_CommandBuffer.SetRenderTarget(GaussianSplatRenderer.Props.GaussianSplatRT, BuiltinRenderTextureType.CurrentActive);
			m_CommandBuffer.ClearRenderTarget(RTClearFlags.Color, new Color(0, 0, 0, 0), 0, 0);

			// add sorting, view calc and drawing commands for each splat object
			Material matComposite = SortAndRenderSplats(cam, m_CommandBuffer);

			// compose
			m_CommandBuffer.BeginSample(s_ProfCompose);
			m_CommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			m_CommandBuffer.DrawProcedural(Matrix4x4.identity, matComposite, 0, MeshTopology.Triangles, 3, 1);
			m_CommandBuffer.EndSample(s_ProfCompose);
			m_CommandBuffer.ReleaseTemporaryRT(GaussianSplatRenderer.Props.GaussianSplatRT);
		}
	}
}