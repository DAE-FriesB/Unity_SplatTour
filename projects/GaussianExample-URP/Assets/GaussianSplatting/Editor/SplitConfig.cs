﻿// SPDX-License-Identifier: MIT

using GaussianSplatting.Runtime;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Http.Headers;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets;
using UnityEditor.Graphs;
using UnityEngine;
using static GaussianSplatting.Editor.GaussianSplatAssetCreator;
using UnityEditor.AddressableAssets.Settings;
using Object = UnityEngine.Object;


namespace GaussianSplatting.Editor
{
	class SplitConfig
	{
		public SplatSplitter _splitter { get; private set; }
		public bool SplitterInScene => _splitter != null;
		public bool CanCreateSplitter => _splitter == null && SplitterOwner != null;
		public GameObject SplitterOwner { get; set; }

		public int NumAssetsToCreate => _splitter == null? 0:  _splitter.NumColumns * _splitter.NumRows + 1;
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
			EditorGUI.indentLevel--;
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
			//float scaleFactor = 1f;
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


		public void CreateAddressables(string baseName, List<string> assetPaths)
		{
			//TODO: Create addressables for this asset

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings)
			{
				var group = settings.FindGroup(baseName);
				if (group != null)
				{
					settings.RemoveGroup(group);
				}

				//var template = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupTemplate>("Assets/AddressableAssetsData/AssetGroupTemplates/Packed Assets");
				var template = settings.GroupTemplateObjects.OfType<AddressableAssetGroupTemplate>().FirstOrDefault();
				group = settings.CreateGroup(baseName, false, false, true, template.SchemaObjects, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
				
				var entriesAdded = assetPaths.Select(p => AddToAddressableGroup(p, settings, group)).ToList();


				group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
				settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
				AssetDatabase.SaveAssetIfDirty(group);
				AssetDatabase.SaveAssetIfDirty(settings);
			}

		}

		private static AddressableAssetEntry AddToAddressableGroup(string assetPath, AddressableAssetSettings settings, AddressableAssetGroup group)
		{
			GUID assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath);

			var e = settings.CreateOrMoveEntry(assetGuid.ToString(), group, false, false);
			return e;
		}

	}
}
