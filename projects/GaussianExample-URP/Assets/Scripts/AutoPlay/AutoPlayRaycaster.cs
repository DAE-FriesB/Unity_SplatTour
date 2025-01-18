using UnityEngine;

public class AutoPlayRotationInput : CameraRotationInput
{
	private readonly Camera _camera;
	public Vector3 LookatTarget = Vector3.zero;
	public float MaxRotSpeed { get; set; }
	public AutoPlayRotationInput(Camera camera) : base(camera)
	{
		_camera = camera;
	}
	public override void Update()
	{
		//base.Update();
		Quaternion lookRotation = Quaternion.LookRotation(LookatTarget - _camera.transform.position, Vector3.up);
		Quaternion targetRotation = Quaternion.RotateTowards(_camera.transform.rotation, lookRotation, Time.deltaTime * MaxRotSpeed);
		base.UpdateTargetRotation(targetRotation);
	}

}
public class AutoPlayRaycaster : VirtualTourRaycaster
{
	public Vector3 MouseTarget { get; set; }
	public AutoPlayRaycaster(Camera camera, IndicatorModel indicator, LayerMask wallsAndFloorMask) : base(camera, indicator, wallsAndFloorMask)
	{
		MouseTarget = indicator.Position;
	}

	protected override Ray CalculateMouseRay()
	{
		return new Ray(_camera.transform.position, MouseTarget - _camera.transform.position);
	}
}
