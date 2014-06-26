// ! DO NOT MESS WITH THE PREPROCESSOR FLAGS !
// The flag will be uncommented and commented by the menu items editor script. Do not change it manually, or you will break things.

//#define PLAYMAKER_PRESENT

#if PLAYMAKER_PRESENT
using UnityEngine;
using System.Collections;

namespace HutongGames.PlayMaker.Actions {
	#region GFGrid
	#region AxisColors
	public abstract class FsmGFAxisColors : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Colours of each individual axis")]
		[RequiredField]
		public FsmColor axisColorsX, axisColorsY, axisColorsZ;
	}

	[Tooltip("Sets the colours of the grid's axes.")]
	public class SetAxisColors : FsmGFAxisColors {
		protected override void DoAction() {
			grid.axisColors.x = axisColorsX.Value;
			grid.axisColors.y = axisColorsY.Value;
			grid.axisColors.z = axisColorsZ.Value;
		}
	}

	[Tooltip("Gets the colours of the grid's axes.")]
	public class GetAxisColors : FsmGFAxisColors {
		protected override void DoAction() {
			axisColorsX.Value = grid.axisColors.x;
			axisColorsY.Value = grid.axisColors.y;
			axisColorsZ.Value = grid.axisColors.z;
		}
	}
	#endregion

	#region useSeparateRenderColor
	public abstract class FsmGFUseSeparateRenderColor : FsmGFStateActionGetSet<GFGrid> {
		[RequiredField]
		[Tooltip("Whether to use separate colour for bothe rendering and drawing the grid.")]
		public FsmBool useSeparateRenderColor;
	}

	[Tooltip("Sets whether to use separate colour for bothe rendering and drawing the grid.")]
	public class SetUseSeparateRenderColor : FsmGFUseSeparateRenderColor {
		protected override void DoAction () {
			grid.useSeparateRenderColor = useSeparateRenderColor.Value;
		}
	}

	[Tooltip("Gets whether to use separate colour for bothe rendering and drawing the grid.")]
	public class GetUseSeparateRenderColor : FsmGFUseSeparateRenderColor {
		protected override void DoAction () {
			useSeparateRenderColor.Value = grid.useSeparateRenderColor;
		}
	}
	#endregion

	#region renderAxisColors
	public abstract class FsmGFRenderAxisColors : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Colours of each individual axis")]
		[RequiredField]
		public FsmColor renderAxisColorsX, renderAxisColorsY, renderAxisColorsZ;
	}

	[Tooltip("Sets the separate colours for rendering grid.")]
	public class SetRenderAxisColors : FsmGFRenderAxisColors {
		protected override void DoAction() {
			grid.renderAxisColors.x = renderAxisColorsX.Value;
			grid.renderAxisColors.y = renderAxisColorsY.Value;
			grid.renderAxisColors.z = renderAxisColorsZ.Value;
		}
	}

	[Tooltip("Gets the separate colours for rendering grid.")]
	public class GetRenderAxisColors : FsmGFRenderAxisColors {
		protected override void DoAction() {
			renderAxisColorsX.Value = grid.renderAxisColors.x;
			renderAxisColorsY.Value = grid.renderAxisColors.y;
			renderAxisColorsZ.Value = grid.renderAxisColors.z;
		}
	}
	#endregion

	#region hideGrid
	public abstract class FsmGFHideGrid : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to hide the grid completely (rendering & drawing).")]
		[RequiredField]
		public FsmBool hideGrid;
	}

	[Tooltip("Sets whether to hide the grid completely.")]
	public class SetHideGrid : FsmGFHideGrid {
		protected override void DoAction () {
			grid.hideGrid = hideGrid.Value;
		}
	}

	[Tooltip("Gets whether to hide the grid completely.")]
	public class GetHideGrid : FsmGFHideGrid {
		protected override void DoAction () {
			hideGrid.Value = grid.hideGrid;
		}
	}
	#endregion

	#region hideOnPlay
	public abstract class FsmGFHideOnPlay : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to hide the grid completely in play mode (rendering & drawing).")]
		[RequiredField]
		public FsmBool hideOnPlay;
	}

	[Tooltip("Sets whether to hide the grid in play mode.")]
	public class SetHideOnPlay : FsmGFHideOnPlay {
		protected override void DoAction () {
			grid.hideOnPlay = hideOnPlay.Value;
		}
	}

	[Tooltip("Gets whether to hide the grid in play mode.")]
	public class GetHideOnPlay : FsmGFHideOnPlay {
		protected override void DoAction () {
			hideOnPlay.Value = grid.hideOnPlay;
		}
	}
	#endregion

	#region hideAxis
	public abstract class FsmGFHideAxis : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to hide the grid's X-axis.")]
		[RequiredField]
		public FsmBool hideAxisX;
		[Tooltip("Whether to hide the grid's Y-axis.")]
		[RequiredField]
		public FsmBool hideAxisY;
		[Tooltip("Whether to hide the grid's Z-axis.")]
		[RequiredField]
		public FsmBool hideAxisZ;

	}

	[Tooltip("Sets whether to which of the grid's axes to hide.")]
	public class SetHideAxis : FsmGFHideAxis {
		protected override void DoAction() {
			grid.hideAxis.x = hideAxisX.Value;
			grid.hideAxis.y = hideAxisY.Value;
			grid.hideAxis.z = hideAxisZ.Value;
		}
	}

	[Tooltip("Gets whether to which of the grid's axes to hide.")]
	public class GetHideAxis : FsmGFHideAxis {
		protected override void DoAction() {
			hideAxisX.Value = grid.hideAxis.x;
			hideAxisY.Value = grid.hideAxis.y;
			hideAxisZ.Value = grid.hideAxis.z;
		}
	}
	#endregion

	#region drawOrigin
	public abstract class FsmGFDrawOrigin : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to draw a little gizmo sphere at the grid's origin.")]
		[RequiredField]
		public FsmBool drawOrigin;
	}

	[Tooltip("Sets whether to draw a little gizmo sphere at the grid's origin.")]
	public class SetDrawOrigin : FsmGFDrawOrigin {
		protected override void DoAction () {
			grid.drawOrigin = drawOrigin.Value;
		}
	}

	[Tooltip("Gets whether to draw a little gizmo sphere at the grid's origin.")]
	public class GetDrawOrigin : FsmGFDrawOrigin {
		protected override void DoAction () {
			drawOrigin.Value = grid.drawOrigin;
		}
	}
	#endregion

	#region originOffset
	public abstract class FsmGFOriginOffset : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Offests the grid's origin from its `transform.position`.")]
		[RequiredField]
		public FsmVector3 originOffset;
	}

	[Tooltip("Sets the grid's origin offset.")]
	public class SetOriginOffset : FsmGFOriginOffset {
		protected override void DoAction () {
			grid.originOffset = originOffset.Value;
		}
	}

	[Tooltip("Gets the grid's origin offset.")]
	public class GetOriginOffset : FsmGFOriginOffset {
		protected override void DoAction () {
			originOffset.Value = grid.originOffset;
		}
	}
	#endregion

	#region renderGrid
	public abstract class FsmGFRenderGrid : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to render the grid.")]
		[RequiredField]
		public FsmBool renderGerid;
	}

	[Tooltip("Sets whether to render the grid.")]
	public class SetRenderGrid : FsmGFRenderGrid {
		protected override void DoAction () {
			grid.renderGrid = renderGerid.Value;
		}
	}

	[Tooltip("Sets whether to render the grid.")]
	public class GetRenderGrid : FsmGFRenderGrid {
		protected override void DoAction () {
			renderGerid.Value = grid.renderGrid;
		}
	}
	#endregion

	#region renderMaterial
	public abstract class FsmGFRenderMaterial : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Material for the grid renderer.")]
		[RequiredField]
		public FsmMaterial renderMaterial;
	}

	[Tooltip("Sets the material for the grid renderer.")]
	public class SetRenderMaterial : FsmGFRenderMaterial {
		protected override void DoAction() {
			grid.renderMaterial = renderMaterial.Value;
		}
	}

	[Tooltip("Gets the material for the grid renderer.")]
	public class GetRenderMaterial : FsmGFRenderMaterial {
		protected override void DoAction() {
			renderMaterial.Value = grid.renderMaterial;
		}
	}
	#endregion

	#region relativeSize
	public abstract class FsmGFRelativeSize : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether the size is relative to the grid's \"spacing\" or absolute.")]
		[RequiredField]
		public FsmBool relativeSize;
	}

	[Tooltip("Set whether the size is relative to the grid's \"spacing\" or absolute.")]
	public class SetRelativeSize : FsmGFRelativeSize {
		protected override void DoAction () {
			grid.relativeSize = relativeSize.Value;
		}
	}

	[Tooltip("Get whether the size is relative to the grid's \"spacing\" or absolute.")]
	public class GetRelativeSize : FsmGFRelativeSize {
		protected override void DoAction () {
			relativeSize.Value = grid.relativeSize;
		}
	}
	#endregion

	#region size
	public abstract class FsmGFSize : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("The size of the grid.")]
		[RequiredField]
		public FsmVector3 size;
	}

	[Tooltip("Sets the size of the grid.")]
	public class SetSize : FsmGFSize {
		protected override void DoAction () {
			grid.size = size.Value;
		}
	}

	[Tooltip("Gets the size of the grid.")]
	public class GetSize : FsmGFSize {
		protected override void DoAction () {
			size.Value = grid.size;
		}
	}
	#endregion

	#region renderFrom
	public abstract class FsmGFRenderFrom : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("The lower limit of the rendering range.")]
		[RequiredField]
		public FsmVector3 renderFrom;
	}

	[Tooltip("Sets the lower limit of the rendering range.")]
	public class SetRenderFrom : FsmGFRenderFrom {
		protected override void DoAction () {
			grid.renderFrom = renderFrom.Value;
		}
	}

	[Tooltip("Gets the lower limit of the rendering range.")]
	public class GetRenderFrom : FsmGFRenderFrom {
		protected override void DoAction () {
			renderFrom.Value = grid.renderFrom;
		}
	}
	#endregion

	#region renderTo
	public abstract class FsmGFRenderTo : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("The upper limit of the rendering range.")]
		[RequiredField]
		public FsmVector3 renderTo;
	}

	[Tooltip("Sets the upper limit of the rendering range.")]
	public class SetRenderTo : FsmGFRenderTo {
		protected override void DoAction () {
			grid.renderTo = renderTo.Value;
		}
	}

	[Tooltip("Gets the upper limit of the rendering range.")]
	public class GetRenderTo : FsmGFRenderTo {
		protected override void DoAction () {
			renderTo.Value = grid.renderTo;
		}
	}
	#endregion

	#region useCustomRenderRange
	public abstract class FsmGFCustomRenderingRange : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Whether to use the the custom rendering range.")]
		[RequiredField]
		public FsmBool useCustomRenderingRange;
	}

	[Tooltip("Sets whether to use the the custom rendering range.")]
	public class SetCustomRenderingRange : FsmGFCustomRenderingRange {
		protected override void DoAction() {
			grid.useCustomRenderRange = useCustomRenderingRange.Value;
		}
	}

	[Tooltip("Gets whether to use the the custom rendering range.")]
	public class GetCustomRenderingRange : FsmGFCustomRenderingRange {
		protected override void DoAction() {
			useCustomRenderingRange.Value = grid.useCustomRenderRange;
		}
	}
	#endregion

	#region renderLineWidth
	public abstract class FsmGFRenderedLineWidth : FsmGFStateActionGetSet<GFGrid> {
		[Tooltip("Width of the rendering lines.")]
		[RequiredField]
		public FsmInt renderLineWidth;
	}

	[Tooltip("Sets the width of the rendering lines.")]
	public class SetRenderedLineWidth : FsmGFRenderedLineWidth{
		protected override void DoAction() {
			grid.renderLineWidth = renderLineWidth.Value;
		}
	}

	[Tooltip("Gets the width of the rendering lines.")]
	public class GetRenderedLineWidth : FsmGFRenderedLineWidth{
		protected override void DoAction() {
			renderLineWidth.Value = grid.renderLineWidth;
		}
	}
	#endregion
	#endregion

	#region GFLayeredGrid
	#region plane
	/// @todo Implement plane
	/*
	public abstract class FsmGFLPlane : FsmGFStateAction<GFLayeredGrid> {
		public FsmString gridPlane;
	}

	public class FsmGFSetPlane : FsmGFLPlane {
		protected override void DoAction () {
			grid.gridPlane = (GFGrid.GridPlane)Enum.Parse (typeof(GFGrid.GridPlane), gridPlane.Value);
		}
	}

	public class FsmGFGetPlane : FsmGFLPlane {
		protected override void DoAction () {
			gridPlane.Value = Enum.GetName (typeof(GFGrid.GridPlane), grid.gridPlane);
		}
	}
	*/
	#endregion

	#region depth
	public abstract class FsmGFLDepth : FsmGFStateActionGetSetLayerd<GFLayeredGrid> {
		[Tooltip("Distance between two grid layers.")]
		[RequiredField]
		public FsmFloat depth;
	}

	[Tooltip("Sets the distance between two grid layers.")]
	public class SetDepth : FsmGFLDepth {
		protected override void DoAction () {
			grid.depth = depth.Value;
		}
	}

	[Tooltip("Gets the distance between two grid layers.")]
	public class GetDepth : FsmGFLDepth {
		protected override void DoAction () {
			depth.Value = grid.depth;
		}
	}
	#endregion
	#endregion

	#region GFRectGrid
	#region spacing
	public abstract class FsmGFRSpacing : FsmGFStateActionGetSetRect {
		[Tooltip("Spacing of the rectangular grid.")]
		[RequiredField]
		public FsmVector3 spacing;
	}

	[Tooltip("Sets the spacing of the rectangular grid.")]
	public class SetSpacing : FsmGFRSpacing {
		protected override void DoAction () {
			grid.spacing = spacing.Value;
		}
	}

	[Tooltip("Gets the spacing of the rectangular grid.")]
	public class GetSpacing : FsmGFRSpacing {
		protected override void DoAction () {
			spacing.Value = grid.spacing;
		}
	}
	#endregion

	#region right
	[Tooltip("Gets the grid's local \"right\" direction scaled by the spacing.")]
	public class GetRight : FsmGFStateActionGetSetRect {
		[Tooltip("The grid's local \"right\" direction scaled by the spacing.")]
		public FsmVector3 right;
		protected override void DoAction () {
			right.Value = grid.right;
		}
	}
	#endregion

	#region up
	[Tooltip("Gets the grid's local \"up\" direction scaled by the spacing.")]
	public class GetUp : FsmGFStateActionGetSetRect {
		[Tooltip("The grid's local \"up\" direction scaled by the spacing.")]
		public FsmVector3 up;
		protected override void DoAction () {
			up.Value = grid.up;
		}
	}
	#endregion

	#region forward
	[Tooltip("Gets the grid's local \"forward\" direction scaled by the spacing.")]
	public class GetForward : FsmGFStateActionGetSetRect {
		[Tooltip("The grid's local \"forward\" direction scaled by the spacing.")]
		public FsmVector3 forward;
		protected override void DoAction () {
			forward.Value = grid.forward;
		}
	}
	#endregion
	#endregion

	#region GFHexGrid
	#region radius
	public abstract class FsmGFHRadius : FsmGFStateActionGetSetHex {
		[Tooltip("Radius of the hex grid's hexes, i.e. the distance form the centre to a vertex.")]
		[RequiredField]
		public FsmFloat radius;
	}

	[Tooltip("Sets the radius of the hex grid's hexes, i.e. the distance form the centre to a vertex.")]
	public class SetHexRadius : FsmGFHRadius {
		protected override void DoAction () {
			grid.radius = radius.Value;
		}
	}

	[Tooltip("Gets the radius of the hex grid's hexes, i.e. the distance form the centre to a vertex.")]
	public class GetHexRadius : FsmGFHRadius {
		protected override void DoAction () {
			radius.Value = grid.depth;
		}
	}
	#endregion

	#region side
	[Tooltip("Gets the hex grid's \"side\", which is 1.5 times the radius.")]
	public class GetSide : FsmGFStateActionGetSetHex {
		[Tooltip("The hex grid's \"side\", which is 1.5 times the radius.")]
		public FsmFloat side;
		protected override void DoAction () {
			side.Value = grid.side;
		}
	}
	#endregion

	#region height
	[Tooltip("Gets the hex grid's \"height\", which is the full width of the hex.")]
	public class GetHeight : FsmGFStateActionGetSetHex {
		[Tooltip("The hex grid's \"height\", which is the full width of the hex.")]
		public FsmFloat height;
		protected override void DoAction () {
			height.Value = grid.height;
		}
	}
	#endregion

	#region width
	[Tooltip("Gets the hex grid's \"height\", which is the distance between opposite vertices, i.e. twice the radius.")]
	public class GetWidth : FsmGFStateActionGetSetHex {
		[Tooltip("The hex grid's \"height\", which is the distance between opposite vertices.")]
		public FsmFloat width;
		protected override void DoAction () {
			width.Value = grid.width;
		}
	}
	#endregion

	#region hexSideMode
	/// @todo Implement hexSideMode
	#endregion

	#region hexTopMode
	/// @todo Implement hexTopMode
	#endregion

	#region gridStyle
	/// @todo Implement gridStyle
	#endregion
	#endregion

	#region GFPolarGrid
	#region radius
	public abstract class FsmGFPRadius : FsmGFStateActionGetSetPolar {
		[Tooltip("Radius of the polar grid.")]
		[RequiredField]
		public FsmFloat radius;
	}

	[Tooltip("Sets the radius of the polar grid.")]
	public class SetPolarRadius : FsmGFPRadius {
		protected override void DoAction () {
			grid.radius = radius.Value;
		}
	}

	[Tooltip("Gets the radius of the polar grid.")]
	public class GetPolarRadius : FsmGFPRadius {
		protected override void DoAction () {
			radius.Value = grid.depth;
		}
	}
	#endregion

	#region smoothness
	public abstract class FsmGFPSmoothness : FsmGFStateActionGetSetPolar {
		[Tooltip("Smoothness of the polar grid.")]
		[RequiredField]
		public FsmInt smoothness;
	}

	[Tooltip("Sets the smoothness of the polar grid.")]
	public class SetSmoothness : FsmGFPSmoothness {
		protected override void DoAction () {
			if( smoothness.Value < 1 )
				smoothness.Value = 1;
			grid.smoothness = smoothness.Value;
		}
	}

	[Tooltip("Gets the smoothness of the polar grid.")]
	public class GetSmoothness : FsmGFPSmoothness {
		protected override void DoAction () {
			smoothness.Value = grid.smoothness;
		}
	}
	#endregion

	#region sectors
	public abstract class FsmGFPSectors : FsmGFStateActionGetSetPolar {
		[Tooltip("Amount of sectors in the polar grid.")]
		[RequiredField]
		public FsmInt sectors;
	}

	[Tooltip("Sets the amount of sectors in the polar grid.")]
	public class SetSectors : FsmGFPSectors {
		protected override void DoAction () {
			if( sectors.Value < 1 )
				sectors.Value = 1;
			grid.sectors = sectors.Value;
		}
	}

	[Tooltip("Gets the amount of sectors in the polar grid.")]
	public class GetSectors : FsmGFPSectors {
		protected override void DoAction () {
			sectors.Value = grid.sectors;
		}
	}
	#endregion

	#region angle
	[Tooltip("Gets the angle between two sectors in the polar grid (in radians).")]
	public class FsmGFPAngle : FsmGFStateActionGetSetPolar {
		[Tooltip("Angle between two sectors in the polar grid (in radians).")]
		public FsmFloat angle;
		protected override void DoAction () {
			angle.Value = grid.angle;
		}
	}
	#endregion

	#region angleDeg
	[Tooltip("Gets the angle between two sectors in the polar grid (in degrees).")]
	public class FsmGFPAngleDeg : FsmGFStateActionGetSetPolar {
		[Tooltip("Angle between two sectors in the polar grid (in degrees).")]
		public FsmFloat angleDeg;
		protected override void DoAction () {
			angleDeg.Value = grid.angleDeg;
		}
	}
	#endregion
	#endregion
}
#endif // PLAYMAKER_PRESENT