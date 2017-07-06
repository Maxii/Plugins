using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_stats.html")]
public class MF_B_Stats : MF_AbstractStats {

	public override float shield {
		get { return _shield; }
		set { _shield = value; 
			UpdateStats();
			if ( _shield <= 0f ) {
				//
			}
		}
	}
	public override float shieldMax {
		get { return _shieldMax; }
		set { _shieldMax = value;
			UpdateStats();
		}
	}
	public override float armor {
		get { return _armor; }
		set { _armor = value;
			UpdateStats();
			if ( value <= 0f ) {
				//
			}
		}
	}
	public override float armorMax {
		get { return _armorMax; }
		set { _armorMax = value;
			UpdateStats();
		}
	}
	public override float health {
		get { return _health; }
		set { _health = value;
			UpdateStats();
			if ( _health <= 0f ) {
				DoDying();
			}
		}
	}
	public override float healthMax {
		get { return _healthMax; }
		set { _healthMax = value;
			UpdateStats();
		}

	}
	public override float energy {
		get { return _energy; }
		set { _energy = value;
			UpdateStats();
			if ( _energy <= 0f ) {
				//
			}
		}
	}
	public override float energyMax {
		get { return _energyMax; }
		set { _energyMax = value;
			UpdateStats();
		}
	}

	public override float ApplyDamage ( MFnum.StatType stat, float damage, MFnum.DamageType damType, Vector3 loc, AudioSource audio, float multS, float multA, float multH, float multE, float multPen, float addReduce ) {
		if ( damage == 0 ) { return 0f; } // early out

		float strength = 0;
		float maxStrength = 0f;

		if ( stat == MFnum.StatType.Shield ) { strength = shield; maxStrength = shieldMax; }
		else if ( stat == MFnum.StatType.Armor ) { strength = armor; maxStrength = armorMax; }
		else if ( stat == MFnum.StatType.Health ) { strength = health; maxStrength = healthMax; }
		else if ( stat == MFnum.StatType.Energy ) { strength = energy; maxStrength = energyMax; }

		if ( strength <= 0 && damage > 0 ) { return damage; } // is damage, and health is at min 
		if ( strength >= maxStrength && damage < 0 ) { return damage; } // is heal, and health is at max

		float d = damage; // damage to strength
		float r = damage; // damage to pass along

		d = Mathf.Clamp( d, 	-maxStrength + strength, strength ); // damage can't do more than available strength, heals can't do more than available missing strength
		r = damage - d ;

		if ( stat == MFnum.StatType.Shield ) { shield -= d; }
		else if ( stat == MFnum.StatType.Armor ) { armor -= d; }
		else if ( stat == MFnum.StatType.Health ) { health -= d; }
		else if ( stat == MFnum.StatType.Energy ) { energy -= d; }

		return r;
	}

	public override void DoDying () {
		// must be before fx script
		if ( shield > 0 ) { shield = 0f; }
		if ( armor > 0 ) { armor = 0f; }
		if ( health > 0 ) { health = 0f; }
		if ( energy > 0 ) { energy = 0f; }

		base.DoDying();
	}

}
