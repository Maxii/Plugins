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
		TextAsset txt = Resources.Load("CtxDemoLocalization", typeof(TextAsset)) as TextAsset;
		Localization.LoadCSV(txt);

		if (Localization.language == "English")
			menuBar.SetChecked((int)LanguageItem.English, true);
		else if (Localization.language == "French")
			menuBar.SetChecked((int)LanguageItem.French, true);
		else if (Localization.language == "Italian")
			menuBar.SetChecked((int)LanguageItem.Italian, true);
		else if (Localization.language == "German")
			menuBar.SetChecked((int)LanguageItem.German, true);
		else if (Localization.language == "Spanish")
			menuBar.SetChecked((int)LanguageItem.Spanish, true);
	}
	
	public void OnMenuSelection()
	{
		int itemID = CtxMenu.current.selectedItem;
		LanguageItem li = (LanguageItem)itemID;
		string itemText = CtxMenu.current.GetText(itemID);
		
		if (li >= LanguageItem.English && li < LanguageItem.End)
		{
			Localization.language = itemText;
			Debug.Log("Language selected: "+itemText);
		}
		else
		{
			Debug.Log("Menu Command: "+itemText);
		}
	}
}
