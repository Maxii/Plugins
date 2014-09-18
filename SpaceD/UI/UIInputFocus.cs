using UnityEngine;
using System.Collections;

[AddComponentMenu("SpaceD UI/Input Focus")]
public class UIInputFocus : MonoBehaviour
{
	public UISprite target;
	
	public string normalSprite;
	public string selectedSprite;
	
	public Color normalColor = Color.white;
	public Color selectedColor = Color.white;
	
	public bool makePixelPerfect = true;
	
	void Start()
	{
		if (this.target != null)
		{
			this.normalSprite = this.target.spriteName;
			this.normalColor = this.target.color;
		}
	}
	
	public void OnSelect(bool isSelected)
	{
		if (this.target != null)
		{
			if (!string.IsNullOrEmpty(this.normalSprite) && !string.IsNullOrEmpty(this.selectedSprite))
			{
				this.target.spriteName = isSelected ? this.selectedSprite : this.normalSprite;
				
				if (this.makePixelPerfect)
					this.target.MakePixelPerfect();
			}
			
			this.target.color = isSelected ? this.selectedColor : this.normalColor;
		}
	}
}
