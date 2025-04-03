using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEditor.VersionControl;
using UnityEngine;

namespace GaussianSplatting.Runtime
{
	public class SplatSplitter : MonoBehaviour
	{
		//Split settings
		public Vector2 PartitionSize;
		public Vector2 CenterOffset;
		public int NumRows;
		public int NumColumns;
		public int MainChunkIndex;

		private Bounds[] _bounds = null;
		private const float _boundsHeight = 50f;

		private GaussianSplatRenderer _defaultRenderer;
		private Dictionary<int,SplatPartition> _partitions;
		[SerializeField]
		private GameObject _splatPartitionPrefab;

		private void OnValidate()
		{
			_bounds = new Bounds[NumRows * NumColumns];
			for (int idx = 0; idx < _bounds.Length; ++idx)
			{
				_bounds[idx] = CalculateBounds(idx);
			}
			_partitions = new Dictionary<int, SplatPartition>();

		}

		private void Awake()
		{
			_defaultRenderer = GetComponent<GaussianSplatRenderer>();
		}
		// Update is called once per frame
		void Update()
		{
			for (int idx = 0; idx < _partitions.Count; ++idx)
			{
				if (!_partitions.ContainsKey(idx)) continue;
				if (_partitions[idx] == null) continue;
				_partitions[idx].enabled = IsVisibleInCamera(idx);

				//TODO: update rendering order
				if (_partitions[idx].enabled)
				{
					Vector3 toCenter = _bounds[idx].center - Camera.main.transform.position;
					toCenter.y = 0f;
					_partitions[idx].RenderOrder = (uint)Mathf.RoundToInt(toCenter.sqrMagnitude);
				}

			}
		}
		public SplatPartition CreatePartition(int partitionIndex)
		{
			GameObject instance = GameObject.Instantiate(_splatPartitionPrefab, transform);
			instance.transform.localRotation = Quaternion.identity;
			instance.transform.localScale = Vector3.one;
			Bounds b = GetBounds(partitionIndex);
			instance.transform.position = b.center;

			var partition = instance.GetComponent<SplatPartition>();
			partition.PartitionIndex = partitionIndex;
			_partitions.Add(partitionIndex, partition);
			return partition;
		}

		public void InitializeRenderer()
		{
			_defaultRenderer.ReserveResources(_partitions.Values.ToArray());
		}

		public void SplatLoaded(int partitionIndex, GaussianSplatAsset asset)
		{
			var partition = _partitions[partitionIndex];
			partition.Asset = asset;

			if (IsVisibleInCamera(partitionIndex))
			{
				partition.enabled = true;
			}
			_defaultRenderer.OnPartitionLoaded(partition);
		}

		bool IsVisibleInCamera(int partitionIndex)
		{
			if (partitionIndex == -1) return true;
			//Shoot 16 rays (4x4) from camera to bounds
			Bounds bounds = GetBounds(partitionIndex);
			if (bounds.Contains(Camera.main.transform.position))
			{
				return true;
			}
			int vertRays = 5;
			int horRays = 5;
			float xSpacing = horRays <= 1 ? 0 : 1f / (horRays - 1);
			float ySPacing = vertRays <= 1 ? 0 : 1f / (vertRays - 1);
			float startX = horRays <= 1 ? 0.5f : 0f;
			float startY = vertRays <= 1 ? 0.5f : 0f;
			for (int x = 0; x < horRays; ++x)
			{
				for (int y = 0; y < vertRays; ++y)
				{
					Vector3 viewPortPoint = new Vector3(startX + x * xSpacing, startY + y * ySPacing);
					Ray r = Camera.main.ViewportPointToRay(viewPortPoint);
#if UNITY_EDITOR
					//Debug.DrawRay(r.origin + r.direction * Camera.main.nearClipPlane, r.direction * (Camera.main.farClipPlane - Camera.main.nearClipPlane), Color.blue);
#endif
					if (bounds.IntersectRay(r, out float dist))
					{
						if (dist > Camera.main.nearClipPlane && dist < Camera.main.farClipPlane)
							return true;
					}

					Ray r2 = new Ray(r.origin + r.direction * Camera.main.farClipPlane, -r.direction);
#if UNITY_EDITOR
					//Debug.DrawRay(r2.origin, r2.direction * (Camera.main.farClipPlane - Camera.main.nearClipPlane), Color.magenta);
#endif

					if (bounds.IntersectRay(r2, out float dist2))
					{
						if (dist2 > 0 && dist2 < Camera.main.farClipPlane - Camera.main.nearClipPlane)
							return true;
					}

				}
			}
			return false;

			//Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
			//bool isVisible = GeometryUtility.TestPlanesAABB(planes, bounds);
			//return isVisible;

		}


#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			for (int row = 0; row < NumRows; ++row)
			{
				for (int col = 0; col < NumColumns; ++col)
				{
					int partitionIndex = GetPartitionIndex(row, col);
					if (Application.isPlaying && !IsVisibleInCamera(partitionIndex)) continue;
					Gizmos.color = Color.yellow;
					Bounds bounds = CalculateBounds(partitionIndex, 10);
					Gizmos.DrawWireCube(bounds.center, bounds.size);
				}
			}

			if (!Application.isPlaying || IsVisibleInCamera(MainChunkIndex))
			{
				Bounds mainBounds = CalculateBounds(MainChunkIndex, 10);
				Gizmos.color = Color.green;
				Gizmos.DrawWireCube(mainBounds.center, mainBounds.size);
			}
		}
#endif

		private int GetPartitionIndex(int row, int column)
		{
			return row * NumColumns + column;
		}

		public Bounds GetBounds(int partitionIndex)
		{
			return _bounds[partitionIndex];
		}
		private Bounds CalculateBounds(int partitionIndex, float height = _boundsHeight)
		{
			Vector3 chunkSize = new Vector3(PartitionSize.x, height, PartitionSize.y);
			Vector3 center = transform.position + new Vector3(CenterOffset.x, 0f, CenterOffset.y);
			Vector3 startPos = center - new Vector3(chunkSize.x * (NumColumns - 1) / 2f, 0, chunkSize.z * (NumRows - 1) / 2f);

			int row = partitionIndex / NumColumns;
			int col = partitionIndex % NumColumns;

			return new Bounds(startPos + new Vector3(chunkSize.x * col, 0f, chunkSize.z * row),
				chunkSize);
		}

		public bool IsInBounds(int partitionIndex, Vector3 pos)
		{
			return GetBounds(partitionIndex).Contains(pos);
		}
		public int GetPartitionIndex(Vector3 pos)
		{
			for (int idx = 0; idx < _bounds.Length; ++idx)
			{
				if (IsInBounds(idx, pos)) return idx;
			}
			return -1;
		}


	}
}
