using UnityEngine;
using System.Collections;

public class ForwardHandler : MonoBehaviour
{
	void OnMenuSelection(int selection)
	{
		Debug.Log("ForwardHandler.OnMenuSelection() "+selection+" from "+CtxObject.sender);
	}
}
