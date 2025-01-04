using JetBrains.Annotations;
using UnityEngine;

public class TransformData
{
	public Vector3 Position { get => WorldMatrix.GetColumn(3); }
	public Quaternion Rotation { get => QuaternionFromMatrix(WorldMatrix); }


	public Transform ParentTransform { get; set; }
	Matrix4x4 Matrix { get; }
	Matrix4x4 WorldMatrix => (ParentTransform?.localToWorldMatrix ?? Matrix4x4.identity) * Matrix;
	public TransformData(Matrix4x4 appliedTransform, Matrix4x4 transform, Matrix4x4 parserTransform)
	{


		Matrix4x4 fixYup = Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0))* Matrix4x4.Scale(new Vector3(-1, 1, 1)) ;
		Matrix = Matrix4x4.identity;
		Matrix *= fixYup;
		Matrix *= appliedTransform;
		Matrix *= parserTransform;
		Matrix *= transform;
	}

	public Ray Ray => new Ray(Position, Rotation*Vector3.forward);

	public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
	{
		// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
		Quaternion q = new Quaternion();
		q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
		q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
		q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
		q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
		q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
		q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
		q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
		return q;
	}

}
