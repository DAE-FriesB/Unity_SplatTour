using System.Collections;
using UnityEngine;

namespace Timing
{
	public class DefaultDeltaTimeProvider : ITimeService
	{
		public float DeltaTime => Time.deltaTime;

		public IEnumerator WaitForSeconds(float delay)
		{
			yield return new WaitForSeconds(delay);
		}
	}
}