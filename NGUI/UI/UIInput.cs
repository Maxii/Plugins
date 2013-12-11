//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY)
#define MOBILE
#endif

using UnityEngine;
using System.Collections.Generic;

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
	static protected int mDrawEnd = 0;

#if MOBILE
	static protected TouchScreenKeyboard mKeyboard;
#else
	static protected string mLastIME = "";
	static protected TextEditor mEditor = null;
#endif

	/// <summary>
	/// Default text used by the input's label.
	/// </summary>

	public string defaultText { get { return mDefaultText; } set { mDefaultText = value; } }

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
#if MOBILE
			if (isSelected && mKeyboard != null && mKeyboard.active) return mKeyboard.text;
#else
			if (isSelected && mEditor != null) return mEditor.content.text;
#endif
			return mValue;
		}
		set
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (mDoInit) Init();
			mDrawStart = 0;
			mDrawEnd = 0;
#if MOBILE
			if (isSelected && mKeyboard != null)
				mKeyboard.text = value;

			if (this.value != value)
			{
				mValue = value;
				if (isSelected && mKeyboard != null) mKeyboard.text = value;
				SaveToPlayerPrefs(mValue);
				UpdateLabel();
				ExecuteOnChange();
			}
#else
			if (isSelected && mEditor != null)
			{
				if (mEditor.content.text != value)
				{
					mEditor.content.text = value;
					UpdateLabel();
					ExecuteOnChange();
					return;
				}
			}

			if (this.value != value)
			{
				mValue = value;

				if (isSelected && mEditor != null)
				{
					mEditor.content.text = value;
					mEditor.OnLostFocus();
					mEditor.OnFocus();
					mEditor.MoveTextEnd();
				}
				SaveToPlayerPrefs(mValue);
				UpdateLabel();
				ExecuteOnChange();
			}
#endif
		}
	}

	/// <summary>
	/// Whether the input field needs to draw an ASCII cursor.
	/// </summary>

#if MOBILE
	protected bool needsTextCursor { get { return (isSelected && mKeyboard != null); } }
#else
	protected bool needsTextCursor { get { return isSelected; } }
#endif

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
	protected int cursorPosition { get { return (isSelected && mEditor != null) ? mEditor.selectPos : value.Length; } }
#endif

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
		else value = mValue;
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

		if (label != null && NGUITools.IsActive(this))
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
					TouchScreenKeyboard.Open(mValue, (TouchScreenKeyboardType)((int)keyboardType), inputType == InputType.AutoCorrect);
			}
			else
#endif
			{
				Input.imeCompositionMode = IMECompositionMode.On;
				Input.compositionCursorPos = (UICamera.current != null && UICamera.current.cachedCamera != null) ?
					UICamera.current.cachedCamera.WorldToScreenPoint(label.worldCorners[0]) :
					label.worldCorners[0];
#if !MOBILE
				mEditor = new TextEditor();
				mEditor.content = new GUIContent(mValue);
				mEditor.OnFocus();
				mEditor.MoveTextEnd();
#endif
				mDrawStart = 0;
				mDrawEnd = 0;
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

		if (label != null && NGUITools.IsActive(this))
		{
			mValue = value;
#if MOBILE
			if (mKeyboard != null)
			{
				mKeyboard.active = false;
				mKeyboard = null;
			}
#else
			mEditor = null;
#endif
			if (string.IsNullOrEmpty(mValue))
			{
				label.text = mDefaultText;
				label.color = mDefaultColor;
			}
			else label.text = mValue;

			Input.imeCompositionMode = IMECompositionMode.Off;
			RestoreLabelPivot();
		}
		
		selection = null;
		UpdateLabel();
	}

	/// <summary>
	/// Update the text based on input.
	/// </summary>

#if MOBILE
	void Update()
	{
		if (mKeyboard != null && isSelected && NGUITools.IsActive(this))
		{
			string val = mKeyboard.text;

			if (mValue != val)
			{
				mValue = "";

				for (int i = 0; i < val.Length; ++i)
				{
					char c = val[i];
					if (onValidate != null) c = onValidate(mValue, mValue.Length, c);
					else if (validation != Validation.None) c = Validate(mValue, mValue.Length, c);
					if (c != 0) mValue += c;
				}

				if (characterLimit > 0 && mValue.Length > characterLimit)
					mValue = mValue.Substring(0, characterLimit);
				
				UpdateLabel();
				ExecuteOnChange();

				if (mValue != val) mKeyboard.text = mValue;
			}

			if (mKeyboard.done)
			{
#if !UNITY_3_5
				if (!mKeyboard.wasCanceled)
#endif
					Submit();
				mKeyboard = null;
				isSelected = false;
			}
		}
	}
