using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WMG_World_Map : MonoBehaviour {
	
	public GameObject background;
	public GameObject MapManagerObject;
	public GameObject PanParentObject;
	public Object NodePrefab;
	public Object LinkPrefab;
	public GameObject CurrentNode;
	private WMG_Graph_Manager theMap;
	private WMG_Random_Graph theMapGenerator;
	
	public float zoomSpeed = 0.3f;
	public float zoomFactor = 2;
	public int numZoomLevels = 2;
	private int zoomLevel;
	private bool isZooming;
	private WMG_Node activatingNodesStart;

	// Use this for initialization
	void Start () {
		UIEventListener.Get(background).onDrag += OnMapBackgroundDragged;
		UIEventListener.Get(background).onScroll += OnMapBackgroundScrolled;
		theMap = MapManagerObject.GetComponent<WMG_Graph_Manager>();
		theMapGenerator = MapManagerObject.GetComponent<WMG_Random_Graph>();
		theMapGenerator.nodePrefab = NodePrefab;
		theMapGenerator.linkPrefab = LinkPrefab;
		CurrentNode = theMap.CreateNode(NodePrefab, PanParentObject);
//		CurrentNode.transform.parent = PanParentObject.transform;
		WMG_Node_World_Map startNode = CurrentNode.GetComponent<WMG_Node_World_Map>();
		theMap.SetActive(startNode.SelectionObject,true);
		theMapGenerator.GenerateGraphFromNode(startNode);
		
		foreach (GameObject child in theMap.LinksParent) {
			theMap.SetActive(child,false);
		}
		foreach (GameObject child in theMap.NodesParent) {
			UIEventListener.Get(child).onClick += OnNodeClick;
			UIEventListener.Get(child).onHover += OnNodeHover;
			WMG_Node_World_Map aNode = child.GetComponent<WMG_Node_World_Map>();
			AnimateSelection(aNode);
			if (aNode.id != startNode.id) theMap.SetActive(child,false);
		}
		ActivateNeighbors(startNode);
	}
	
	void Update() {
		// Since NGUI upgrade to 3.0, boxcollider size does not seem to update so manually updating here
		UIWidget bgWid = background.GetComponent<UIWidget>();
		BoxCollider bgCol = background.GetComponent<BoxCollider>();
		bgCol.size = new Vector3(bgWid.width, bgWid.height, 0);
	}
	
	void OnNodeClick(GameObject go) {
		if (CurrentNode != go) {
			WMG_Node_World_Map cNode = CurrentNode.GetComponent<WMG_Node_World_Map>();
			WMG_Node_World_Map newNode = go.GetComponent<WMG_Node_World_Map>();
			theMap.SetActive(cNode.SelectionObject,false);
			theMap.SetActive(newNode.SelectionObject,true);
			
			ActivateNeighbors(newNode);
			AnimatePath(false, theMap.FindShortestPathBetweenNodes(cNode, newNode));
			
			CurrentNode = go;
		}
	}
	
	void OnNodeHover(GameObject go, bool hover) {
		WMG_Node_World_Map tNode = go.GetComponent<WMG_Node_World_Map>();
		if (hover) {
			if (tNode.hoverState) return; // Since click events send out hover true events
			tNode.hoverState = true;
		}
		else {
			tNode.hoverState = false;
		}
		
		if (CurrentNode != go) {
			WMG_Node_World_Map fNode = CurrentNode.GetComponent<WMG_Node_World_Map>();
			AnimatePath(hover, theMap.FindShortestPathBetweenNodes(fNode, tNode));
		}
	}
	
	void ActivateNeighbors(WMG_Node fromNode) {
		for (int i = 0; i < fromNode.numLinks; i++) {
			WMG_Link aLink = fromNode.links[i].GetComponent<WMG_Link>();
			if (!theMap.activeInHierarchy(aLink.gameObject)) {
				// Activate and animate links expanding from source node to end node
				activatingNodesStart = fromNode;
				theMap.SetActive(aLink.gameObject,true);
				UIWidget aLinkW = aLink.objectToScale.GetComponent<UIWidget>();
				int origScale = Mathf.RoundToInt(aLinkW.height);
				float p1y = fromNode.transform.localPosition.y + (fromNode.radius) * Mathf.Sin(Mathf.Deg2Rad*fromNode.linkAngles[i]);
				float p1x = fromNode.transform.localPosition.x + (fromNode.radius) * Mathf.Cos(Mathf.Deg2Rad*fromNode.linkAngles[i]);
				Vector3 origPos = aLink.transform.localPosition;
				Vector3 newPos = new Vector3(p1x, p1y, origPos.z);
				aLink.transform.localPosition = newPos;
				TweenPosition.Begin(aLink.gameObject, 1, origPos);
				aLinkW.height = 0;
				TweenHeight tSca = TweenHeight.Begin(aLinkW, 1, origScale);
				tSca.callWhenFinished = "endActivatingNeighbors";
				tSca.eventReceiver = this.gameObject;
			}
		}
	}
	
	void ActivateNeighborNodes(WMG_Node fromNode) {
		for (int i = 0; i < fromNode.numLinks; i++) {
			WMG_Link aLink = fromNode.links[i].GetComponent<WMG_Link>();
			WMG_Node aLinkTo = aLink.toNode.GetComponent<WMG_Node>();
			if (aLinkTo.id == fromNode.id) aLinkTo = aLink.fromNode.GetComponent<WMG_Node>();
			theMap.SetActive(aLinkTo.gameObject,true);
		}
	}
	
	void endActivatingNeighbors() {
		ActivateNeighborNodes(activatingNodesStart);
	}
	
	void AnimateSelection(WMG_Node_World_Map theNode) {
		float duration = 0.4f;
		TweenPosition tPos; 
		tPos = TweenPosition.Begin(theNode.SelTopRight, duration, new Vector3 (20,20,0));
		tPos.style = UITweener.Style.PingPong;
		tPos = TweenPosition.Begin(theNode.SelBotLeft, duration, new Vector3 (-20,-20,0));
		tPos.style = UITweener.Style.PingPong;
		tPos = TweenPosition.Begin(theNode.SelBotRight, duration, new Vector3 (20,-20,0));
		tPos.style = UITweener.Style.PingPong;
		tPos = TweenPosition.Begin(theNode.SelTopLeft, duration, new Vector3 (-20,20,0));
		tPos.style = UITweener.Style.PingPong;
	}
	
	void AnimatePath(bool show, List<WMG_Link> theLinks) {
		foreach (WMG_Link aLink in theLinks) {
			if (show) {
				// Do some ping pong tween scaling to visually show the results
				float originalScale = aLink.objectToScale.transform.localScale.x;
				TweenScale tsc = TweenScale.Begin(aLink.objectToScale, 0.3f, new Vector3(originalScale * 2, aLink.objectToScale.transform.localScale.y, aLink.objectToScale.transform.localScale.z));
				tsc.style = UITweener.Style.PingPong;
			}
			else {
				TweenScale existingTween = aLink.objectToScale.GetComponent<TweenScale>();
				if (existingTween != null) {
					aLink.objectToScale.transform.localScale = existingTween.from;
					Destroy(existingTween);
				}
			}
		}
	}
	
	void OnMapBackgroundDragged(GameObject go, Vector2 delta) {
		delta = new Vector2(delta.x * UICamera.currentCamera.orthographicSize, delta.y * UICamera.currentCamera.orthographicSize);
		if (UICamera.currentTouchID == -2) {
			// Right mouse drag is pan
			PanParentObject.transform.localPosition = new Vector3(	PanParentObject.transform.localPosition.x + delta.x, 
																	PanParentObject.transform.localPosition.y + delta.y, 
																	PanParentObject.transform.localPosition.z);
		}
	}
	
	void OnMapBackgroundScrolled(GameObject go, float delta) {
		UIStretch theStretch = background.GetComponent<UIStretch>();
		if (delta > 0) {
			if (zoomLevel > -numZoomLevels && !isZooming) {
				zoomLevel--;
				isZooming = true;
				float orthoSize = Mathf.Pow(zoomFactor,zoomLevel);
				TweenOrthoSize tos = TweenOrthoSize.Begin(UICamera.currentCamera.gameObject, zoomSpeed, orthoSize);
				theStretch.relativeSize.x = orthoSize;
				theStretch.relativeSize.y = orthoSize;
				tos.callWhenFinished = "endZooming";
				tos.eventReceiver = this.gameObject;
			}
		}
		else {
			if (zoomLevel < numZoomLevels && !isZooming) {
				zoomLevel++;
				isZooming = true;
				float orthoSize = Mathf.Pow(zoomFactor,zoomLevel);
				TweenOrthoSize tos = TweenOrthoSize.Begin(UICamera.currentCamera.gameObject, zoomSpeed, orthoSize);
				tos.callWhenFinished = "endZoomUpdateBackground";
				tos.eventReceiver = this.gameObject;
			}
		}
	}
	
	void endZooming() {
		isZooming = false;
	}
	
	void endZoomUpdateBackground() {
		isZooming = false;
		UIStretch theStretch = background.GetComponent<UIStretch>();
		theStretch.relativeSize.x = UICamera.currentCamera.orthographicSize;
		theStretch.relativeSize.y = UICamera.currentCamera.orthographicSize;
	}
}
