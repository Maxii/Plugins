// Version 3.1
// ©2014 Starscene Software. All rights reserved. Redistribution of source code without permission not allowed.

using UnityEngine;
using Vectrosity;

[AddComponentMenu("Vectrosity/VisibilityControlAlways")]
public class VisibilityControlAlways : MonoBehaviour {

	RefInt m_objectNumber;
	VectorLine m_vectorLine;
	bool m_destroyed = false;
	
	public RefInt objectNumber {
		get {return m_objectNumber;}
	}
	
	public void Setup (VectorLine line) {
		VectorManager.VisibilitySetup (transform, line, out m_objectNumber);
		VectorManager.DrawArrayLine2 (m_objectNumber.i);
		m_vectorLine = line;
	}

	void OnDestroy () {
		if (m_destroyed) return;	// Paranoia check
		m_destroyed = true;
		VectorManager.VisibilityRemove (m_objectNumber.i);
		VectorLine.Destroy (ref m_vectorLine);
	}
}