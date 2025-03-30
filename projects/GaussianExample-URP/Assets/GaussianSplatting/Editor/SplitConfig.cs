// SPDX-License-Identifier: MIT

using GaussianSplatting.Runtime;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Http.Headers;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static GaussianSplatting.Editor.GaussianSplatAssetCreator;


namespace GaussianSplatting.Editor
{
	class SplitConfig
	{
		public SplatSplitter _splitter { get; private set; }
		public bool SplitterInScene => _splitter != null;
		public bool CanCreateSplitter => _splitter == null && SplitterOwner != null;
		public GameObject SplitterOwner { get; set; }

		public void AutoDetectSplitterInScene()
		{
			_splitter = GameObject.FindAnyObjectByType<SplatSplitter>();
			if (_splitter != null)
				SplitterOwner = _splitter.gameObject;
			else
				SplitterOwner = GameObject.FindAnyObjectByType<SplatLoader>().gameObject;

		}

		public void CreateSplitter()
		{
			if (SplitterOwner == null) return;
			if (_splitter != null) return;

			_splitter = SplitterOwner.AddComponent<SplatSplitter>();
		}

		public void DrawEditorGUI(bool enabled)
		{
			EditorGUI.indentLevel++;
			//GS Root object
			GUILayout.BeginHorizontal();
			SplitterOwner = EditorGUILayout.ObjectField("GS root object", SplitterOwner, typeof(GameObject), true) as GameObject;
			GUI.enabled = enabled && CanCreateSplitter;
			if (GUILayout.Button("Add Splitter"))
			{
				CreateSplitter();
			}
			GUI.enabled = enabled;
			GUILayout.EndHorizontal();
			if (_splitter == null)
			{
				EditorGUI.indentLevel--;
				return;
			}

			//splits settings
			SerializedObject obj = new SerializedObject(_splitter);
			EditorGUILayout.PropertyField(obj.FindProperty(nameof(SplatSplitter.PartitionSize)));
			EditorGUILayout.PropertyField(obj.FindProperty(nameof(SplatSplitter.NumColumns)));
			EditorGUILayout.PropertyField(obj.FindProperty(nameof(SplatSplitter.NumRows)));
			var chunkIdxProp = obj.FindProperty(nameof(_splitter.MainChunkIndex));
			int numChunkIndexes = _splitter.NumColumns * _splitter.NumRows;
			chunkIdxProp.intValue = EditorGUILayout.IntSlider("Main Index", _splitter.MainChunkIndex, 0, numChunkIndexes - 1);
			obj.ApplyModifiedProperties();

		}

		public Dictionary<int, NativeArray<InputSplatData>> CalculatePartitions(NativeArray<InputSplatData> inputData)
		{
			if (_splitter == null)
			{
				return new Dictionary<int, NativeArray<InputSplatData>> { { -1, inputData } };
			}
			Dictionary<int, NativeArray<InputSplatData>> partitions = new();
			Dictionary<int, List<InputSplatData>> tempPartitionLists = new()
			{
				{ -1, new List<InputSplatData>() }
			};
			int numPartitions = _splitter.NumRows * _splitter.NumColumns;
			for (int idx = 0; idx < numPartitions; ++idx)
			{
				tempPartitionLists.Add(idx, new List<InputSplatData>());
			}
			float scaleFactor = 1f;
			for (int idx = 0; idx < inputData.Length; ++idx)
			{
				InputSplatData data = inputData[idx];
				Vector3 worldPos = _splitter.transform.TransformPoint(data.pos);
				int partitionIdx = _splitter.GetPartitionIndex(worldPos);
				if (partitionIdx >= 0)
				{
					Bounds b = _splitter.GetBounds(partitionIdx);
					data.pos = data.pos - _splitter.transform.InverseTransformPoint(b.center);
					//data.rot = data.rot;
					//Vector3 scale = data.scale;
					//scale.x *= scaleFactor;
					//scale.y *= scaleFactor;
					//data.scale = scale;
				}


				tempPartitionLists[partitionIdx].Add(data);

			}

			for (int idx = -1; idx < numPartitions; ++idx)
			{
				partitions.Add(idx, new NativeArray<InputSplatData>(tempPartitionLists[idx].ToArray(), Allocator.Persistent));
				tempPartitionLists[idx].Clear();
				GC.Collect();
			}
			return partitions;
		}


		public void CreateAddressables(string baseName, GaussianSplatAsset asset)
		{
			//TODO: Create addressables for this asset
			
		}

	}
}
