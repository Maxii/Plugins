using UnityEngine;

[AddComponentMenu("SpaceD UI/Image Thumb")]
public class UIImageThumb : MonoBehaviour
{
	public UISprite target;
	
	public string normalSprite;
	public string hoverSprite;
	public string pressedSprite;
	
	void Start()
	{
		// Get the normal sprite name
		if (this.target != null)
			this.normalSprite = this.target.spriteName;
	}
	
	void UpdateImage()
	{
		if (this.enabled && this.target != null)
		{
			if (!string.IsNullOrEmpty(this.normalSprite) && !string.IsNullOrEmpty(this.hoverSprite))
				this.target.spriteName = UICamera.IsHighlighted(gameObject) ? this.hoverSprite : this.normalSprite;
		}
	}
	
	void OnHover(bool isOver)
	{
		if (this.enabled && this.target != null)
		{
			if (!string.IsNullOrEmpty(this.normalSprite) && !string.IsNullOrEmpty(this.hoverSprite))
			{
				this.target.spriteName = isOver ? this.hoverSprite : this.normalSprite;
				this.target.Update();
			}
		}
	}
	
	void OnPress(bool pressed)
	{
		if (this.enabled && pressed && !string.IsNullOrEmpty(this.pressedSprite))
		{
			this.target.spriteName = this.pressedSprite;
			this.target.Update();
		}
		else this.UpdateImage();
	}
}
