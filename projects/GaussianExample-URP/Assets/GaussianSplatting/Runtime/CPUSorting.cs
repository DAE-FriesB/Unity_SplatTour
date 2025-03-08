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
		public void Sort(ref uint[] inputValues, ref uint[] inputKeys)
		{

			for (int shiftAmount = 0; shiftAmount < 32; shiftAmount += _shiftSize)
			{
				CountSort(ref inputValues, ref inputKeys, shiftAmount);
			}
		}

		private static uint GetBaseValue(uint value, int shiftAmount)
		{
			value = (value >> shiftAmount) & _shiftMask;
			return value;
		}
		private void CountSort(ref uint[] inputValues, ref uint[] inputKeys, int shiftAmt)
		{
			for (int i = 0; i < _base; i++)
			{
				_countArr[i] = 0;
			}

			
			for (int i = 0; i < _numItems; i++)
			{

				uint baseValue =  GetBaseValue(inputValues[i], shiftAmt);
				_countArr[baseValue]++;
			}

			//PrefixSum
			for (int i = 1; i < _base; i++)
			{
				_countArr[i] += _countArr[i - 1];
			}

			for (int i = (int)_numItems - 1; i >= 0; i--)
			{
				uint index = GetBaseValue(inputValues[i], shiftAmt);
				_outputValues[_countArr[index] - 1] = inputValues[i];
				_outputKeys[_countArr[index] - 1] = inputKeys[i];
				_countArr[index]--;
			}

			for (int i = 0; i < _numItems; i++)
			{
				inputValues[i] = _outputValues[i];
				inputKeys[i] = _outputKeys[i];
			}
		}
	}
}
