using UnityEngine;
using System.Collections;

[ExecuteInEditMode()]
[RequireComponent(typeof(UIButton))]
public class UIIconButton : MonoBehaviour {
	
	public enum UIIconButtonTypes {
		Heart,
		Plus,
		Decline,
		Star,
	}
	
	public UISprite sprite;
	public UIButton button;
	private UIIconButtonTypes mType;
	
	public UIIconButtonTypes type { 
		get { return mType; }
		set {
			mType = value;
			OnTypeChanged();
		} 
	}
	
	void Start()
	{
		if (button == null) button = GetComponent<UIButton>();
		if (sprite == null) return;
		
		switch (sprite.spriteName)
		{
		case "IconButton_Heart_Normal":
			mType = UIIconButtonTypes.Heart;
			break;
		case "IconButton_Plus_Normal":
			mType = UIIconButtonTypes.Plus;
			break;
		case "IconButton_Decline_Normal":
			mType = UIIconButtonTypes.Decline;
			break;
		case "IconButton_Star_Normal":
			mType = UIIconButtonTypes.Star;
			break;
		}
	}
	
	private void OnTypeChanged()
	{
		if (sprite == null) return;
		
		switch (mType)
		{
			case UIIconButtonTypes.Heart:
			{
				sprite.spriteName = "IconButton_Heart_Normal";
				button.normalSprite = "IconButton_Heart_Normal";
				button.hoverSprite = "IconButton_Heart_Hover";
				button.pressedSprite = "IconButton_Heart_Pressed";
				button.disabledSprite = "IconButton_Heart_Normal";
				break;
			}
			case UIIconButtonTypes.Plus:
			{
				sprite.spriteName = "IconButton_Plus_Normal";
				button.normalSprite = "IconButton_Plus_Normal";
				button.hoverSprite = "IconButton_Plus_Hover";
				button.pressedSprite = "IconButton_Plus_Pressed";
				button.disabledSprite = "IconButton_Plus_Normal";
				break;
			}
			case UIIconButtonTypes.Decline:
			{
				sprite.spriteName = "IconButton_Decline_Normal";
				button.normalSprite = "IconButton_Decline_Normal";
				button.hoverSprite = "IconButton_Decline_Hover";
				button.pressedSprite = "IconButton_Decline_Pressed";
				button.disabledSprite = "IconButton_Decline_Normal";
				break;
			}
			case UIIconButtonTypes.Star:
			{
				sprite.spriteName = "IconButton_Star_Normal";
				button.normalSprite = "IconButton_Star_Normal";
				button.hoverSprite = "IconButton_Star_Hover";
				button.pressedSprite = "IconButton_Star_Pressed";
				button.disabledSprite = "IconButton_Star_Normal";
				break;
			}
		}

		sprite.Update();
		sprite.MarkAsChanged();
		sprite.MakePixelPerfect();
	}
}
