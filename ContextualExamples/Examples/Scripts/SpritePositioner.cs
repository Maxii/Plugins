using UnityEngine;
using System.Collections;

public class SpritePositioner : MonoBehaviour
{
	public Transform targetObject;
	public float targetDistance = 1f;
	
	private UISprite sprite;
	private UIPanel panel;
	
	void Start()
	{
		sprite = GetComponent<UISprite>();
		panel = NGUITools.FindInParents<UIPanel>(gameObject);
	}
	
	void Update()
	{
		Camera cam = Camera.mainCamera;
		
		if (cam == null || targetObject == null || sprite == null)
			return;
		
		Vector3 screenPos;
		
		Collider collider = targetObject.collider;
		if (collider != null)
		{
			Vector3 pos = collider.bounds.center;
		
			screenPos = cam.WorldToScreenPoint(pos);
			
			float tanFOV = Mathf.Tan(cam.fov*0.5f*Mathf.Deg2Rad);
			if (tanFOV != 0f)
				screenPos += Offset(pos, targetDistance, cam.transform.position, tanFOV);
		}
		else
		{
			screenPos = cam.WorldToScreenPoint(targetObject.position);
		}
			
		float sw = Screen.width;
		float sh = Screen.height;
		
		if (screenPos.z > 0f && screenPos.x >= 0f && screenPos.x < sw && screenPos.y >= 0f && screenPos.y < sh)
		{
			sprite.enabled = true;
			screenPos.z = 0f;
			
			UICamera uiCam = UICamera.FindCameraForLayer(gameObject.layer);
			Vector3 worldPos = uiCam.cachedCamera.ScreenToWorldPoint(screenPos);
			
			transform.localPosition = panel.cachedTransform.InverseTransformPoint(worldPos);
			sprite.MakePixelPerfect();
		}
		else
		{
			sprite.enabled = false;
		}
	}
	
	Vector3 Offset(Vector3 pos, float radius, Vector3 camPos, float tanFOV)
	{
		Vector3 delta = pos - camPos;
		float range = delta.magnitude;
		
		float offset = Screen.height * 0.5f * radius / (range * tanFOV);
		
		return new Vector3(-offset, offset, 0f);
	}
}
