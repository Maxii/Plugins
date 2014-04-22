//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY)
#define MOBILE
#endif

using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Input field makes it possible to enter custom information within the UI.
/// </summary>

[AddComponentMenu("NGUI/UI/Input Field")]
public class UIInput : MonoBehaviour
{
	public enum InputType
	{
		Standard,
		AutoCorrect,
		Password,
	}

	public enum Validation
	{
		None,
		Integer,
		Float,
		Alphanumeric,
		Username,
		Name,
	}

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

	public delegate char OnValidate (string text, int charIndex, char addedChar);

	/// <summary>
	/// Currently active input field. Only valid during callbacks.
	/// </summary>

	static public UIInput current;

	/// <summary>
	/// Currently selected input field, if any.
	/// </summary>

	static public UIInput selection;

	/// <summary>
	/// Text label used to display the input's value.
	/// </summary>

	public UILabel label;

	/// <summary>
	/// Type of data expected by the input field.
	/// </summary>

	public InputType inputType = InputType.Standard;

	/// <summary>
	/// Keyboard type applies to mobile keyboards that get shown.
	/// </summary>

	public KeyboardType keyboardType = KeyboardType.Default;

	/// <summary>
	/// What kind of validation to use with the input field's data.
	/// </summary>

	public Validation validation = Validation.None;

	/// <summary>
	/// Maximum number of characters allowed before input no longer works.
	/// </summary>

	public int characterLimit = 0; 

	/// <summary>
	/// Field in player prefs used to automatically save the value.
	/// </summary>

	public string savedAs;

	/// <summary>
	/// Object to select when Tab key gets pressed.
	/// </summary>

	public GameObject selectOnTab;

	/// <summary>
	/// Color of the label when the input field has focus.
	/// </summary>

	public Color activeTextColor = Color.white;

	/// <summary>
	/// Color used by the caret symbol.
	/// </summary>

	public Color caretColor = new Color(1f, 1f, 1f, 0.8f);

	/// <summary>
	/// Color used by the selection rectangle.
	/// </summary>

	public Color selectionColor = new Color(1f, 223f / 255f, 141f / 255f, 0.5f);

	/// <summary>
	/// Event delegates triggered when the input field submits its data.
	/// </summary>

	public List<EventDelegate> onSubmit = new List<EventDelegate>();

	/// <summary>
	/// Event delegates triggered when the input field's text changes for any reason.
	/// </summary>

	public List<EventDelegate> onChange = new List<EventDelegate>();

	/// <summary>
	/// Custom validation callback.
	/// </summary>

	public OnValidate onValidate;

	/// <summary>
	/// Input field's value.
	/// </summary>

	[SerializeField][HideInInspector] protected string mValue;

	protected string mDefaultText = "";
	protected Color mDefaultColor = Color.white;
	protected float mPosition = 0f;
	protected bool mDoInit = true;
	protected UIWidget.Pivot mPivot = UIWidget.Pivot.TopLeft;

	static protected int mDrawStart = 0;

#if MOBILE
	static protected TouchScreenKeyboard mKeyboard;
#else
	protected int mSelectionStart = 0;
	protected int mSelectionEnd = 0;
	protected UITexture mHighlight = null;
	protected UITexture mCaret = null;
	protected Texture2D mBlankTex = null;
	protected float mNextBlink = 0f;

	static protected string mLastIME = "";
#endif

	/// <summary>
	/// Default text used by the input's label.
	/// </summary>

	public string defaultText
	{
		get
		{
			return mDefaultText;
		}
		set
		{
			if (mDoInit) Init();
			mDefaultText = value;
			UpdateLabel();
		}
	}

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
			return mValue;
		}
		set
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (mDoInit) Init();
			mDrawStart = 0;

#if MOBILE && !UNITY_3_5
			// BB10's implementation has a bug in Unity
			if (Application.platform == RuntimePlatform.BB10Player)
				value = value.Replace("\\b", "\b");
