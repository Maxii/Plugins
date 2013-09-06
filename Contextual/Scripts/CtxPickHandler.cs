//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// A component which drives the picking process for the context menu objects
/// in a scene. Attach this component to the camera game object that is used
/// to render the objects for which you want context menus.
/// </summary>
[AddComponentMenu("Contextual/Context Object Pick Handler")]
public class CtxPickHandler : MonoBehaviour
{
	#region Public Variables
	
	/// <summary>
	/// The layer mask which will be used to filter ray casts for the pick handler.
	/// If this is 0 then the default ray cast filtering will be used.
	/// </summary>
	public int pickLayers = 1;
	
	/// <summary>
	/// The mouse button used to trigger the display of the context menu.
	/// Used only for Windows/Mac standalone and Web player.
	/// </summary>
	public int menuButton = 0;
	
	/// <summary>
	/// If this flag is clear then this object will make itself the fall through
	/// event receiver for the NGUI UICamera on startup. Otherwise mouse/touch
	/// events will need to come from your code.
	/// </summary>
	public bool dontUseFallThrough = false;
	
	#endregion
	
	#region Private Variables
	
	private CtxObject tracking;
	private CtxObject lastTracked = null;
	
	#endregion
	
	#region Event Handling
	
	void Start()
	{
		// We rely on NGUI to send us the events it doesn't handle. This
		// allows us to avoid having to check UI hits before processing
		// an event. However, if other code is using the fall through then
		// other arrangements will need to be made.
		if (! dontUseFallThrough)
			UICamera.fallThrough = gameObject;
	}
	
	/// <summary>
	/// Handles the OnPress event. Normally this will come through NGUI as
	/// by default this component will register itself with UICamera as the
	/// fallthrough event handler. However, if you don't want this to
	/// be the fallthrough handler then set the dontUseFallThrough flag and
	/// call this function directly from your own event handlers.
	/// </summary>
	public void OnPress(bool isPressed)
	{
		if (isPressed)
		{
		#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WEBPLAYER
			// For mouse platforms we go through the additional step of filtering
			// out the events that don't match our specified mouse button.
			if (Input.GetMouseButtonDown(menuButton))
		#endif
			{
				if (lastTracked != null)
				{
					//lastTracked.HideMenu();
					lastTracked = null;
				}
				
				tracking = Pick(Input.mousePosition);
			}
		}
		else
		{
			if (tracking)
			{
			#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WEBPLAYER
				if (Input.GetMouseButtonUp(menuButton))
			#endif
				{
					CtxObject picked = Pick(Input.mousePosition);
					if (tracking == picked)
					{
						tracking.ShowMenu();
						lastTracked = tracking;
					}
				}
					
				tracking = null;
			}
		}
	}
	
	#endregion
	
	#region Private Functions
	
	CtxObject Pick(Vector3 mousePos)
	{
		Camera cam = camera;
		if (cam == null)
			cam = Camera.mainCamera;
		
		Ray ray = cam.ScreenPointToRay(mousePos);

		RaycastHit hit = new RaycastHit();
		int layerMask = (pickLayers != 0) ? pickLayers : Physics.kDefaultRaycastLayers;
		
		if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask))
		{
			return hit.collider.gameObject.GetComponent<CtxObject>();
		}
			
		return null;
	}
	
	#endregion
}
