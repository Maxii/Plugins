using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class WMG_Pie_Graph : WMG_Graph_Manager {

	public bool autoRefresh = true; // Set to false, and then manually call refreshGraph for performance boost

	public float pieSize;

	public enum resizeTypes {none, pixel_padding, percentage_padding}
	public resizeTypes resizeType;

	[System.Flags]
	public enum ResizeProperties {
		FontSizeLegends	 	= 1 << 0,
		LegendEntrySpacing	= 1 << 1,
		ExplodeLength		= 1 << 2,
		LabelExplodeLength	= 1 << 3,
		LegendSwatchSize	= 1 << 4,
		FontSizeLabels		= 1 << 5,
		LegendOffset		= 1 << 6,
		CircleBGoffset		= 1 << 7
	}
	
	[WMG_EnumFlagAttribute]
	public ResizeProperties resizeProperties;

	public Vector2 leftRightPadding;
	public Vector2 topBotPadding;

	public float animationDuration;
	public float sortAnimationDuration;
	public GameObject background;
	public GameObject slicesParent;
	public GameObject backgroundCircle;
	public float bgCircleOffset;
	public WMG_Legend legend;
	public Object legendEntryPrefab;
	public Object nodePrefab;
	public List<float> sliceValues;
	public List<string> sliceLabels;
	public List<Color> sliceColors;
	public enum sortMethod {None, Largest_First, Smallest_First, Alphabetically, Reverse_Alphabetically};
	public sortMethod sortBy;
	public bool swapColorsDuringSort;
	public bool explodeSymmetrical;
	public float explodeLength;
	public float doughnutRadius;
	public WMG_Enums.labelTypes sliceLabelType;
	public float sliceLabelExplodeLength;
	public float sliceLabelFontSize;
	public int numberDecimalsInPercents;
	public bool limitNumberSlices;
	public int maxNumberSlices;
	public bool includeOthers;
	public string includeOthersLabel;
	public Color includeOthersColor;

	public WMG_Data_Source sliceValuesDataSource;
	public WMG_Data_Source sliceLabelsDataSource;
	public WMG_Data_Source sliceColorsDataSource;

	// Original property values for use with dynamic resizing
	private float origWidth;
	private float origHeight;
	private float origPieSize;
	private Vector2 origLeftRightPadding;
	private Vector2 origTopBotPadding;
	private float origExplodeLength;
	private float origSliceLabelExplodeLength;
	private float origSliceLabelFontSize;
	private float origBgCircleOffset;

	// Cache
	private float cachedPieSize;
	private resizeTypes cachedResizeType;
	private ResizeProperties cachedResizeProperties;
	private float cachedContainerWidth;
	private float cachedContainerHeight;
	private Vector2 cachedLeftRightPadding;
	private Vector2 cachedTopBotPadding;
	private float cachedAnimationDuration;
	private float cachedSortAnimationDuration;
	private float cachedBgCircleOffset;
	private List<float> cachedSliceValues = new List<float>();
	private List<string> cachedSliceLabels = new List<string>();
	private List<Color> cachedSliceColors = new List<Color>();
	private sortMethod cachedSortBy;
	private bool cachedSwapColorsDuringSort;
	private bool cachedExplodeSymmetrical;
	private float cachedExplodeLength;
	private WMG_Enums.labelTypes cachedSliceLabelType;
	private float cachedSliceLabelExplodeLength;
	private float cachedSliceLabelFontSize;
	private int cachedNumberDecimalsInPercents;
	private bool cachedLimitNumberSlices;
	private int cachedMaxNumberSlices;
	private bool cachedIncludeOthers;
	private string cachedIncludeOthersLabel;
	private Color cachedIncludeOthersColor;
	private float cachedDoughnutRadius;

	// Changed flags
	private bool graphChanged;
	private bool resizeChanged;
	private bool doughtnutChanged;
	private bool explodeSymmetricalChanged;
	
	private List<GameObject> slices = new List<GameObject>();
	private int numSlices = 0;
	private bool isOtherSlice = false;
	private float otherSliceValue = 0;
	private float totalVal = 0;
	private bool animSortSwap;
	private bool isAnimating;

	// texture variables used for doughnut
	private Color[] colors;
	private Color[] cachedColors;
	private int texSize;
	private int alphaBorderSize;
	private Sprite pieSprite;

	void Start () {
		checkCache(); // Set all cache variables to the current values
		setCacheFlags(true); // Set all cache change flags to true to update everything on start
		if (animationDuration > 0) UpdateVisuals(true);
		setOriginalPropertyValues();
		createTextureData();
	}

	public void setOriginalPropertyValues() {
		origWidth = getSpriteWidth(this.gameObject);
		origHeight = getSpriteHeight(this.gameObject);
		origPieSize = pieSize;
		origLeftRightPadding = leftRightPadding;
		origTopBotPadding = topBotPadding;
		origExplodeLength = explodeLength;
		origSliceLabelExplodeLength = sliceLabelExplodeLength;
		origSliceLabelFontSize = sliceLabelFontSize;
		origBgCircleOffset = bgCircleOffset;
	}

	void checkCache() {
		updateCacheAndFlag<float>(ref cachedPieSize, pieSize, ref graphChanged);
		updateCacheAndFlag<resizeTypes>(ref cachedResizeType, resizeType, ref resizeChanged);
		updateCacheAndFlag<ResizeProperties>(ref cachedResizeProperties, resizeProperties, ref resizeChanged);
		updateCacheAndFlag<float>(ref cachedContainerWidth, getSpriteWidth(this.gameObject), ref resizeChanged);
		updateCacheAndFlag<float>(ref cachedContainerHeight, getSpriteHeight(this.gameObject), ref resizeChanged);
		updateCacheAndFlag<Vector2>(ref cachedLeftRightPadding, leftRightPadding, ref graphChanged);
		updateCacheAndFlag<Vector2>(ref cachedTopBotPadding, topBotPadding, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedAnimationDuration, animationDuration, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedSortAnimationDuration, sortAnimationDuration, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedBgCircleOffset, bgCircleOffset, ref graphChanged);
		updateCacheAndFlagList<float>(ref cachedSliceValues, sliceValues, ref graphChanged);
		updateCacheAndFlagList<string>(ref cachedSliceLabels, sliceLabels, ref graphChanged);
		updateCacheAndFlagList<Color>(ref cachedSliceColors, sliceColors, ref graphChanged);
		updateCacheAndFlag<sortMethod>(ref cachedSortBy, sortBy, ref graphChanged);
		updateCacheAndFlag<bool>(ref cachedSwapColorsDuringSort, swapColorsDuringSort, ref graphChanged);
		updateCacheAndFlag<bool>(ref cachedExplodeSymmetrical, explodeSymmetrical, ref explodeSymmetricalChanged);
		updateCacheAndFlag<float>(ref cachedExplodeLength, explodeLength, ref graphChanged);
		updateCacheAndFlag<WMG_Enums.labelTypes>(ref cachedSliceLabelType, sliceLabelType, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedSliceLabelExplodeLength, sliceLabelExplodeLength, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedSliceLabelFontSize, sliceLabelFontSize, ref graphChanged);
		updateCacheAndFlag<int>(ref cachedNumberDecimalsInPercents, numberDecimalsInPercents, ref graphChanged);
		updateCacheAndFlag<bool>(ref cachedLimitNumberSlices, limitNumberSlices, ref graphChanged);
		updateCacheAndFlag<int>(ref cachedMaxNumberSlices, maxNumberSlices, ref graphChanged);
		updateCacheAndFlag<bool>(ref cachedIncludeOthers, includeOthers, ref graphChanged);
		updateCacheAndFlag<string>(ref cachedIncludeOthersLabel, includeOthersLabel, ref graphChanged);
		updateCacheAndFlag<Color>(ref cachedIncludeOthersColor, includeOthersColor, ref graphChanged);
		updateCacheAndFlag<float>(ref cachedDoughnutRadius, doughnutRadius, ref doughtnutChanged);

		legend.checkCache();
		if (graphChanged) {
			legend.setLegendChanged();
		}
		if (legend.LegendChanged()) {
			graphChanged = true;
		}
	}

	void setCacheFlags(bool val) {
		graphChanged = val;
		resizeChanged = val;
		doughtnutChanged = val;
		explodeSymmetricalChanged = val;

		legend.setCacheFlags(val);
	}

	void Update () {
		if (autoRefresh) {
			Refresh();
		}
	}

	public void Refresh() {
		checkCache(); // Check current vs cached values for all graph and series variables
		refresh(); // Only does stuff if cache changes
		setCacheFlags(false); // Set all cache change flags to false
	}
	
	void refresh() {
		// Update from possible data sources
		UpdateFromDataSources();

		// Update from container size
		UpdateFromContainer();

		// Update textures based on doughnut radius
		UpdateDoughnut();

		// Update explode symmetrical
		UpdateExplodeSymmetrical();

		// Sets the colors, fill amount, and rotation of the pie slices, as well as set labels for pie slices
		if (!isAnimating && !resizeChanged && graphChanged) {
			UpdateVisuals(false);
		}

		// Update legend position
		legend.updateLegend();
	}

	void UpdateFromDataSources() {
		if (sliceValuesDataSource != null) {
			sliceValues = sliceValuesDataSource.getData<float>();
		}
		if (sliceLabelsDataSource != null) {
			sliceLabels = sliceLabelsDataSource.getData<string>();
		}
		if (sliceColorsDataSource != null) {
			sliceColors = sliceColorsDataSource.getData<Color>();
		}
		if (sliceValuesDataSource != null || sliceLabelsDataSource != null || sliceColorsDataSource != null) {
			if (sortBy != sortMethod.None) sortData();
		}
	}

	void UpdateExplodeSymmetrical() {
		if (explodeSymmetricalChanged) {
			graphChanged = true;
			for (int i = 0; i < numSlices; i++) {
				WMG_Pie_Graph_Slice pieSlice =  slices[i].GetComponent<WMG_Pie_Graph_Slice>();
				changeExplodeSymmetrical(pieSlice);
			}
		}
	}

	void changeExplodeSymmetrical(WMG_Pie_Graph_Slice pieSlice) {
		SetActive(pieSlice.objectToMask, explodeSymmetrical);
		if (explodeSymmetrical) {
			changeSpriteParent(pieSlice.objectToColor, pieSlice.objectToMask);
		}
		else {
			changeSpriteParent(pieSlice.objectToColor, pieSlice.gameObject);
			bringSpriteToFront(pieSlice.objectToLabel);
		}
	}

	void UpdateFromContainer() {
		if (resizeChanged) {
			if (resizeType != resizeTypes.none) {
				// Adjust background padding for percentage padding, otherwise keep padding in pixels
				if (resizeType == resizeTypes.percentage_padding) {
					leftRightPadding = new Vector2 (origLeftRightPadding.x * cachedContainerWidth / origWidth, origLeftRightPadding.y * cachedContainerWidth / origWidth);
					topBotPadding = new Vector2 (origTopBotPadding.x * cachedContainerHeight / origHeight, origTopBotPadding.y * cachedContainerHeight / origHeight);
				}

				pieSize = Mathf.Min(cachedContainerWidth - leftRightPadding.x - leftRightPadding.y, cachedContainerHeight - topBotPadding.x - topBotPadding.y);

				float smallerFactor = pieSize / origPieSize;

				if ((resizeProperties & ResizeProperties.FontSizeLegends) == ResizeProperties.FontSizeLegends) {
					legend.legendEntryFontSize = smallerFactor * legend.origLegendEntryFontSize;
				}
				if ((resizeProperties & ResizeProperties.LegendEntrySpacing) == ResizeProperties.LegendEntrySpacing) {
					legend.legendEntryWidth = smallerFactor * legend.origLegendEntryWidth;
					legend.legendEntryHeight = smallerFactor * legend.origLegendEntryHeight;
					legend.legendEntrySpacing = smallerFactor * legend.origLegendEntrySpacing;
				}
				if ((resizeProperties & ResizeProperties.ExplodeLength) == ResizeProperties.ExplodeLength) {
					explodeLength = smallerFactor * origExplodeLength;
				}
				if ((resizeProperties & ResizeProperties.LabelExplodeLength) == ResizeProperties.LabelExplodeLength) {
					sliceLabelExplodeLength = smallerFactor * origSliceLabelExplodeLength;
				}
				if ((resizeProperties & ResizeProperties.LegendSwatchSize) == ResizeProperties.LegendSwatchSize) {
					legend.pieSwatchSize = smallerFactor * legend.origPieSwatchSize;
				}
				if ((resizeProperties & ResizeProperties.FontSizeLabels) == ResizeProperties.FontSizeLabels) {
					sliceLabelFontSize = smallerFactor * origSliceLabelFontSize;
				}
				if ((resizeProperties & ResizeProperties.LegendOffset) == ResizeProperties.LegendOffset) {
					legend.offsetX = smallerFactor * legend.origOffsetX;
					legend.offsetY = smallerFactor * legend.origOffsetY;
				}
				if ((resizeProperties & ResizeProperties.CircleBGoffset) == ResizeProperties.CircleBGoffset) {
					bgCircleOffset = smallerFactor * origBgCircleOffset;
				}
				UpdateVisuals(true);
			}
		}
	}

	public Vector2 getPaddingOffset() {
		return new Vector2(-leftRightPadding.x/2f + leftRightPadding.y/2f, topBotPadding.x/2f - topBotPadding.y/2f);
	}

	public List<GameObject> getSlices() {
		return slices;
	}

	void UpdateData() {
		// Find the total number of slices
		isOtherSlice = false;
		numSlices = sliceValues.Count;
		if (limitNumberSlices) {
			if (numSlices > maxNumberSlices) {
				numSlices = maxNumberSlices;
				if (includeOthers) {
					isOtherSlice = true;
					numSlices++;
				}
			}
		}
		
		// Find Other Slice Value and Total Value
		otherSliceValue = 0;
		totalVal = 0;
		for (int i = 0; i < sliceValues.Count; i++) {
			totalVal += sliceValues[i];
			if (isOtherSlice && i >= maxNumberSlices) {
				otherSliceValue += sliceValues[i];
			}
			if (limitNumberSlices && !isOtherSlice && i >= maxNumberSlices) {
				totalVal -= sliceValues[i];
			}
		}
	}

	void CreateOrDeleteSlicesBasedOnValues() {
		// Create pie slices based on sliceValues data
		for (int i = 0; i < numSlices; i++) {
			if (sliceLabels.Count <= i) sliceLabels.Add("");
			if (sliceColors.Count <= i) sliceColors.Add(Color.white);
			if (slices.Count <= i) {
				GameObject curObj = CreateNode(nodePrefab, slicesParent);
				slices.Add(curObj);
				WMG_Pie_Graph_Slice pieSlice = curObj.GetComponent<WMG_Pie_Graph_Slice>();
				setTexture(pieSlice.objectToColor, pieSprite);
				setTexture(pieSlice.objectToMask, pieSprite);
				changeExplodeSymmetrical(pieSlice);
			}
			if (legend.legendEntries.Count <= i) {
				legend.createLegendEntry(legendEntryPrefab);
			}
		}
		for (int i = slices.Count - 1; i >= 0; i--) {
			if (slices[i] != null && i >= numSlices) {
				WMG_Pie_Graph_Slice theSlice = slices[i].GetComponent<WMG_Pie_Graph_Slice>();
				DeleteNode(theSlice);
				slices.RemoveAt(i);
			}
		}
		
		// If there are more sliceLegendEntries or slices than sliceValues data, delete the extras
		for (int i = legend.legendEntries.Count - 1; i >= 0; i--) {
			if (legend.legendEntries[i] != null && i >= numSlices) {
				legend.deleteLegendEntry(i);
			}
		}
	}
	
	void UpdateVisuals(bool noAnim) {
		// Update internal bookkeeping variables
		UpdateData();

		// Creates and deletes slices and slice legend objects based on the slice values
		CreateOrDeleteSlicesBasedOnValues();

		// Update background
		changeSpriteHeight(background, Mathf.RoundToInt(pieSize + topBotPadding.x + topBotPadding.y));
		changeSpriteWidth(background, Mathf.RoundToInt(pieSize + leftRightPadding.x + leftRightPadding.y));
		changeSpriteSize(backgroundCircle, Mathf.RoundToInt(pieSize + bgCircleOffset), Mathf.RoundToInt(pieSize + bgCircleOffset));
		Vector2 offset = getPaddingOffset();
		changeSpritePositionTo(slicesParent, new Vector3(-offset.x, -offset.y));

		if (animationDuration == 0 && sortBy != sortMethod.None) sortData();
		float curTotalRot = 0;
		if (!noAnim) animSortSwap = false; // Needed because if sortAnimationDuration = 0, nothing sets animSortSwap to false
		for (int i = 0; i < numSlices; i++) {
			WMG_Legend_Entry entry = legend.legendEntries[i];
			// Update Pie Slices
			float newAngle =  -1 * curTotalRot;
			if (newAngle < 0) newAngle += 360;
			WMG_Pie_Graph_Slice pieSlice =  slices[i].GetComponent<WMG_Pie_Graph_Slice>();
			if (sliceLabelType != WMG_Enums.labelTypes.None && !activeInHierarchy(pieSlice.objectToLabel)) SetActive(pieSlice.objectToLabel,true);
			if (sliceLabelType == WMG_Enums.labelTypes.None && activeInHierarchy(pieSlice.objectToLabel)) SetActive(pieSlice.objectToLabel,false);

			if (!explodeSymmetrical) {
				changeSpriteSize(pieSlice.objectToColor, Mathf.RoundToInt(pieSize), Mathf.RoundToInt(pieSize));
			}
			else {
				int newSize = Mathf.RoundToInt(pieSize + explodeLength*2);
				changeSpriteSize(pieSlice.objectToColor, newSize, newSize);
				changeSpriteSize(pieSlice.objectToMask, newSize + Mathf.RoundToInt(explodeLength*4), newSize + Mathf.RoundToInt(explodeLength*4));
			}

			// Set Slice Data and maybe Other Slice Data
			Color sliceColor = sliceColors[i];
			string sliceLabel = sliceLabels[i];
			float sliceValue = sliceValues[i];
			if (isOtherSlice && i == numSlices - 1) {
				sliceColor = includeOthersColor;
				sliceLabel = includeOthersLabel;
				sliceValue = otherSliceValue;
			}

			// Hide if 0
			if (sliceValue == 0) {
				SetActive(pieSlice.objectToLabel, false);
			}

			float slicePercent = sliceValue / totalVal;
			float afterExplodeAngle = newAngle * -1 + 0.5f * slicePercent * 360;
			float sliceLabelRadius = sliceLabelExplodeLength + pieSize / 2;
			float sin = Mathf.Sin(afterExplodeAngle * Mathf.Deg2Rad);
			float cos = Mathf.Cos(afterExplodeAngle * Mathf.Deg2Rad);

			if (!noAnim && animationDuration > 0) {
				isAnimating = true;
				WMG_Anim.animFill(pieSlice.objectToColor, animationDuration, Ease.Linear, slicePercent);
				WMG_Anim.animPosition(pieSlice.objectToLabel, animationDuration, Ease.Linear, new Vector3(sliceLabelRadius * sin, 
				                                                                                              sliceLabelRadius * cos));
				int newI = i;
				WMG_Anim.animPositionCallbackC(slices[i], animationDuration, Ease.Linear, new Vector3(explodeLength * sin, 
				                                                                                          explodeLength * cos), ()=> shrinkSlices(newI));
				if (!explodeSymmetrical) {
					WMG_Anim.animRotation(pieSlice.objectToColor, animationDuration, Ease.Linear, new Vector3(0, 0, newAngle), false);
					WMG_Anim.animPosition(pieSlice.objectToColor, animationDuration, Ease.Linear, Vector3.zero);
				}
				else {
					WMG_Anim.animRotation(pieSlice.objectToColor, animationDuration, Ease.Linear, Vector3.zero, false);
					Vector2 newPos = new Vector2(-explodeLength * sin, -explodeLength * cos);
					float sin2 = Mathf.Sin(newAngle * Mathf.Deg2Rad);
					float cos2 = Mathf.Cos(newAngle * Mathf.Deg2Rad);
					WMG_Anim.animPosition(pieSlice.objectToColor, animationDuration, Ease.Linear, new Vector3( cos2 * newPos.x + sin2 * newPos.y, cos2 * newPos.y - sin2 * newPos.x));
					// Mask
					WMG_Anim.animRotation(pieSlice.objectToMask, animationDuration, Ease.Linear, new Vector3(0, 0, newAngle), false);
					WMG_Anim.animFill(pieSlice.objectToMask, animationDuration, Ease.Linear, slicePercent);
				}
			}
			else {
				changeSpriteFill(pieSlice.objectToColor, slicePercent);
				pieSlice.objectToLabel.transform.localPosition = new Vector3(sliceLabelRadius * sin, 
				                                                             sliceLabelRadius * cos);
				slices[i].transform.localPosition =  new Vector3(explodeLength * sin, 
				                                                 explodeLength * cos);
				if (!explodeSymmetrical) {
					pieSlice.objectToColor.transform.localEulerAngles = new Vector3(0, 0, newAngle);
					pieSlice.objectToColor.transform.localPosition = Vector3.zero;
				}
				else {
					pieSlice.objectToColor.transform.localEulerAngles = Vector3.zero;
					Vector2 newPos = new Vector2(-explodeLength * sin, -explodeLength * cos);
					float sin2 = Mathf.Sin(newAngle * Mathf.Deg2Rad);
					float cos2 = Mathf.Cos(newAngle * Mathf.Deg2Rad);
					pieSlice.objectToColor.transform.localPosition = new Vector3( cos2 * newPos.x + sin2 * newPos.y, cos2 * newPos.y - sin2 * newPos.x);
					// Mask
					pieSlice.objectToMask.transform.localEulerAngles = new Vector3(0, 0, newAngle);
					changeSpriteFill(pieSlice.objectToMask, slicePercent);
				}
			}

			// Update slice color
			changeSpriteColor(pieSlice.objectToColor, sliceColor);
			changeSpriteColor(pieSlice.objectToMask, sliceColor);

			// Update slice labels
			changeLabelText(pieSlice.objectToLabel, getLabelText(sliceLabel, sliceLabelType, sliceValue, slicePercent, numberDecimalsInPercents));
			pieSlice.objectToLabel.transform.localScale = new Vector3(sliceLabelFontSize, sliceLabelFontSize);

			// Update Gameobject names
			slices[i].name = sliceLabel;
			legend.legendEntries[i].name = sliceLabel;

			// Update legend
			changeLabelText(entry.label, getLabelText(sliceLabel, legend.labelType, sliceValue, slicePercent, legend.numDecimals));
			changeSpriteColor(entry.swatchNode, sliceColor);
			
			// Hide legend if 0
			if (sliceValue == 0) {
				SetActive(entry.gameObject, false);
			}
			else {
				SetActive(entry.gameObject, true);
			}
			
			curTotalRot += slicePercent * 360;
		}
	}

	void shrinkSlices(int sliceNum) {
		if (!animSortSwap && sortBy != sortMethod.None) animSortSwap = sortData();
		if (animSortSwap) {
			if (sortAnimationDuration > 0) {
				WMG_Anim.animScaleCallbackC(slices[sliceNum], sortAnimationDuration / 2, Ease.Linear, Vector3.zero, ()=> enlargeSlices(sliceNum));
			}
			else {
				isAnimating = false;
				UpdateVisuals(true);
			}
		}
		else {
			isAnimating = false;
		}
	}

	void enlargeSlices(int sliceNum) {
		if (sliceNum == 0) {
			UpdateVisuals(true);
		}
		WMG_Anim.animScaleCallbackC(slices[sliceNum], sortAnimationDuration / 2, Ease.Linear, Vector3.one, ()=> endSortAnimating(sliceNum));
	}

	void endSortAnimating(int sliceNum) {
		if (sliceNum == numSlices - 1) {
			animSortSwap = false;
			isAnimating = false;
		}
	}
	
	bool sortData() {
		bool wasASwap = false;
		bool flag = true;
		bool shouldSwap = false;
		float temp;
		string tempL;
		GameObject tempGo;
		int numLength = numSlices;
		for (int i = 1; (i <= numLength) && flag; i++) {
			flag = false;
			for (int j = 0; j < (numLength - 1); j++ ) {
				shouldSwap = false;
				if (sortBy == sortMethod.Largest_First) {
					if (sliceValues[j+1] > sliceValues[j]) shouldSwap = true;
				}
				else if (sortBy == sortMethod.Smallest_First) {
					if (sliceValues[j+1] < sliceValues[j]) shouldSwap = true;
				}
				else if (sortBy == sortMethod.Alphabetically) {
					if (sliceLabels[j+1].CompareTo(sliceLabels[j]) == -1) shouldSwap = true;
				}
				else if (sortBy == sortMethod.Reverse_Alphabetically) {
					if (sliceLabels[j+1].CompareTo(sliceLabels[j]) == 1) shouldSwap = true;
				}
				if (shouldSwap) {
					// Swap values
					temp = sliceValues[j];
					sliceValues[j] = sliceValues[j+1];
					sliceValues[j+1] = temp;
					// Swap labels
					tempL = sliceLabels[j];
					sliceLabels[j] = sliceLabels[j+1];
					sliceLabels[j+1] = tempL;
					// Swap Slices
					tempGo = slices[j];
					slices[j] = slices[j+1];
					slices[j+1] = tempGo;
					// Swap Colors
					if (swapColorsDuringSort) {
						Color tempC = sliceColors[j];
						sliceColors[j] = sliceColors[j+1];
						sliceColors[j+1] = tempC;
					}
					flag = true;
					wasASwap = true;
				}
			}
		}
		return wasASwap;
	}



	void UpdateDoughnut() {
		if (doughtnutChanged) {
			updateTexColors();
			pieSprite.texture.SetPixels(colors);
			pieSprite.texture.Apply();
		}
	}

	void createTextureData() {
		GameObject temp = GameObject.Instantiate(nodePrefab) as GameObject;
		Texture2D origTex = getTexture(temp.GetComponent<WMG_Pie_Graph_Slice>().objectToColor);
		pieSprite = WMG_Util.createSprite(origTex.width, origTex.height);
		Destroy(temp);
		texSize = pieSprite.texture.width;
		colors = new Color[texSize*texSize];
		cachedColors = pieSprite.texture.GetPixels();
		// get alphaBorderSize, used to determine maximum doughnut radius
		Color[] tempC = pieSprite.texture.GetPixels(texSize/2, 0, 1, texSize/2);
		for (int i = 0; i < tempC.Length; i++) {
			if (tempC[i].a != 0) {
				alphaBorderSize = i+5;
				break;
			}
		}
	}

	void updateTexColors() {
		for (int i = 0; i < texSize; i++) {
			for (int j = 0; j < texSize; j++) {
				int centerX = i - texSize / 2;
				int centerY = j - texSize / 2;
				float dist = Mathf.Sqrt(centerX * centerX + centerY * centerY);
				if (dist < doughnutRadius && dist < texSize/2 - alphaBorderSize) {
					colors[i + texSize * j] = new Color(1, 1, 1, 0);
				}
				else {
					colors[i + texSize * j] = cachedColors[i + texSize * j];
				}
			}
		}
	}
}
