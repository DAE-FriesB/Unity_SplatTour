/******************************************************************************
 * DeviceRadixSort
 * Device Level 8-bit LSD Radix Sort using reduce then scan
 *
 * SPDX-License-Identifier: MIT
 * Copyright Thomas Smith 5/17/2024
 * https://github.com/b0nes164/GPUSorting
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 ******************************************************************************/

#define US_DIM          128U        // The number of threads in an Upsweep threadblock
#define SCAN_DIM        128U        // The number of threads in a Scan threadblock
#define RADIX           256U        // Number of digit bins
#define RADIX_MASK      255U        // Mask of digit bins

cbuffer cbGpuSorting : register(b0)
{
	uint e_numKeys;
	uint e_radixShift;
	uint e_threadBlocks;
	uint padding;
};

RWStructuredBuffer<uint> b_sort;
RWStructuredBuffer<uint> b_alt;
RWStructuredBuffer<uint> b_sortValues;
RWStructuredBuffer<uint> b_altValues;
RWStructuredBuffer<uint> b_counts;

//gets called with threadgroups (1,1,1)
[numthreads(RADIX, 1, 1)]
void CSInitCounts(uint3 dtId : SV_DispatchThreadID)
{
	uint idx = dtId.x;
	if (idx >= RADIX)
		return;

	b_counts[idx] = 0;
}

//gets called with threadGroups ((e_numKeys+1023) / 1024 , 1,1)
[numthreads(1024, 1, 1)]
void CSRadixCount(uint3 dtId : SV_DispatchThreadID)
{

	uint uniqueIndex = dtId.x;
	if (uniqueIndex >= e_numKeys)return;

	uint digit = (b_sortValues[uniqueIndex] >> e_radixShift) & RADIX_MASK;
	//b_sortValues[uniqueIndex] = 0;
	InterlockedAdd(b_counts[digit], 1);
}

//gets called with threadgroups (1,1,1)
[numthreads(1, 1, 1)]
void CSPrefixSum(uint3 dtId : SV_DispatchThreadID)
{
	[unroll(RADIX)]
		for (uint i = 1; i < RADIX; i++)
		{
			b_counts[i] += b_counts[i - 1];
		}
}

//gets called with threadGroups ((e_numKeys+1023) / 1024 , 1,1)
[numthreads(1024, 1, 1)]
void CSRadixSort(uint3 dtId : SV_DispatchThreadID, uint3 gtId: SV_GroupThreadID, uint3 gId: SV_GroupID)
{
	uint uniqueIndex = dtId.x;
	if (uniqueIndex >= e_numKeys)return;

	uniqueIndex = e_numKeys - 1 - uniqueIndex;

	uint digit = (b_sortValues[uniqueIndex] >> e_radixShift) & RADIX_MASK;
	uint pos;
	// Ensure all threads have completed the InterlockedAdd operation
	GroupMemoryBarrierWithGroupSync();
	InterlockedAdd(b_counts[digit], -1, pos);
	// Ensure all threads have completed the InterlockedAdd operation
    GroupMemoryBarrierWithGroupSync();

	// Write the sorted values to the output buffers
	b_altValues[pos - 1] = b_sortValues[uniqueIndex];
	b_alt[pos - 1] = b_sort[uniqueIndex];

}


