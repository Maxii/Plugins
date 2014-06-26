// ! DO NOT MESS WITH THE PREPROCESSOR FLAGS !
// The flag will be uncommented and commented by the menu items editor script. Do not change it manually, or you will break things.

//#define PLAYMAKER_PRESENT

#if PLAYMAKER_PRESENT
using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker.Actions;

namespace HutongGames.PlayMaker.Actions {
	[ActionCategory( "Grid Framework" )]
	/// <summary>Abstract base class for all Playmaker actions involving Grid Framework</summary>
	public abstract class FsmGFStateAction<T> : FsmStateAction where T : GFGrid {
		#region Cache variables
		[RequiredField]
		[CheckForComponent(typeof(GFGrid))]
		[Tooltip("GameObject that carries the grid, defaults to the owner of the FSM.")]
		/// <summary>The GameObject that carries the grid this action will refert to.</summary>
		public FsmOwnerDefault gridGameObject;
		/// <summary>The actual grid component used for all actions.</summary>
		protected T grid;
		#endregion

		/// <summary>Whether to run the action every frame.</summary>
		/// 
		/// If the action is running every frame i will never call the Finish() method.
		public bool everyFrame;

		/// <summary>This is where the action itself is performed.</summary>
		protected abstract void DoAction ();

		#region Common methods
		public override void Reset () {
			everyFrame = false;
		}

		public override void OnEnter () {
			if (!SetupCaches ())
				return;
			DoAction ();

			if (!everyFrame) {
				Finish ();
			}
		}

		public override void OnUpdate () {
			DoAction ();
		}

		/// <summary>Makes sure the `grid` is set to something.</summary>
		/// The method assigns a gid component to the `grid` instance variable. If is fails in finding the 
		/// component it will return `false`, preventing null exceptions.
		/// 
		/// First the method tries if there is already a variable to the grid component. If not, then it tries 
		/// to find a component on the given gameObject (by default the owner). If yes, then it uses that.
		protected bool SetupCaches () {
			//if (!Fsm.GetOwnerDefaultTarget (gridGameObject))
			//	return false;
			grid = Fsm.GetOwnerDefaultTarget (gridGameObject).GetComponent<T> ();
			return (grid != null);
		}
		#endregion
	}

	// The following inheritance steps are not really necessary, but they make it easier to categorize actions based on their types.
	#region Get & Set actions
	/// <summary>Abstract class for all Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSet<T> : FsmGFStateAction<T> where T : GFGrid {}
	
	/// <summary>Abstract class for all common Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSetGrid : FsmGFStateActionGetSet<GFGrid> {}

	/// <summary>Abstract class for all rectangular Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSetRect : FsmGFStateActionGetSet<GFRectGrid> {}

	/// <summary>Abstract class for all layered Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSetLayerd<T> : FsmGFStateActionGetSet<T> where T : GFLayeredGrid {}

	/// <summary>Abstract class for all hexagonal Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSetHex : FsmGFStateActionGetSetLayerd<GFHexGrid> {}

	/// <summary>Abstract class for all polar Grid Framework Playmaker Get&Set actions.</summary>
	public abstract class FsmGFStateActionGetSetPolar : FsmGFStateActionGetSetLayerd<GFPolarGrid> {}
	#endregion

	#region Method actions
	/// <summary>Abstract class for all Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethod<T> : FsmGFStateAction<T> where T : GFGrid {}

	/// <summary>Abstract class for all common Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethodGrid : FsmGFStateActionMethod<GFGrid>{}

	/// <summary>Abstract class for all rectangular Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethodRect : FsmGFStateActionMethod<GFRectGrid>{}

	/// <summary>Abstract class for all layered Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethodLayered<T> : FsmGFStateActionMethod<T> where T : GFLayeredGrid{}

	/// <summary>Abstract class for all hexagonal Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethodHex : FsmGFStateActionMethodLayered<GFHexGrid>{}

	/// <summary>Abstract class for all polar Grid Framework Playmaker method actions.</summary>
	public abstract class FsmGFStateActionMethodPolar : FsmGFStateActionMethodLayered<GFPolarGrid>{}
	#endregion
}
#endif // PLAYMAKER_PRESENT