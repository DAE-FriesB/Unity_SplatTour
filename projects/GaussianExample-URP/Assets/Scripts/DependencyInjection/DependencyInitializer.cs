using UnityEngine;

public class DependencyInitializer : MonoBehaviour
{
	[SerializeField]
	private DependencyConfig _config;
	private void Awake()
	{
		_config.Build();
	}
}
