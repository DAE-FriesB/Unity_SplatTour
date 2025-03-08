using System.Linq;
using UnityEngine;

public class SplatStraightner: MonoBehaviour
{
	[System.Serializable]
	public class Anchor
	{
		public bool IsSet;
		public Vector3 Position;
	}


	public Anchor[] _anchors = new Anchor[3] { new Anchor(), new Anchor(), new Anchor() };
	public Anchor _anchorToSet => _anchors.FirstOrDefault(a => a.IsSet == false);

}