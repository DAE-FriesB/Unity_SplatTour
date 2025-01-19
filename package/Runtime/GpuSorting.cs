using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using System.Linq;
using static UnityEngine.Rendering.DebugUI;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;

namespace GaussianSplatting.Runtime
{



	// GPU (uint key, uint payload) 8 bit-LSD radix sort, using reduce-then-scan
	// Copyright Thomas Smith 2024, MIT license
	// https://github.com/b0nes164/GPUSorting

	public class GpuSorting
	{
		//The size of a threadblock partition in the sort
		const uint DEVICE_RADIX_SORT_PARTITION_SIZE = 3840;

		//The size of our radix in bits
		const uint DEVICE_RADIX_SORT_BITS = 8;

		//Number of digits in our radix, 1 << DEVICE_RADIX_SORT_BITS
		const uint DEVICE_RADIX_SORT_RADIX = 256;

		//Number of sorting passes required to sort a 32bit key, KEY_BITS / DEVICE_RADIX_SORT_BITS
		const uint DEVICE_RADIX_SORT_PASSES = 4;

		//Keywords to enable for the shader
		private LocalKeyword m_vulkanKeyword;

		public struct Args
		{
			public uint count;
			public GraphicsBuffer inputKeys;
			public GraphicsBuffer inputValues;
			public SupportResources resources;
			internal int workGroupCount;
		}

		public struct SupportResources
		{
			public GraphicsBuffer altBuffer;
			public GraphicsBuffer altPayloadBuffer;
			public GraphicsBuffer countsBuffer;
			//public GraphicsBuffer passHistBuffer;
			//public GraphicsBuffer globalHistBuffer;

			public static SupportResources Load(uint count)
			{
				//This is threadBlocks * DEVICE_RADIX_SORT_RADIX
				//uint scratchBufferSize = DivRoundUp(count, DEVICE_RADIX_SORT_PARTITION_SIZE) * DEVICE_RADIX_SORT_RADIX;
				//uint reducedScratchBufferSize = DEVICE_RADIX_SORT_RADIX * DEVICE_RADIX_SORT_PASSES;

				var target = GraphicsBuffer.Target.Structured;
				var resources = new SupportResources
				{
					altBuffer = new GraphicsBuffer(target, (int)count, 4) { name = "DeviceRadixAlt" },
					altPayloadBuffer = new GraphicsBuffer(target, (int)count, 4) { name = "DeviceRadixAltPayload" },
					countsBuffer = new GraphicsBuffer(target, (int)DEVICE_RADIX_SORT_RADIX, 4) { name = "DeviceRadixCounts" },
					//passHistBuffer = new GraphicsBuffer(target, (int)scratchBufferSize, 4) { name = "DeviceRadixPassHistogram" },
					//globalHistBuffer = new GraphicsBuffer(target, (int)reducedScratchBufferSize, 4) { name = "DeviceRadixGlobalHistogram" },
				};
				return resources;
			}

			public void Dispose()
			{
				altBuffer?.Dispose();
				altPayloadBuffer?.Dispose();
				countsBuffer?.Dispose();
				//globalHistBuffer?.Dispose();

				altBuffer = null;
				altPayloadBuffer = null;
				countsBuffer = null;
				//passHistBuffer = null;
				//globalHistBuffer = null;
			}
		}

		readonly ComputeShader m_CS;
		//readonly int m_kernelInitDeviceRadixSort = -1;
		readonly int m_kernelInitCounts = -1;
		readonly int m_kernelRadixCount = -1;
		readonly int m_kernelPrefixSim = -1;
		readonly int m_kernelRadixSort = -1;
		//readonly int m_kernelScan = -1;
		//readonly int m_kernelDownsweep = -1;

		readonly bool m_Valid;

		public bool Valid => m_Valid;

