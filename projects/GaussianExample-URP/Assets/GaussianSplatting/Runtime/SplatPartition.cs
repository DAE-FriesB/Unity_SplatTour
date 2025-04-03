// SPDX-License-Identifier: MIT

using UnityEngine;

namespace GaussianSplatting.Runtime
{
	public class SplatPartition : MonoBehaviour
	{
		public int PartitionIndex { get; set; }
		public uint RenderOrder { get; set; }
		public bool IsActive { get; set; } = false;
		public bool ShouldRender => IsActive && this.isActiveAndEnabled && this.HasValidAsset;
		public int StartIndex { get; set; }
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

	}
}