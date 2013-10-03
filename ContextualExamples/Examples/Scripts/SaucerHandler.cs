using UnityEngine;
using System.Collections;

public class SaucerHandler : MonoBehaviour
{
	enum MenuCommands
	{
		Small,
		Medium,
		Large,
		
		ColorScheme1 = 5,
		ColorScheme2,
		ColorScheme3,
		ColorScheme4,
		ColorScheme5,

		ScoutBuzzAirliners = 10,
		ScoutFlyInCircles,
		ScoutHoverAimlessly,
		
		AbductorAbductSpecimens = 20,
		AbductorEraseMemory,
		AbductorUseProbeDevice,
		
		EnforcerAttackMilitary = 30,
		EnforcerAttackCivilians,
		EnforcerDeployUltimateWeapon,
		
		TransportLandAtWhiteHouse = 40,
		TransportDeployGrays,
		TransportRecoverGrays,
		
		SpyFakeAutopsy = 50,
		SpyMakeCropCircles,
		SpyBuzzRadarTowers
	}
	
	public Material[] hullMaterials;
	public Material[] glowMaterials;
	
	void OnMenuSelection(int item)
	{
		MenuCommands cmd = (MenuCommands)item;
		
		switch (cmd)
		{
		case MenuCommands.Small:
			transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
			break;
		case MenuCommands.Medium:
			transform.localScale = Vector3.one;
			break;
		case MenuCommands.Large:
			transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
			break;
		
		case MenuCommands.ColorScheme1:
		case MenuCommands.ColorScheme2:
		case MenuCommands.ColorScheme3:
		case MenuCommands.ColorScheme4:
		case MenuCommands.ColorScheme5:
		{
			int cs = item - (int)MenuCommands.ColorScheme1;
			SetColorScheme(hullMaterials[cs], glowMaterials[cs]);
			break;
		}

		case MenuCommands.ScoutBuzzAirliners:
		case MenuCommands.ScoutFlyInCircles:
		case MenuCommands.ScoutHoverAimlessly:
		
		case MenuCommands.AbductorAbductSpecimens:
		case MenuCommands.AbductorEraseMemory:
		case MenuCommands.AbductorUseProbeDevice:
		
		case MenuCommands.EnforcerAttackMilitary:
		case MenuCommands.EnforcerAttackCivilians:
		case MenuCommands.EnforcerDeployUltimateWeapon:
		
		case MenuCommands.TransportLandAtWhiteHouse:
		case MenuCommands.TransportDeployGrays:
		case MenuCommands.TransportRecoverGrays:
		
		case MenuCommands.SpyFakeAutopsy:
		case MenuCommands.SpyMakeCropCircles:
		case MenuCommands.SpyBuzzRadarTowers:
			Debug.Log("Saucer Command "+cmd.ToString());
			break;
		}
	}
		
	void OnShowMenu(CtxMenu menu)
	{
		//Debug.Log("SaucerHandler.OnShowMenu() "+menu);
	}

	void SetColorScheme(Material hullMat, Material glowMat)
	{
		MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer mr in meshRenderers)
		{
			if (mr.material.name.Contains("Glow"))
				mr.material = glowMat;
			else
				mr.material = hullMat;
		}
	}
}