#endif
			// Validate all input
			value = Validate(value);
#if MOBILE
			if (isSelected && mKeyboard != null && mCached != value)
			{
				mKeyboard.text = value;
				mCached = value;
			}

			if (mValue != value)
			{
				mValue = value;
				if (!isSelected) SaveToPlayerPrefs(value);
				UpdateLabel();
				ExecuteOnChange();
			}
#else
			if (mValue != value)
			{
				mValue = value;

				if (isSelected)
				{
					if (string.IsNullOrEmpty(value))
					{
						mSelectionStart = 0;
						mSelectionEnd = 0;
					}
					else
					{
						mSelectionStart = value.Length;
						mSelectionEnd = mSelectionStart;
					}
				}
				else SaveToPlayerPrefs(value);

				UpdateLabel();
				ExecuteOnChange();
			}
#endif
		}
	}

	[System.Obsolete("Use UIInput.isSelected instead")]
	public bool selected { get { return isSelected; } set { isSelected = value; } }

	/// <summary>
	/// Whether the input is currently selected.
	/// </summary>

	public bool isSelected
	{
		get
		{
			return selection == this;
		}
		set
		{
			if (!value) { if (isSelected) UICamera.selectedObject = null; }
			else UICamera.selectedObject = gameObject;
		}
	}

	/// <summary>
	/// Current position of the cursor.
	/// </summary>

#if MOBILE
	protected int cursorPosition { get { return value.Length; } }
#else
	protected int cursorPosition { get { return isSelected ? mSelectionEnd : value.Length; } }
#endif

	/// <summary>
	/// Validate the specified text, returning the validated version.
	/// </summary>

	public string Validate (string val)
	{
		if (string.IsNullOrEmpty(val)) return "";

		StringBuilder sb = new StringBuilder(val.Length);

		for (int i = 0; i < val.Length; ++i)
		{
			char c = val[i];
			if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
			else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
			if (c != 0) sb.Append(c);
		}

		if (characterLimit > 0 && sb.Length > characterLimit)
			return sb.ToString(0, characterLimit);
		return sb.ToString();
	}

	/// <summary>
	/// Automatically set the value by loading it from player prefs if possible.
	/// </summary>

	void Start ()
	{
		if (string.IsNullOrEmpty(mValue))
		{
			if (!string.IsNullOrEmpty(savedAs) && PlayerPrefs.HasKey(savedAs))
				value = PlayerPrefs.GetString(savedAs);
		}
		else value = mValue.Replace("\\n", "\n");
	}

	/// <summary>
	/// Labels used for input shouldn't support rich text.
	/// </summary>

	protected void Init ()
	{
		if (mDoInit && label != null)
		{
			mDoInit = false;
			mDefaultText = label.text;
			mDefaultColor = label.color;
			label.supportEncoding = false;

			if (label.alignment == NGUIText.Alignment.Justified)
			{
				label.alignment = NGUIText.Alignment.Left;
				Debug.LogWarning("Input fields using labels with justified alignment are not supported at this time", this);
			}

			mPivot = label.pivot;
			mPosition = label.cachedTransform.localPosition.x;
			UpdateLabel();
		}
	}

	/// <summary>
	/// Save the specified value to player prefs.
	/// </summary>

	protected void SaveToPlayerPrefs (string val)
	{
		if (!string.IsNullOrEmpty(savedAs))
		{
			if (string.IsNullOrEmpty(val)) PlayerPrefs.DeleteKey(savedAs);
			else PlayerPrefs.SetString(savedAs, val);
		}
	}

	/// <summary>
	/// Selection event, sent by the EventSystem.
	/// </summary>

	protected virtual void OnSelect (bool isSelected)
	{
		if (isSelected) OnSelectEvent();
		else OnDeselectEvent();
	}

	/// <summary>
	/// Notification of the input field gaining selection.
	/// </summary>

	protected void OnSelectEvent ()
	{
		selection = this;

		if (mDoInit) Init();

		if (label != null && NGUITools.GetActive(this))
		{
			label.color = activeTextColor;
#if MOBILE
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
				mKeyboard = (inputType == InputType.Password) ?
					TouchScreenKeyboard.Open(mValue, TouchScreenKeyboardType.Default, false, false, true) :
					TouchScreenKeyboard.Open(mValue, (TouchScreenKeyboardType)((int)keyboardType), inputType == InputType.AutoCorrect, label.multiLine, false, false, defaultText);
			}
			else
#endif
			{
				Vector2 pos = (UICamera.current != null && UICamera.current.cachedCamera != null) ?
					UICamera.current.cachedCamera.WorldToScreenPoint(label.worldCorners[0]) :
					label.worldCorners[0];
				pos.y = Screen.height - pos.y;
				Input.imeCompositionMode = IMECompositionMode.On;
				Input.compositionCursorPos = pos;
#if !MOBILE
				mSelectionStart = 0;
				mSelectionEnd = string.IsNullOrEmpty(mValue) ? 0 : mValue.Length;
#endif
				mDrawStart = 0;
			}
			UpdateLabel();
		}
	}

	/// <summary>
	/// Notification of the input field losing selection.
	/// </summary>

	protected void OnDeselectEvent ()
	{
		if (mDoInit) Init();

		if (label != null && NGUITools.GetActive(this))
		{
			mValue = value;
#if MOBILE
			if (mKeyboard != null)
			{
				mKeyboard.active = false;
				mKeyboard = null;
			}
#endif
			if (string.IsNullOrEmpty(mValue))
			{
				label.text = mDefaultText;
				label.color = mDefaultColor;
			}
			else label.text = mValue;

			Input.imeCompositionMode = IMECompositionMode.Auto;
			RestoreLabelPivot();
		}
		
		selection = null;
		UpdateLabel();
	}

	/// <summary>
	/// Update the text based on input.
	/// </summary>

