using UnityEngine;
using System.Collections;

public class SpaceD_LoadingBar : MonoBehaviour {
	
	public bool Example = false;
	public UISprite fillSprite;
	
	void Start()
	{
		// Start the example loading bar progress coroutine
		if (this.Example)
			this.StartCoroutine(LoadingProgress());
	}
	
	void OnEnable()
	{
		// Start the example loading bar progress coroutine
		if (this.Example)
			this.StartCoroutine(LoadingProgress());
	}
	
	public void SetAmount(float amount)
	{
		if (this.fillSprite == null) return;
		this.fillSprite.fillAmount = amount;
	}
	
	private IEnumerator LoadingProgress()
	{
		float Duration = 4.0f;
		float ResetDelay = 1.0f;
		
		// Reset to 0%
		this.SetAmount(0f);
		
		// Get the timestamp
		float startTime = Time.time;
		
		while (Time.time < (startTime + Duration))
		{
			float RemainingTime = (startTime + Duration) - Time.time;
			float ElapsedTime = Duration - RemainingTime;
			
			// update the percent value
			this.SetAmount(ElapsedTime / Duration);
			
			yield return 0;
		}
		
		// Round to 100%
		this.SetAmount(1f);
		
		// Duration of the display of the notification
		yield return new WaitForSeconds(ResetDelay);
		
		// Get the timestamp
		startTime = Time.time;
		
		while (Time.time < (startTime + Duration))
		{
			float RemainingTime = (startTime + Duration) - Time.time;
			
			// update the percent value
			this.SetAmount(RemainingTime / Duration);
			
			yield return 0;
		}
		
		// Reset to 0%
		this.SetAmount(0f);
		
		// Duration of the display of the notification
		yield return new WaitForSeconds(ResetDelay);
		
		// Start it again
		StartCoroutine(LoadingProgress());
	}
}
