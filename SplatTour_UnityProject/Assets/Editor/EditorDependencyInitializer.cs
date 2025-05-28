using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[InitializeOnLoad]
public static class EditorDependencyInitializer
{

	static EditorDependencyInitializer()
	{
		EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
	}

	private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
	{
		if (obj == PlayModeStateChange.ExitingEditMode)
		{
			LoadDependencies();
		}
	}

	static void LoadDependencies()
	{
		var config = AssetDatabase.LoadAssetAtPath<DependencyConfig>("Assets\\Resources\\DependencyConfig.asset");
		Assert.IsNotNull(config, "DependencyConfig not found!");
		config.Build();
	}
}
