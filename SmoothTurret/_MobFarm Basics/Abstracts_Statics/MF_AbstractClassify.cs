using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractclassify.html")]
public abstract class MF_AbstractClassify : MonoBehaviour {

	public FactionsBlock factions;

	[HideInInspector] public MF_AbstractSelection selectionScript;
	[HideInInspector] public MFnum.FactionType myFaction;

	[System.Serializable]
	public class FactionsBlock {
		public MFnum.FactionType[] enemies;
		public MFnum.FactionType[] allies;
		public MFnum.FactionType[] neutral;
	}

	public virtual void Awake () {
		selectionScript = GetComponent<MF_AbstractSelection>();
	}

	public virtual MFnum.Relation FindRelation ( MFnum.FactionType faction ) {
		if ( TestRelation( faction, factions.enemies ) == true ) { return MFnum.Relation.Enemy; }
		if ( TestRelation( faction, factions.allies ) == true ) { return MFnum.Relation.Ally; }
		if ( TestRelation( faction, factions.neutral ) == true ) { return MFnum.Relation.Neutral; }
		return MFnum.Relation.Unknown;
	}

	bool TestRelation ( MFnum.FactionType faction, MFnum.FactionType[] array ) {
		for ( int i=0; i < array.Length; i++ ) {
			if ( faction == array[i] ) { return true; }
		}
		return false;
	}

}
