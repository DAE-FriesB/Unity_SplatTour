using UnityEngine;

public class DependencyInitializer : MonoBehaviour
{
	[SerializeField]
	private DependencyConfig _config;
	private void Awake()
	{
		if(_config)
		_config.Build();
	}
}