#if MOBILE
	string mCached = "";

	void Update()
	{
		if (mKeyboard != null && isSelected)
		{
			string text = mKeyboard.text;

			if (mCached != text)
			{
				mCached = text;
				value = text;
			}

			if (mKeyboard.done)
			{
#if !UNITY_3_5
				if (!mKeyboard.wasCanceled)
#endif
					Submit();
				mKeyboard = null;
				isSelected = false;
				mCached = "";
			}
		}
	}
#else
	void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (isSelected)
		{
			if (mDoInit) Init();

			if (selectOnTab != null && Input.GetKeyDown(KeyCode.Tab))
			{
				UICamera.selectedObject = selectOnTab;
				return;
			}

			string ime = Input.compositionString;

			// There seems to be an inconsistency between IME on Windows, and IME on OSX.
			// On Windows, Input.inputString is always empty while IME is active. On the OSX it is not.
			if (string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(Input.inputString))
			{
				// Process input ignoring non-printable characters as they are not consistent.
				// Windows has them, OSX may not. They get handled inside OnGUI() instead.
				string s = Input.inputString;

				for (int i = 0; i < s.Length; ++i)
				{
					char ch = s[i];
					if (ch >= ' ') Insert(ch.ToString());
				}
			}

			// Append IME composition
			if (mLastIME != ime)
			{
				mSelectionEnd = string.IsNullOrEmpty(ime) ? mSelectionStart : mValue.Length + ime.Length;
				mLastIME = ime;
				UpdateLabel();
				ExecuteOnChange();
			}

			// Blink the caret
			if (mCaret != null && mNextBlink < RealTime.time)
			{
				mNextBlink = RealTime.time + 0.5f;
				mCaret.enabled = !mCaret.enabled;
			}
		}
	}

	/// <summary>
	/// Unfortunately Unity 4.3 and earlier doesn't offer a way to properly process events outside of OnGUI.
	/// </summary>

	void OnGUI ()
	{
		if (isSelected && Event.current.rawType == EventType.KeyDown)
			ProcessEvent(Event.current);
	}

	/// <summary>
	/// Handle the specified event.
	/// </summary>

	bool ProcessEvent (Event ev)
	{
		if (label == null) return false;

		RuntimePlatform rp = Application.platform;

		bool isMac = (
			rp == RuntimePlatform.OSXEditor ||
			rp == RuntimePlatform.OSXPlayer ||
			rp == RuntimePlatform.OSXWebPlayer);

		bool ctrl = isMac ?
			((ev.modifiers & EventModifiers.Command) != 0) :
			((ev.modifiers & EventModifiers.Control) != 0);

		bool shift = ((ev.modifiers & EventModifiers.Shift) != 0);

		switch (ev.keyCode)
		{
			case KeyCode.Backspace:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					if (mSelectionStart == mSelectionEnd)
					{
						if (mSelectionStart < 1) return true;
						--mSelectionEnd;
					}
					Insert("");
				}
				return true;
			}

			case KeyCode.Delete:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					if (mSelectionStart == mSelectionEnd)
					{
						if (mSelectionStart >= mValue.Length) return true;
						++mSelectionEnd;
					}
					Insert("");
				}
				return true;
			}

			case KeyCode.LeftArrow:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = Mathf.Max(mSelectionEnd - 1, 0);
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.RightArrow:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = Mathf.Min(mSelectionEnd + 1, mValue.Length);
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.PageUp:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = 0;
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.PageDown:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = mValue.Length;
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.Home:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					if (label.multiLine)
					{
						mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.Home);
					}
					else mSelectionEnd = 0;

					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.End:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					if (label.multiLine)
					{
						mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.End);
					}
					else mSelectionEnd = mValue.Length;

					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.UpArrow:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.UpArrow);
					if (mSelectionEnd != 0) mSelectionEnd += mDrawStart;
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			case KeyCode.DownArrow:
			{
				ev.Use();

				if (!string.IsNullOrEmpty(mValue))
				{
					mSelectionEnd = label.GetCharacterIndex(mSelectionEnd, KeyCode.DownArrow);
					if (mSelectionEnd != label.processedText.Length) mSelectionEnd += mDrawStart;
					else mSelectionEnd = mValue.Length;
					if (!shift) mSelectionStart = mSelectionEnd;
					UpdateLabel();
				}
				return true;
			}

			// Copy
			case KeyCode.C:
			{
				if (ctrl)
				{
					ev.Use();
					NGUITools.clipboard = GetSelection();
				}
				return true;
			}

			// Paste
			case KeyCode.V:
			{
				if (ctrl)
				{
					ev.Use();
					Insert(NGUITools.clipboard);
				}
				return true;
			}

			// Cut
			case KeyCode.X:
			{
				if (ctrl)
				{
					ev.Use();
					NGUITools.clipboard = GetSelection();
					Insert("");
				}
				return true;
			}

			// Submit
			case KeyCode.Return:
			case KeyCode.KeypadEnter:
			{
				ev.Use();
				
				if (label.multiLine && !ctrl && label.overflowMethod != UILabel.Overflow.ClampContent)
				{
					Insert("\n");
				}
				else
				{
					UICamera.currentScheme = UICamera.ControlScheme.Controller;
					UICamera.currentKey = ev.keyCode;
					Submit();
					UICamera.currentKey = KeyCode.None;
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Insert the specified text string into the current input value, respecting selection and validation.
	/// </summary>

	protected virtual void Insert (string text)
	{
		string left = GetLeftText();
		string right = GetRightText();
		int rl = right.Length;

		StringBuilder sb = new StringBuilder(left.Length + right.Length + text.Length);
		sb.Append(left);

		// Append the new text
		for (int i = 0, imax = text.Length; i < imax; ++i)
		{
			// Can't go past the character limit
			if (characterLimit > 0 && sb.Length + rl >= characterLimit) break;

			// If we have an input validator, validate the input first
			char c = text[i];
			if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
			else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);

			// Append the character if it hasn't been invalidated
			if (c != 0) sb.Append(c);
		}

		// Advance the selection
		mSelectionStart = sb.Length;
		mSelectionEnd = mSelectionStart;

		// Append the text that follows it, ensuring that it's also validated after the inserted value
		for (int i = 0, imax = right.Length; i < imax; ++i)
		{
			char c = right[i];
			if (onValidate != null) c = onValidate(sb.ToString(), sb.Length, c);
			else if (validation != Validation.None) c = Validate(sb.ToString(), sb.Length, c);
			if (c != 0) sb.Append(c);
		}

		mValue = sb.ToString();
		UpdateLabel();
		ExecuteOnChange();
	}

	/// <summary>
	/// Get the text to the left of the selection.
	/// </summary>

	protected string GetLeftText ()
	{
		int min = Mathf.Min(mSelectionStart, mSelectionEnd);
		return (string.IsNullOrEmpty(mValue) || min < 0) ? "" : mValue.Substring(0, min);
	}

	/// <summary>
	/// Get the text to the right of the selection.
	/// </summary>

	protected string GetRightText ()
	{
		int max = Mathf.Max(mSelectionStart, mSelectionEnd);
		return (string.IsNullOrEmpty(mValue) || max >= mValue.Length) ? "" : mValue.Substring(max);
	}

	/// <summary>
	/// Get currently selected text.
	/// </summary>

	protected string GetSelection ()
	{
		if (string.IsNullOrEmpty(mValue) || mSelectionStart == mSelectionEnd)
		{
			return "";
		}
		else
		{
			int min = Mathf.Min(mSelectionStart, mSelectionEnd);
			int max = Mathf.Max(mSelectionStart, mSelectionEnd);
			return mValue.Substring(min, max - min);
		}
	}

	/// <summary>
	/// Helper function that retrieves the index of the character under the mouse.
	/// </summary>

	protected int GetCharUnderMouse ()
	{
		Vector3[] corners = label.worldCorners;
		Ray ray = UICamera.currentRay;
		Plane p = new Plane(corners[0], corners[1], corners[2]);
		float dist;
		return p.Raycast(ray, out dist) ? mDrawStart + label.GetCharacterIndexAtPosition(ray.GetPoint(dist)) : 0;
	}

	/// <summary>
	/// Move the caret on press.
	/// </summary>

	protected virtual void OnPress (bool isPressed)
	{
		if (isPressed && isSelected && label != null && UICamera.currentScheme == UICamera.ControlScheme.Mouse)
		{
			mSelectionEnd = GetCharUnderMouse();
			if (!Input.GetKey(KeyCode.LeftShift) &&
				!Input.GetKey(KeyCode.RightShift)) mSelectionStart = mSelectionEnd;
			UpdateLabel();
		}
	}

	/// <summary>
	/// Drag selection.
	/// </summary>

	protected virtual void OnDrag (Vector2 delta)
	{
		if (label != null && UICamera.currentScheme == UICamera.ControlScheme.Mouse)
		{
			mSelectionEnd = GetCharUnderMouse();
			UpdateLabel();
		}
	}

	/// <summary>
	/// Ensure we've released the dynamically created resources.
	/// </summary>

	void OnDisable () { Cleanup(); }

	/// <summary>
	/// Cleanup.
	/// </summary>

	protected virtual void Cleanup ()
	{
		if (mHighlight)
		{
			NGUITools.Destroy(mHighlight.gameObject);
			mHighlight = null;
		}

		if (mCaret)
		{
			NGUITools.Destroy(mCaret.gameObject);
			mCaret = null;
		}

		if (mBlankTex)
		{
			NGUITools.Destroy(mBlankTex);
			mBlankTex = null;
		}
	}
#endif // !MOBILE

	/// <summary>
	/// Submit the input field's text.
	/// </summary>

	public void Submit ()
	{
		if (NGUITools.GetActive(this))
		{
			current = this;
			mValue = value;
			EventDelegate.Execute(onSubmit);
			SaveToPlayerPrefs(mValue);
			current = null;
		}
	}

	/// <summary>
	/// Update the visual text label.
	/// </summary>

	public void UpdateLabel ()
	{
		if (label != null)
		{
			if (mDoInit) Init();
			bool selected = isSelected;
			string fullText = value;
			bool isEmpty = string.IsNullOrEmpty(fullText) && string.IsNullOrEmpty(Input.compositionString);
			label.color = (isEmpty && !selected) ? mDefaultColor : activeTextColor;
			string processed;

			if (isEmpty)
			{
				processed = selected ? "" : mDefaultText;
				RestoreLabelPivot();
			}
			else
			{
				if (inputType == InputType.Password)
				{
					processed = "";
					for (int i = 0, imax = fullText.Length; i < imax; ++i) processed += "*";
				}
				else processed = fullText;

				// Start with text leading up to the selection
				int selPos = selected ? Mathf.Min(processed.Length, cursorPosition) : 0;
				string left = processed.Substring(0, selPos);

				// Append the composition string and the cursor character
				if (selected) left += Input.compositionString;

				// Append the text from the selection onwards
				processed = left + processed.Substring(selPos, processed.Length - selPos);

				// Clamped content needs to be adjusted further
				if (selected && label.overflowMethod == UILabel.Overflow.ClampContent)
				{
					// Determine what will actually fit into the given line
					int offset = label.CalculateOffsetToFit(processed);

					if (offset == 0)
					{
						mDrawStart = 0;
						RestoreLabelPivot();
					}
					else if (selPos < mDrawStart)
					{
						mDrawStart = selPos;
						SetPivotToLeft();
					}
					else if (offset < mDrawStart)
					{
						mDrawStart = offset;
						SetPivotToLeft();
					}
					else
					{
						offset = label.CalculateOffsetToFit(processed.Substring(0, selPos));

						if (offset > mDrawStart)
						{
							mDrawStart = offset;
							SetPivotToRight();
						}
					}

					// If necessary, trim the front
					if (mDrawStart != 0)
						processed = processed.Substring(mDrawStart, processed.Length - mDrawStart);
				}
				else
				{
					mDrawStart = 0;
					RestoreLabelPivot();
				}
			}

			label.text = processed;
#if !MOBILE
			if (selected)
			{
				int start = mSelectionStart - mDrawStart;
				int end = mSelectionEnd - mDrawStart;

				// Blank texture used by selection and caret
				if (mBlankTex == null)
				{
					mBlankTex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
					for (int y = 0; y < 2; ++y)
						for (int x = 0; x < 2; ++x)
							mBlankTex.SetPixel(x, y, Color.white);
					mBlankTex.Apply();
				}

				// Create the selection highlight
				if (start != end)
				{
					if (mHighlight == null)
					{
						mHighlight = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
						mHighlight.name = "Input Highlight";
						mHighlight.mainTexture = mBlankTex;
						mHighlight.fillGeometry = false;
						mHighlight.pivot = label.pivot;
						mHighlight.SetAnchor(label.cachedTransform);
					}
					else
					{
						mHighlight.pivot = label.pivot;
						mHighlight.MarkAsChanged();
					}
				}

				// Create the caret
				if (mCaret == null)
				{
					mCaret = NGUITools.AddWidget<UITexture>(label.cachedGameObject);
					mCaret.name = "Input Caret";
					mCaret.mainTexture = mBlankTex;
					mCaret.fillGeometry = false;
					mCaret.pivot = label.pivot;
					mCaret.SetAnchor(label.cachedTransform);
				}
				else
				{
					mCaret.pivot = label.pivot;
					mCaret.MarkAsChanged();
					mCaret.enabled = true;
				}

				// Fill the selection
				if (start != end)
				{
					label.PrintOverlay(start, end, mCaret.geometry, mHighlight.geometry, caretColor, selectionColor);
					mHighlight.enabled = mHighlight.geometry.hasVertices;
				}
				else
				{
					label.PrintOverlay(start, end, mCaret.geometry, null, caretColor, selectionColor);
					if (mHighlight != null) mHighlight.enabled = false;
				}

				// Reset the blinking time
				mNextBlink = RealTime.time + 0.5f;
			}
			else Cleanup();
#endif
		}
	}

	/// <summary>
	/// Set the label's pivot to the left.
	/// </summary>

	protected void SetPivotToLeft ()
	{
		Vector2 po = NGUIMath.GetPivotOffset(mPivot);
		po.x = 0f;
		label.pivot = NGUIMath.GetPivot(po);
	}

	/// <summary>
	/// Set the label's pivot to the right.
	/// </summary>

	protected void SetPivotToRight ()
	{
		Vector2 po = NGUIMath.GetPivotOffset(mPivot);
		po.x = 1f;
		label.pivot = NGUIMath.GetPivot(po);
	}

	/// <summary>
	/// Restore the input label's pivot point.
	/// </summary>

	protected void RestoreLabelPivot ()
	{
		if (label != null && label.pivot != mPivot)
			label.pivot = mPivot;
	}

	/// <summary>
	/// Validate the specified input.
	/// </summary>

	protected char Validate (string text, int pos, char ch)
	{
		// Validation is disabled
		if (validation == Validation.None || !enabled) return ch;

		if (validation == Validation.Integer)
		{
			// Integer number validation
			if (ch >= '0' && ch <= '9') return ch;
			if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
		}
		else if (validation == Validation.Float)
		{
			// Floating-point number
			if (ch >= '0' && ch <= '9') return ch;
			if (ch == '-' && pos == 0 && !text.Contains("-")) return ch;
			if (ch == '.' && !text.Contains(".")) return ch;
		}
		else if (validation == Validation.Alphanumeric)
		{
			// All alphanumeric characters
			if (ch >= 'A' && ch <= 'Z') return ch;
			if (ch >= 'a' && ch <= 'z') return ch;
			if (ch >= '0' && ch <= '9') return ch;
		}
		else if (validation == Validation.Username)
		{
			// Lowercase and numbers
			if (ch >= 'A' && ch <= 'Z') return (char)(ch - 'A' + 'a');
			if (ch >= 'a' && ch <= 'z') return ch;
			if (ch >= '0' && ch <= '9') return ch;
		}
		else if (validation == Validation.Name)
		{
			char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
			char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';

			if (ch >= 'a' && ch <= 'z')
			{
				// Space followed by a letter -- make sure it's capitalized
				if (lastChar == ' ') return (char)(ch - 'a' + 'A');
				return ch;
			}
			else if (ch >= 'A' && ch <= 'Z')
			{
				// Uppercase letters are only allowed after spaces (and apostrophes)
				if (lastChar != ' ' && lastChar != '\'') return (char)(ch - 'A' + 'a');
				return ch;
			}
			else if (ch == '\'')
			{
				// Don't allow more than one apostrophe
				if (lastChar != ' ' && lastChar != '\'' && nextChar != '\'' && !text.Contains("'")) return ch;
			}
			else if (ch == ' ')
			{
				// Don't allow more than one space in a row
				if (lastChar != ' ' && lastChar != '\'' && nextChar != ' ' && nextChar != '\'') return ch;
			}
		}
		return (char)0;
	}

	/// <summary>
	/// Execute the OnChange callback.
	/// </summary>

	protected void ExecuteOnChange ()
	{
		if (EventDelegate.IsValid(onChange))
		{
			current = this;
			EventDelegate.Execute(onChange);
			current = null;
		}
	}
}
