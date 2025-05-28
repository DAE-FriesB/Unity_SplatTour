// SPDX-License-Identifier: MIT

using UnityEngine;

namespace GaussianSplatting.Runtime
{
	public class SplatPartition : MonoBehaviour
	{
		public int PartitionIndex { get; set; }
		public int RenderOrder { get; set; } = -1;
		public bool IsActive { get; set; } = false;
		public bool ShouldRender => IsActive && this.isActiveAndEnabled && this.HasValidAsset && StartIndex != -1;
		public int StartIndex { get; set; } = -1;
		public int SplatCount { get; set; }
		
		public GaussianSplatAsset Asset;
		public bool HasValidAsset => IsAssetValid(Asset);
		public static bool IsAssetValid(GaussianSplatAsset asset) =>
			asset != null &&
			asset.splatCount > 0 &&
			asset.formatVersion == GaussianSplatAsset.kCurrentVersion &&
			asset.posData != null &&
			asset.otherData != null &&
			asset.shData != null &&
			asset.colorData != null;

		//private void OnEnable()
		//{
		//	GaussianSplatRenderSystem.instance.SetSplatActive(this, true);
		//}
		//private void OnDisable()
		//{
		//	GaussianSplatRenderSystem.instance.SetSplatActive(this, false);
		//}
	}
}