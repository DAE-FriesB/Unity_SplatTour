using System;
using System.Collections.Generic;

namespace Dependencies
{
	public static class DependencyService
	{
		private class DependencySingleton<T> : DependencySingleton
		{
			private T _instance;
			private readonly Func<T> _createFunc;

			public T DependencyInstance
			{
				get
				{
					if (_instance == null)
					{
						_instance = _createFunc();
					}
					return _instance;
				}
			}
			public DependencySingleton(Func<T> createFunc)
			{
				_createFunc = createFunc;
			}
		}

		private abstract class DependencySingleton
		{

		}

		private static Dictionary<Type, DependencySingleton> _dependencyMappings = new Dictionary<Type, DependencySingleton>();
		
		public static void Clear()
		{
			_dependencyMappings.Clear();
		}
		public static void RegisterService<T>(Func<T> createFunc)
		{
			Type type = typeof(T);
			if (_dependencyMappings.ContainsKey(type)) return;
			_dependencyMappings.Add(typeof(T), new DependencySingleton<T>(createFunc));
		}
		public static T GetService<T>()
		{
			if (!_dependencyMappings.TryGetValue(typeof(T), out DependencySingleton dependency))
			{
				throw new NotImplementedException($"No services registered with type {typeof(T)}");
			}
			return (dependency as DependencySingleton<T>).DependencyInstance;
		}
	}
}