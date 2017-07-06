using UnityEngine;
using System.Collections;

public class MF_LensFlare : MonoBehaviour {

	[Tooltip("The LensFlare to control.")]
	public LensFlare flare;
	[Tooltip("If true, the flare will be set to active upon Awake()")]
	public bool beginActive;
	[Tooltip("Time in seconds to change state between on an off. Used by MF_FxController.")]
	public float fadeTime; // seconds for MF_FxController
	[Tooltip("Scale the apparent size of the flare by distance.")]
	public bool scaleByDistance;
	[Tooltip("Scale the apparent size by the total scale in the hierarchy.")]
	public bool scaleByHierarchy;
	[Tooltip("Max distance the flare can be seen from. Also used to calculate when apparent size should be 0.")]
	public float maxDistance;
	[Tooltip("Apparent size when at 0 distance to the camera.")]
	public float brightness;
	[Tooltip("Use the defined custom curve to govern size falloff. The built-in default mimics brightness to appear as an object with a consistant size with range.")]
	public bool useCustomCurve;
	[Tooltip("A custom size falloff curve.\nVertical axis is percent of brightness.\nHorizontal is distance from camera out to Max Distance.")]
	public AnimationCurve brightnessCurve;
	[Tooltip("An additional multiplier that can be used to scale brightness.")]
	public float multiplier = 1f; // use for emitter strength, such as an engine throttle %

	AnimationCurve defaultCurve;
	AnimationCurve currentCurve;
	Transform cameraTrans;
	float distance;
	float fadeMult;
	float fadeGoal;
	bool timerRunning;
	float scaleMult;

	void Awake () {
		if ( flare == null ) { flare = gameObject.GetComponent<LensFlare>(); }
		 
		// stored curve - mimics flare to appear as an object with a consistant physical size
		defaultCurve = new AnimationCurve(
			new Keyframe( 0f, 1f, -25.39425f, -25.39425f ),
			new Keyframe( 0.05089224f, 0.2368828f, -2.828486f, -2.828486f ),
			new Keyframe( 0.2290285f, 0.04278612f, -0.1754783f, -0.1754783f ),
			new Keyframe( 1f, 0.004545453f, 0f, 0f )
		);
		fadeMult = beginActive ? 1f : 0f;
		if ( scaleByHierarchy == true ) {
			scaleMult = Mathf.Max( transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z );
		} else {
			scaleMult = 1f;
		}
	}

	void OnDisable () {
		timerRunning = false;
	}

	void Update () {
		if ( flare ) {
			if ( brightness == 0f || fadeMult == 0f || multiplier == 0f ) {
				flare.brightness = 0f;
				return;
			}
			if ( scaleByDistance == true ) {
				if ( Camera.main ) {
					cameraTrans = Camera.main.transform;
					distance = Vector3.Distance( cameraTrans.position, flare.transform.position );
					if ( distance <= maxDistance ) {
						currentCurve = useCustomCurve ? brightnessCurve : defaultCurve; // select curve to use
						flare.brightness = currentCurve.Evaluate( distance / maxDistance ) * brightness * fadeMult * multiplier * scaleMult;
					} else {
						flare.brightness = 0f;
					}
				}
			} else {
				flare.brightness = brightness * fadeMult * multiplier;
			}
		}
	}

	public void FadeIn () {
		fadeGoal = 1f;
		if ( timerRunning == false && gameObject.activeInHierarchy == true ) { StartCoroutine( FadeTimer() ); }
	}

	public void FadeOut () {
		fadeGoal = 0f;
		if ( timerRunning == false && gameObject.activeInHierarchy == true ) { StartCoroutine( FadeTimer() ); }
	}

	IEnumerator FadeTimer () {
		while ( fadeMult > 0f && fadeMult < 1f || timerRunning == false ) {
			timerRunning = true;
			float step = Time.deltaTime / fadeTime; 
			float mult = (fadeGoal - .5f) * 2f; // gives -1 or 1
			fadeMult = Mathf.Clamp( fadeMult + (step * mult),	0f, 1f );
			yield return null;
		}
		timerRunning = false;
	}

}
