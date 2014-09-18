using UnityEngine;
using System.Collections;
 
class AnimateTiledTexture : MonoBehaviour
{
	public UITexture uiTexture;
    public int columns = 2;
    public int rows = 2;
    public float framesPerSecond = 10f;

    //the current frame to display
    private int index = 0;
 
    void Start()
    {
		if (this.uiTexture == null) this.uiTexture = this.GetComponent<UITexture>();

		if (this.uiTexture != null)
		{
        	//set the tile size of the texture (in UV units), based on the rows and columns
        	Vector2 size = new Vector2(1f / columns, 1f / rows);

			this.uiTexture.uvRect = new Rect(this.uiTexture.uvRect.x, this.uiTexture.uvRect.y, size.x, size.y);
		}
    }

	void OnEnable()
	{
		if (this.uiTexture != null)
			this.StartCoroutine("updateTiling");
	}

	void OnDisable()
	{
		this.StopCoroutine("updateTiling");
	}
 
    private IEnumerator updateTiling()
    {
		if (this.uiTexture == null) yield break;

        while (true)
        {
            //move to the next index
            index++;
            if (index >= rows * columns)
                index = 0;
 
            //split into x and y indexes
            Vector2 offset = new Vector2((float)index / columns - (index / columns), //x index
                                          (index / columns) / (float)rows);          //y index
 			
			this.uiTexture.uvRect = new Rect(offset.x, offset.y, this.uiTexture.uvRect.width, this.uiTexture.uvRect.height);

            yield return new WaitForSeconds(1f / framesPerSecond);
        }
 
    }
}