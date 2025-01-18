using UnityEngine;

public class CameraRotationInput
{
	private readonly Camera _camera;

	private Vector3 _prevMousePos;

	public CameraRotationInput(Camera camera)
	{
		_camera = camera;
	}

	Vector3 GetMousePos3D()
	{
		return new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
	}

	public virtual void Update()
	{
		if (Input.GetMouseButtonDown(1))
		{
			_prevMousePos = GetMousePos3D();
		}
		else if (Input.GetMouseButton(1))
		{
			Vector3 prevForward = _camera.ScreenToWorldPoint(_prevMousePos) - _camera.transform.position;
			Vector3 mousePos = GetMousePos3D();
			Vector3 forward = _camera.ScreenToWorldPoint(mousePos) - _camera.transform.position;
			_prevMousePos = mousePos;

			Quaternion rotationAmount = Quaternion.FromToRotation(forward, prevForward);
			Quaternion targetRotation = rotationAmount * _camera.transform.rotation;
			
			UpdateTargetRotation(targetRotation);
		}
	}

	protected void UpdateTargetRotation(Quaternion targetRotation)
	{
		targetRotation = Quaternion.LookRotation(targetRotation * Vector3.forward, Vector3.up);

		Vector3 targetEuler = targetRotation.eulerAngles;
		float eulerX = Mathf.DeltaAngle(0f, targetEuler.x);
		if (eulerX > -80f && eulerX < 80f)
		{
			_camera.transform.rotation = targetRotation;
		}

		
	}
}
