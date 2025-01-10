using System;

namespace GaussianSplatting.Runtime
{
	public class CPUSorting
	{
		private readonly uint[] _outputValues, _outputKeys;
		private readonly uint _numItems;
		private readonly uint[] _countArr = new uint[10];

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
			int n = inputValues.Length;
			uint max = GetMax(ref inputValues, n);
		
			for (uint exp = 1; max / exp > 0; exp *= 10)
			{
				CountSort(ref inputValues, ref inputKeys, n, exp);
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

		private void CountSort(ref uint[] inputValues, ref uint[] inputKeys, int n, uint exp)
		{
			for (int i = 0; i < 10; i++)
			{
				_countArr[i] = 0;
			}

			for (int i = 0; i < n; i++)
			{
				_countArr[(inputValues[i] / exp) % 10]++;
			}

			for (int i = 1; i < 10; i++)
			{
				_countArr[i] += _countArr[i - 1];
			}

			for (int i = n - 1; i >= 0; i--)
			{
				uint index = (inputValues[i] / exp) % 10;
				_outputValues[_countArr[index] - 1] = inputValues[i];
				_outputKeys[_countArr[index] - 1] = inputKeys[i];
				_countArr[index]--;
			}

			for (int i = 0; i < n; i++)
			{
				inputValues[i] = _outputValues[i];
				inputKeys[i] = _outputKeys[i];
			}
		}
	}
}
