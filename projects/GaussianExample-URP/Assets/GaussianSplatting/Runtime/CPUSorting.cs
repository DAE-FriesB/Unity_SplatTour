﻿using System;
using System.Linq;

namespace GaussianSplatting.Runtime
{
	public class CPUSorting
	{
		private readonly uint[] _outputValues, _outputKeys;
		private readonly uint _numItems;
		private readonly uint[] _countArr = new uint[_base];
		private const uint _base = 256;
		private const int _shiftSize = 8;
		private const uint _shiftMask = _base - 1;
		public CPUSorting(uint count)
		{

			_outputValues = new uint[count];
			_outputKeys = new uint[count];

			_numItems = count;
		}

		public void InitializeIndices(ref uint[] indices)
		{
			for (uint idx = 0; idx < _numItems; ++idx)
			{
				indices[idx] = idx;
			}
		}
		public void Sort(ref uint[] inputValues, ref uint[] inputKeys, int startIndex, int count)
		{

			for (int shiftAmount = 0; shiftAmount < 32; shiftAmount += _shiftSize)
			{
				CountSort(ref inputValues, ref inputKeys, shiftAmount, startIndex, count);
			}
		}

		//private static uint GetMax(ref uint[] inputValues, int startIndex, int amount)
		//{
		//	uint max = inputValues[0];
		//	for (int i = 1; i < n; i++)
		//	{
		//		if (inputValues[i] > max)
		//		{
		//			max = inputValues[i];
		//		}
		//	}
		//	return max;
		//}

		private static uint GetBaseValue(uint value, int shiftAmount)
		{
			value = (value >> shiftAmount) & _shiftMask;
			return value;
		}
		private void CountSort(ref uint[] inputValues, ref uint[] inputKeys, int shiftAmt, int startIndex, int amount)
		{
			for (int i = 0; i < _base; i++)
			{
				_countArr[i] = 0;
			}

			//Count amount values with base
			for (int i = 0; i < amount; i++)
			{
				int arrIndex = i + startIndex;
				uint baseValue =  GetBaseValue(inputValues[arrIndex], shiftAmt);
				_countArr[baseValue]++;
			}

			//PrefixSum
			for (int i = 1; i < _base; i++)
			{
				_countArr[i] += _countArr[i - 1];
			}

			//Move around values
			for (int i = (int)amount - 1; i >= 0; i--)
			{
				
				int sourceIndex = startIndex + i;
				uint index = GetBaseValue(inputValues[sourceIndex], shiftAmt);

				int targetIndex = startIndex + (int)_countArr[index] - 1;
				_outputValues[targetIndex] = inputValues[sourceIndex];
				_outputKeys[targetIndex] = inputKeys[sourceIndex];
				_countArr[index]--;
			}

			for (int i = 0; i < amount; i++)
			{
				int arrIndex = startIndex + i;
				inputValues[arrIndex] = _outputValues[arrIndex];
				inputKeys[arrIndex] = _outputKeys[arrIndex];
			}
		}

		internal void ReorderPartitions(SplatPartition[] partitions, ref int[] previousOrder, ref uint[] inputValues, ref uint[] inputKeys)
		{
			//int[] previousStartIndices = new int[partitions.Length];
			//for(int idx =0; idx < previousOrder.Length; ++idx)
			//{
			//	int prevPartitionIdx = previousOrder[idx];
			//	var partition = partitions.Single(p => p.PartitionIndex == prevPartitionIdx);
			//	previousStartIndices[idx] = partition.StartIndex;
			//}

			int startIndex = 0;
			for(int pIdx = 0; pIdx < partitions.Length; ++pIdx)
			{
				var partition = partitions[pIdx];
				int prevStartIndex = partition.StartIndex;
				previousOrder[pIdx] = partition.PartitionIndex;

				if (prevStartIndex == -1) continue;
				
				partition.StartIndex = startIndex;

				for(int splatIdx = 0; splatIdx < partition.SplatCount; ++splatIdx)
				{
					int fromIndex = prevStartIndex + splatIdx;
					int toIndex = startIndex + splatIdx;

					_outputKeys[toIndex] = inputKeys[fromIndex];
					_outputValues[toIndex] = inputValues[fromIndex];
				}
				startIndex += partition.SplatCount;
			}

			while(startIndex > 0)
			{
				--startIndex;
				inputValues[startIndex] = _outputValues[startIndex];
				inputKeys[startIndex] = _outputKeys[startIndex];
			}


		}


	}
}
