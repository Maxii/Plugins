using UnityEngine;
using System.Collections;

[ExecuteInEditMode()]
public class UITinyButton : MonoBehaviour {

	public enum UITinyButtonTypes {
		Decline,
		Accept,
		Social,
		Mail,
	}

	public UISprite sprite;
	private UITinyButtonTypes mType;

	public UITinyButtonTypes type { 
		get { return mType; }
		set {
			mType = value;
			OnTypeChanged();
		} 
	}

	void Start()
	{
		if (sprite == null) return;
		
		switch (sprite.spriteName)
		{
		case "tinyButton_Accept":
			mType = UITinyButtonTypes.Accept;
			break;
		case "tinyButton_Decline":
			mType = UITinyButtonTypes.Decline;
			break;
		case "tinyButton_Social":
			mType = UITinyButtonTypes.Social;
			break;
		case "tinyButton_Mail":
			mType = UITinyButtonTypes.Mail;
			break;
		}
	}
	
	private void OnTypeChanged()
	{
		if (sprite == null) return;

		switch (mType)
		{
			case UITinyButtonTypes.Accept:
				sprite.spriteName = "tinyButton_Accept";
				sprite.transform.localPosition = new Vector3(0f, 0f, 0f);
				break;
			case UITinyButtonTypes.Decline:
				sprite.spriteName = "tinyButton_Decline";
				sprite.transform.localPosition = new Vector3(0f, 1f, 0f);
				break;
			case UITinyButtonTypes.Social:
				sprite.spriteName = "tinyButton_Social";
				sprite.transform.localPosition = new Vector3(0f, 1f, 0f);
				break;
			case UITinyButtonTypes.Mail:
				sprite.spriteName = "tinyButton_Mail";
				sprite.transform.localPosition = new Vector3(0f, 1f, 0f);
				break;
		}

		sprite.MakePixelPerfect();
	}
}
