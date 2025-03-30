using JetBrains.Annotations;
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

		private GaussianSplatRenderer[] _partitionRenderers;
		[SerializeField]
		private GameObject _splatPartitionPrefab;

		private void OnValidate()
		{
			_bounds = new Bounds[NumRows * NumColumns];
			for (int idx = 0; idx < _bounds.Length; ++idx)
			{
				_bounds[idx] = CalculateBounds(idx);
			}
			_partitionRenderers = new GaussianSplatRenderer[_bounds.Length];
		}
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{

		}
		// Update is called once per frame
		void Update()
		{
			for(int idx = 0; idx < _partitionRenderers.Length; ++idx)
			{
				if (_partitionRenderers[idx] == null) continue;
				_partitionRenderers[idx].enabled = IsVisibleInCamera(idx);
			}
			
			//TODO: update rendering order

		}

		public void SplatLoaded(int partitionIndex, GaussianSplatAsset asset)
		{
			//Instantiate prefab
			GameObject instance = GameObject.Instantiate(_splatPartitionPrefab, transform);
			Bounds b = GetBounds(partitionIndex);
			instance.transform.position = b.center;
			var r = instance.GetComponent<GaussianSplatRenderer>();
			_partitionRenderers[partitionIndex] = r;
			r.m_Asset = asset;
			if (IsVisibleInCamera(partitionIndex))
			{
				r.enabled = true;
			}
		}

		bool IsVisibleInCamera(int partitionIndex)
		{
			//Shoot 16 rays (4x4) from camera to bounds
			Bounds bounds = GetBounds(partitionIndex);
			if (bounds.Contains(Camera.main.transform.position))
			{
				return true;
			}
//			int vertRays = 3;
//			int horRays = 4;
//			float xSpacing = horRays <= 1 ? 0 : 1f / (horRays - 1);
//			float ySPacing = vertRays <= 1 ? 0 : 1f / (vertRays - 1);
//			float startX = horRays <= 1 ? 0.5f : 0f;
//			float startY = vertRays <= 1 ? 0.5f : 0f;
//			for (int x = 0; x < horRays; ++x)
//			{
//				for (int y = 0; y < vertRays; ++y)
//				{
//					Vector3 viewPortPoint = new Vector3(startX + x * xSpacing, startY + y * ySPacing);
//					Ray r = Camera.main.ViewportPointToRay(viewPortPoint);
//#if UNITY_EDITOR && false
//					Debug.DrawRay(r.origin + r.direction * Camera.main.nearClipPlane, r.direction * (Camera.main.farClipPlane - Camera.main.nearClipPlane), Color.blue);
//#endif
//					if (bounds.IntersectRay(r, out float dist) && dist > Camera.main.nearClipPlane && dist < Camera.main.farClipPlane)
//					{
//						return true;
//					}
//				}
//			}
//			return false;

			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
			bool isVisible = GeometryUtility.TestPlanesAABB(planes, bounds);
			return isVisible;

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
					Bounds bounds = CalculateBounds(partitionIndex,10);
					Gizmos.DrawWireCube(bounds.center, bounds.size);
				}
			}

			if (!Application.isPlaying || IsVisibleInCamera(MainChunkIndex))
			{
				Bounds mainBounds = CalculateBounds(MainChunkIndex,10);
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
