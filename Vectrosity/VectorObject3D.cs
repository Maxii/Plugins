// Version 5.0
// ©2015 Starscene Software. All rights reserved. Redistribution of source code without permission not allowed.

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Vectrosity {

public class VectorObject3D : MonoBehaviour, IVectorObject {
	
	bool m_updateVerts = true;
	bool m_updateUVs = true;
	bool m_updateColors = true;
	bool m_updateTris = true;
	Mesh m_mesh;
	VectorLine m_vectorLine;
			
	public void SetVectorLine (VectorLine vectorLine, Texture tex, Material mat) {
		gameObject.AddComponent<MeshRenderer>();
		gameObject.AddComponent<MeshFilter>();
		m_vectorLine = vectorLine;
		GetComponent<MeshRenderer>().material = mat;
		GetComponent<MeshRenderer>().material.mainTexture = tex;
		SetupMesh();
	}
	
	public void Enable (bool enable) {
		if (this == null) return;	// Prevent random null ref error when stopping play in editor
		GetComponent<MeshRenderer>().enabled = enable;
	}
	
	public void SetTexture (Texture tex) {
		GetComponent<MeshRenderer>().material.mainTexture = tex;
	}

	public void SetMaterial (Material mat) {
		GetComponent<MeshRenderer>().material = mat;
		GetComponent<MeshRenderer>().material.mainTexture = m_vectorLine.texture;
	}
	
	void SetupMesh () {
		m_mesh = new Mesh();
		m_mesh.name = m_vectorLine.name;
		m_mesh.hideFlags = HideFlags.HideAndDontSave;
		GetComponent<MeshFilter>().mesh = m_mesh;
	}
	
	void LateUpdate () {
		if (m_updateVerts) {
			m_mesh.vertices = m_vectorLine.lineVertices;
			m_updateVerts = false;
			m_mesh.RecalculateBounds();
		}
		if (m_updateUVs) {
			m_mesh.uv = m_vectorLine.lineUVs;
			m_updateUVs = false;
		}
		if (m_updateColors) {
			m_mesh.colors32 = m_vectorLine.lineColors;
			m_updateColors = false;
		}
		if (m_updateTris) {
			m_mesh.SetTriangles (m_vectorLine.lineTriangles, 0);
			m_updateTris = false;
		}
	}
	
	public void SetName (string name) {
		if (m_mesh == null) return;
		m_mesh.name = name;
	}
	
	public void UpdateVerts () {
		m_updateVerts = true;
	}
	
	public void UpdateUVs () {
		m_updateUVs = true;
	}
	
	public void UpdateColors () {
		m_updateColors = true;
	}
	
	public void UpdateTris () {
		m_updateTris = true;
	}
	
	public void UpdateMeshAttributes () {
		m_mesh.Clear();
		m_updateVerts = true;
		m_updateUVs = true;
		m_updateColors = true;
		m_updateTris = true;
	}
	
	public void CalculateNormals () {
		m_mesh.RecalculateNormals();
	}
	
	public Vector3[] GetNormals () {
		return m_mesh.normals;
	}
	
	public void SetTangents (Vector4[] tangents) {
		m_mesh.tangents = tangents;
	}
	
	public void ClearMesh () {
		if (m_mesh == null) return;
		m_mesh.Clear();
	}
}

}