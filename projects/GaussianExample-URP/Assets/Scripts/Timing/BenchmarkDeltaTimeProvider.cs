using System.Collections;

namespace Timing
{
	public class BenchmarkDeltaTimeProvider : ITimeService
	{
		private const int _defaultFramerate = 20;
		private const float _defaultDeltaTime = 1f / _defaultFramerate;
		public float DeltaTime => _defaultDeltaTime;

		public IEnumerator WaitForSeconds(float delay)
		{
			while(delay > 0f)
			{
				delay -= DeltaTime;
				yield return null;
			}
		}
	}
}