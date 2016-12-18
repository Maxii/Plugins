using UnityEngine;
using System.Collections;
using UnityEditor;
using Pathfinding.RVO;

[CustomEditor(typeof(Pathfinding.RVO.RVOSimulator))]
public class RVOSimulatorEditor : UnityEditor.Editor {
	public override void OnInspectorGUI () {
		DrawDefaultInspector();
	}
}
