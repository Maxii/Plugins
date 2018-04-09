using UnityEngine;
using System.Collections;

public class WMG_Skill_Tree : MonoBehaviour {
	
	public GameObject shieldTree;
	public GameObject engineTree;
	public GameObject weaponTree;
	public GameObject shieldTreeBackground;
	public GameObject engineTreeBackground;
	public GameObject weaponTreeBackground;
	public GameObject toolTip;
	public GameObject toolTipText1;
	public GameObject toolTipText2;
	public GameObject toolTipText3;
	public GameObject toolTipText4;
	private float hoverTreeDuration = 0.2f;
	private float treeClickAnimDuration = 1;
	private bool shieldClicked = false;
	private bool engineClicked = false;
	private bool weaponClicked = false;
	private bool treeAnimating = false;
	private UILabel toolTipText1Label;
	private UILabel toolTipText2Label;
	private UILabel toolTipText3Label;
	private UILabel toolTipText4Label;
	
	// Use this for initialization
	void Start () {
		// Convert to 3d
		this.transform.localPosition = new Vector3 (0,300,900);
		shieldTree.transform.localEulerAngles = new Vector3 (0, 15, 0);
		weaponTree.transform.localEulerAngles = new Vector3 (345, 0, 0);
		engineTree.transform.localEulerAngles = new Vector3 (0, 345, 0);
		
		UIEventListener.Get(shieldTreeBackground).onHover += OnShieldTreeHover;
		UIEventListener.Get(engineTreeBackground).onHover += OnEngineTreeHover;
		UIEventListener.Get(weaponTreeBackground).onHover += OnWeaponTreeHover;
		UIEventListener.Get(shieldTreeBackground).onClick += OnShieldTreeClick;
		UIEventListener.Get(engineTreeBackground).onClick += OnEngineTreeClick;
		UIEventListener.Get(weaponTreeBackground).onClick += OnWeaponTreeClick;
		
		toolTipText1Label = toolTipText1.GetComponent<UILabel>();
		toolTipText2Label = toolTipText2.GetComponent<UILabel>();
		toolTipText3Label = toolTipText3.GetComponent<UILabel>();
		toolTipText4Label = toolTipText4.GetComponent<UILabel>();
		
		foreach (Transform child in shieldTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				UIEventListener.Get(child.gameObject).onHover += OnSkillNodeHover;
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = false;
			}
		}
		foreach (Transform child in engineTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				UIEventListener.Get(child.gameObject).onHover += OnSkillNodeHover;
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = false;
			}
		}
		foreach (Transform child in weaponTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				UIEventListener.Get(child.gameObject).onHover += OnSkillNodeHover;
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = false;
			}
		}
	}
	
	void OnShieldTreeHover(GameObject go, bool hover) {
		if (!treeAnimating && !shieldClicked) {
			if (hover) {
				Quaternion newRot = shieldTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(0,10,0);
				TweenRotation.Begin(shieldTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,175/255f);
			}
			else {
				Quaternion newRot = shieldTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(0,15,0);
				TweenRotation.Begin(shieldTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,100/255f);
			}
		}
	}
	
	void OnEngineTreeHover(GameObject go, bool hover) {
		if (!treeAnimating && !engineClicked) {
			if (hover) {
				Quaternion newRot = engineTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(0,350,0);
				TweenRotation.Begin(engineTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,175/255f);
			}
			else {
				Quaternion newRot = engineTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(0,345,0);
				TweenRotation.Begin(engineTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,100/255f);
			}
		}
	}
	
	void OnWeaponTreeHover(GameObject go, bool hover) {
		if (!treeAnimating && !weaponClicked) {
			if (hover) {
				Quaternion newRot = weaponTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(350,0,0);
				TweenRotation.Begin(weaponTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,175/255f);
			}
			else {
				Quaternion newRot = weaponTree.transform.localRotation;
				newRot.eulerAngles = new Vector3(345,0,0);
				TweenRotation.Begin(weaponTree, hoverTreeDuration, newRot);
				TweenAlpha.Begin(go,hoverTreeDuration,100/255f);
			}
		}
	}
	
	void OnSkillNodeHover(GameObject go, bool hover) {
		if (hover) {
			toolTipText1Label.text = SkillTreeGetSkillText1(go.name);
			toolTipText2Label.text = SkillTreeGetSkillText2(go.name);
			toolTipText3Label.text = SkillTreeGetSkillText3(go.name);
			toolTipText4Label.text = SkillTreeGetSkillText4(go.name);
			SetActive(toolTip,true);
			toolTip.transform.position = go.transform.position;
			toolTip.transform.localPosition = new Vector3(toolTip.transform.localPosition.x, toolTip.transform.localPosition.y, toolTip.transform.localPosition.z - 70);
			if (toolTip.transform.localPosition.y > -130) toolTip.transform.localPosition = new Vector3 (toolTip.transform.localPosition.x, -130, toolTip.transform.localPosition.z);
			if (toolTip.transform.localPosition.y < -470) toolTip.transform.localPosition = new Vector3 (toolTip.transform.localPosition.x, -470, toolTip.transform.localPosition.z);
			if (toolTip.transform.localPosition.x >= 5) toolTip.transform.localPosition = new Vector3 (toolTip.transform.localPosition.x - 170, toolTip.transform.localPosition.y, toolTip.transform.localPosition.z);
			else toolTip.transform.localPosition = new Vector3 (toolTip.transform.localPosition.x + 170, toolTip.transform.localPosition.y, toolTip.transform.localPosition.z);
		}
		else {
			SetActive(toolTip,false);
		}
	}
	
	void OnShieldTreeClick(GameObject go) {
		if (!treeAnimating && !shieldClicked) {
			shieldClicked = !shieldClicked;
			treeAnimating = true;
			Quaternion newRot = shieldTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(0,0,90);
			TweenRotation.Begin(shieldTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(shieldTree, treeClickAnimDuration, new Vector3(0,100,-350));
			tpos.method = UITweener.Method.EaseIn;
			TweenAlpha.Begin(go,treeClickAnimDuration,70/255f);
			
			foreach (Transform child in shieldTree.transform) {
				WMG_Link aLink = child.GetComponent<WMG_Link>();
				if (aLink != null) {
					newRot.eulerAngles = new Vector3(0,0,180);
					TweenRotation tro = TweenRotation.Begin(aLink.objectToScale, treeClickAnimDuration, newRot);
					tro.method = UITweener.Method.BounceIn;
				}
			}
			TweenAlpha.Begin(engineTree,treeClickAnimDuration,0);
			TweenAlpha.Begin(weaponTree,treeClickAnimDuration,0);
			
			tpos.callWhenFinished = "endShieldTreeClicked";
			tpos.eventReceiver = this.gameObject;
		}
		if (!treeAnimating && shieldClicked) {
			shieldClicked = !shieldClicked;
			treeAnimating = true;
			Quaternion newRot = shieldTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(0,15,0);
			TweenRotation.Begin(shieldTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(shieldTree, treeClickAnimDuration, new Vector3(0,0,0));
			TweenAlpha.Begin(go,treeClickAnimDuration,100/255f);
			
			foreach (Transform child in shieldTree.transform) {
				WMG_Link aLink = child.GetComponent<WMG_Link>();
				if (aLink != null) {
					newRot.eulerAngles = new Vector3(0,0,0);
					TweenRotation.Begin(aLink.objectToScale, treeClickAnimDuration, newRot);
				}
			}
			TweenAlpha.Begin(engineTree,treeClickAnimDuration,1);
			TweenAlpha.Begin(weaponTree,treeClickAnimDuration,1);
			
			tpos.callWhenFinished = "endTreeClickAnim";
			tpos.eventReceiver = this.gameObject;
			
			foreach (Transform child in shieldTree.transform) {
				WMG_Node aNode = child.GetComponent<WMG_Node>();
				if (aNode != null) {
					BoxCollider aCol = child.GetComponent<BoxCollider>();
					if (aCol != null) aCol.enabled = false;
				}
			}
		}
		
	}
	
	void OnEngineTreeClick(GameObject go) {
		if (!treeAnimating && !engineClicked) {
			engineClicked = !engineClicked;
			treeAnimating = true;
			Quaternion newRot = engineTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(0,0,270);
			TweenRotation.Begin(engineTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(engineTree, treeClickAnimDuration, new Vector3(0,100,-350));
			tpos.method = UITweener.Method.EaseIn;
			TweenAlpha.Begin(go,treeClickAnimDuration,70/255f);
			
			foreach (Transform child in engineTree.transform) {
				WMG_Link aLink = child.GetComponent<WMG_Link>();
				if (aLink != null) {
					newRot.eulerAngles = new Vector3(0,0,180);
					TweenRotation tro = TweenRotation.Begin(aLink.objectToScale, treeClickAnimDuration, newRot);
					tro.method = UITweener.Method.BounceIn;
				}
			}
			TweenAlpha.Begin(shieldTree,treeClickAnimDuration,0);
			TweenAlpha.Begin(weaponTree,treeClickAnimDuration,0);
			
			tpos.callWhenFinished = "endEngineTreeClicked";
			tpos.eventReceiver = this.gameObject;
		}
		if (!treeAnimating && engineClicked) {
			engineClicked = !engineClicked;
			treeAnimating = true;
			Quaternion newRot = engineTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(0,345,0);
			TweenRotation.Begin(engineTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(engineTree, treeClickAnimDuration, new Vector3(0,0,0));
			TweenAlpha.Begin(go,treeClickAnimDuration,100/255f);
			
			foreach (Transform child in engineTree.transform) {
				WMG_Link aLink = child.GetComponent<WMG_Link>();
				if (aLink != null) {
					newRot.eulerAngles = new Vector3(0,0,0);
					TweenRotation.Begin(aLink.objectToScale, treeClickAnimDuration, newRot);
				}
			}
			TweenAlpha.Begin(shieldTree,treeClickAnimDuration,1);
			TweenAlpha.Begin(weaponTree,treeClickAnimDuration,1);
			
			tpos.callWhenFinished = "endTreeClickAnim";
			tpos.eventReceiver = this.gameObject;
			
			foreach (Transform child in engineTree.transform) {
				WMG_Node aNode = child.GetComponent<WMG_Node>();
				if (aNode != null) {
					BoxCollider aCol = child.GetComponent<BoxCollider>();
					if (aCol != null) aCol.enabled = false;
				}
			}
		}
	}
	
	void OnWeaponTreeClick(GameObject go) {
		if (!treeAnimating && !weaponClicked) {
			weaponClicked = !weaponClicked;
			treeAnimating = true;
			Quaternion newRot = weaponTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(0,0,0);
			TweenRotation.Begin(weaponTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(weaponTree, treeClickAnimDuration, new Vector3(0,100,-350));
			tpos.method = UITweener.Method.EaseIn;
			TweenAlpha.Begin(go,treeClickAnimDuration,70/255f);
			
			TweenAlpha.Begin(shieldTree,treeClickAnimDuration,0);
			TweenAlpha.Begin(engineTree,treeClickAnimDuration,0);
			
			tpos.callWhenFinished = "endWeaponTreeClicked";
			tpos.eventReceiver = this.gameObject;
		}
		if (!treeAnimating && weaponClicked) {
			weaponClicked = !weaponClicked;
			treeAnimating = true;
			Quaternion newRot = weaponTree.transform.localRotation;
			newRot.eulerAngles = new Vector3(345,0,0);
			TweenRotation.Begin(weaponTree, treeClickAnimDuration, newRot);
			TweenPosition tpos = TweenPosition.Begin(weaponTree, treeClickAnimDuration, new Vector3(0,-150,0));
			TweenAlpha.Begin(go,treeClickAnimDuration,100/255f);
			
			TweenAlpha.Begin(shieldTree,treeClickAnimDuration,1);
			TweenAlpha.Begin(engineTree,treeClickAnimDuration,1);
			
			tpos.callWhenFinished = "endTreeClickAnim";
			tpos.eventReceiver = this.gameObject;
			
			foreach (Transform child in weaponTree.transform) {
				WMG_Node aNode = child.GetComponent<WMG_Node>();
				if (aNode != null) {
					BoxCollider aCol = child.GetComponent<BoxCollider>();
					if (aCol != null) aCol.enabled = false;
				}
			}
		}
	}
	
	void endShieldTreeClicked() {
		endTreeClickAnim();
		foreach (Transform child in shieldTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = true;
			}
		}
	}
	
	void endEngineTreeClicked() {
		endTreeClickAnim();
		foreach (Transform child in engineTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = true;
			}
		}
	}
	
	void endWeaponTreeClicked() {
		endTreeClickAnim();
		foreach (Transform child in weaponTree.transform) {
			WMG_Node aNode = child.GetComponent<WMG_Node>();
			if (aNode != null) {
				BoxCollider aCol = child.GetComponent<BoxCollider>();
				if (aCol != null) aCol.enabled = true;
			}
		}
	}
	
	void endTreeClickAnim() {
		treeAnimating = false;
	}
	
	// Data for the tooltip text in this example is just stored in this script, alternatively the data could be stored on the node itself.
	// You could attach the data to the node itself by creating another script and attaching it to each node, or by extending the WMG Node script.
	// If extending WMG Node, create a new prefab with your extended version attached and remove the WMG Node script, and then use the Swap Prefabs editor action.
	
	string SkillTreeGetSkillText1(string skillName) {
		if (skillName == "Engines") {
			return "Engines (0 / 5)";
		}
		else if (skillName == "Reactors") {
			return "Reactors (0 / 5)";
		}
		else if (skillName == "Speed Boost") {
			return "Speed Boost (0 / 2)";
		}
		else if (skillName == "Barrel Roll") {
			return "Barrel Roll (0 / 2)";
		}
		else if (skillName == "Overdrive") {
			return "Overdrive (0 / 1)";
		}
		else if (skillName == "Shields") {
			return "Shields (0 / 5)";
		}
		else if (skillName == "Hull") {
			return "Hull (0 / 5)";
		}
		else if (skillName == "Reflect") {
			return "Reflect (0 / 2)";
		}
		else if (skillName == "Subsystems") {
			return "Subsystems (0 / 2)";
		}
		else if (skillName == "Fortress") {
			return "Fortress (0 / 1)";
		}
		else if (skillName == "Beams") {
			return "Beams (0 / 5)";
		}
		else if (skillName == "Cannons") {
			return "Cannons (0 / 5)";
		}
		else if (skillName == "Launchers") {
			return "Launchers (0 / 5)";
		}
		else if (skillName == "Pulse Charge") {
			return "Pulse Charge (0 / 3)";
		}
		else if (skillName == "Rapid Fire") {
			return "Rapid Fire (0 / 3)";
		}
		else if (skillName == "EMP Nuke") {
			return "EMP Nuke (0 / 3)";
		}
		else if (skillName == "Antigravity Nuke") {
			return "Antigravity Nuke (0 / 3)";
		}
		else if (skillName == "Whirlwind Beam") {
			return "Whirlwind Beam (0 / 1)";
		}
		else if (skillName == "Cone Cannon") {
			return "Cone Cannon (0 / 1)";
		}
		else if (skillName == "Thermo Warhead") {
			return "Thermo Warhead (0 / 1)";
		}
		else if (skillName == "Weapons Basics") {
			return "Weapon Skills";
		}
		else if (skillName == "Engines Basics") {
			return "Engine Skills";
		}
		else if (skillName == "Defenses Basics") {
			return "Defensive Skills";
		}
		else {
			return "";
		}
	}
	
	string SkillTreeGetSkillText2(string skillName) {
		if (skillName == "Engines") {
			return "Acceleration: 100%\nMax Speed: 100%\nTurn Speed: 100%";
		}
		else if (skillName == "Reactors") {
			return "Shield Recharge Speed: 100%\nWeapon Recharge Speed: 100%";
		}
		else if (skillName == "Speed Boost") {
			return "Burst forward to escape\nor chase down enemies\nCooldown: 15 seconds";
		}
		else if (skillName == "Barrel Roll") {
			return "Tactical manuever used to dodge\ndevastating beams and missiles\nCooldown: 10 seconds";
		}
		else if (skillName == "Overdrive") {
			return "Temporarily boost acceleration,\nmax speed, turn speed,\nand weapon recharge\nCooldown: 1 minute";
		}
		else if (skillName == "Shields") {
			return "Shield Density: 100%\nDamage Reduction: 0%";
		}
		else if (skillName == "Hull") {
			return "Hull: 100%\nDamage Reduction: 0%\nCargo Capacity: 100%";
		}
		else if (skillName == "Reflect") {
			return "Chance to reflect beams: 5%";
		}
		else if (skillName == "Subsystems") {
			return "Unlocks ability to use\npoint defense turrets";
		}
		else if (skillName == "Fortress") {
			return "Temporarily immune to all damage\nEffect Lasts: 10 seconds\nCooldown: 1 minute";
		}
		else if (skillName == "Beams") {
			return "Ability to equip basic beams\nDamage: 100%\nRange: 100%";
		}
		else if (skillName == "Cannons") {
			return "Ability to equip basic cannons\nDamage: 100%\nRange: 100%";
		}
		else if (skillName == "Launchers") {
			return "Ability to equip basic launchers\nDamage: 100%\nRange: 100%";
		}
		else if (skillName == "Pulse Charge") {
			return "Charge up a powerful beam attack\nDamage: 100%";
		}
		else if (skillName == "Rapid Fire") {
			return "Double the rate of cannon fire\nEffect lasts: 5 seconds";
		}
		else if (skillName == "EMP Nuke") {
			return "Launch a nuke that disables\nenemy ship and engine systems\nRadius: 100%";
		}
		else if (skillName == "Antigravity Nuke") {
			return "Launch a nuke that sucks in\nsmall enemy ships\nRadius: 100%";
		}
		else if (skillName == "Whirlwind Beam") {
			return "Charge up a spinning beam\nattack to quickly disperse\nsurrounding enemies\nCooldown: 1 minute";
		}
		else if (skillName == "Cone Cannon") {
			return "Rapidly fire cannons\nin a spread of fire\nCooldown: 1 minute";
		}
		else if (skillName == "Thermo Warhead") {
			return "Launch a devastating warhead\nCooldown: 30 seconds";
		}
		else {
			return "";
		}
	}
	
	string SkillTreeGetSkillText3(string skillName) {
		if (skillName == "Engines") {
			return "Next Level";
		}
		else if (skillName == "Reactors") {
			return "Next Level";
		}
		else if (skillName == "Speed Boost") {
			return "Next Level";
		}
		else if (skillName == "Barrel Roll") {
			return "Next Level";
		}
		else if (skillName == "Overdrive") {
			return "";
		}
		else if (skillName == "Shields") {
			return "Next Level";
		}
		else if (skillName == "Hull") {
			return "Next Level";
		}
		else if (skillName == "Reflect") {
			return "Next Level";
		}
		else if (skillName == "Subsystems") {
			return "Next Level";
		}
		else if (skillName == "Fortress") {
			return "";
		}
		else if (skillName == "Beams") {
			return "Next Level";
		}
		else if (skillName == "Cannons") {
			return "Next Level";
		}
		else if (skillName == "Launchers") {
			return "Next Level";
		}
		else if (skillName == "Pulse Charge") {
			return "Next Level";
		}
		else if (skillName == "Rapid Fire") {
			return "Next Level";
		}
		else if (skillName == "EMP Nuke") {
			return "Next Level";
		}
		else if (skillName == "Antigravity Nuke") {
			return "Next Level";
		}
		else if (skillName == "Whirlwind Beam") {
			return "";
		}
		else if (skillName == "Cone Cannon") {
			return "";
		}
		else if (skillName == "Thermo Warhead") {
			return "";
		}
		else {
			return "";
		}
	}
	
	
	string SkillTreeGetSkillText4(string skillName) {
		if (skillName == "Engines") {
			return "Acceleration: + 10%\nMax Speed: + 10%\nTurn Speed: + 10%";
		}
		else if (skillName == "Reactors") {
			return "Shield Recharge Speed: + 20%\nWeapon Recharge Speed: 20%";
		}
		else if (skillName == "Speed Boost") {
			return "Cooldown: 8 seconds";
		}
		else if (skillName == "Barrel Roll") {
			return "Cooldown: 5 seconds";
		}
		else if (skillName == "Overdrive") {
			return "";
		}
		else if (skillName == "Shields") {
			return "Shield Density: + 30%\nDamage Reduction: + 5%";
		}
		else if (skillName == "Hull") {
			return "Hull: + 30%\nDamage Reduction: + 5%\nCargo Capacity: + 20%";
		}
		else if (skillName == "Reflect") {
			return "Chance to reflect beams: 10%";
		}
		else if (skillName == "Subsystems") {
			return "Unlocks ability to use drone bay";
		}
		else if (skillName == "Fortress") {
			return "";
		}
		else if (skillName == "Beams") {
			return "Damage: + 15%\nRange: + 10%";
		}
		else if (skillName == "Cannons") {
			return "Damage: + 15%\nRange: + 10%";
		}
		else if (skillName == "Launchers") {
			return "Damage: + 15%\nRange: + 10%";
		}
		else if (skillName == "Pulse Charge") {
			return "Damage: + 50%";
		}
		else if (skillName == "Rapid Fire") {
			return "Effect lasts: 8 seconds";
		}
		else if (skillName == "EMP Nuke") {
			return "Radius: + 20%";
		}
		else if (skillName == "Antigravity Nuke") {
			return "Radius: + 20%";
		}
		else if (skillName == "Whirlwind Beam") {
			return "";
		}
		else if (skillName == "Cone Cannon") {
			return "";
		}
		else if (skillName == "Thermo Warhead") {
			return "";
		}
		else {
			return "";
		}
	}
	
	void SetActive(GameObject obj, bool state) {
		#if (UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0)
			obj.SetActiveRecursively(state);
		#else
		    obj.SetActive(state);
		#endif
	}
	
}
