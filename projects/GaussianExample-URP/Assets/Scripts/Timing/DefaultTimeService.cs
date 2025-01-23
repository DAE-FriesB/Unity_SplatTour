using System.Collections;
using UnityEngine;

namespace Timing
{
	public class DefaultTimeService : ITimeService
	{
		public float DeltaTime => Time.deltaTime;

		public IEnumerator WaitForSeconds(float delay)
		{
			yield return new WaitForSeconds(delay);
		}
	}
}