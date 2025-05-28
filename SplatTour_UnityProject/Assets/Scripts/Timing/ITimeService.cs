using System.Collections;

namespace Timing
{
	public interface ITimeService
	{
		public float DeltaTime { get; }
		public IEnumerator WaitForSeconds(float delay);
	}
}