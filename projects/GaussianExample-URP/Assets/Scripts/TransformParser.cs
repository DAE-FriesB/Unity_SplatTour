using UnityEngine;
using Newtonsoft.Json;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class TransformParser
{
	[System.Serializable]
	private class FrameData
	{
		[JsonProperty("transform_matrix")]
		public double[][] Transform_Matrix { get; set; }
	}

	[System.Serializable]
	private class TransformJsonData
	{
		[JsonProperty("applied_transform")]
		public double[][] Applied_Transform;
		[JsonProperty("frames")]
		public FrameData[] Frames { get; set; }
	}

	[System.Serializable]
	private class DataParser_Transforms
	{
		[JsonProperty("transform")]
		public double[][] TransformMatrix { get; set; }

		[JsonProperty("scale")]
		public double Scale { get; set; }
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct CameraProperties
	{
		public int CamerId;
		public int ModelId;
		public ulong Width;
		public ulong Height;
		public string ModelName;
	}

	struct CameraInfo
	{
		public int NumParams;
		public string ModelName;
		public CameraInfo(string modelName, int numParams)
		{
			NumParams = numParams;
			ModelName = modelName;
		}
	}
	static readonly Dictionary<int, CameraInfo> CameraModels = new Dictionary<int, CameraInfo>
	{
		{0, new CameraInfo("SIMPLE_PINHOLE",3)},
		{1, new CameraInfo("PINHOLE",4)},
		{2, new CameraInfo("SIMPLE_RADIAL",4)},
		{3, new CameraInfo("RADIAL",5)},
		{4, new CameraInfo("OPENCV",8)},
		{5, new CameraInfo("OPENCV_FISHEYE",8)},
		{6, new CameraInfo("FULL_OPENCV",12)},
		{7, new CameraInfo("FOV",5)},
		{8, new CameraInfo("SIMPLE_RADIAL_FISHEYE",4)},
		{9, new CameraInfo("RADIAL_FISHEYE",5)},
		{10,new CameraInfo("THIN_PRISM_FISHEYE",12)}
	};


	public TransformData[] ParseBinary(TextAsset asset)
	{
		using (MemoryStream ms = new MemoryStream(asset.bytes))
		{
			using (var br = new BinaryReader(ms))
			{
				int numCameras = br.ReadInt32();

				TransformData[] transforms = new TransformData[numCameras];
				for (int idx = 0; idx < numCameras; ++idx)
				{
					CameraProperties properties = new CameraProperties();
					properties.CamerId = br.ReadInt32();
					properties.ModelId = br.ReadInt32();
					properties.Width = br.ReadUInt64();
					properties.Height = br.ReadUInt64();
					properties.ModelName = CameraModels[properties.ModelId].ModelName;
					int numParams = CameraModels[properties.ModelId].NumParams;
				}

				return transforms;
			}
		}
	}

	public Matrix4x4 ParseDataParser_Transforms(TextAsset asset)
	{
		var data = JsonConvert.DeserializeObject<DataParser_Transforms>(asset.text);
		Matrix4x4 matrix = ParseMatrix(data.TransformMatrix);
		matrix = Matrix4x4.Scale(Vector3.one * (float)data.Scale)*matrix;

		return matrix;
	}

	public TransformData[] ParseTransforms(TextAsset asset, Matrix4x4 parserMatrix)
	{
		var data = JsonConvert.DeserializeObject<TransformJsonData>(asset.text);

		Matrix4x4 appliedTransform = ParseMatrix(data.Applied_Transform);

		Vector4[] matrixVectors = new Vector4[4];
		TransformData[] results = new TransformData[data.Frames.Length];
		for (int idx = 0; idx < data.Frames.Length; idx++)
		{
			var frame = data.Frames[idx];
			for (int vecIdx = 0; vecIdx < matrixVectors.Length; ++vecIdx)
			{
				matrixVectors[vecIdx] = new Vector4(
					(float)frame.Transform_Matrix[vecIdx][0],
					(float)frame.Transform_Matrix[vecIdx][1],
					(float)frame.Transform_Matrix[vecIdx][2],
					(float)frame.Transform_Matrix[vecIdx][3]
					);
			}
			results[idx] = new TransformData(appliedTransform, new Matrix4x4(matrixVectors[0], matrixVectors[1], matrixVectors[2], matrixVectors[3]).transpose, parserMatrix);
		}

		return results;
	}

	static Matrix4x4 ParseMatrix(double[][] values)
	{
		Vector4[] matrixVectors = new Vector4[4];
		matrixVectors[3] = new Vector4(0, 0, 0, 1);
		for (int vecIdx = 0; vecIdx < values.Length; ++vecIdx)
		{
			matrixVectors[vecIdx] = new Vector4(
				(float)values[vecIdx][0],
				(float)values[vecIdx][1],
				(float)values[vecIdx][2],
				(float)values[vecIdx][3]
				);
		}
		return new Matrix4x4(matrixVectors[0], matrixVectors[1], matrixVectors[2], matrixVectors[3]).transpose;
	}

}
