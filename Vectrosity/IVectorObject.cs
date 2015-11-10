using UnityEngine;

namespace Vectrosity {

interface IVectorObject {
	void SetName (string name);
	
	void UpdateVerts ();
	
	void UpdateUVs ();
	
	void UpdateColors ();
	
	void UpdateTris ();
	
	void UpdateMeshAttributes ();
	
	void CalculateNormals ();
	
	Vector3[] GetNormals ();
	
	void SetTangents (Vector4[] tangents);
	
	void ClearMesh ();
	
	void SetMaterial (Material material);
	
	void SetTexture (Texture texture);
	
	void Enable (bool enable);
	
	void SetVectorLine (VectorLine vectorLine, Texture texture, Material material);
}

}