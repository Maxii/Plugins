//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editable text input field.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Input Field")]
public class UIInput : UIWidgetContainer
{
	public delegate char Validator (string currentText, char nextChar);

	public enum KeyboardType
	{
		Default = 0,
		ASCIICapable = 1,
		NumbersAndPunctuation = 2,
		URL = 3,
		NumberPad = 4,
		PhonePad = 5,
		NamePhonePad = 6,
		EmailAddress = 7,
	}

	/// <summary>
	/// Current input, available inside OnSubmit callbacks.
	/// </summary>

	static public UIInput current;

	/// <summary>
	/// Text label modified by this input.
	/// </summary>

	public UILabel label;

	/// <summary>
	/// Maximum number of characters allowed before input no longer works.
	/// </summary>

	public int maxChars = 0;

	/// <summary>
	/// Visual carat character appended to the end of the text when typing.
	/// </summary>

	public string caratChar = "|";

	/// <summary>
	/// Field in player prefs used to automatically save the value.
	/// </summary>

	public string playerPrefsField;

	/// <summary>
	/// Delegate used for validation.
	/// </summary>

	public Validator validator;

	/// <summary>
	/// Type of the touch screen keyboard used on iOS and Android devices.
	/// </summary>

	public KeyboardType type = KeyboardType.Default;

	/// <summary>
	/// Whether this input field should hide its text.
	/// </summary>

	public bool isPassword = false;

	/// <summary>
	/// Whether to use auto-correction on mobile devices.
	/// </summary>

	public bool autoCorrect = false;

	/// <summary>
	/// Whether the label's text value will be used as the input's text value on start.
	/// By default the label is just a tooltip of sorts, letting you choose helpful
	/// half-transparent text such as "Press Enter to start typing", while the actual
	/// value of the input field will remain empty.
	/// </summary>

	public bool useLabelTextAtStart = false;

	/// <summary>
	/// Color of the label when the input field has focus.
	/// </summary>

	public Color activeColor = Color.white;

	/// <summary>
	/// Object to select when Tab key gets pressed.
	/// </summary>

	public GameObject selectOnTab;

	/// <summary>
	/// Callbacks triggered when the input field submits the text.
	/// </summary>

	public List<EventDelegate> onSubmit = new List<EventDelegate>();

	// Deprecated functionality, kept for backwards compatibility
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string functionName = "OnSubmit";

	string mText = "";
	string mDefaultText = "";
	Color mDefaultColor = Color.white;
	UIWidget.Pivot mPivot = UIWidget.Pivot.Left;
	float mPosition = 0f;

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
	TouchScreenKeyboard mKeyboard;
#else
	string mLastIME = "";
#endif

	/// <summary>
	/// Convenience function, for consistency with everything else.
	/// </summary>

	[System.Obsolete("Use UIInput.value instead")]
	public string text { get { return this.value; } set { this.value = value; } }

	/// <summary>
	/// Input field's current text value.
	/// </summary>

	public string value
	{
		get
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return "";
#endif
			if (mDoInit) Init();
			return mText;
		}
		set
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (mDoInit) Init();

