using UnityEngine;
using System.Collections;

public class SaucerHandler : MonoBehaviour
{
	public enum MenuCommands
	{
		Small,
		Medium,
		Large,
		
		ColorSchemeMirage = 5,
		ColorSchemeVineyard,
		ColorSchemeSunset,
		ColorSchemeHolly,
		ColorSchemeEmbers,

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

	public MenuCommands currentColorScheme;
	public Material[] hullMaterials;
	public Material[] glowMaterials;
	
	public void OnMenuSelection(int item)
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
		
		case MenuCommands.ColorSchemeMirage:
		case MenuCommands.ColorSchemeVineyard:
		case MenuCommands.ColorSchemeSunset:
		case MenuCommands.ColorSchemeHolly:
		case MenuCommands.ColorSchemeEmbers:
		{
			int cs = item - (int)MenuCommands.ColorSchemeMirage;
			SetColorScheme(hullMaterials[cs], glowMaterials[cs]);
			currentColorScheme = cmd;
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
		
	public void OnShowMenu(CtxObject obj)
	{
		//Debug.Log("SaucerHandler.OnShowMenu() "+obj);
		obj.SetChecked((int)currentColorScheme, true);
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
