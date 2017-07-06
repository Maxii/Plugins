using UnityEngine;
using System.Collections;

namespace MFnum {
	public enum NatoAlphabet { None, Alpha, Bravo, Charlie, Delta, Echo, Foxtrot, Golf, Hotel, India, Juliett, Kilo, Lima, Mike, November, 
							   Oscar, Papa, Quebec, Romeo, Sierra, Tango, Uniform, Victor, Whiskey, Xray, Yankee, Zulu }
	public enum Relation { Unknown, Enemy, Ally, Neutral }
	public enum ScanMethodType { Tags, Layers }
	public enum FactionType { None, Side0, Side1, Side2, Side3 }; // if using layers or tags to designate factions, these names need to match the layer or tag names
	public enum DamageType { General, Kinetic, Explosive, Thermal, Chemical, Energy, Ion } // sci-fi types
	//	public enum DamageType { General, Sharp, Blunt, Water, Air, Earth, Fire, Frost, Arcane, Time, Bleed, Poison, Mental, Death, Life } // alternate fantasy types example
	public enum StatType { General, Shield, Armor, Health, Energy }
	public enum MarkType { Detected, Analyzed, PoI, JamSource }

}