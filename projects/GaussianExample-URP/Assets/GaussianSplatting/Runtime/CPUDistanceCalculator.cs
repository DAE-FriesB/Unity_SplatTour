using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GaussianSplatting.Runtime
{
	public class CPUDistanceCalculator
	{
		private const int _max_Dist = 100;
		private readonly Vector3[]  _posData;
		
		public CPUDistanceCalculator(NativeArray<uint> posData, int formatSize, int count)
		{

			_posData = new Vector3[count];
			LoadSplatPositions(posData,formatSize);
			
			
		}
		internal void CalcDistances(Matrix4x4 viewProjMatrix, ref uint[] splatSortIndices, ref uint[] splatDistancesArr)
		{
			Vector3 pos = Vector3.zero;
			float maxDist = 0;
			float minDist = 0;
			for (uint idx = 0; idx < splatDistancesArr.Length; ++idx)
			{
				uint origIdx = splatSortIndices[idx];
				pos = _posData[origIdx];
				pos = viewProjMatrix.MultiplyPoint(pos);
				maxDist = Mathf.Max(maxDist, pos.z);
				minDist = Mathf.Min(minDist, pos.z);
				uint dist = FloatToSortableUint(pos.z);
				
				splatDistancesArr[idx] = dist;
			}
		}
		private static float SqrDistance(ref Vector3 a, ref Vector3 b)
		{
			float x = a.x - b.x;
			float y = a.y - b.y;
			float z = a.z - b.z;

			return x * x + y * y + z * z;
		}

		unsafe void LoadSplatPositions(NativeArray<uint> dataArr, int formatSize)
		{
			byte* inputPtr = (byte*)(dataArr.GetUnsafePtr());
			Vector3 result = new Vector3();
			for (int idx = 0; idx< _posData.Length; ++idx, inputPtr += formatSize)
			{
				result.x = *(float*)(inputPtr);
				result.y = *(float*)(inputPtr + 4);
				result.z = *(float*)(inputPtr + 8);

				_posData[idx] = result;
			}
			
		}

		uint FloatToSortableUint(float f)
		{
			uint fu;
			unsafe
			{
				fu = *((uint*)(&f));
			}

			uint mask = (uint)(-((int)(fu >> 31)) | 0x80000000);
			return fu ^ mask;
		}


	}
}
