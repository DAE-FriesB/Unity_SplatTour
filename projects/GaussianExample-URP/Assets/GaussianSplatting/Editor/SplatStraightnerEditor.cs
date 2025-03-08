using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;
using static SplatStraightner;

[CustomEditor(typeof(SplatStraightner))]
public class SplatStraightnerEditor : Editor
{

	public override void OnInspectorGUI()
	{
		

		SplatStraightner straightener = target as SplatStraightner;
		var anchors = straightener._anchors;
		if (straightener == null)
		{
			GUILayout.Label("select the GaussianSplatting gameObject to straighten");
		}
		else
		{
			GUILayout.Label("");
		}

		GUI.enabled = anchors.Any(a => a.IsSet);
		if (GUILayout.Button("Reset Anchors"))
		{
			foreach (var anchor in anchors)
			{
				anchor.IsSet = false;
			}
		}
		GUI.enabled = straightener._anchorToSet != null;
		if (GUILayout.Button("Center anchor to view"))
		{
			CenterAnchorToView(straightener);
		}

		if (GUILayout.Button("Set Anchor"))
		{
			straightener._anchorToSet.IsSet = true;

			if (straightener._anchorToSet != null && straightener._anchorToSet.Position == Vector3.zero)
			{
				CenterAnchorToView(straightener);
			}
		}

		GUI.enabled = straightener != null && anchors.All(a => a.IsSet);
		if (GUILayout.Button("Straighten"))
		{
			Undo.RecordObject(straightener.transform, "straighten");
			Plane plane1 = new Plane(anchors[0].Position, anchors[1].Position, anchors[2].Position);
			Vector3[] localPositions = anchors.Select(a => straightener.transform.InverseTransformPoint(a.Position)).ToArray();
			Vector3 normal = plane1.normal;
			if(Vector3.Dot(normal, Vector3.up) < 1)
			{
				normal = -normal;
			}
			straightener.transform.rotation = Quaternion.FromToRotation(normal, Vector3.up)*straightener.transform.rotation;

			for (int idx = 0; idx < anchors.Length; idx++)
			{
				Anchor anchor = anchors[idx];
				anchor.Position = straightener.transform.TransformPoint(localPositions[idx]);
			}

			float yOffset = 0f - anchors[0].Position.y;
			//move to 0 height
			straightener.transform.position += Vector3.up * yOffset;
			for (int idx = 0; idx < anchors.Length; idx++)
			{
				Anchor anchor = anchors[idx];
				anchor.Position = straightener.transform.TransformPoint(localPositions[idx]);
			}
		}
		
	
	}
	private void OnSceneGUI()
	{
		SplatStraightner straightener = target as SplatStraightner;
		DrawHandles(straightener);
	}

	void CenterAnchorToView(SplatStraightner straightener)
	{
		Camera cam = SceneView.GetAllSceneCameras().FirstOrDefault();
		if (cam != null)
		{
			Plane zeroPlane = new Plane(Vector3.up, Vector3.zero);
			if (zeroPlane.Raycast(new Ray(cam.transform.position, cam.transform.forward), out float dist))
			{
				straightener._anchorToSet.Position = cam.transform.position + cam.transform.forward * dist;
			}
		}
	}
	void DrawHandles(SplatStraightner straightener)
	{
		if (straightener == null) return;
		foreach (var anchor in straightener._anchors)
		{
			if (anchor.IsSet || anchor == straightener._anchorToSet)
			{
				anchor.Position = Handles.PositionHandle(anchor.Position, Quaternion.identity);
				Handles.DrawWireDisc(anchor.Position, Vector3.up, 1f);
			}
		}

	}


}
