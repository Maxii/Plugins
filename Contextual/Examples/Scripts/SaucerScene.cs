using UnityEngine;
using System.Collections;

public class SaucerScene : MonoBehaviour
{
	public Material[] hullMaterials;
	public Material[] glowMaterials;
	
	private static SaucerScene instance;
	public static SaucerScene Instance
	{
		get { return instance; }
	}

	void Awake()
	{
		if (instance == null)
			instance = this;
	}
	
	void OnDestroy()
	{
		if (instance == this)
			instance = null;
	}
}
