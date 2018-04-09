using UnityEngine;
using System.Collections;

// Contains GUI system dependent functions

public class WMG_GUI_Functions : MonoBehaviour {
	
	public enum WMGpivotTypes {Bottom, BottomLeft, BottomRight, Center, Left, Right, Top, TopLeft, TopRight};
	
	public void SetActive(GameObject obj, bool state) {
		obj.SetActive(state);
	}
	
	public bool activeInHierarchy(GameObject obj) {
		return obj.activeInHierarchy;
	}
	
	public void SetActiveAnchoredSprite(GameObject obj, bool state) {
		SetActive(obj, state);
	}

	public Texture2D getTexture(GameObject obj) {
		return (Texture2D)obj.GetComponent<UITexture>().mainTexture;
	}

	public void setTexture(GameObject obj, Sprite sprite) {
		obj.GetComponent<UITexture>().mainTexture = sprite.texture;
	}

	public void changeSpriteFill(GameObject obj, float fill) {
		UIBasicSprite theSprite = obj.GetComponent<UIBasicSprite>();
		theSprite.fillAmount = fill;
	}

	public void changeRadialSpriteRotation(GameObject obj, Vector3 newRot) {
		newRot += new Vector3(0,0,180);
		obj.transform.localEulerAngles = newRot;
	}
	
