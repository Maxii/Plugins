using UnityEngine;
using System.Collections;

public class ButtonHandler : MonoBehaviour
{
	enum Difficulty
	{
		Sissy,
		Normal,
		Heroic,
		Legendary,
		AreYouKidding
	}
	
	void OnDifficultySelection(int item)
	{
		Difficulty d = (Difficulty)item;
		Debug.Log("Difficulty: "+d.ToString());
	}

	enum InputDevice
	{
		Mouse,
		Keyboard,
		Joystick,
		GamePad,
		HeadTracker,
		Count
	}
	
	void OnInputDeviceSelection(int item)
	{
		string msg = "";
		for (InputDevice d = InputDevice.Mouse; d<InputDevice.Count; d++)
		{
			if (CtxMenu.current.IsChecked((int)d))
				msg += d.ToString()+" ";
		}
		
		if (msg == "")
			msg = "None";
		
		Debug.Log("Devices: "+msg);
	}
	
	enum PlaybackItem
	{
		Beginning,
		Reverse,
		Stop,
		Play,
		Forward,
		End
	}
	
	void OnPlaybackWidgetSelection(int item)
	{
		PlaybackItem pbi = (PlaybackItem)item;
		Debug.Log("PlaybackWidget: "+pbi.ToString());
	}
	
	void OnShowDifficultyMenu(CtxMenuButton button)
	{
		Debug.Log("ButtonHandler.OnShowDifficultyMenu() "+button);
	}
		
	void OnHideDifficultyMenu(CtxMenuButton button)
	{
		Debug.Log("ButtonHandler.OnHideDifficultyMenu() "+button);
	}

	void OnShowInputOptionsMenu(CtxMenuButton button)
	{
		Debug.Log("ButtonHandler.OnShowInputOptionsMenu() "+button);
	}
	
	enum HelpItem
	{
		Units,
		Structures,
		Crafting,
		GamePlay,
		UserInterface
	}
	
	void OnHelpSelection(int item)
	{
		HelpItem hi = (HelpItem)item;
		Debug.Log("ButtonHandler.OnHelpSelection() "+hi.ToString());
	}
}
