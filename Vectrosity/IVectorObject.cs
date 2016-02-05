using UnityEngine;

namespace Vectrosity {

interface IVectorObject {
	void SetName (string name);
	
	void UpdateVerts ();
	
	void UpdateUVs ();
	
	void UpdateColors ();
	
	void UpdateTris ();
	
	void UpdateNormals ();
	
	void UpdateTangents ();
	
	void UpdateMeshAttributes ();
		
	void ClearMesh ();
	
	void SetMaterial (Material material);
	
	void SetTexture (Texture texture);
	
	void Enable (bool enable);
	
	void SetVectorLine (VectorLine vectorLine, Texture texture, Material material);
	
	void Destroy ();

	int VertexCount ();
}

}