			if (mText != value)
			{
				mText = value;
				SaveToPlayerPrefs(mText);
			}

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
			if (mKeyboard != null) mKeyboard.text = value;
#endif
			if (label != null)
			{
				if (string.IsNullOrEmpty(value)) value = mDefaultText;

				label.supportEncoding = false;
				label.text = selected ? value + caratChar : value;
				label.showLastPasswordChar = selected;
				label.color = (selected || value != mDefaultText) ? activeColor : mDefaultColor;
			}
		}
	}

	/// <summary>
	/// Whether the input is currently selected.
	/// </summary>

	public bool selected
	{
		get
		{
			return UICamera.selectedObject == gameObject;
		}
		set
		{
			if (!value && UICamera.selectedObject == gameObject) UICamera.selectedObject = null;
			else if (value) UICamera.selectedObject = gameObject;
		}
	}

	/// <summary>
	/// Set the default text of an input.
	/// </summary>

	public string defaultText
	{
		get
		{
			return mDefaultText;
		}
		set
		{
			if (label.text == mDefaultText) label.text = value;
			mDefaultText = value;
		}
	}

	/// <summary>
	/// Labels used for input shouldn't support color encoding.
	/// </summary>

	protected void Init ()
	{
		if (mDoInit)
		{
			mDoInit = false;
			if (label == null) label = GetComponentInChildren<UILabel>();

			if (label != null)
			{
				if (useLabelTextAtStart) mText = label.text;
				mDefaultText = label.text;
				mDefaultColor = label.color;
				label.supportEncoding = false;
				label.password = isPassword;
				label.maxLineCount = 1;
				mPivot = label.pivot;
				mPosition = label.cachedTransform.localPosition.x;
			}
			else enabled = false;
		}
	}

	/// <summary>
	/// Save the specified value to player prefs.
	/// </summary>

	void SaveToPlayerPrefs (string val)
	{
		if (!string.IsNullOrEmpty(playerPrefsField))
		{
			if (string.IsNullOrEmpty(val))
			{
				PlayerPrefs.DeleteKey(playerPrefsField);
			}
			else
			{
				PlayerPrefs.SetString(playerPrefsField, val);
			}
		}
	}

	bool mDoInit = true;

	void Awake ()
	{
		if (label == null) label = GetComponentInChildren<UILabel>();
		
		if (!string.IsNullOrEmpty(playerPrefsField) && PlayerPrefs.HasKey(playerPrefsField))
		{
			value = PlayerPrefs.GetString(playerPrefsField);
		}
	}

	/// <summary>
	/// Remove legacy functionality.
	/// </summary>

	void Start ()
	{
		if (EventDelegate.IsValid(onSubmit))
		{
			if (eventReceiver != null || !string.IsNullOrEmpty(functionName))
			{
				eventReceiver = null;
				functionName = null;
#if UNITY_EDITOR
				if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
		}
		else if (eventReceiver == null && !EventDelegate.IsValid(onSubmit))
		{
			// Kept for backwards compatibility
			eventReceiver = gameObject;
		}
	}

	/// <summary>
	/// If the object is currently highlighted, it should also be selected.
	/// </summary>

	void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (UICamera.IsHighlighted(gameObject))
			OnSelect(true);
	}

	/// <summary>
	/// Remove the selection.
	/// </summary>

	void OnDisable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (UICamera.IsHighlighted(gameObject))
			OnSelect(false);
	}

	/// <summary>
	/// Selection event, sent by UICamera.
	/// </summary>

	void OnSelect (bool isSelected)
	{
		if (mDoInit) Init();

		if (label != null && enabled && NGUITools.GetActive(gameObject))
		{
			if (isSelected)
			{
				mText = (!useLabelTextAtStart && label.text == mDefaultText) ? "" : label.text;
				label.color = activeColor;
				if (isPassword) label.password = true;

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
				if (Application.platform == RuntimePlatform.IPhonePlayer ||
					Application.platform == RuntimePlatform.Android
#if UNITY_WP8 
					|| Application.platform == RuntimePlatform.WP8Player
#endif
#if UNITY_BLACKBERRY
					|| Application.platform == RuntimePlatform.BB10Player
#endif
)
				{
					if (isPassword)
					{
						mKeyboard = TouchScreenKeyboard.Open(mText, TouchScreenKeyboardType.Default, false, false, true);
					}
					else
					{
						mKeyboard = TouchScreenKeyboard.Open(mText, (TouchScreenKeyboardType)((int)type), autoCorrect);
					}
				}
				else
#endif
				{
					Input.imeCompositionMode = IMECompositionMode.On;
					Input.compositionCursorPos = UICamera.currentCamera.WorldToScreenPoint(label.worldCorners[0]);
				}
				UpdateLabel();
			}
			else
			{
#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
				if (mKeyboard != null)
				{
					mKeyboard.active = false;
				}
#endif
				if (string.IsNullOrEmpty(mText))
				{
					label.text = mDefaultText;
					label.color = mDefaultColor;
					if (isPassword) label.password = false;
				}
				else label.text = mText;

				label.showLastPasswordChar = false;
				Input.imeCompositionMode = IMECompositionMode.Off;
				RestoreLabel();
			}
		}
	}

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
	/// <summary>
	/// Update the text and the label by grabbing it from the iOS/Android keyboard.
	/// </summary>

	void Update()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (mKeyboard != null)
		{
			string val = mKeyboard.text;

			if (mText != val)
			{
				mText = "";

				for (int i = 0; i < val.Length; ++i)
				{
					char ch = val[i];
					if (validator != null) ch = validator(mText, ch);
					if (ch != 0) mText += ch;
				}

				if (maxChars > 0 && mText.Length > maxChars) mText = mText.Substring(0, maxChars);
				UpdateLabel();
				if (mText != val) mKeyboard.text = mText;
				SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);
			}

			if (mKeyboard.done)
			{
				mKeyboard = null;
				Submit();
				selected = false;
			}
		}
	}
#else
	void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (selected)
		{
			if (selectOnTab != null && Input.GetKeyDown(KeyCode.Tab))
			{
				UICamera.selectedObject = selectOnTab;
			}

			// Note: this won't work in the editor. Only in the actual published app. Unity blocks control-keys in the editor.
			if (Input.GetKeyDown(KeyCode.V) &&
#if UNITY_STANDALONE_OSX
				(Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.LeftApple)))
