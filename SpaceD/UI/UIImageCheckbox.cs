using UnityEngine;

[AddComponentMenu("SpaceD UI/Image Checkbox")]
public class UIImageCheckbox : MonoBehaviour
{
	public UISprite target;
	public string normalSprite;
	public string hoverSprite;
	public bool makePixelPerfect = true;
	
	void Start ()
	{
		if (target != null)
			this.normalSprite = this.target.spriteName;
	}
	
	void OnHover(bool isOver)
	{
		if (this.target != null && !string.IsNullOrEmpty(this.normalSprite) && !string.IsNullOrEmpty(this.hoverSprite))
		{
			this.target.spriteName = isOver ? this.hoverSprite : this.normalSprite;
			
			if (this.makePixelPerfect)
				this.target.MakePixelPerfect();
		}
	}
}