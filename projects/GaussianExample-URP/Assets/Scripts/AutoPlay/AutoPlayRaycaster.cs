using UnityEngine;
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
