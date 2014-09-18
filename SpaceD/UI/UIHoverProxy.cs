using UnityEngine;
using System.Collections;

public class UIHoverProxy : MonoBehaviour {

	[SerializeField] private GameObject target;

	void Start()
	{
		if (this.target == null)
			this.enabled = false;
	}

	void OnHover(bool isOver)
	{
		this.target.SendMessage("OnHover", isOver, SendMessageOptions.DontRequireReceiver);
	}
}
