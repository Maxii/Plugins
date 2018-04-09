using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class WMG_Editor : MonoBehaviour {
	
	public Object LoadObject;
	
	public int selectedPrefabNode;
	public List<Object> prefabNodes = new List<Object>();
	
	public int selectedPrefabLink;
	public List<Object> prefabLinks = new List<Object>();
	
	public bool applyColor;
	public Color newNodeColor;
	public Color newLinkColor;
	
	public Object prefabLinkSel;
	public Object prefabNodeSel;
	
	private GameObject background;
	private GameObject dragSelect;
	private GameObject deleteButton;
	private GameObject saveButton;
	private GameObject swapPrefabsButton;
	private GameObject simpleGraphGenButton;
	private GameObject controlsParent;
	private GameObject dragSelectNodes;
	private GameObject dragSelectLinks;
	private GameObject selectInspectorShow;
	private GameObject selectInspectorList;
	private GameObject prefabNodesList;
	private GameObject prefabLinksList;
	private GameObject sliderLinkDegreeStep;
	private GameObject sliderLinkDegreeStepNumLabel;
	private GameObject sliderLinkLengthStep;
	private GameObject sliderLinkLengthStepNumLabel;
	private GameObject graphConfirmPopup;
	private GameObject graphConfirmYesButton;
	private GameObject graphConfirmNoButton;
	private GameObject graphListTypes;
	private GameObject graphAddToNodeSelected;
	private GameObject graphPreview;
	private GameObject graphSetParameters;
	private GameObject selectionObjectsToggle;
	
	public List<GameObject> EditorGUIObjects = new List<GameObject>();
	
	public int linkDegreeStep;
	public float linkLengthStep;
	
	public float zoomSpeed = 0.3f;
	public float zoomFactor = 2;
	public int numZoomLevels = 2;
	
	private GameObject MapManagerObject;
	private WMG_Graph_Manager theMap;
	private int dragStateH;
	private int dragStateV;
	private int zoomLevel;
	private bool isZooming;
	private GameObject dummyLink;
	private GameObject dummyNode;
	private float dummyNodeOffsetX;
	private float dummyNodeOffsetY;
	private bool cantCreateNode;
	private int inspectorSelectType;
	private List<GameObject> graphGenObjects;

	// Use this for initialization
	void Awake () {
		if (LoadObject != null) {
			string savedMapName = LoadObject.name;
			GameObject mapObj = Instantiate(LoadObject) as GameObject;
			mapObj.name = savedMapName; // Removes the (Clone) so when saved prompts for overwrite instead of create new
			mapObj.transform.parent = this.transform.parent;
			mapObj.transform.localScale = Vector3.one;
			mapObj.transform.localPosition = Vector3.zero;
			mapObj.transform.localEulerAngles = Vector3.zero;
			MapManagerObject = mapObj;
			theMap = mapObj.GetComponent<WMG_Graph_Manager>();
		}
		
		background = EditorGUIObjects[0];
		dragSelect = EditorGUIObjects[1];
		graphConfirmPopup = EditorGUIObjects[2];
		graphConfirmYesButton = EditorGUIObjects[3];
		graphConfirmNoButton = EditorGUIObjects[4];
		saveButton = EditorGUIObjects[5];
		swapPrefabsButton = EditorGUIObjects[6];
		simpleGraphGenButton = EditorGUIObjects[7];
		deleteButton = EditorGUIObjects[8];
		controlsParent = EditorGUIObjects[9];
		dragSelectNodes = EditorGUIObjects[10];
		dragSelectLinks = EditorGUIObjects[11];
		selectInspectorShow = EditorGUIObjects[12];
		selectInspectorList = EditorGUIObjects[13];
		selectionObjectsToggle = EditorGUIObjects[14];
		prefabNodesList = EditorGUIObjects[15];
		prefabLinksList = EditorGUIObjects[16];
		sliderLinkDegreeStep = EditorGUIObjects[17];
		sliderLinkDegreeStepNumLabel = EditorGUIObjects[18];
		sliderLinkLengthStep = EditorGUIObjects[19];
		sliderLinkLengthStepNumLabel = EditorGUIObjects[20];
		graphListTypes = EditorGUIObjects[21];
		graphAddToNodeSelected = EditorGUIObjects[22];
		graphPreview = EditorGUIObjects[23];
		graphSetParameters = EditorGUIObjects[24];
		
	}
	
	void Start () {
		if (LoadObject == null) {
			if (MapManagerObject == null) {
				return;
			}
			else {
				theMap = MapManagerObject.transform.GetComponent<WMG_Graph_Manager>();
			}
		}
		
		// Populate Possible Node and Link Prefabs in Popup lists
		UIPopupList preNodesList = prefabNodesList.GetComponent<UIPopupList>();
		preNodesList.items.Clear();
		for (int i = 0; i < prefabNodes.Count; i++) {
			int stringLength = prefabNodes[i].name.Length;
			string result;
			if (stringLength <= 10) result = i+". "+prefabNodes[i].name.Substring(0,stringLength);
			else result = i+". "+prefabNodes[i].name.Substring(0,10)+"..";
			preNodesList.items.Add(result);
			if (i == 0) preNodesList.value = result;
		}
		
		UIPopupList preLinksList = prefabLinksList.GetComponent<UIPopupList>();
		preLinksList.items.Clear();
		for (int i = 0; i < prefabLinks.Count; i++) {
			int stringLength = prefabLinks[i].name.Length;
			string result;
			if (stringLength <= 10) result = i+". "+prefabLinks[i].name.Substring(0,stringLength);
			else result = i+". "+prefabLinks[i].name.Substring(0,10)+"..";
			preLinksList.items.Add(result);
			if (i == 0) preLinksList.value = result;
		}
		
		// Background Events
		UIEventListener.Get(background).onClick += OnEditorBackgroundClicked;
		UIEventListener.Get(background).onDrag += OnEditorBackgroundDragged;
		UIEventListener.Get(background).onPress += OnEditorBackgroundPressed;
		UIEventListener.Get(background).onScroll += OnEditorBackgroundScrolled;
		UIEventListener.Get(background).onDrop += OnEditorBackgroundDropped;
		// Control events
		UIEventListener.Get(deleteButton).onClick += onDeleteButtonClicked;
		UIEventListener.Get(saveButton).onClick += onSaveButtonClicked;
		UIEventListener.Get(swapPrefabsButton).onClick += onSwapPrefabsButtonClicked;
		UIEventListener.Get(simpleGraphGenButton).onClick += onSimpleGraphGenButtonClicked;
		UIEventListener.Get(graphConfirmYesButton).onClick += onGraphConfirmYesClicked;
		UIEventListener.Get(graphConfirmNoButton).onClick += onGraphConfirmNoClicked;
		
		// Add editor selection objects to all existing nodes and links is done in OnActivateSelectObjects
		loadEditorObjects();
		CreateNodeSelections();
		CreateLinkSelections();
		CreateDummyLinkAndNode(); // Used for previewing what is about to be created
	}

	void loadEditorObjects() {
		foreach(Transform child in theMap.transform) {
			if (child.gameObject.name.StartsWith("WMG_Node")) {
				theMap.NodesParent.Add(child.gameObject);
			}
			if (child.gameObject.name.StartsWith("WMG_Link")) {
				theMap.LinksParent.Add(child.gameObject);
			}
		}
	}
	
	void Update() {
		// Since NGUI upgrade to 3.0, boxcollider size does not seem to update so manually updating here
		UIWidget bgWid = background.GetComponent<UIWidget>();
		BoxCollider bgCol = background.GetComponent<BoxCollider>();
		bgCol.size = new Vector3(bgWid.width, bgWid.height, 0);
		// Update Colors
		if (applyColor) {
			foreach (GameObject node in theMap.NodesParent) {
				WMG_Node aNode = node.GetComponent<WMG_Node>();
				if (aNode != null && aNode.isSelected) {
					UIWidget aNode2 = aNode.objectToColor.GetComponent<UIWidget>();
					aNode2.color = newNodeColor;
				}
			}
			foreach (GameObject link in theMap.LinksParent) {
				WMG_Link aLink = link.GetComponent<WMG_Link>();
				if (aLink != null && aLink.isSelected) {
					UIWidget aLink2 = aLink.objectToColor.GetComponent<UIWidget>();
					aLink2.color = newLinkColor;
				}
			}
		}
	}
	
	
	// ---------- On Link Functions
	
	void OnLinkClicked(GameObject go) {
		if (Input.GetKey(KeyCode.LeftControl)) {
		}
		else {
			DeselectAllLinks();
			DeselectAllNodes();
		}
		WMG_Link theLink = go.transform.parent.GetComponent<WMG_Link>();
		if (theLink != null) SelectLink(theLink, !theLink.isSelected);
	}
	
	
	
	
	
	
	// ---------- On Node Functions
	
	void OnNodeClicked(GameObject go) {
		if (Input.GetKey(KeyCode.LeftControl)) {
		}
		else {
			DeselectAllLinks();
			DeselectAllNodes();
		}
		WMG_Node theNode = go.transform.parent.GetComponent<WMG_Node>();
		if (theNode != null) SelectNode(theNode, !theNode.isSelected);
		
	}
	
	void OnNodeDrag(GameObject go, Vector2 delta) {
		delta = new Vector2(delta.x * UICamera.currentCamera.orthographicSize, delta.y * UICamera.currentCamera.orthographicSize);
		WMG_Node theNode = go.transform.parent.GetComponent<WMG_Node>();
		if (UICamera.currentTouchID == -1) {
			// Left mouse node drag is move
			if (theNode.isSelected) {
				// Drag multiple selected nodes
				foreach (GameObject node in theMap.NodesParent) {
					WMG_Node aNode = node.GetComponent<WMG_Node>();
					if (aNode != null) {
						if (aNode.isSelected) {
							aNode.Reposition(aNode.transform.localPosition.x + delta.x, aNode.transform.localPosition.y + delta.y);
						}
					}
				}
				// Move node respects angle and length steps if there is a selected link
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl)) {
					theNode.transform.position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(	Mathf.Clamp01(UICamera.currentTouch.pos.x / Screen.width),
																											Mathf.Clamp01(UICamera.currentTouch.pos.y / Screen.height),0));
					for (int i = 0; i < theNode.numLinks; i++) {
						WMG_Link aLink = theNode.links[i].GetComponent<WMG_Link>();
						if (aLink.isSelected) {
							WMG_Node toNode = aLink.toNode.GetComponent<WMG_Node>();
							if (toNode.id == theNode.id) toNode = aLink.fromNode.GetComponent<WMG_Node>();
							if (Input.GetKey(KeyCode.LeftShift)) {
								theNode.RepositionRelativeToNode(toNode, true, linkDegreeStep, 0);
							}
							if (Input.GetKey(KeyCode.LeftControl)) {
								theNode.RepositionRelativeToNode(toNode, false, 0, linkLengthStep);
							}
							break;
						}
					}
				}
			}
			else {
				// Drag the node where started dragging
				theNode.Reposition(theNode.transform.localPosition.x + delta.x, theNode.transform.localPosition.y + delta.y);
			}
		}
		else if (UICamera.currentTouchID == -2) {
			// Right mouse node drag is possible new node / link creation
			WMG_Node dumNode = dummyNode.GetComponent<WMG_Node>();
			WMG_Link dumLink = dummyLink.GetComponent<WMG_Link>();
			UIWidget dumLinkW = dumLink.objectToScale.GetComponent<UIWidget>();
			if (dumLinkW.height > 0) {
				dummyNodeOffsetX = dumNode.radius * Mathf.Cos(Mathf.Deg2Rad * dumNode.linkAngles[0]);
				dummyNodeOffsetY = dumNode.radius * Mathf.Sin(Mathf.Deg2Rad * dumNode.linkAngles[0]);
			}
			dummyNode.transform.position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(	Mathf.Clamp01(UICamera.currentTouch.pos.x / Screen.width),
																									Mathf.Clamp01(UICamera.currentTouch.pos.y / Screen.height),0));
			
			dumNode.Reposition(	dumNode.transform.localPosition.x - dummyNodeOffsetX, 
								dumNode.transform.localPosition.y - dummyNodeOffsetY);
			
			
			if (Input.GetKey(KeyCode.LeftShift)) {
				dumNode.RepositionRelativeToNode(theNode, true, linkDegreeStep, 0);
			}
			if (Input.GetKey(KeyCode.LeftControl)) {
				dumNode.RepositionRelativeToNode(theNode, false, 0, linkLengthStep);
			}
		}
	}
	
	void OnNodePress(GameObject go, bool pressed) {
		WMG_Node fromNode = go.transform.parent.GetComponent<WMG_Node>();
		if (UICamera.currentTouchID == -2) {
			if (pressed) {
				cantCreateNode = false;
				dummyNode.transform.parent = fromNode.transform.parent;
				dummyLink.transform.parent = fromNode.transform.parent;
				dummyNode.transform.position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(	Mathf.Clamp01(UICamera.currentTouch.pos.x / Screen.width),
																										Mathf.Clamp01(UICamera.currentTouch.pos.y / Screen.height),0));
				WMG_Link dumLink = dummyLink.GetComponent<WMG_Link>();
				UIWidget dumLinkW = dumLink.objectToScale.GetComponent<UIWidget>();
				dumLinkW.height = 0;
				theMap.SetActive(dummyLink,true);
				
				fromNode.links.Add(dummyLink);
				fromNode.linkAngles.Add(0);
				fromNode.numLinks++;
				dumLink.Setup(fromNode.gameObject, dummyNode, -1, true);
				dumLink.name = "previewDummyLink";
				dummyNodeOffsetX = 0;
				dummyNodeOffsetY = 0;
			}
			else {
				fromNode.numLinks--;
				fromNode.links.RemoveAt(fromNode.numLinks);
				fromNode.linkAngles.RemoveAt(fromNode.numLinks);
				theMap.SetActive(dummyLink,false);
			}
		}
		else {
			// Left mouse button node press / drag cant create node for background dropped events
			cantCreateNode = true;
		}
	}
	
	void OnNodeDrop(GameObject go, GameObject orig) {
		WMG_Node fromNode = orig.transform.parent.GetComponent<WMG_Node>();
		if (fromNode != null) {
			CreateLinkBetweenNodes(fromNode, go.transform.parent.gameObject);
		}
	}
	
	
	
	
	
	
	// ---------- Editor Background Functions
	
	void OnEditorBackgroundClicked(GameObject go) {
		if (Input.GetKey(KeyCode.LeftControl)) {
		}
		else if (Input.GetKey(KeyCode.LeftShift)) {
		}
		else {
			DeselectAllLinks();
			DeselectAllNodes();
		}
		if (UICamera.currentTouchID == -2) {
			// Right mouse button click creates node
			GameObject curObj = CreateNode();
			curObj.transform.position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(Mathf.Clamp01(UICamera.currentTouch.pos.x / Screen.width),
																								Mathf.Clamp01(UICamera.currentTouch.pos.y / Screen.height),0));
		}
	}
	
	void OnEditorBackgroundPressed(GameObject go, bool pressed) {
		if (UICamera.currentTouchID == -1) {
			if (pressed) {
				// Set drag object active, deselect nodes and links
				dragSelect.transform.position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(Mathf.Clamp01(UICamera.currentTouch.pos.x / Screen.width),
																										Mathf.Clamp01(UICamera.currentTouch.pos.y / Screen.height),0));
				dragStateH = 0;
				dragStateV = 0;
				theMap.SetActive(dragSelect.gameObject,true);
				foreach (GameObject node in theMap.NodesParent) {
					WMG_Node aNode = node.GetComponent<WMG_Node>();
					if (aNode != null) {
						aNode.wasSelected = aNode.isSelected;
					}
				}
				foreach (GameObject link in theMap.LinksParent) {
					WMG_Link aLink = link.GetComponent<WMG_Link>();
					if (aLink != null) {
						aLink.wasSelected = aLink.isSelected;
					}
				}
				if (Input.GetKey(KeyCode.LeftControl)) {
				}
				else if (Input.GetKey(KeyCode.LeftShift)) {
				}
				else {
					DeselectAllLinks();
					DeselectAllNodes();
				}
			}
			else {
				// Set drag object inactive
				theMap.SetActive(dragSelect.gameObject,false);
				UIWidget dragSelectW = dragSelect.GetComponent<UIWidget>();
				dragSelectW.height = 1;
				dragSelectW.width = 1;
			}
		}
		
	}
	
	public void OnEditorBackgroundDropped(GameObject go, GameObject orig) {
		// Create new node and link between nodes
		WMG_Node fromNode = orig.transform.parent.GetComponent<WMG_Node>();
		if (fromNode != null && !cantCreateNode) {	
			GameObject curObj = CreateNode();
			curObj.transform.parent = dummyNode.transform.parent;
			curObj.transform.localPosition = new Vector3(dummyNode.transform.localPosition.x + dummyNodeOffsetX, dummyNode.transform.localPosition.y + dummyNodeOffsetY, 0);
			CreateLinkBetweenNodes(fromNode, curObj);
		}
	}
	
	void OnEditorBackgroundDragged(GameObject go, Vector2 delta) {
		delta = new Vector2(delta.x * UICamera.currentCamera.orthographicSize, delta.y * UICamera.currentCamera.orthographicSize);
		if (UICamera.currentTouchID == -2) {
			// Right mouse drag is pan
			MapManagerObject.transform.localPosition = new Vector3(	MapManagerObject.transform.localPosition.x + delta.x, 
																	MapManagerObject.transform.localPosition.y + delta.y, 
																	MapManagerObject.transform.localPosition.z);
		}
		else if (UICamera.currentTouchID == -1) {
			// Left mouse drag is to perform multi-selection
			float minDragX = 0;
			float maxDragX = 0;
			float minDragY = 0;
			float maxDragY = 0;
			
			UIWidget dragSelectW = dragSelect.GetComponent<UIWidget>();
			// All of this is needed because the NGUI sprite can't have a negative scale
			// Ensure the delta is positive, but negative when changing directions to decrease the size
			
			// Horizontal portion
			if (delta.x < 0) {
				if (dragStateH == 1 || dragStateH == -2) dragStateH = -2;
				else dragStateH = -1;
			}
			if (delta.x > 0) {
				if (dragStateH == -1 || dragStateH == 2) dragStateH = 2;
				else dragStateH = 1;
			}
			float deltaX = Mathf.Abs(delta.x);
			if (Mathf.Abs(dragStateH) == 2) {
				deltaX *= -1;
				if (dragSelectW.width + deltaX <= 0) {
					if (dragStateH == 2) dragStateH = 1;
					if (dragStateH == -2) dragStateH = -1;
					deltaX = -1 * deltaX - 2 * dragSelectW.width;
				}
			}
			// Vertical portion
			if (delta.y < 0) {
				if (dragStateV == 1 || dragStateV == -2) dragStateV = -2;
				else dragStateV = -1;
			}
			if (delta.y > 0) {
				if (dragStateV == -1 || dragStateV == 2) dragStateV = 2;
				else dragStateV = 1;
			}
			float deltaY = Mathf.Abs(delta.y);
			if (Mathf.Abs(dragStateV) == 2) {
				deltaY *= -1;
				if (dragSelectW.height + deltaY <= 0) {
					if (dragStateV == 2) dragStateV = 1;
					if (dragStateV == -2) dragStateV = -1;
					deltaY = -1 * deltaY - 2 * dragSelectW.height;
				}
			}
			// Update the scale and position of the drag select sprite
			dragSelectW.width += Mathf.RoundToInt(deltaX);
			dragSelectW.height += Mathf.RoundToInt(deltaY);
			dragSelect.transform.localPosition = new Vector3(dragSelect.transform.localPosition.x + 0.5f * delta.x, dragSelect.transform.localPosition.y + 0.5f * delta.y, dragSelect.transform.localPosition.z);
			// Used to find what is underneath the selection box
			minDragX = dragSelect.transform.localPosition.x - dragSelectW.width / 2f;
			maxDragX = dragSelect.transform.localPosition.x + dragSelectW.width / 2f;
			minDragY = dragSelect.transform.localPosition.y - dragSelectW.height / 2f;
			maxDragY = dragSelect.transform.localPosition.y + dragSelectW.height / 2f;
			// Multi-select nodes based on the updated drag select sprite
			UIToggle checkNodes = dragSelectNodes.GetComponent<UIToggle>();
			if (checkNodes.value) {
				foreach (GameObject node in theMap.NodesParent) {
					Vector3 nodePos = GetLocalPositionRelativeToManager(node);
					WMG_Node aNode = node.GetComponent<WMG_Node>();
					if (aNode != null && aNode.id != -1) {
						bool hoveredOver = 	nodePos.x > minDragX && nodePos.x < maxDragX && 
											nodePos.y > minDragY && nodePos.y < maxDragY;
						if (Input.GetKey(KeyCode.LeftShift)) {
							SelectNode(aNode, aNode.wasSelected || hoveredOver);
						}
						else if (Input.GetKey(KeyCode.LeftControl)) {
							SelectNode(aNode, aNode.wasSelected != hoveredOver); // Requires wasSelected, otherwise each drag event will reverse isSelected
						}
						else {
							SelectNode(aNode, hoveredOver);
						}
					}
				}
			}
			UIToggle checkLinks = dragSelectLinks.GetComponent<UIToggle>();
			if (checkLinks.value) {
				foreach (GameObject link in theMap.LinksParent) {
					Vector3 linkPos = GetLocalPositionRelativeToManager(link);
					WMG_Link aLink = link.GetComponent<WMG_Link>();
					if (aLink != null && aLink.id != -1) {
						bool hoveredOver = 	linkPos.x > minDragX && linkPos.x < maxDragX && 
											linkPos.y > minDragY && linkPos.y < maxDragY;
						if (Input.GetKey(KeyCode.LeftShift)) {
							SelectLink(aLink, aLink.wasSelected || hoveredOver);
						}
						else if (Input.GetKey(KeyCode.LeftControl)) {
							SelectLink(aLink, aLink.wasSelected != hoveredOver); // Requires wasSelected, otherwise each drag event will reverse isSelected
						}
						else {
							SelectLink(aLink, hoveredOver);
						}
					}
				}
			}
		}
	}
	
	void OnEditorBackgroundScrolled(GameObject go, float delta) {
		UIStretch theStretch = background.GetComponent<UIStretch>();
		if (delta > 0) {
			if (zoomLevel > -numZoomLevels && !isZooming) {
				zoomLevel--;
				if (zoomLevel != 0) theMap.SetActive(controlsParent,false);
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
				if (zoomLevel != 0) theMap.SetActive(controlsParent,false);
				isZooming = true;
				float orthoSize = Mathf.Pow(zoomFactor,zoomLevel);
				TweenOrthoSize tos = TweenOrthoSize.Begin(UICamera.currentCamera.gameObject, zoomSpeed, orthoSize);
				tos.callWhenFinished = "endZoomUpdateBackground";
				tos.eventReceiver = this.gameObject;
			}
		}
	}
	
	
	
	
	
	// ---------- Editor UI Functions
	
	
	void onDeleteButtonClicked(GameObject go) {
		
		List<WMG_Node> theNodes = new List<WMG_Node>();
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) theNodes.Add(aNode);
		}
		foreach (WMG_Node aNode in theNodes) {
			theMap.DeleteNode(aNode);
		}
		
		List<WMG_Link> theLinks = new List<WMG_Link>();
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null && aLink.isSelected) theLinks.Add(aLink);
		}
		foreach (WMG_Link aLink in theLinks) {
			theMap.DeleteLink(aLink);
		}
		
	}
	
	void OnActivateSelectObjects() {
		UIToggle showSelections = selectionObjectsToggle.GetComponent<UIToggle>();
		if (showSelections.value) {
			CreateLinkSelections();
			CreateNodeSelections();
		}
		else {
			DeleteLinkSelections();
			DeleteNodeSelections();
		}
	}
	
	void onSaveButtonClicked(GameObject go) {
		#if UNITY_EDITOR
		// Delete editor objects associated with the graph
		DestroyImmediate(dummyLink);
		DestroyImmediate(dummyNode);
		DeleteLinkSelections();
		DeleteNodeSelections();
		
        string localPath = "Assets/Resources/Prefabs/" + theMap.gameObject.name + ".prefab";
		Object prefab = AssetDatabase.LoadAssetAtPath(localPath, typeof(GameObject));
        if (prefab != null) {
            if (EditorUtility.DisplayDialog("Are you sure?", "The prefab already exists. Do you want to overwrite it?", "Yes", "No")) {
				PrefabUtility.ReplacePrefab(theMap.gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
			}
        }
        else {
			prefab = PrefabUtility.CreateEmptyPrefab(localPath);
   			PrefabUtility.ReplacePrefab(theMap.gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
		}
		
		// Recreate editor objects associated with the graph
		CreateLinkSelections();
		CreateNodeSelections();
		CreateDummyLinkAndNode();
		#endif
	}
	
	public void OnSelectionChange() {
		UIPopupList inspList = selectInspectorList.GetComponent<UIPopupList>();
		if (inspList.value == "Root") inspectorSelectType = 0;
		else if (inspList.value == "Scale Object") inspectorSelectType = 1;
		else if (inspList.value == "Color Object") inspectorSelectType = 2;
		else if (inspList.value == "Label Object") inspectorSelectType = 3;
	}
	
	public void OnSelectionChangePrefabNodes() {
		UIPopupList preNodesList = prefabNodesList.GetComponent<UIPopupList>();
		if (!int.TryParse(preNodesList.value.Substring(0,2),out selectedPrefabNode)) {
			int.TryParse(preNodesList.value.Substring(0,1),out selectedPrefabNode);
		}
		
	}
	
	public void OnSelectionChangePrefabLinks() {
		UIPopupList preLinksList = prefabLinksList.GetComponent<UIPopupList>();
		if (!int.TryParse(preLinksList.value.Substring(0,2),out selectedPrefabLink)) {
			int.TryParse(preLinksList.value.Substring(0,1),out selectedPrefabLink);
		}
	}
	
	public void OnSliderChangeLinkDegree() {
		UISlider linkDegree = sliderLinkDegreeStep.GetComponent<UISlider>();
		int stepNumber = Mathf.RoundToInt((linkDegree.numberOfSteps - 1)*linkDegree.value);
		switch (stepNumber) {
			case 0: linkDegreeStep = 5; break;
			case 1: linkDegreeStep = 10; break;
			case 2: linkDegreeStep = 15; break;
			case 3: linkDegreeStep = 20; break;
			case 4: linkDegreeStep = 30; break;
			case 5: linkDegreeStep = 45; break;
			case 6: linkDegreeStep = 90; break;
		}
		UILabel theLabel = sliderLinkDegreeStepNumLabel.GetComponent<UILabel>();
		theLabel.text = linkDegreeStep.ToString();
	}
	
	public void OnSliderChangeLinkLength() {
		UISlider linkLength = sliderLinkLengthStep.GetComponent<UISlider>();
		linkLengthStep = linkLength.value * 100;
		UILabel theLabel = sliderLinkLengthStepNumLabel.GetComponent<UILabel>();
		theLabel.text = Mathf.RoundToInt(linkLengthStep).ToString();
	}
	
	void onSwapPrefabsButtonClicked(GameObject go) {
		List<WMG_Link> theLinks = new List<WMG_Link>();
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null && aLink.isSelected) theLinks.Add(aLink);
		}
		foreach (WMG_Link aLink in theLinks) {
			WMG_Node fromNode = aLink.fromNode.GetComponent<WMG_Node>();
			GameObject newLink = theMap.CreateLink(fromNode, aLink.toNode, prefabLinks[selectedPrefabLink], null);
			theMap.DeleteLink(aLink);
			WMG_Link newLink2 = newLink.GetComponent<WMG_Link>();
			ApplyLinkEditorObjects(newLink2);
			SelectLink(newLink2, true);
		}
		List<WMG_Node> theNodes = new List<WMG_Node>();
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) theNodes.Add(aNode);
		}
		foreach (WMG_Node aNode in theNodes) {
			GameObject newNode = theMap.ReplaceNodeWithNewPrefab(aNode, prefabNodes[selectedPrefabNode]);
			WMG_Node theNode2 = newNode.GetComponent<WMG_Node>();
			ApplyNodeEditorObjects(theNode2);
			SelectNode(theNode2, true);
		}
	}
	
	
	
	
	
	// ---------- Graph Generator Functions
	
	void onSimpleGraphGenButtonClicked(GameObject go) {
		theMap.SetActive(graphConfirmPopup,true);
		WMG_Node fromNode = GetOneSelectedNode();
		UIButton addToNode = graphAddToNodeSelected.GetComponent<UIButton>();
		addToNode.isEnabled = fromNode != null;
		#if UNITY_EDITOR
		Selection.activeGameObject = graphSetParameters;
		#endif
		OnSelectionChangeGraphTypes();
	}
	
	void onGraphConfirmNoClicked(GameObject go) {
		UIToggle previewGraph = graphPreview.GetComponent<UIToggle>();
		previewGraph.value = false;
		theMap.SetActive(graphConfirmPopup,false);
	}
	
	public void OnSelectionChangeGraphTypes() {
		UIPopupList graphList = graphListTypes.GetComponent<UIPopupList>();
		WMG_Node fromNode = GetOneSelectedNode();
		UIButton addToNode = graphAddToNodeSelected.GetComponent<UIButton>();
		UIButton previewGraph = graphPreview.GetComponent<UIButton>();
		previewGraph.isEnabled = true;
		if (fromNode == null) {
			addToNode.isEnabled = false;
		}
		else {
			addToNode.isEnabled = true;
			// Graph types that don't support adding to existing selected node
			if (graphList.value == "Grid") {
				addToNode.isEnabled = false;
			}
		}
		if (graphList.value == "Random Graph") {
			RemoveAllScriptsExceptSpecified("WMG_Random_Graph");
			
			WMG_Random_Graph randomGraph = graphSetParameters.GetComponent<WMG_Random_Graph>();
			if (randomGraph == null) {
				graphSetParameters.AddComponent<WMG_Random_Graph>();
				randomGraph = graphSetParameters.GetComponent<WMG_Random_Graph>();
				randomGraph.setManager(MapManagerObject);
				randomGraph.nodePrefab = prefabNodes[selectedPrefabNode];
				randomGraph.linkPrefab = prefabLinks[selectedPrefabLink];
				// Set some defaults
				randomGraph.numNodes = 50;
				randomGraph.minAngle = 15;
				randomGraph.minAngleRange = 0;
				randomGraph.maxAngleRange = 360;
				randomGraph.minRandomNumberNeighbors = 2;
				randomGraph.maxRandomNumberNeighbors = 5;
				randomGraph.minRandomLinkLength = 50;
				randomGraph.maxRandomLinkLength = 100;
				randomGraph.centerPropogate = false;
				randomGraph.noLinkIntersection = true;
				randomGraph.noNodeIntersection = true;
				randomGraph.noNodeIntersectionRadiusPadding = 15;
				randomGraph.maxNeighborAttempts = 100;
				randomGraph.noLinkNodeIntersection = true;
				randomGraph.noLinkNodeIntersectionRadiusPadding = 15;
			}
		}
		else if (graphList.value == "Grid") {
			RemoveAllScriptsExceptSpecified("WMG_Grid");
			previewGraph.isEnabled = false;
			
			WMG_Grid squareGrid = graphSetParameters.GetComponent<WMG_Grid>();
			if (squareGrid == null) {
				graphSetParameters.AddComponent<WMG_Grid>();
				squareGrid = graphSetParameters.GetComponent<WMG_Grid>();
				//squareGrid.setManager(MapManagerObject);
				squareGrid.nodePrefab = prefabNodes[selectedPrefabNode];
				squareGrid.linkPrefab = prefabLinks[selectedPrefabLink];
				// Set some defaults
				squareGrid.gridNumNodesX = 5;
				squareGrid.gridNumNodesY = 5;
			}
		}
	}
	
	void RemoveAllScriptsExceptSpecified(string scriptName) {
		foreach (Component child in graphSetParameters.GetComponents<MonoBehaviour>()) {
			if (child.GetType().ToString() != scriptName) {
				Destroy(child);
			}
		}
	}
	
	void OnActivateGraphPreview() {
		UIToggle previewGraph = graphPreview.GetComponent<UIToggle>();
		UIPanel graphConfirmPanel = graphConfirmPopup.GetComponent<UIPanel>();
		if (previewGraph.value) {
			graphConfirmPanel.alpha = 0.2f;
			createAddGraphObjects();
		}
		else {
			graphConfirmPanel.alpha = 1;
			UIPopupList graphList = graphListTypes.GetComponent<UIPopupList>();
			if (graphList.value == "Random Graph") {
				destroyAddGraphObjects();
			}
		}
	}
	
	void createAddGraphObjects() {
		UIToggle addToNode = graphAddToNodeSelected.GetComponent<UIToggle>();
		WMG_Node fromNode = GetOneSelectedNode();
		WMG_Grid squareGrid = graphSetParameters.GetComponent<WMG_Grid>();
		if (squareGrid != null) {
			squareGrid.nodePrefab = prefabNodes[selectedPrefabNode];
			squareGrid.linkPrefab = prefabLinks[selectedPrefabLink];
			squareGrid.Refresh();
			graphGenObjects = squareGrid.GetNodesAndLinks();
		}
		WMG_Random_Graph randomGraph = graphSetParameters.GetComponent<WMG_Random_Graph>();
		if (randomGraph != null) {
			randomGraph.nodePrefab = prefabNodes[selectedPrefabNode];
			randomGraph.linkPrefab = prefabLinks[selectedPrefabLink];
			if (addToNode.value) {
				graphGenObjects = randomGraph.GenerateGraphFromNode(fromNode);
			}
			else {
				graphGenObjects = randomGraph.GenerateGraph();
			}
		}
	}
	
	void destroyAddGraphObjects() {
		WMG_Node fromNode = GetOneSelectedNode();
		if (graphGenObjects != null) {
			List<WMG_Node> theNodes = new List<WMG_Node>();
			foreach (GameObject child in graphGenObjects) {
				WMG_Node aNode = child.GetComponent<WMG_Node>();
				if (fromNode != null && aNode != null && aNode.id != fromNode.id) theNodes.Add(aNode);
				else if (fromNode == null && aNode != null) theNodes.Add(aNode);
			}
			foreach (WMG_Node child in theNodes) {
				theMap.DeleteNode(child);
			}
		}
	}
	
	void onGraphConfirmYesClicked(GameObject go) {
		UIToggle previewGraph = graphPreview.GetComponent<UIToggle>();
		WMG_Node fromNode = GetOneSelectedNode();
		if (!previewGraph.value) {
			// Preview was not checked so need to create
			createAddGraphObjects();
		}
		// Add Editor Objects
		foreach (GameObject child in graphGenObjects) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (fromNode != null && aNode != null && aNode.id != fromNode.id) ApplyNodeEditorObjects(aNode);
			else if (fromNode == null && aNode != null) ApplyNodeEditorObjects(aNode);
			WMG_Link aLink = child.GetComponent<WMG_Link>();
			if (aLink != null) {
				ApplyLinkEditorObjects(aLink);
			}
		}
		graphGenObjects = null;
		previewGraph.value = false;
		theMap.SetActive(graphConfirmPopup,false);
		// Remove Scrips from add parameters
		foreach (Component child in graphSetParameters.GetComponents<MonoBehaviour>()) {
			Destroy(child);
		}
	}
	
	
	
	// ---------- Helper Functions
	
	GameObject CreateNode() {
		GameObject theNode = theMap.CreateNode(prefabNodes[selectedPrefabNode], null);
		WMG_Node theNode2 = theNode.GetComponent<WMG_Node>();
		ApplyNodeEditorObjects(theNode2);
		return theNode;
	}
	
	void CreateLinkBetweenNodes(WMG_Node fromNode, GameObject toN) {
		WMG_Node toNode = toN.GetComponent<WMG_Node>();
		// Check to see if there is already a link
		bool alreadyALink = false;
		for (int i = 0; i < fromNode.numLinks; i++) {
			WMG_Link aLink = fromNode.links[i].GetComponent<WMG_Link>();
			WMG_Node aLinkFrom = aLink.fromNode.GetComponent<WMG_Node>();
			WMG_Node aLinkTo = aLink.toNode.GetComponent<WMG_Node>();
			if (aLinkFrom.id == toNode.id || aLinkTo.id == toNode.id) {
				alreadyALink = true;
				break;
			}
		}
		if (!alreadyALink) {
			// Create link between nodes
			GameObject newLink = theMap.CreateLink(fromNode, toN, prefabLinks[selectedPrefabLink], null);
			WMG_Link newLink2 = newLink.GetComponent<WMG_Link>();
			ApplyLinkEditorObjects(newLink2);
		}
	}
	
	void ApplyNodeEditorObjects(WMG_Node theNode) {
		if (applyColor) {
			UIWidget theNode2 = theNode.objectToColor.GetComponent<UIWidget>();
			theNode2.color = newNodeColor;
		}
		CreateNodeSelection(theNode.gameObject);
	}
	
	void ApplyLinkEditorObjects(WMG_Link theLink) {
		if (applyColor) {
			UIWidget theLink2 = theLink.objectToColor.GetComponent<UIWidget>();
			theLink2.color = newLinkColor;
		}
		CreateLinkSelection(theLink.gameObject);
	}
	
	void endZooming() {
		isZooming = false;
		if (zoomLevel == 0) theMap.SetActive(controlsParent,true);
	}
	
	void endZoomUpdateBackground() {
		isZooming = false;
		if (zoomLevel == 0) theMap.SetActive(controlsParent,true);
		UIStretch theStretch = background.GetComponent<UIStretch>();
		theStretch.relativeSize.x = UICamera.currentCamera.orthographicSize;
		theStretch.relativeSize.y = UICamera.currentCamera.orthographicSize;
	}
	
	
	void SelectNode(WMG_Node aNode, bool isSelected) {
		aNode.isSelected = isSelected;
		foreach (Transform child in aNode.transform) {
			if (child.name == "WMG_Node_Sel") {
				foreach (Transform childSel in child) {
					theMap.SetActive(childSel.gameObject,isSelected);
				}
			}
		}
		UIToggle showInspector = selectInspectorShow.GetComponent<UIToggle>();
		if (showInspector.value) {
			#if UNITY_EDITOR
			Selection.objects = GetInspectorSelection();
			#endif
		}
	}
	
	void SelectLink(WMG_Link aLink, bool isSelected) {
		aLink.isSelected = isSelected;
		foreach (Transform child in aLink.transform) {
			if (child.name == "WMG_Link_Sel") {
				foreach (Transform childSel in child) {
					theMap.SetActive(childSel.gameObject,isSelected);
				}
			}
		}
		UIToggle showInspector = selectInspectorShow.GetComponent<UIToggle>();
		if (showInspector.value) {
			#if UNITY_EDITOR
			Selection.objects = GetInspectorSelection();
			#endif
		}
	}
	
	GameObject[] GetInspectorSelection() {
		int index = 0;
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) index++;
		}
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null && aLink.isSelected) index++;
		}
		GameObject[] theSelection = new GameObject[index];
		index = 0;
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) {
				if (inspectorSelectType == 0) {
					theSelection.SetValue(aNode.gameObject,index);
				}
				else if (inspectorSelectType == 1) {
					theSelection.SetValue(aNode.objectToScale,index);
				}
				else if (inspectorSelectType == 2) {
					theSelection.SetValue(aNode.objectToColor,index);
				}
				else if (inspectorSelectType == 3) {
					theSelection.SetValue(aNode.objectToLabel,index);
				}
				index++;
			}
		}
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null && aLink.isSelected) {
				if (inspectorSelectType == 0) {
					theSelection.SetValue(aLink.gameObject,index);
				}
				else if (inspectorSelectType == 1) {
					theSelection.SetValue(aLink.objectToScale,index);
				}
				else if (inspectorSelectType == 2) {
					theSelection.SetValue(aLink.objectToColor,index);
				}
				else if (inspectorSelectType == 3) {
					theSelection.SetValue(aLink.objectToLabel,index);
				}
				index++;
			}
		}
		return theSelection;
	}
	
	void DeselectAllNodes() {
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) SelectNode(aNode, false);
		}
	}
	
	void DeselectAllLinks() {
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null && aLink.isSelected) SelectLink(aLink, false);
		}
	}
	
	void CreateNodeSelection(GameObject theNode) {
		GameObject curObj = Instantiate(prefabNodeSel) as GameObject;
		curObj.name = "WMG_Node_Sel";
		Vector3 localPos = curObj.transform.localPosition;
		curObj.transform.parent = theNode.transform;
		curObj.transform.localScale = Vector3.one;
		curObj.transform.localPosition = localPos;
		curObj.transform.localEulerAngles = Vector3.zero;
		UIEventListener.Get(curObj).onClick += OnNodeClicked;
		UIEventListener.Get(curObj).onDrag += OnNodeDrag;
		UIEventListener.Get(curObj).onDrop += OnNodeDrop;
		UIEventListener.Get(curObj).onPress += OnNodePress;
		foreach (Transform child in curObj.transform) {
			theMap.SetActive(child.gameObject,false);
		}
		if (selectionObjectsToggle.GetComponent<UIToggle>().value) {
			BoxCollider bc = theNode.GetComponent<BoxCollider>();
			if (bc != null) bc.enabled = false;
		}
	}
	
	void CreateLinkSelection(GameObject theLink) {
		GameObject curObj = Instantiate(prefabLinkSel) as GameObject;
		curObj.name = "WMG_Link_Sel";
		Vector3 localPos = curObj.transform.localPosition;
		curObj.transform.parent = theLink.transform;
		curObj.transform.localScale = Vector3.one;
		curObj.transform.localPosition = localPos;
		curObj.transform.localEulerAngles = Vector3.zero;
		UIEventListener.Get(curObj).onClick += OnLinkClicked;
		foreach (Transform child in curObj.transform) {
			theMap.SetActive(child.gameObject,false);
		}
		if (selectionObjectsToggle.GetComponent<UIToggle>().value) {
			BoxCollider bc = theLink.GetComponent<BoxCollider>();
			if (bc != null) bc.enabled = false;
		}
	}
	
	void CreateNodeSelections() {
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node theNode = node.GetComponent<WMG_Node>();
			if (theNode != null) {
				CreateNodeSelection(node);
			}
		}
	}
	
	void CreateLinkSelections() {
		foreach (GameObject link in theMap.LinksParent) {
			if (!selectionObjectsToggle.GetComponent<UIToggle>().value) {
				BoxCollider bc = link.GetComponent<BoxCollider>();
				if (bc != null) bc.enabled = true;
			}
			WMG_Link theLink = link.GetComponent<WMG_Link>();
			if (theLink != null) {
				CreateLinkSelection(link);
			}
		}
	}
	
	void DeleteNodeSelections() {
		foreach (GameObject node in theMap.NodesParent) {
			if (!selectionObjectsToggle.GetComponent<UIToggle>().value) {
				BoxCollider bc = node.GetComponent<BoxCollider>();
				if (bc != null) bc.enabled = true;
			}
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null) {
				foreach (Transform child in aNode.transform) {
					if (child.name == "WMG_Node_Sel") DestroyImmediate(child.gameObject);
				}
			}
		}
	}
	
	void DeleteLinkSelections() {
		foreach (GameObject link in theMap.LinksParent) {
			WMG_Link aLink = link.GetComponent<WMG_Link>();
			if (aLink != null) {
				foreach (Transform child in aLink.transform) {
					if (child.name == "WMG_Link_Sel") DestroyImmediate(child.gameObject);
				}
			}
		}
	}
	
	void CreateDummyLinkAndNode() {
		GameObject curObj = Instantiate(prefabNodes[selectedPrefabNode]) as GameObject;
		Vector3 origScale = curObj.transform.localScale;
		curObj.transform.parent = theMap.transform;
		curObj.transform.localScale = origScale;
		WMG_Node curNode = curObj.GetComponent<WMG_Node>();
		curNode.SetID(-1);
		curObj.name = "previewDummyNode";
		dummyNode = curObj;
		theMap.SetActive(dummyNode,false);
		
		curObj = Instantiate(prefabLinks[selectedPrefabLink]) as GameObject;
		Vector3 linkLocalPos = curObj.transform.localPosition;
		curObj.transform.parent = theMap.transform;
		curObj.transform.localScale = Vector3.one;
		curObj.transform.localPosition = linkLocalPos;
		curObj.name = "previewDummyLink";
		curObj.GetComponent<Collider>().enabled = false;
		dummyLink = curObj;
		theMap.SetActive(dummyLink,false);
		
		curNode.links.Add(dummyLink);
		curNode.linkAngles.Add(0);
		curNode.numLinks++;
		
		WMG_Link dumLink = dummyLink.GetComponent<WMG_Link>();
		dumLink.SetId(-1);
	}
	
	WMG_Node GetOneSelectedNode() {
		int countOfSelectedNodes = 0;
		WMG_Node theNode = dummyNode.GetComponent<WMG_Node>();
		foreach (GameObject node in theMap.NodesParent) {
			WMG_Node aNode = node.GetComponent<WMG_Node>();
			if (aNode != null && aNode.isSelected) {
				if (countOfSelectedNodes == 0) {
					theNode = aNode;
				}
				countOfSelectedNodes++;
			}
		}
		if (countOfSelectedNodes == 1) {
			return theNode;
		}
		else {
			return null;
		}
	}
	
	Vector3 GetLocalPositionRelativeToManager(GameObject go) {
		Vector3 returnPosition = go.transform.localPosition;
		Transform curParent = go.transform.parent;
		returnPosition = new Vector3(returnPosition.x + curParent.localPosition.x, returnPosition.y + curParent.localPosition.y, returnPosition.z + curParent.localPosition.z);
		while (curParent != MapManagerObject.transform) {
			curParent = curParent.parent;
			if (curParent == null) break;
			returnPosition = new Vector3(returnPosition.x + curParent.localPosition.x, returnPosition.y + curParent.localPosition.y, returnPosition.z + curParent.localPosition.z);
		}
		return returnPosition;
	}
}
