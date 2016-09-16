using UnityEngine;
using System.Collections;

public class MF_UtilityKeys : MonoBehaviour {

	[Tooltip("Pause/Unpause the game.")]
	public string pause = "p";
	[Tooltip("Plays one frame every time the key is pressed.")]
	public string frame = "f";

	[Header("Time Scale:")]
	public string timeDecrease = "[";
	public string timeIncrease = "]";
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
