using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static GaussianSplatting.Runtime.GaussianSplatAsset;

namespace GaussianSplatting.Runtime
{
	public class CPUDistanceCalculator
	{
		private const int _max_Dist = 100;
		private readonly Vector3[] _posData;

		public CPUDistanceCalculator(Vector3[] posData, NativeArray<ChunkInfo>? chunkInfos, int numChunks, int formatSize, int count)
		{

			_posData = posData;
			LoadSplatPositions(posData, formatSize);

			if (chunkInfos.HasValue && numChunks > 0)
			{
				TransformSplatsByChunk(chunkInfos.Value, numChunks);
			}

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



		unsafe void LoadSplatPositions(NativeArray<uint> dataArr, int formatSize, int startIdx, int endIdx)
		{


			byte* inputPtr = (byte*)(dataArr.GetUnsafePtr());
			Vector3 result = new Vector3();

			for (int idx = 0; idx < _posData.Length; ++idx, inputPtr += formatSize)
			{
				switch (formatSize)
				{
					case 4:
						DecodeNorm11ToVec(inputPtr, ref result);
						break;
					default:
						DecodeFloat32ToVec(inputPtr, ref result);
						break;
				}
				_posData[idx] = result;
			}

		}

		void TransformSplatsByChunk(NativeArray<ChunkInfo> chunkInfos, int numChunks)
		{
			const int splatsPerChunk = 256;

			int chunkInfoStride = UnsafeUtility.SizeOf<ChunkInfo>();

			int prevChunkIdx = 0;
			ChunkInfo chunk = chunkInfos[0];

			for (int idx = 0; idx < _posData.Length; ++idx)
			{
				//Get chunk info
				int chunkIdx = idx / splatsPerChunk;
				if (chunkIdx != prevChunkIdx)
				{
					chunk = chunkInfos[chunkIdx];
					prevChunkIdx = chunkIdx;
				}

				Vector3 posMin = new Vector3(chunk.posX.x, chunk.posY.x, chunk.posZ.x);
				Vector3 posMax = new Vector3(chunk.posX.y, chunk.posY.y, chunk.posZ.y);

				Vector3 pos = _posData[idx];
				_posData[idx] = new Vector3
					(
						x: Mathf.Lerp(posMin.x, posMax.x, pos.x),
						y: Mathf.Lerp(posMin.y, posMax.y, pos.y),
						z: Mathf.Lerp(posMin.z, posMax.z, pos.z)
					);

			}



		}
		private static unsafe void DecodeFloat32ToVec(byte* inputPtr, ref Vector3 result)
		{
			result.x = *(float*)(inputPtr);
			result.y = *(float*)(inputPtr + 4);
			result.z = *(float*)(inputPtr + 8);
		}

		private static unsafe void DecodeNorm11ToVec(byte* inputPtr, ref Vector3 result)
		{
			uint encoded = *(uint*)(inputPtr);
			DecodeNorm11ToVec(encoded, ref result);
		}

		private static void DecodeNorm11ToVec(uint encoded, ref Vector3 result)
		{
			result.x = (encoded & 0x7FF) / 2047.0f;           // Extract and decode the 11-bit x component
			result.y = ((encoded >> 11) & 0x3FF) / 1023.0f;   // Extract and decode the 10-bit y component
			result.z = ((encoded >> 21) & 0x7FF) / 2047.0f;   // Extract and decode the 11-bit z component
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