#else
	void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (isSelected && NGUITools.IsActive(this))
		{
			if (mDoInit) Init();

			if (selectOnTab != null && Input.GetKeyDown(KeyCode.Tab))
			{
				UICamera.selectedObject = selectOnTab;
				return;
			}

			// Process input
			Append(Input.inputString);

			if (mLastIME != Input.compositionString)
			{
				mLastIME = Input.compositionString;
				UpdateLabel();
				ExecuteOnChange();
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
		RuntimePlatform rp = Application.platform;
		bool isMac = (rp == RuntimePlatform.OSXEditor || rp == RuntimePlatform.OSXPlayer || rp == RuntimePlatform.OSXWebPlayer);
		bool ctrl = isMac ? (ev.modifiers == EventModifiers.Command) : (ev.modifiers == EventModifiers.Control);

		switch (ev.keyCode)
		{
			case KeyCode.Backspace:
			{
				ev.Use();
				mEditor.Backspace();
				UpdateLabel();
				ExecuteOnChange();
				return true;
			}

			case KeyCode.Delete:
			{
				ev.Use();
				mEditor.Delete();
				UpdateLabel();
				ExecuteOnChange();
				return true;
			}

			case KeyCode.LeftArrow:
			{
				ev.Use();
				mEditor.MoveLeft();
				UpdateLabel();
				return true;
			}

			case KeyCode.RightArrow:
			{
				ev.Use();
				mEditor.MoveRight();
				UpdateLabel();
				return true;
			}
			
			case KeyCode.Home:
			case KeyCode.UpArrow:
			{
				ev.Use();
				mEditor.MoveTextStart();
				UpdateLabel();
				return true;
			}

			case KeyCode.End:
			case KeyCode.DownArrow:
			{
				ev.Use();
				mEditor.MoveTextEnd();
				UpdateLabel();
				return true;
			}

			// Copy
			case KeyCode.C:
			{
				if (ctrl)
				{
					ev.Use();
					NGUITools.clipboard = value;
				}
				return true;
			}

			// Paste
			case KeyCode.V:
			{
				if (ctrl)
				{
					ev.Use();
					Append(NGUITools.clipboard);
				}
				return true;
			}

			// Cut
			case KeyCode.X:
			{
				if (ctrl)
				{
					ev.Use();
					NGUITools.clipboard = value;
					value = "";
				}
				return true;
			}

			// Submit
			case KeyCode.Return:
			case KeyCode.KeypadEnter:
			{
				ev.Use();
				
				if (ctrl && label != null && label.overflowMethod != UILabel.Overflow.ClampContent)
				{
					char c = '\n';
					if (onValidate != null) c = onValidate(mEditor.content.text, mEditor.selectPos, c);
					else if (validation != Validation.None) c = Validate(mEditor.content.text, mEditor.selectPos, c);

					// Append the character
					if (c != 0)
					{
						mEditor.Insert(c);
						UpdateLabel();
						ExecuteOnChange();
					}
				}
				else
				{
					UICamera.currentKey = ev.keyCode;
					Submit();
					UICamera.currentKey = KeyCode.None;
					isSelected = false;
					UpdateLabel();
					ExecuteOnChange();
				}
				return true;
			}
		}
		return false;
	}
#endif

	/// <summary>
	/// Submit the input field's text.
	/// </summary>

	protected void Submit ()
	{
		if (NGUITools.IsActive(this))
		{
			current = this;
			mValue = value;
			EventDelegate.Execute(onSubmit);
			SaveToPlayerPrefs(mValue);
			current = null;
		}
	}

#if !MOBILE
	/// <summary>
	/// Append the specified text to the end of the current.
	/// </summary>

	protected virtual void Append (string input)
	{
		if (string.IsNullOrEmpty(input)) return;

		for (int i = 0, imax = input.Length; i < imax; ++i)
		{
			char c = input[i];

			if (c >= ' ')
			{
				// Can't go past the character limit
				if (characterLimit > 0 && mEditor.content.text.Length >= characterLimit) continue;

				// If we have an input validator, validate the input first
				if (onValidate != null) c = onValidate(mEditor.content.text, mEditor.selectPos, c);
				else if (validation != Validation.None) c = Validate(mEditor.content.text, mEditor.selectPos, c);

				// If the input is invalid, skip it
				if (c == 0) continue;

				// Append the character
				mEditor.Insert(c);
			}
		}
		UpdateLabel();
		ExecuteOnChange();
	}
#endif

	/// <summary>
	/// Update the visual text label.
	/// </summary>

	protected void UpdateLabel ()
	{
		if (label != null)
		{
			if (mDoInit) Init();
			bool selected = isSelected;
			string fullText = value;
			bool isEmpty = string.IsNullOrEmpty(fullText);
			string processed;

			if (isEmpty)
			{
				processed = selected ? (needsTextCursor ? "|" : "") : mDefaultText;
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
					
				if (selected)
				{
					// Append the composition string and the cursor character
					left += Input.compositionString;
					if (needsTextCursor) left += "|";
				}
				
				// Append the text from the selection onwards
				processed = left + processed.Substring(selPos, processed.Length - selPos);

				if (label.overflowMethod == UILabel.Overflow.ClampContent)
				{
					// Determine what will actually fit into the given line
					if (selected)
					{
						if (mDrawEnd == 0) mDrawEnd = selPos;

						// Offset required in order to print the part leading up to the cursor
						string visible = processed.Substring(0, Mathf.Min(mDrawEnd, processed.Length));
						int leftMargin = label.CalculateOffsetToFit(visible);

						// The cursor is no longer within bounds
						if (selPos < leftMargin || selPos >= mDrawEnd)
						{
							leftMargin = label.CalculateOffsetToFit(left);
							mDrawStart = leftMargin;
							mDrawEnd = left.Length;
						}
						else if (leftMargin != mDrawStart)
						{
							// The left margin shifted -- happens when deleting or adding characters
							mDrawStart = leftMargin;
						}
					}

					// If the text doesn't fit, we want to change the label to use a right-hand pivot point
					if (mDrawStart != 0)
					{
						processed = processed.Substring(mDrawStart, processed.Length - mDrawStart);
						if (mPivot == UIWidget.Pivot.Left) label.pivot = UIWidget.Pivot.Right;
						else if (mPivot == UIWidget.Pivot.TopLeft) label.pivot = UIWidget.Pivot.TopRight;
						else if (mPivot == UIWidget.Pivot.BottomLeft) label.pivot = UIWidget.Pivot.BottomRight;
					}
					else RestoreLabelPivot();
				}
				else RestoreLabelPivot();
			}

			label.text = processed;
			label.color = (isEmpty && !selected) ? mDefaultColor : activeTextColor;
		}
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
