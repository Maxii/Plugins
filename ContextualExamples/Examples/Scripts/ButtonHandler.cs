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
	
	public void OnDifficultySelection()
	{
		Difficulty d = (Difficulty)CtxMenuButton.current.selectedItem;
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
	
	public void OnInputDeviceSelection()
	{
		//int item = CtxMenuButton.current.selectedItem;
		
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
	
	public void OnPlaybackWidgetSelection()
	{
		int item = CtxMenuButton.current.selectedItem;
		
		PlaybackItem pbi = (PlaybackItem)item;
		Debug.Log("PlaybackWidget: "+pbi.ToString());
	}
	
	public void OnShowDifficultyMenu()
	{
		Debug.Log("ButtonHandler.OnShowDifficultyMenu() "+CtxMenuButton.current);
	}
		
	public void OnHideDifficultyMenu()
	{
		Debug.Log("ButtonHandler.OnHideDifficultyMenu() "+CtxMenuButton.current);
	}

	public void OnShowInputOptionsMenu()
	{
		Debug.Log("ButtonHandler.OnShowInputOptionsMenu() "+CtxMenuButton.current);
	}
	
	enum HelpItem
	{
		Units,
		Structures,
		Crafting,
		GamePlay,
		UserInterface
	}
	
	public void OnHelpSelection()
	{
		HelpItem hi = (HelpItem)CtxPopup.current.selectedItem;
		Debug.Log("ButtonHandler.OnHelpSelection() "+hi.ToString());
	}
}
