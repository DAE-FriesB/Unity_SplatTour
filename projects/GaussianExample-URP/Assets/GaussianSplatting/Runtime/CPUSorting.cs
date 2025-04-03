using System;

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

		private static uint GetMax(ref uint[] inputValues, int n)
		{
			uint max = inputValues[0];
			for (int i = 1; i < n; i++)
			{
				if (inputValues[i] > max)
				{
					max = inputValues[i];
				}
			}
			return max;
		}

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
				uint index = GetBaseValue(inputValues[i], shiftAmt);

				int sourceIndex = startIndex + i;
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
	}
}
