using System.Collections.Generic;

namespace Analysis
{
	public class FPSCalculator
	{
		private Queue<float> _frameTimes;
		private float _recordedDuration = 0f;
		public float AverageFPS { get; private set; }
		public FPSCalculator(int maxFPS = 100)
		{
			_frameTimes = new Queue<float>(maxFPS);
		}

		public void RegisterFrame(float deltaTime)
		{
			_frameTimes.Enqueue(deltaTime);
			_recordedDuration += deltaTime;

			while (_frameTimes.Count > 1 && _recordedDuration > 1.0f)
			{
				_recordedDuration -= _frameTimes.Dequeue();
			}

			AverageFPS = _frameTimes.Count / _recordedDuration;

		}
	}
}
