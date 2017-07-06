using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_utilitykeys.html")]
public class MF_UtilityKeys : MonoBehaviour {

	[Split1(30, "Pause/Unpause the game.")]
	public string pause = "p";
	[Split1(30, "Plays one frame every time the key is pressed.")]
	public string frame = "f";
	[Split2(40, "The frame key will play frames while held down")]
	public bool playWhileHeld;

	[Header("Time Scale:")]
	[Split1("Halves the current time scale.")]
	public string timeDecrease = "[";
	[Split2("Doubles the current time scale.")]
	public string timeIncrease = "]";
	[Split1("Shows the current time scale, or may be used to enter a specific time scale.")]
	[SerializeField] private float _playScale = 1f;
	public float playScale { 
		get { return _playScale; }
		set { _playScale = value;
			if ( paused == false ) { Time.timeScale = _playScale; }
		}
	}

	void OnValidate () {
		playScale = _playScale;
	}

	bool paused;
	bool oneFrame;

	void Start () {
		AudioListener.pause = false;
	}
	
	void Update () {

		if ( oneFrame == true ) {
			Pause( true );
		}

		if ( Input.GetKeyDown( pause ) ) {
			Pause( !paused );
			oneFrame = false;
		}

		if ( Input.GetKeyDown( frame ) ) {
			oneFrame = true;
			Pause( false );
		}

		if ( playWhileHeld == true && Input.GetKey( frame ) ) {
			oneFrame = true;
			Pause( false );
		}

		if ( Input.GetKeyDown( timeDecrease ) ) {
			playScale *= .5f;
		}

		if ( Input.GetKeyDown( timeIncrease ) ) {
			playScale *= 2f;
		}

	}

	void Pause ( bool p ) {
		if ( p == true ) {
			Time.timeScale = 0f;
		} else {
			Time.timeScale = _playScale;
		}
		AudioListener.pause = p;
		paused = p;
	}
}
