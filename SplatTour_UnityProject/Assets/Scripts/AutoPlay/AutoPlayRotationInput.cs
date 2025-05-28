using Dependencies;
using Timing;
using UnityEngine;

public class AutoPlayRotationInput : CameraRotationInput
{
	private readonly Camera _camera;
	public Vector3 LookatTarget = Vector3.zero;
	public float MaxRotSpeed { get; set; }
	private ITimeService _timeService;
	public AutoPlayRotationInput(Camera camera) : base(camera)
	{
		_timeService = DependencyService.GetService<ITimeService>();
		_camera = camera;
	}
	public override void Update()
	{
		//base.Update();
		Quaternion lookRotation = Quaternion.LookRotation(LookatTarget - _camera.transform.position, Vector3.up);
		Quaternion targetRotation = Quaternion.RotateTowards(_camera.transform.rotation, lookRotation, _timeService.DeltaTime* MaxRotSpeed);
		base.UpdateTargetRotation(targetRotation);
	}

}
