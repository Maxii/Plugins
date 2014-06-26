// ! DO NOT MESS WITH THE PREPROCESSOR FLAGS !
// The flag will be uncommented and commented by the menu items editor script. Do not change it manually, or you will break things.

//#define PLAYMAKER_PRESENT

#if PLAYMAKER_PRESENT
using UnityEngine;
using System.Collections;

namespace HutongGames.PlayMaker.Actions {
	#region Grid Methods
	[Tooltip("Converts world coordinates to grid coordinates")]
	public class WorldToGrid : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 worldPosition;
		public FsmVector3 gridPosition;
		protected override void DoAction() { gridPosition.Value = grid.WorldToGrid(worldPosition.Value); }
	}

	[Tooltip("Converts grid coordinates to world coordinates")]
	public class GridToWorld : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 gridPosition;
		public FsmVector3 worldPosition;
		protected override void DoAction() { worldPosition.Value = grid.GridToWorld(gridPosition.Value); }
	}

	[Tooltip("Returns the world coordinates of the nearest vertex")]
	public class NearestVertexW : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 vertex;
		protected override void DoAction() { vertex.Value = grid.NearestVertexW (worldPoint.Value);	}
	}

	/// @todo implement NearestFaceW

	[Tooltip("Returns the world coordinates of the nearest box")]
	public class NearestBoxW : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 box;
		protected override void DoAction() { box.Value = grid.NearestBoxW (worldPoint.Value); }
	}

	[Tooltip("Returns the grid coordinates of the nearest vertex")]
	public class NearestVertexG : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 vertex;
		protected override void DoAction() { vertex.Value = grid.NearestVertexG (worldPoint.Value);	}
	}

	/// @todo implement NearestFaceG

	[Tooltip("Returns the grid coordinates of the nearest box")]
	public class NearestBoxG : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 box;
		protected override void DoAction() { box.Value = grid.NearestBoxG (worldPoint.Value); }
	}

	[Tooltip("Aligns a position `Vector3` to the gird's \"spacing\"")]
	public class GridAlignVector3 : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 pos, scale;
		[RequiredField]
		public FsmBool ignoreX, ignoreY, ignoreZ;

		public FsmVector3 result;
		protected override void DoAction() { result.Value = grid.AlignVector3 (pos.Value, scale.Value, new GFBoolVector3 (ignoreX.Value, ignoreY.Value, ignoreZ.Value)); }
	}

	[Tooltip("Aligns a `Transform`'s position to the gird's \"spacing\"")]
	public class GridAlignTransform : FsmGFStateActionMethodGrid {
		[RequiredField]
		[CheckForComponent(typeof(Transform))]
		public FsmObject transform;
		[RequiredField]
		public FsmBool rotate, ignoreX, ignoreY, ignoreZ;
		protected override void DoAction() { grid.AlignTransform ((Transform)transform.Value, rotate.Value, new GFBoolVector3 (ignoreX.Value, ignoreY.Value, ignoreZ.Value)); }
	}

	[Tooltip("Scales a direction `Vector3` to the gird's \"spacing\"")]
	public class GridScaleVector3 : FsmGFStateActionMethodGrid {
		[RequiredField]
		public FsmVector3 scl;
		[RequiredField]
		public FsmBool ignoreX, ignoreY, ignoreZ;

		public FsmVector3 result;
		protected override void DoAction() { result.Value = grid.ScaleVector3 (scl.Value, new GFBoolVector3 (ignoreX.Value, ignoreY.Value, ignoreZ.Value)); }
	}

	[Tooltip("Scales a `Transform`'s scale to the gird's \"spacing\"")]
	public class GridScaleTransform : FsmGFStateActionMethodGrid {
		[RequiredField]
		[CheckForComponent(typeof(Transform))]
		public FsmObject transform;
		[RequiredField]
		public FsmBool ignoreX, ignoreY, ignoreZ;

		protected override void DoAction() { grid.ScaleTransform ((Transform)transform.Value, new GFBoolVector3 (ignoreX.Value, ignoreY.Value, ignoreZ.Value)); }
	}

	/*
	public class FsmGFGridRenderGrid : FsmGFStateAction<GFGrid> {
		public FsmVector3 from, to;
		public FsmInt width;
		public FsmColor colorsX, colorsY, colorsZ;

		[RequiredField]
		[CheckForComponent(typeof(Camera))]
		public FsmObject cam;

		[RequiredField]
		[CheckForComponent(typeof(Transform))]
		public FsmObject camTransform;

		protected override void DoAction() {
			grid.RenderGrid (from.Value, to.Value, new GFColorVector3(colorsX.Value, colorsY.Value, colorsZ.Value), width.Value, (Camera)cam.Value, (Transform)camTransform.Value);
		}
	}
	*/

	/*
	public class FsmGFGridDrawGrid : FsmGFStateAction<GFGrid> {
		public FsmVector3 from, to;
		protected override void DoAction() { grid.DrawGrid (from.Value, to.Value); }
	}
	*/

	/// @todo implement GetVectrosityPoints

	/// @todo implement GetVectrosityPointsSeparate
	#endregion

	#region Hex Grid Methods
	[Tooltip("Converts world coordinates to odd herring coordinates")]
	public class WorldToHerringOdd : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 world;
		public FsmVector3 herringOdd;

		protected override void DoAction(){ herringOdd.Value = grid.WorldToHerringOdd(world.Value); }
	}

	[Tooltip("Converts world coordinates to rhombic coordinates")]
	public class WorldToRhombic : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 world;
		public FsmVector3 rhombic;

		protected override void DoAction(){ rhombic.Value = grid.WorldToRhombic(world.Value); }
	}

	/// @todo WorldToCubic

	[Tooltip("Converts odd herring coordinates to world coordinates")]
	public class HerringOddToWorld : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 herringOdd;
		public FsmVector3 world;

		protected override void DoAction(){ world.Value = grid.HerringOddToWorld(herringOdd.Value); }
	}

	[Tooltip("Converts odd herring coordinates to rhombic coordinates")]
	public class HerringOddToRhombic : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 herringOdd;
		public FsmVector3 rhombic;

		protected override void DoAction(){ rhombic.Value = grid.HerringOddToRhombic(herringOdd.Value); }
	}

	/// @todo Implement HerringOddToCubic

	[Tooltip("Converts rhombic coordinates to world coordinates")]
	public class RhombicToWorld : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 rhombic;
		public FsmVector3 world;

		protected override void DoAction() { world.Value = grid.RhombicToWorld(rhombic.Value); }
	}

	[Tooltip("Converts rhombic coordinates to odd herring coordinates")]
	public class RhombicToHerringOdd : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 rhombic;
		public FsmVector3 herringOdd;

		protected override void DoAction() { herringOdd.Value = grid.RhombicToHerringOdd(rhombic.Value); }
	}

	/// @todo Implement RhombicToCubic

	/// @todo Implement CubicToWorld
	/// @todo Implement CubicToHerringOdd
	/// @todo Implement CubicToRhombic

	[Tooltip("Returns the rhombic coordinates of the nearest vertex")]
	public class NearestVertexR : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 world;
		public FsmVector3 vertex;

		protected override void DoAction() {vertex.Value = grid.NearestVertexR(world.Value); }
	}

	/// @todo Implement NearestFaceR

	[Tooltip("Returns the rhombic coordinates of the nearest box")]
	public class NearestBoxR : FsmGFStateActionMethodHex {
		[RequiredField]
		public FsmVector3 world;
		public FsmVector3 box;

		protected override void DoAction() {box.Value = grid.NearestBoxR(world.Value); }
	}

	/// @todo Implement NearestVertexC
	/// @todo Implement NearestFaceC
	/// @todo Implement NearestBoxC
	#endregion

	#region Polar Methods
	[Tooltip("Converts world coordinates to polar coordinates.")]
	public class WorldToPolar : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 polarPoint;
		protected override void DoAction() { polarPoint.Value = grid.WorldToPolar(worldPoint.Value); }
	}

	[Tooltip("Converts polar coordinates to world coordinates.")]
	public class PolarToWorld : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 polarPoint;
		public FsmVector3 worldPoint;
		protected override void DoAction() { worldPoint.Value = grid.PolarToWorld(polarPoint.Value); }
	}

	[Tooltip("Converts grid coordinates to polar coordinates.")]
	public class GridToPolar : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 gridPoint;
		public FsmVector3 polarPoint;
		protected override void DoAction() { polarPoint.Value = grid.GridToPolar (gridPoint.Value); }
	}

	[Tooltip("Converts polar coordinates to grid coordinates.")]
	public class PolarToGrid : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 polarPoint;
		public FsmVector3 gridPoint;
		protected override void DoAction() { gridPoint.Value = grid.PolarToGrid (polarPoint.Value); }
	}

	/// @todo Implement Angle2Sector and Sector2Angle
	/*
	public class FsmGFPolarGridAngle2Sector : FsmGFStateActionMethodPolar {
		public FsmFloat angle, sector;
		public FsmInt modeInt;

		protected override void DoAction() {
			angle.Value = grid.Angle2Sector (angle.Value, (GFAngleMode)modeInt.Value);
		}
	}
	*/

	/// @todo Implement Angle2Rotation

	[Tooltip("Returns the rotation that corresponds to a given sector.")]
	public class Sector2Rotation : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmFloat sector;
		public FsmQuaternion rotation;
		protected override void DoAction() { rotation.Value = grid.Sector2Rotation (sector.Value); }
	}

	[Tooltip("Returns the rotation that corresponds to a given world point.")]
	public class World2Rotation : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 world;
		public FsmQuaternion rotation;
		protected override void DoAction() { rotation.Value = grid.World2Rotation (world.Value); }
	}

	/// @todo Implement World2Angle

	[Tooltip("Returns the sector that corresponds to a given world point.")]
	public class World2Sector : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 world;
		public FsmFloat sector;
		protected override void DoAction() { sector.Value = grid.World2Sector (world.Value); }
	}

	[Tooltip("Returns the radius from a given world point to the grid's centre.")]
	public class World2Radius : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 world;
		public FsmFloat radius;
		protected override void DoAction() { radius.Value = grid.World2Radius (world.Value); }
	}

	[Tooltip("Returns the polar coordinates of the nearest vertex")]
	public class NearestVertexP : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 vertex;
		protected override void DoAction() { vertex.Value = grid.NearestVertexP (worldPoint.Value); }
	}

	/// @todo Implement NearestFaceP

	[Tooltip("Returns the polar coordinates of the nearest box")]
	public class NearestBoxP : FsmGFStateActionMethodPolar {
		[RequiredField]
		public FsmVector3 worldPoint;
		public FsmVector3 box;
		protected override void DoAction() { box.Value = grid.NearestBoxP (worldPoint.Value); }
	}

	/// @todo Implement AlignRotateTransform
	#endregion
}
#endif // PLAYMAKER_PRESENT