		public GpuSorting(ComputeShader cs)
		{
			m_CS = cs;
			if (cs)
			{
				//m_kernelInitDeviceRadixSort = cs.FindKernel("InitDeviceRadixSort");
				m_kernelInitCounts = cs.FindKernel("CSInitCounts");
				m_kernelRadixCount = cs.FindKernel("CSRadixCount");
				m_kernelPrefixSim = cs.FindKernel("CSPrefixSum");
				m_kernelRadixSort = cs.FindKernel("CSRadixSort");
				//m_kernelScan = cs.FindKernel("Scan");
				//m_kernelDownsweep = cs.FindKernel("Downsweep");
			}

			m_Valid = m_kernelInitCounts >= 0 && cs.IsSupported(m_kernelInitCounts)
				&& m_kernelRadixCount >= 0 && cs.IsSupported(m_kernelRadixCount)
				&& m_kernelPrefixSim >= 0 && cs.IsSupported(m_kernelPrefixSim)
				&& m_kernelRadixSort >= 0 && cs.IsSupported(m_kernelRadixSort);


			m_vulkanKeyword = new LocalKeyword(cs, "VULKAN");

			if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
				cs.EnableKeyword(m_vulkanKeyword);
			else
				cs.DisableKeyword(m_vulkanKeyword);
		}

		static uint DivRoundUp(uint x, uint y) => (x + y - 1) / y;

		//Can we remove the last 4 padding without breaking?
		struct SortConstants
		{
			public uint numKeys;                        // The number of keys to sort
			public uint radixShift;                     // The radix shift value for the current pass
			public uint threadBlocks;                   // threadBlocks
			public uint padding0;                       // Padding - unused
		}
		uint sumArray(uint[] arr)
		{
			uint sum = 0;
			foreach (var val in arr)
			{
				sum += val;
			}
			return sum;
		}
		public void Dispatch(CommandBuffer cmd, Args args)
		{
			Assert.IsTrue(Valid);

			GraphicsBuffer srcKeyBuffer = args.inputKeys;
			GraphicsBuffer srcPayloadBuffer = args.inputValues;
			GraphicsBuffer dstKeyBuffer = args.resources.altBuffer;
			GraphicsBuffer dstPayloadBuffer = args.resources.altPayloadBuffer;
			GraphicsBuffer countsBuffer = args.resources.countsBuffer;

			SortConstants constants = default;
			constants.numKeys = args.count;
			constants.threadBlocks = DivRoundUp(args.count, 1024);

			// Setup overall constants

			m_CS.SetInt("e_numKeys", (int)constants.numKeys);
			m_CS.SetInt("e_threadBlocks", (int)constants.threadBlocks);

			uint[] valuesArr = new uint[constants.numKeys];
			uint[] dbgArr = new uint[DEVICE_RADIX_SORT_RADIX];
			bool isPlaying = Application.isPlaying;


			// Execute the sort algorithm in 8-bit increments
			for (constants.radixShift = 0; constants.radixShift < 32; constants.radixShift += DEVICE_RADIX_SORT_BITS)
			{

				m_CS.SetInt( "e_radixShift", (int)constants.radixShift);
				m_CS.SetBuffer(m_kernelInitCounts, "b_counts", countsBuffer);
				m_CS.Dispatch(m_kernelInitCounts, 1, 1, 1);


				//RadixCount
				m_CS.SetBuffer(m_kernelRadixCount, "b_sortValues", srcPayloadBuffer);
				m_CS.SetBuffer(m_kernelRadixCount, "b_counts", countsBuffer);
				m_CS.Dispatch(m_kernelRadixCount, (int)constants.threadBlocks, 1, 1);


				//PrefixCount
				m_CS.SetBuffer(m_kernelPrefixSim, "b_counts", countsBuffer);
				m_CS.Dispatch(m_kernelPrefixSim, 1, 1, 1);


				//RadixSort
				m_CS.SetBuffer(m_kernelRadixSort, "b_counts", countsBuffer);
				m_CS.SetBuffer(m_kernelRadixSort, "b_sort", srcKeyBuffer);
				m_CS.SetBuffer(m_kernelRadixSort, "b_sortValues", srcPayloadBuffer);
				m_CS.SetBuffer(m_kernelRadixSort, "b_alt", dstKeyBuffer);
				m_CS.SetBuffer(m_kernelRadixSort, "b_altValues", dstPayloadBuffer);
				m_CS.Dispatch(m_kernelRadixSort, (int)constants.threadBlocks, 1, 1);



				// Swap
				(srcKeyBuffer, dstKeyBuffer) = (dstKeyBuffer, srcKeyBuffer);
				(srcPayloadBuffer, dstPayloadBuffer) = (dstPayloadBuffer, srcPayloadBuffer);
			}


		}
	}
}