#else
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
#endif
			{
				Append(NGUITools.clipboard);
			}
			
			if (mLastIME != Input.compositionString)
			{
				mLastIME = Input.compositionString;
				UpdateLabel();
			}
		}
	}
#endif

	/// <summary>
	/// Input event, sent by UICamera.
	/// </summary>

	void OnInput (string input)
	{
		if (mDoInit) Init();

		if (selected && enabled && NGUITools.GetActive(gameObject))
		{
			// Mobile devices handle input in Update()
			if (Application.platform == RuntimePlatform.Android) return;
			if (Application.platform == RuntimePlatform.IPhonePlayer) return;
			Append(input);
		}
	}

	/// <summary>
	/// Submit the input field's text.
	/// </summary>

	void Submit ()
	{
		current = this;

		if (EventDelegate.IsValid(onSubmit))
		{
			EventDelegate.Execute(onSubmit);
		}
		else if (eventReceiver != null && !string.IsNullOrEmpty(functionName))
		{
			// Legacy functionality support (for backwards compatibility)
			eventReceiver.SendMessage(functionName, mText, SendMessageOptions.DontRequireReceiver);
		}

		SaveToPlayerPrefs(mText);
		current = null;
	}

	/// <summary>
	/// Append the specified text to the end of the current.
	/// </summary>

	void Append (string input)
	{
		for (int i = 0, imax = input.Length; i < imax; ++i)
		{
			char c = input[i];

			if (c == '\b')
			{
				// Backspace
				if (mText.Length > 0)
				{
					mText = mText.Substring(0, mText.Length - 1);
					SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);
				}
			}
			else if (c == '\r' || c == '\n')
			{
				if (UICamera.current.submitKey0 == KeyCode.Return || UICamera.current.submitKey1 == KeyCode.Return)
				{
					// Not multi-line input, or control isn't held
					if (!label.multiLine || (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)))
					{
						Submit();
						selected = false;
						return;
					}
				}

				// If we have an input validator, validate the input first
				if (validator != null) c = validator(mText, c);

				// If the input is invalid, skip it
				if (c == 0) continue;

				// Append the character
				if (c == '\n' || c == '\r')
				{
					if (label.multiLine) mText += "\n";
				}
				else mText += c;

				// Notify the listeners
				SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);
			}
			else if (c >= ' ')
			{
				// If we have an input validator, validate the input first
				if (validator != null) c = validator(mText, c);

				// If the input is invalid, skip it
				if (c == 0) continue;

				// Append the character and notify the "input changed" listeners.
				mText += c;
				SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);
			}
		}

		// Ensure that we don't exceed the maximum length
		UpdateLabel();
	}

	/// <summary>
	/// Update the visual text label, capping it at maxChars correctly.
	/// </summary>

	void UpdateLabel ()
	{
		if (mDoInit) Init();
		if (maxChars > 0 && mText.Length > maxChars) mText = mText.Substring(0, maxChars);

		if (label.font != null)
		{
			// Start with the text and append the IME composition and carat chars
			string processed;

			if (isPassword && selected)
			{
				processed = "";
				for (int i = 0, imax = mText.Length; i < imax; ++i) processed += "*";
				processed += Input.compositionString + caratChar;
			}
			else processed = selected ? (mText + Input.compositionString + caratChar) : mText;

			// Now wrap this text using the specified line width
			label.supportEncoding = false;

			if (label.overflowMethod == UILabel.Overflow.ClampContent)
			{
				if (label.multiLine)
				{
					label.font.WrapText(processed, out processed, label.width, label.height, 0, false, UIFont.SymbolStyle.None);
				}
				else
				{
					string fit = label.font.GetEndOfLineThatFits(processed, label.width, false, UIFont.SymbolStyle.None);

					if (fit != processed)
					{
						processed = fit;
						Vector3 pos = label.cachedTransform.localPosition;
						pos.x = mPosition + label.width;

						if (mPivot == UIWidget.Pivot.Left) label.pivot = UIWidget.Pivot.Right;
						else if (mPivot == UIWidget.Pivot.TopLeft) label.pivot = UIWidget.Pivot.TopRight;
						else if (mPivot == UIWidget.Pivot.BottomLeft) label.pivot = UIWidget.Pivot.BottomRight;

						label.cachedTransform.localPosition = pos;
					}
					else RestoreLabel();
				}
			}

			// Update the label's visible text
			label.text = processed;
			label.showLastPasswordChar = selected;
		}
	}

	/// <summary>
	/// Restore the input label's pivot point and position.
	/// </summary>

	void RestoreLabel ()
	{
		if (label != null)
		{
			label.pivot = mPivot;
			Vector3 pos = label.cachedTransform.localPosition;
			pos.x = mPosition;
			label.cachedTransform.localPosition = pos;
		}
	}
}
