using GaussianSplatting.Runtime;
using Unity.Profiling.LowLevel;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Burst.Intrinsics.X86.Avx;

public class TestGPUSort : MonoBehaviour
{
    public ComputeShader m_cs;
	GraphicsBuffer m_GpuSortKeys;
    GraphicsBuffer m_GpuSortDistances;
    GpuSorting m_Sorter;
    GpuSorting.Args m_SorterArgs;
    GpuSorting.SupportResources _supportResources;

	static readonly ProfilerMarker s_ProfSort = new(ProfilerCategory.Render, "GaussianSplat.Sort", MarkerFlags.SampleGPU);

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		var cb = new CommandBuffer { name = "RenderGaussianSplats" };
		cb.Clear();
		
		m_Sorter = new GpuSorting(m_cs);

        const int count = 10;
        uint[] values = new uint[count] { 256 + 1, 256 + 2, 256 + 3, 256 + 4, 256 + 5, 256 + 6, 1,2,3,4 };
        uint[] indices = new uint[count];
        for(uint idx = 0; idx < count; idx++)
        {
            indices[idx] = idx;
        }
		
        m_GpuSortKeys = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, 4) { name = "GaussianSplatSortIndices" };
		m_GpuSortDistances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, 4) { name = "GaussianSplatSortDistances" };

        m_GpuSortKeys.SetData(indices);
        m_GpuSortDistances.SetData(values);

		m_SorterArgs.inputKeys = m_GpuSortKeys;
		m_SorterArgs.inputValues = m_GpuSortDistances;
		m_SorterArgs.count = (uint)count;
		if (m_Sorter.Valid)
			m_SorterArgs.resources = GpuSorting.SupportResources.Load((uint)count);



		m_Sorter.Dispatch(cb, m_SorterArgs);


        m_GpuSortDistances.GetData(values);
        m_GpuSortKeys.GetData(indices);

	}

	// Update is called once per frame
	void Update()
    {
        
    }
}
