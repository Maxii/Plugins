using UnityEngine;
using System.Collections;

[ExecuteInEditMode()]
public class SpaceDUIWindowTitle : MonoBehaviour {
	
	private string lastText;
	private UILabel title0;
	[SerializeField] private UILabel title1;
	[SerializeField] private UILabel title2;
	[SerializeField] private UILabel title3;
	[SerializeField] private UILabel title4;
	
	void Start()
	{
		this.title0 = this.gameObject.GetComponent<UILabel>();
	}
	
	void Update()
	{
		// Detect change
		if (this.lastText != this.title0.text)
		{
			if (this.title1 != null)
				this.title1.text = this.title0.text;
			if (this.title2 != null)
				this.title2.text = this.title0.text;
			if (this.title3 != null)
				this.title3.text = this.title0.text;
			if (this.title4 != null)
				this.title4.text = this.title0.text;
		}
		
		this.lastText = this.title0.text;
	}
}
