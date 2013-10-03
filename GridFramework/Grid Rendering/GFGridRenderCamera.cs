using UnityEngine;
using System.Collections;

public class GFGridRenderCamera : MonoBehaviour {
	private Camera cam;
	private Transform camTransform;
	public bool renderAlways = false;
	
	void Start(){
		cam = GetComponent<Camera>();
		camTransform = transform;
	}
	
	void OnPostRender(){
		if(cam != Camera.main && !renderAlways)
			return;
		foreach(GFGrid grid in GFGridRenderManager.GridList){
//			Debug.Log(grid.name);
			if(grid.useCustomRenderRange){
				grid.RenderGrid(grid.renderFrom, grid.renderTo, grid.renderLineWidth, cam, camTransform);
			} else{
				grid.RenderGrid(grid.renderLineWidth, cam, camTransform);
			}
		}
	}
}
