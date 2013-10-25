using UnityEngine;
using System.Collections;

public class LocalizeDemo : MonoBehaviour
{
	public CtxMenu menuBar;
	
	enum LanguageItem
	{
		English = 40,
		French,
		Italian,
		German,
		Spanish,
		End
	}
	
	void Start()
	{
		Localization loc = Localization.instance;
		if (loc != null)
		{
			if (loc.currentLanguage == "English")
				menuBar.SetChecked((int)LanguageItem.English, true);
			else if (loc.currentLanguage == "French")
				menuBar.SetChecked((int)LanguageItem.French, true);
			else if (loc.currentLanguage == "Italian")
				menuBar.SetChecked((int)LanguageItem.Italian, true);
			else if (loc.currentLanguage == "German")
				menuBar.SetChecked((int)LanguageItem.German, true);
			else if (loc.currentLanguage == "Spanish")
				menuBar.SetChecked((int)LanguageItem.Spanish, true);
		}
	}
	
	public void OnMenuSelection()
	{
		int itemID = CtxMenu.current.selectedItem;
		LanguageItem li = (LanguageItem)itemID;
		string itemText = CtxMenu.current.GetText(itemID);
		
		if (li >= LanguageItem.English && li < LanguageItem.End)
		{
			Localization.instance.currentLanguage = itemText;
			Debug.Log("Language selected: "+itemText);
		}
		else
		{
			Debug.Log("Menu Command: "+itemText);
		}
	}
}