	public void changeSpriteColor(GameObject obj, Color aColor) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		theSprite.color = aColor;
	}
	
	public void changeSpriteWidth(GameObject obj, int aWidth) {
//		obj.transform.localScale = new Vector3(aWidth, obj.transform.localScale.y, obj.transform.localScale.z);
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		if (theSprite.isAnchored) {
			if (theSprite.pivot == UIWidget.Pivot.Left || theSprite.pivot == UIWidget.Pivot.BottomLeft || theSprite.pivot == UIWidget.Pivot.TopLeft) {
				theSprite.rightAnchor.Set (0, aWidth);
			} else if (theSprite.pivot == UIWidget.Pivot.Right || theSprite.pivot == UIWidget.Pivot.BottomRight|| theSprite.pivot == UIWidget.Pivot.TopRight) {
				theSprite.leftAnchor.Set (0, aWidth);
			} else {
				theSprite.rightAnchor.Set (0, aWidth / 2f);
				theSprite.leftAnchor.Set (0, aWidth / 2f);
			}
		} else {
			theSprite.width = aWidth;
		}
		NGUITools.UpdateWidgetCollider(obj);
	}
	
	public void changeSpriteHeight(GameObject obj, int aHeight) {
//		obj.transform.localScale = new Vector3(obj.transform.localScale.x, aHeight, obj.transform.localScale.z);
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		if (theSprite.isAnchored) {
			if (theSprite.pivot == UIWidget.Pivot.Bottom || theSprite.pivot == UIWidget.Pivot.BottomLeft || theSprite.pivot == UIWidget.Pivot.BottomRight) {
				theSprite.topAnchor.Set (0, aHeight);
			} else if (theSprite.pivot == UIWidget.Pivot.Top || theSprite.pivot == UIWidget.Pivot.TopLeft || theSprite.pivot == UIWidget.Pivot.TopRight) {
				theSprite.bottomAnchor.Set (0, aHeight);
			} else {
				theSprite.topAnchor.Set (0, aHeight / 2f);
				theSprite.bottomAnchor.Set (0, aHeight / 2f);
			}
		} else {
			theSprite.height = aHeight;
		}
		NGUITools.UpdateWidgetCollider(obj);
	}
	
	public void setTextureMaterial(GameObject obj, Material aMat) {
		UITexture curTex = obj.GetComponent<UITexture>();
		curTex.material = new Material(aMat);
	}
	
	public Material getTextureMaterial(GameObject obj) {
		UIDrawCall drawCall = obj.GetComponent<UIWidget>().drawCall;
		if (drawCall == null) return null;
		return drawCall.dynamicMaterial;
	}

	public void changeSpriteSize(GameObject obj, int aWidth, int aHeight) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;

		theSprite.width = aWidth;
		theSprite.height = aHeight;
		NGUITools.UpdateWidgetCollider(obj);
	}

	public Vector2 getSpriteSize(GameObject obj) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		return new Vector2(theSprite.width, theSprite.height);
	}
	
	public void changeAreaShadingWidthHeight(GameObject obj, int aWidth, int aHeight) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		
		theSprite.width = aWidth;
		theSprite.height = aHeight;
		
		// hide the object and save on draw calls
		if (theSprite.drawCall != null) {
			if (aWidth < 2 || aHeight < 2) {
				theSprite.drawCall.GetComponent<Renderer>().enabled = false;
			}
			else {
				theSprite.drawCall.GetComponent<Renderer>().enabled = true;
			}
		}
	}
	
	public void changeBarWidthHeight(GameObject obj, int aWidth, int aHeight) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		
		theSprite.width = aWidth;
		theSprite.height = aHeight;
		NGUITools.UpdateWidgetCollider(obj);
		
		// hide the object
		if (aWidth < 2 || aHeight < 2) {
			SetActive(obj, false);
		}
		else {
			SetActive(obj, true);
		}
	}
	
	public float getSpriteWidth(GameObject obj) {
//		return obj.transform.localScale.x;
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite.width == 2) return 0;
		return theSprite.width;
	}
	
	public float getSpriteHeight(GameObject obj) {
//		return obj.transform.localScale.y;
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite.height == 2) return 0;
		return theSprite.height;
	}
	
	public void changeLabelText(GameObject obj, string aText) {
		UILabel theLabel = obj.GetComponent<UILabel>();
		theLabel.text = aText;
	}

	public void changeLabelFontSize(GameObject obj, int newFontSize) {
		UILabel theLabel = obj.GetComponent<UILabel>();
		theLabel.fontSize = newFontSize;
	}
	
	public float getSpritePositionX(GameObject obj) {
		return obj.transform.localPosition.x;
	}
	
	public float getSpritePositionY(GameObject obj) {
		return obj.transform.localPosition.y;
	}
	
	public float getSpriteOffsetX(GameObject obj) {
		return 0;
	}
	
	public float getSpriteFactorX(GameObject obj) {
		return 0;
	}
	
	public float getSpriteOffsetY(GameObject obj) {
		return 0;
	}
	
	public float getSpriteFactorY(GameObject obj) {
		return 0;
	}
	
	public float getSpriteFactorY2(GameObject obj) {
		float factor = 0;
		UISprite theSprite = obj.GetComponent<UISprite>();
		if (theSprite.pivot == UIWidget.Pivot.Bottom) {
			factor = 1;
		}
		else if (theSprite.pivot == UIWidget.Pivot.BottomLeft) {
			factor = 1;
		}
		else if (theSprite.pivot == UIWidget.Pivot.BottomRight) {
			factor = 1;
		}
		else if (theSprite.pivot == UIWidget.Pivot.Center) {
			factor = 0.5f;
		}
		else if (theSprite.pivot == UIWidget.Pivot.Left) {
			factor = 0.5f;
		}
		else if (theSprite.pivot == UIWidget.Pivot.Right) {
			factor = 0.5f;
		}
		else if (theSprite.pivot == UIWidget.Pivot.Top) {
			factor = 0;
		}
		else if (theSprite.pivot == UIWidget.Pivot.TopLeft) {
			factor = 0;
		}
		else if (theSprite.pivot == UIWidget.Pivot.TopRight) {
			factor = 0;
		}
		return factor;
	}

	public Vector3 getPositionRelativeTransform(GameObject obj, GameObject relative) {
		return relative.transform.InverseTransformPoint(obj.transform.TransformPoint(Vector3.zero));
	}
	
	public void changePositionByRelativeTransform(GameObject obj, GameObject relative, Vector2 delta) {
		obj.transform.position = relative.transform.TransformPoint(getPositionRelativeTransform(obj, relative) + new Vector3 (delta.x, delta.y, 0));
	}
	
	public void changeSpritePositionTo(GameObject obj, Vector3 newPos) {
		obj.transform.localPosition = new Vector3(newPos.x, newPos.y, newPos.z);
	}
	
	public void changeSpritePositionToX(GameObject obj, float newPos) {
		Vector3 thePos = obj.transform.localPosition;
		obj.transform.localPosition = new Vector3(newPos, thePos.y, thePos.z);
	}
	
	public void changeSpritePositionToY(GameObject obj, float newPos) {
		Vector3 thePos = obj.transform.localPosition;
		obj.transform.localPosition = new Vector3(thePos.x, newPos, thePos.z);
	}
	
	public Vector2 getChangeSpritePositionTo(GameObject obj, Vector2 newPos) {
		return new Vector2(newPos.x, newPos.y);
	}
	
	public void changeSpritePositionRelativeToObjBy(GameObject obj, GameObject relObj, Vector3 changeAmt) {
		Vector3 thePos = relObj.transform.localPosition;
		obj.transform.localPosition = new Vector3(thePos.x + changeAmt.x, thePos.y + changeAmt.y, thePos.z + changeAmt.z);
	}
	
	public void changeSpritePositionRelativeToObjByX(GameObject obj, GameObject relObj, float changeAmt) {
		Vector3 thePos = relObj.transform.localPosition;
		Vector3 curPos = obj.transform.localPosition;
		obj.transform.localPosition = new Vector3(thePos.x + changeAmt, curPos.y, curPos.z);
	}
	
	public void changeSpritePositionRelativeToObjByY(GameObject obj, GameObject relObj, float changeAmt) {
		Vector3 thePos = relObj.transform.localPosition;
		Vector3 curPos = obj.transform.localPosition;
		obj.transform.localPosition = new Vector3(curPos.x, thePos.y + changeAmt, curPos.z);
	}
	
	public void changeSpritePivot(GameObject obj, WMGpivotTypes theType) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		if (theType == WMGpivotTypes.Bottom) {
			theSprite.pivot = UIWidget.Pivot.Bottom;
		}
		else if (theType == WMGpivotTypes.BottomLeft) {
			theSprite.pivot = UIWidget.Pivot.BottomLeft;
		}
		else if (theType == WMGpivotTypes.BottomRight) {
			theSprite.pivot = UIWidget.Pivot.BottomRight;
		}
		else if (theType == WMGpivotTypes.Center) {
			theSprite.pivot = UIWidget.Pivot.Center;
		}
		else if (theType == WMGpivotTypes.Left) {
			theSprite.pivot = UIWidget.Pivot.Left;
		}
		else if (theType == WMGpivotTypes.Right) {
			theSprite.pivot = UIWidget.Pivot.Right;
		}
		else if (theType == WMGpivotTypes.Top) {
			theSprite.pivot = UIWidget.Pivot.Top;
		}
		else if (theType == WMGpivotTypes.TopLeft) {
			theSprite.pivot = UIWidget.Pivot.TopLeft;
		}
		else if (theType == WMGpivotTypes.TopRight) {
			theSprite.pivot = UIWidget.Pivot.TopRight;
		}
	}

	public void changeSpritePivotRaw(GameObject obj, WMGpivotTypes theType) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		if (theType == WMGpivotTypes.Bottom) {
			theSprite.rawPivot = UIWidget.Pivot.Bottom;
		}
		else if (theType == WMGpivotTypes.BottomLeft) {
			theSprite.rawPivot = UIWidget.Pivot.BottomLeft;
		}
		else if (theType == WMGpivotTypes.BottomRight) {
			theSprite.rawPivot = UIWidget.Pivot.BottomRight;
		}
		else if (theType == WMGpivotTypes.Center) {
			theSprite.rawPivot = UIWidget.Pivot.Center;
		}
		else if (theType == WMGpivotTypes.Left) {
			theSprite.rawPivot = UIWidget.Pivot.Left;
		}
		else if (theType == WMGpivotTypes.Right) {
			theSprite.rawPivot = UIWidget.Pivot.Right;
		}
		else if (theType == WMGpivotTypes.Top) {
			theSprite.rawPivot = UIWidget.Pivot.Top;
		}
		else if (theType == WMGpivotTypes.TopLeft) {
			theSprite.rawPivot = UIWidget.Pivot.TopLeft;
		}
		else if (theType == WMGpivotTypes.TopRight) {
			theSprite.rawPivot = UIWidget.Pivot.TopRight;
		}
	}
	
	public void changeSpriteParent(GameObject child, GameObject parent) {
		child.transform.parent = parent.transform;
		child.transform.localPosition = Vector3.zero;
		child.transform.localScale = Vector3.one;
	}
	
	public void bringSpriteToFront(GameObject obj) {
		// Only needed in Daikon
	}
	
	public void sendSpriteToBack(GameObject obj) {
		// Only needed in Daikon
	}
	
	public string getDropdownSelection(GameObject obj) {
		UIPopupList dropdown = obj.GetComponent<UIPopupList>();
		return dropdown.value;
	}
	
	public void setDropdownSelection(GameObject obj, string newval) {
		UIPopupList dropdown = obj.GetComponent<UIPopupList>();
		dropdown.value = newval;
	}
	
	public void addDropdownItem(GameObject obj, string item) {
		UIPopupList dropdown = obj.GetComponent<UIPopupList>();
		dropdown.items.Add(item);
	}
	
	public void deleteDropdownItem(GameObject obj) {
		UIPopupList dropdown = obj.GetComponent<UIPopupList>();
		dropdown.items.RemoveAt(dropdown.items.Count-1);
	}
	
	public void setDropdownIndex(GameObject obj, int index) {
		UIPopupList dropdown = obj.GetComponent<UIPopupList>();
		dropdown.value = dropdown.items[index];
	}
	
	public void setButtonColor(Color aColor, GameObject obj) {
		UILabel aButton = obj.GetComponent<UILabel>();
		aButton.color = aColor;
	}
	
	public bool getToggle(GameObject obj) {
		UIToggle theTog = obj.GetComponent<UIToggle>();
		return theTog.value;
	}
	
	public void setToggle(GameObject obj, bool state) {
		UIToggle theTog = obj.GetComponent<UIToggle>();
		theTog.value = state;
	}
	
	public float getSliderVal(GameObject obj) {
		UISlider theSlider = obj.GetComponent<UISlider>();
		return theSlider.value;
	}
	
	public void setSliderVal(GameObject obj, float val) {
		UISlider theSlider = obj.GetComponent<UISlider>();
		theSlider.value = val;
	}
	
	public void showControl(GameObject obj) {
		SetActive(obj, true);
	}
	
	public void hideControl(GameObject obj) {
		SetActive(obj, false);
	}
	
	public bool getControlVisibility(GameObject obj) {
		return activeInHierarchy(obj);
	}
	
	public void unfocusControl(GameObject obj) {
		// Only needed in Daikon
	}
	
	public bool isDaikon() {
		// Sometimes this may be needed, usually hacky quick fix workaround
		return false;
	}



	public WMGpivotTypes getPivotType(Vector2 pivot) {
		// 0 - 1 left to right
		// 0 - 1 top to bot
		if (pivot.x == 0) {
			if (pivot.y == 0) {
				return WMGpivotTypes.TopLeft;
			} else if (pivot.y == 1) {
				return WMGpivotTypes.BottomLeft;
			} else {
				return WMGpivotTypes.Left;
			}
		} else if (pivot.x == 1) {
			if (pivot.y == 0) {
				return WMGpivotTypes.TopRight;
			} else if (pivot.y == 1) {
				return WMGpivotTypes.BottomRight;
			} else {
				return WMGpivotTypes.Right;
			}
		} else {
			if (pivot.y == 0) {
				return WMGpivotTypes.Top;
			} else if (pivot.y == 1) {
				return WMGpivotTypes.Bottom;
			} else {
				return WMGpivotTypes.Center;
			}
		}
	}

	public void changeSpriteSizeFloat(GameObject obj, float aWidth, float aHeight) {
		UIWidget theSprite = obj.GetComponent<UIWidget>();
		if (theSprite == null) return;
		changeSpriteWidth (obj, Mathf.RoundToInt(aWidth));
		changeSpriteHeight(obj, Mathf.RoundToInt(aHeight));
	}

	//http://answers.unity3d.com/questions/921726/how-to-get-the-size-of-a-unityengineuitext-for-whi.html
	public float getTextWidth(GameObject obj) {
		return getSpriteWidth (obj);
		//Text textComp = obj.GetComponent<Text> ();
		//return textComp.cachedTextGeneratorForLayout.GetPreferredWidth (
		//	textComp.text, textComp.GetGenerationSettings (textComp.GetComponent<RectTransform> ().rect.size));
	}
	//http://answers.unity3d.com/questions/921726/how-to-get-the-size-of-a-unityengineuitext-for-whi.html
	public float getTextHeight(GameObject obj) {
		return getSpriteHeight (obj);
		//Text textComp = obj.GetComponent<Text> ();
		//return textComp.cachedTextGeneratorForLayout.GetPreferredHeight (
		//	textComp.text, textComp.GetGenerationSettings (textComp.GetComponent<RectTransform> ().rect.size));
	}

	public void setAnchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition) {
		UIWidget theSprite = go.GetComponent<UIWidget>();
		if (theSprite == null) return;
		changeSpritePivot (go, getPivotType (pivot));
		theSprite.transform.localPosition = anchoredPosition;
//		RectTransform rt = go.GetComponent<RectTransform> ();
//		rt.pivot = pivot;
//		rt.anchorMin = anchor;
//		rt.anchorMax = anchor;
//		rt.anchoredPosition = anchoredPosition;
	}

	public void SetActiveImage(GameObject obj, bool state) {
		//obj.GetComponent<Image> ().enabled = state;
		UISprite theSprite = obj.GetComponent<UISprite>();
		theSprite.enabled = state;
	}
	
	public void changeLabelColor(GameObject obj, Color newColor) {
		UILabel theLabel = obj.GetComponent<UILabel>();
		theLabel.color = newColor;
		//Text theLabel = obj.GetComponent<Text>();
		//theLabel.color = newColor;
	}
	
	public void changeLabelFontStyle(GameObject obj, FontStyle newFontStyle) {
		UILabel theLabel = obj.GetComponent<UILabel>();
		theLabel.fontStyle = newFontStyle;
		//Text theLabel = obj.GetComponent<Text>();
		//theLabel.fontStyle = newFontStyle;
	}
	
	public void changeLabelFont(GameObject obj, Font newFont) {
		UILabel theLabel = obj.GetComponent<UILabel>();
		theLabel.trueTypeFont = newFont;
		//Text theLabel = obj.GetComponent<Text>();
		//theLabel.font = newFont;
	}

}
