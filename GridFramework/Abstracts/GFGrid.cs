using UnityEngine;
using System.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Specialized;

/// <summary>Abstract base class for all Grid Framework grids.</summary>
/// 
/// This is the standard class all grids are based on. Aside from providing a common set of variables and a template for what methods to use, this class has no practical
/// meaning for end users. Use this as reference for what can be done without having to specify which type of grid you are using. For anything more specific you have to
/// look at the child classes.
[System.Serializable]
public abstract class GFGrid : MonoBehaviour {
	#region nested classes and enums
	/// <summary>Enum for one of the three grid planes.</summary>
	/// 
	/// This enum encapsulates the three grid planes: XY, XZ and YZ. You can also get the integer of enum items, where the integer
	/// corresponds to the missing axis (X = 0, Y = 1, Z = 2):
	/// <code>
	/// // UnityScript:
	/// var myPlane: GFGrid.GridPlane = GFGrid.GFGridPlane.XZ;
	/// var planeIndex: int = (int)myPlane; // sets the variable to 1
	/// 
	/// // C#
	/// GFGrid.GridPlane myPlane = GFGrid.GFGridPlane.XZ;
	/// int planeIndex = (int)myPlane;
	/// </code>
	public enum GridPlane {YZ, XZ, XY};

	// instead of always aligning the centre we can use this to align other regions (WIP)
	private enum AlignReference {Center, RightUpBack, RightUpFront, RightDownBack, RightDownFront, LeftUpBack, LeftUpFront, LeftDownBack, LeftDownFront};

	#region optimization
	protected class TransformCache{
		protected Transform _gridTransform;
		// these parts can be shortened using Transform.hasChanged, but that is Unity 4+ only
		public Vector3 oldPosition;
		public Quaternion oldRotation;
		
		// true if values have changed, false if they re the same
		public bool needRecalculation{ get{ return !(oldPosition == _gridTransform.position && oldRotation == _gridTransform.rotation); } }
		
		public void Recache(){
			oldPosition = _gridTransform.position;
			oldRotation = _gridTransform.rotation;
		}
		
		public TransformCache(GFGrid grid){
			_gridTransform = grid._transform;
			Recache();
		}
	}
	#endregion
	#endregion
	
	#region class members
	#region size and range
	#region Protected
	[SerializeField]
	private bool _relativeSize = false;
	
	[SerializeField]
	protected Vector3 _size = new Vector3(5.0f, 5.0f, 5.0f);
	
	[SerializeField]
	protected Vector3 _renderFrom = Vector3.zero; //first one from, second one to 
	
	[SerializeField]
	protected Vector3 _renderTo = 3*Vector3.one; //first one from, second one to 
	#endregion
	#region Accessors
	/// <summary>Whether the drawing/rendering will scale with spacing.</summary>
	/// 
	/// Set this to <c>true</c> if you want the drawing to have relative size, i.e. to scale with the spacing/radius or whatever the specific grid uses.
	/// Otherwise set it to <c>false</c>.
	/// 
	/// <value><c>true</c> if the grid's size is in grid coordinates; <c>false</c> if it's in world coordinates.</value>
	public bool relativeSize {
		get{return _relativeSize;}
		set{
			if(value == _relativeSize)// needed because the editor fires the setter even if this wasn't changed
				return;
			hasChanged = true;
			_relativeSize = value;
		}
	}

	/// <summary>The size of the visual representation of the grid.</summary>
	/// 
	/// <value>The size of the grid's visual representation.</value>
	/// 
	/// Defines the size of the drawing and rendering of the grid. Keep in mind that the grid is infinitely large, the drawing is just a visual
	/// representation, stretching on all three directions from the origin. The size is either absolute or relative to the grid's other parameters,
	/// depending on the value of <see cref="relativeSize"/>.
	/// 
	/// If you set <see cref="useCustomRenderRange"/> to <c>true</c> that range will override this member. The size is either absolute or relative to the grid's other
	/// parameters, depending on the value of <see cref="relativeSize"/>.
	/// 
	public virtual Vector3 size{
		get{return _size;}
		set{if(value == _size)// needed because the editor fires the setter even if this wasn't changed
			return;
			hasChanged = true;
			_size = Vector3.Max(value, Vector3.zero);}
	}

	/// <summary>Custom lower limit for drawing and rendering.</summary>
	/// <value>Custom lower limit for drawing and rendering.</value>
	/// 
	/// When using a custom rendering range this is the lower left backward limit of the rendering and drawing.
	public virtual Vector3 renderFrom{
		get{return _renderFrom;}
		set{if(value == _renderFrom)// needed because the editor fires the setter even if this wasn't changed
			return;
			hasChanged = true;
			_renderFrom = Vector3.Min(value, renderTo);}
	}

	/// <summary>Custom upper limit for drawing and rendering.</summary>
	/// 
	/// <value>Custom upper limit for drawing and rendering.</value>
	/// 
	/// When using a custom rendering range this is the upper right forward limit of the rendering and drawing.
	public virtual Vector3 renderTo{
		get{return _renderTo;}
		set{if(value == _renderTo)// needed because the editor fires the setter even if this wasn't changed
			return;
			hasChanged = true;
			_renderTo = Vector3.Max(value, _renderFrom);}
	}
	#endregion
	#endregion

	#region Offset
	[SerializeField]
	protected Vector3 _originOffset = Vector3.zero;

	/// <summary>Offset to add to the origin</summary>
	/// 
	/// By default the origin of grids is at the world position of their gameObject (the position of the Transform), this offset allows you to move the grid's pivot point by adding a value to it.
	/// Keep in mind how this will affect the various grid coordinate systems, they are still relative to the grid's origin, not the Transform.
	/// 
	/// In other words, if a point at grid position (1, 2, 0) is at world position (4, 5, 0) and you add and offset of (1, 1, 0), then point's grid position will still be (1, 2, 0),
	/// but its world position will be (5, 6, 0). Here is an example:
	/// <code>
	/// GFGrid myGrid;
	/// Vector3 gPos = new Vector3 (1, 2, 3);
	/// Vector3 wPos = myGrid.GridToWorld (gPos); 
	/// Debug.Log (wPos); // prints (4, 5, 0)
	/// 
	/// myGrid.pivotOffset = new Vector3 (1, 1, 0);
	/// wPos = myGrid.GridToWorld (gPos); 
	/// Debug.Log (wPos); // prints (5, 6, 0)
	/// </code>
	public Vector3 originOffset {
		get {return _originOffset;}
		set {
			if ( value == _originOffset )// needed because the editor fires the setter even if this wasn't changed
				return;
			hasChanged = true;
			_originOffset = value;
		}
	}
	#endregion
	
	#region colours
	protected string defaultShader = "Shader \"Lines/Colored Blended\" {" +
	                                "SubShader { Pass { " +
	                                "	Blend SrcAlpha OneMinusSrcAlpha " +
	                                "	ZWrite Off Cull Off Fog { Mode Off } " +
	                                "	BindChannels {" +
	                                "	Bind \"vertex\", vertex Bind \"color\", color }" +
	                                "} } }";

	/// <summary>Colours of the axes when drawing and rendering.</summary>
	/// 
	/// The colours are stored as three separte entries, corresponding to the three separate axes. They will be used for both drawing
	/// an rendering, unless <see cref="useSeparateRenderColor"/> is set to <c>true</c>.
	public GFColorVector3 axisColors = new GFColorVector3();

	/// <summary>Whether to use the same colours for rendering as for drawing.</summary>
	/// 
	/// If you set this flag to <c>true</c> the rendering will use the colours of <see cref="renderAxisColors"/>, otherwise it will default to <see cref="axisColors"/>.
	/// This is useful if you want to have different colours for rendering and drawing. For example, you could have a clearly visible grid in the editor to work with
	/// and a barely visible grid in the game while playing.
	public bool useSeparateRenderColor = false;

	/// <summary>Separate colours of the axes when rendering.</summary>
	/// 
	/// By default the colours of <see cref="axisColors"/> are used for rendering, however if you set <see cref="useSeparateRenderColor"/> to <c>true</c> these colours
	/// will be used instead. Otherwise this does nothing.
	public GFColorVector3 renderAxisColors = new GFColorVector3(Color.gray);
	#endregion
	
	#region Draw & Render Flags
	/// <summary>Whether to hide the grid completely.</summary>
	/// 
	/// <para>If set to <c>true</c> the grid will be neither drawn nor rendered at all, it then takes precedence over all the other flags.</para>
	public bool hideGrid = false;

	/// <summary>Whether to hide the grid in play mode.</summary>
	/// 
	/// This is similar to <see cref="hideGrid"/>, but only active while in play mode.
	public bool hideOnPlay = false;

	/// <summary>Whether to hide just individual axes.</summary>
	/// 
	/// This hides the individual axes rather than the whole grid.
	public GFBoolVector3 hideAxis = new GFBoolVector3();

	/// <summary>Whether to draw a little sphere at the origin of the grid.</summary>
	/// 
	/// If set to <c>true</c> a small gizmo sphere will be drawn at the origin of the grid. This is not a rendering, so it wil not appear in the game, it is intended
	/// to make selecting the grid in the editor easier.
	public bool drawOrigin = false;

	/// <summary>Whether to render the grid at runtime.</summary>
	/// 
	/// The grid will only be rendered if this flag is set to <c>true</c>, otherwise you won't be able to see the grid in the game.
	public bool renderGrid = true;

	[SerializeField]
	protected bool _useCustomRenderRange = false;
	/// <summary>Use you own values for the range of the rendering.</summary>
	/// 
	/// <value><c>true</c> if using a custom range for rendering and drawing; otherwise, <c>false</c>.</value>
	/// 
	/// If this flag is set to <c>true</c> the grid rendering an drawing will use the values of <see cref="renderFrom"/> and <see cref="renderTo"/> as limits. Otherwise
	/// it will use the <see cref="size"/> instead.
	public bool useCustomRenderRange{get{return _useCustomRenderRange;}set{if(value == _useCustomRenderRange){return;} _useCustomRenderRange = value; hasChanged = true;}}

	[SerializeField]
	protected int _renderLineWidth = 1;

	/// <summary>The width of the lines used when rendering the grid.</summary>
	/// 
	/// <value>The width of the render line.</value>
	/// 
	/// The width of the rendered lines, if it is set to 1 all lines will be one pixel wide, otherwise they will have the specified width in world units.
	public int renderLineWidth{
		get{return _renderLineWidth;}
		set{_renderLineWidth = Mathf.Max(value, 1);}
	}

	/// <summary>The material for rendering, if none is given it uses a default material.</summary>
	/// 
	/// You can use you own material if you want control over the shader used, otherwise this default material will be used:
	/// <code>
	/// new Material("Shader \"Lines/Colored Blended\" {" +
	/// 	"SubShader { Pass { " +
	/// 	"	Blend SrcAlpha OneMinusSrcAlpha " +
	/// 	"	ZWrite Off Cull Off Fog { Mode Off } " +
	/// 	"	BindChannels {" +
	/// 	"	Bind \"vertex\", vertex Bind \"color\", color }" +
	/// 	"} } }"
	/// )
	/// </code>
	public Material renderMaterial = null;
	protected Material defaultRenderMaterial {get {return new Material(defaultShader);}}
	#endregion
	
	#region cache members
	// we store the draw points here for re-use
	protected Vector3[][][] _drawPoints;

	/// <summary>When requesting draw points we store the last request here.</summary>
	/// If the request matches the previous one we can reuse points, otherwise we have to calculate them anew.
	protected Vector3 lastFromPoint, lastToPoint;
	#region optimization
	// caching the transform for performance
	protected Transform cachedTransform; //this is the real cache
	// this is used for access, if there is nothing cached it performs the cache first, then return the component
	protected Transform _transform{
		get{
			if(!cachedTransform)
				cachedTransform = transform;
			return cachedTransform;
		}
	}
	
	// this will be set to true whenever one of the accessors calls its set
	protected TransformCache _transformCache;
	protected TransformCache transformCache {
		get{
			if(_transformCache == null)
				_transformCache = new TransformCache(this);
			return _transformCache;
		}
	}
	protected bool _gridChanged = false;
	protected bool hasChanged {
		get{
			if(_gridChanged || transformCache.needRecalculation){
				_gridChanged = false;
				transformCache.Recache();
				if( GridChangedEvent != null ) // fire the event
					GridChangedEvent( this );
				return true;
			} else{
				return _gridChanged;
			}
		}
		set {
			_gridChanged = value;
			if( GridChangedEvent != null && _gridChanged == true) // fire the event
				GridChangedEvent( this );
		}
	}
	/// <summary>Returns whether we can recyle points fromt he previous drawing.</summary>
	/// <returns>Returns <c>true</c>, if the grid hasn't changed and the range request is the same as last time, <c>false</c> otherwise.</returns>
	/// <param name="from">The lower end of the range request.</param>
	/// <param name="to">The upper end of the range request.</param>
	protected bool RecyclePoints (Vector3 from, Vector3 to) {
		if (!hasChanged && _drawPoints != null && from == lastFromPoint && to == lastToPoint)
			return true;
		else {
			lastFromPoint = from; // set the new range
			lastToPoint = to;
			return false;
		}
	}
	#endregion
	#endregion
	
	#region helper values (read only)
	// the normal X-, Y- and Z- vectors in world-space
	protected Vector3[] units {get {return new Vector3[3]{Vector3.right, Vector3.up, Vector3.forward};}}
	#endregion

	#region Events
	/// <summary>A delegate for handling events when the grid has been changed in such a way that it requires a redraw</summary>
	/// <param name="grid">The grid that calls the delegate</param>
	/// 
	/// This is the delegate type for methods to be called when changes to the grid occur. It is best used together with the #GridChangedEvent event.
	public delegate void GridChangedDelegate( GFGrid grid );

	/// <summary>An even that gets fired </summary>
	/// 
	/// This is the event that gets fired when one of the grid's properties is changed. If the Transform (position or rotation) is changed this event
	/// will only be fired if there is a camera trying to render the grid or some other method tries to draw the gird (like drawing in the editor or 
	/// calling #GetVectrosityPoints). You can learn more about events ont he @ref events page of the user manual.
	public event GridChangedDelegate GridChangedEvent;
	#endregion
	#endregion
	
	#region Grid <-> World coordinate transformation
	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <returns>Grid coordinates of the world point.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Takes in a point in world space and converts it to grid space. Some grids have several coordinate system, so look into the specific class for conversion methods
	/// to other coordinate systems.
	public abstract Vector3 WorldToGrid(Vector3 worldPoint);

	/// <summary>Converts grid coordinates to world coordinates.</summary>
	/// <returns>World coordinates of the grid point.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// Takes in a point in grid space and converts it to world space. Some grids have several coordinate system, so look into the specific class for conversion methods
	/// from other coordinate systems.
	public abstract Vector3 GridToWorld(Vector3 gridPoint);
	#endregion
	
	#region nearest in world space
	/// <summary>Returns the world position of the nearest vertex.</summary>
	/// <returns>World position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Returns the world position of the nearest vertex from a given point in world space. If <paramref name="doDebug"/> is set a small gizmo sphere will be drawn at
	/// that position. This is just an abstract template for the method, look into the specific class for exact implementation.
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug);

	/// @overload
	public Vector3 NearestVertexW(Vector3 worldPoint) {
		return NearestVertexW (worldPoint, false);
	}

	/// <summary>Returns the world position of the nearest face.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a face on the grid. Since the face is enclosed by several vertices, the returned
	/// value is the point in between all of the vertices. You also need to specify on which plane the face lies (optional for hex- and polar grids). If
	/// <paramref name="doDebug"/> is set a small gizmo face will drawn there.
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug);

	/// @overload
	public Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane) {
		return NearestFaceW (worldPoint, plane, false);
	}

	/// <summary>Returns the world position of the nearest box.</summary>
	/// <returns>World position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a box in the grid. Since the box is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices. If <paramref name="doDebug"/> is set a small gizmo box will drawn there.
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug);

	/// @overload
	public Vector3 NearestBoxW(Vector3 worldPoint) {
		return NearestBoxW (worldPoint, false);
	}
	#endregion
	
	#region nearest in grid space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Returns the position of the nerest vertex in grid coordinates from a given point in world space.
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestVertexG(Vector3 worldPoint);

	/// <summary>Returns the grid position of the nearest Face.</summary>
	/// <returns>Grid position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid coordinates of a face on the grid. Since the face is enclosed by several vertices, the returned
	/// value is the point in between all of the vertices. You also need to specify on which plane the face lies (optional for hex- and polar grids).
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane);

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <returns>Grid position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid coordinates of a box in the grid. Since the box is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices.
	/// 
	/// This is just an abstract template for the method, look into the specific class for exact implementation.
	public abstract Vector3 NearestBoxG(Vector3 worldPoint);
	#endregion
	
	#region Align Methods
	/// <summary>Fits a position vector into the grid.</summary>
	/// <returns>Aligned position vector.</returns>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// 
	/// Fits a position inside the grid by using the object’s transform. The exact position depends on whether the components of <paramref name="scale"/> are even or odd
	/// and the exact implementation can be found in the subclasses. The parameter <paramref name="lockAxis"/> makes the function not touch the corresponding coordinate.
	public abstract Vector3 AlignVector3(Vector3 pos, Vector3 scale, GFBoolVector3 ignoreAxis);

	#region Overload
	/// @overload
	/// It aligns the position while respecting all axes and uses a default size of 1 x 1 x 1, it is equal to
	/// <code>AlignVector3(pos, Vector3.one, new GFBoolVector3(false));</code>
	public Vector3 AlignVector3(Vector3 pos){
		return AlignVector3(pos, Vector3.one, new GFBoolVector3(false));
	}

	/// @overload
	/// It aligns the position and uses a default size of 1 x 1 x 1 while leaving the axes to the user, it is equal to
	/// <code>AlignVector3(pos, Vector3.one, lockAxis);</code>
	public Vector3 AlignVector3(Vector3 pos, GFBoolVector3 lockAxis){
		return AlignVector3(pos, Vector3.one, lockAxis);
	}
		
	/// @overload
	/// It aligns the position and respects the axes while using a default size of 1 x 1 x 1, it is equal to
	/// <code>AlignVector3(pos, scale, new GFBoolVector3(false));</code>
	public Vector3 AlignVector3(Vector3 pos, Vector3 scale){
		return AlignVector3(pos, scale, new GFBoolVector3(false));
	}
	#endregion

	/// <summary>Fits a Transform inside the grid (without scaling it).</summary>
	/// <param name="theTransform">The Transform to align.</param>
	/// <param name="rotate">Whether to rotate to the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// 
	/// Fits an object inside the grid by using the object’s Transform. Setting <c>doRotate</c> makes the object take on the grid’s rotation. The parameter <c>lockAxis</c>
	/// makes the function not touch the corresponding coordinate.
	/// 
	/// The resulting position depends on <paramref name="AlignVector3"/>, so please look up how that method works.
	public void AlignTransform(Transform theTransform, bool rotate, GFBoolVector3 ignoreAxis){
		Quaternion oldRotation = theTransform.rotation;
		theTransform.rotation = transform.rotation;

		theTransform.position = AlignVector3(theTransform.position, theTransform.lossyScale, ignoreAxis);
		if(!rotate)
			theTransform.rotation = oldRotation;
	}

	#region overload
	/// @overload
	/// It aligns and rotates the Transform while respecting all axes, it is equal to
	/// <code>AlignTransform(theTransform, true, new GFBoolVector3(false));</code>
	public void AlignTransform(Transform theTransform){
		AlignTransform(theTransform, true, new GFBoolVector3(false));
	}

	/// @overloads
	/// It aligns and rotates the Transform but leaves the axes to the user, it is equal to
	/// <code>AlignTransform(theTransform, true, lockAxis);</code>
	public void AlignTransform(Transform theTransform, GFBoolVector3 lockAxis){
		AlignTransform(theTransform, true, lockAxis);
	}

	/// @overload
	/// It aligns and respects all axes to the user, but leaves the decision of rotation to the user, it is equal to
	/// <code>AlignTransform(theTransform, rotate, new GFBoolVector3(false));</code>
	public void AlignTransform(Transform theTransform, bool rotate){
		AlignTransform(theTransform, rotate, new GFBoolVector3(false));
	}
	#endregion
	#endregion
	
	#region Scale Methods
	/// <summary>Scales a size vector to fit inside a grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// 
	/// This method takes in a vector representing a size and fits it inside the grid. The <param name="lockAxis"> parameter lets you ignore individual axes.
	public abstract Vector3 ScaleVector3(Vector3 scl, GFBoolVector3 ignoreAxis);

	#region overload
	/// @overload
	/// It scales the size while respecting all axes, it is equal to
	/// <code>ScaleVector3(scl, new GFBoolVector3(false));</code>
	public Vector3 ScaleVector3(Vector3 scl){
		return ScaleVector3(scl, new GFBoolVector3(false));
	}
	#endregion
	/// <summary>Scales a Transform to fit the grid (without moving it).</summary>
	/// <param name="theTransform">The Transform to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// 
	/// Scales a Transform to fit inside a grid. The parameter <param name="lockAxis"> makes the function not touch the corresponding coordinate.
	/// 
	/// The resulting position depends on <see cref="ScaleVector3"/>, so please look up how that method works.
	public void ScaleTransform(Transform theTransform, GFBoolVector3 ignoreAxis){
		theTransform.localScale = ScaleVector3(theTransform.localScale, ignoreAxis);
	}

	#region Overload
	/// @overload
	/// It scales the Transform while respecting all axes, it is equal to
	/// <code>ScaleTransform(theTransform, new GFBoolVector3(false));</code>
	public void ScaleTransform(Transform theTransform){
		ScaleTransform(theTransform, new GFBoolVector3(false));
	}
	#endregion
	#endregion

	#region Pivot Offset Methods
	protected void ApplyDrawOffset () {
		// if the pivot offset is 0 skip this step to save performance
		if (originOffset.sqrMagnitude < 0.001)
			return;
		foreach (Vector3[][] set in _drawPoints) {
			foreach (Vector3[] line in set) {
				if (line == null)
					break;
				for (int i = 0; i < 2; i++) {
					line [i] += originOffset;
				}
			}
		}
	}
	#endregion
	
	#region Render Methods
	/// <summary>Renders the grid at runtime</summary>
	/// <param name="from">Lower limit</param>
	/// <param name="to">Upper limit</param>
	/// <param name="colors">Colors for rendering</param>
	/// <param name="width">Width of the line</param>
	/// <param name="cam">Camera for rendering</param>
	/// <param name="camTransform">Transform of the camera</param>
	/// 
	/// Renders the grid with lower and upper limit, a given line width and individual colours for the three axes.
	/// If the lines have line width 1 they will be exactly one pixel wide, and if they have a larger with they will be rendered as billboards (always facing the camera).
	/// If there is no camera and camera Transform passed this won't be possible and the lines will default back to one pixel width.
	/// 
	/// It is not necessary to call this method manually, rather you should just set the @c #renderGrid flag to @c true and let Grid Framework take care of it.
	/// However, if you want total control use this method, usually from within an
	/// <c><a href="http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnPostRender.html">OnPostRender</a></c> method.
	public virtual void RenderGrid(Vector3 from, Vector3 to, GFColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null){
		if(!renderGrid)
			return;

		if(!renderMaterial)
			renderMaterial = defaultRenderMaterial;
		
		CalculateDrawPoints(from, to);
		
		RenderGridLines(colors, width, cam, camTransform);
	}

	#region overload
	/// @overload
	/// Renders the grid using @c #size for lower and upper limits, equal to
	/// <code>RenderGrid(-size, size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);</code>
	public virtual void RenderGrid(int width = 0, Camera cam = null, Transform camTransform = null){
		RenderGrid(-size, size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}

	/// @overload
	/// Renders the grid using @c #axisColors (or @c #renderAxisColors if @c #useSeparateRenderColor is <c>true</c>) as colours, equal to
	/// <code>RenderGrid(from, to, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);</code>
	public void RenderGrid(Vector3 from, Vector3 to, int width = 0, Camera cam = null, Transform camTransform = null){
		RenderGrid(from, to, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}
	#endregion

	protected void RenderGridLines(GFColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null){
		renderMaterial.SetPass(0);
		
		if(width <= 1 || !cam || !camTransform){// use simple lines for width 1 or if no camera was passed
			GL.Begin(GL.LINES);
			for(int i = 0; i < 3; i++){
				if(hideAxis[i])
					continue;
				GL.Color(colors[i]);
				foreach(Vector3[] line in _drawPoints[i]){
					if(line == null) continue;
						GL.Vertex(line[0]); GL.Vertex(line[1]);
				}
			}
			GL.End();
		} else{// quads for "lines" with width
			GL.Begin(GL.QUADS);
			float mult = Mathf.Max(0, 0.5f * width); //the multiplier, half the desired width
			
			for(int i = 0; i < 3; i++){
				GL.Color(colors[i]);
				if(hideAxis[i])
					continue;
				
				//sample a direction vector, one per direction is enough (using the first line of each line set (<- !!! ONLY TRUE FOR RECT GRIDS !!!)
				Vector3 dir = new Vector3();
				if(_drawPoints[i].Length > 0) //can't get a line if the set is empty
					dir = Vector3.Cross(_drawPoints[i][0][0] - _drawPoints[i][0][1], camTransform.forward).normalized;
				//multiply dir with the world length of one pixel in distance
				if(cam.isOrthoGraphic){
					dir *= (cam.orthographicSize * 2) / cam.pixelHeight;
				} else{// (the 50 below is just there to smooth things out)
					dir *= (cam.ScreenToWorldPoint(new Vector3(0, 0, 50)) - cam.ScreenToWorldPoint(new Vector3(20, 0, 50))).magnitude/20;
				}
				
				foreach(Vector3[] line in _drawPoints[i]){
					if(line == null) continue;
					// if the grid is not rectangular we need to change dir every time
					if (this.GetType() != typeof(GFRectGrid)) {
						dir = Vector3.Cross(line[0] - line[1], camTransform.forward).normalized;
						if(cam.isOrthoGraphic){
							dir *= (cam.orthographicSize * 2) / cam.pixelHeight;
						} else{// (the 50 below is just there to smooth things out)
							dir *= (cam.ScreenToWorldPoint(new Vector3(0, 0, 50)) - cam.ScreenToWorldPoint(new Vector3(20, 0, 50))).magnitude/20;
						}
					}
					GL.Vertex(line[0]-mult*dir); GL.Vertex(line[0]+mult*dir); GL.Vertex(line[1]+mult*dir); GL.Vertex(line[1]-mult*dir);
				}
			}
			GL.End();
		}
	}
	#endregion
	
	#region Draw Methods
	/// <summary>Draws the grid using gizmos.</summary>
	/// <param name="from">Lower limit of the drawing.</param>
	/// <param name="to">Upper limit s drawing.</param>
	/// 
	/// This method draws the grid in the editor using gizmos. There is usually no reason to call this method manually, you should instead set the drawing flags of the
	/// grid itself. However, if you must, call this method from inside
	/// <c><a href="http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDrawGizmos.html">OnDrawGizmos</a></c>.
	public virtual void DrawGrid(Vector3 from, Vector3 to){
		//don't draw if not supposed to
		if(hideGrid)
			return;
		
		CalculateDrawPoints(from, to);
		
		for(int i = 0; i < 3; i++){
			if(hideAxis[i])
				continue;
			Gizmos.color = axisColors[i];
			foreach(Vector3[] line in _drawPoints[i]){
				if (line == null) continue;
				Gizmos.DrawLine(line[0], line[1]);
			}
		}
		
		//draw a sphere at the centre
		if(drawOrigin){
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(_transform.position, 0.3f);
		}
	}

	/// @overload
	/// Uses the size as limits, it is equivalent to
	/// <code>DrawGrid(-size, size);</code>
	public virtual void DrawGrid(){
		DrawGrid(-size, size);
	}

	protected static void DrawSphere (Vector3 pos, float rad = 0.3f){
		Gizmos.DrawSphere(pos, rad);
	}
	#endregion
	
	#region Calculate Draw Points
	#region overload
	protected virtual Vector3[][][] CalculateDrawPoints(){
		return CalculateDrawPoints(-size, size);
	}
	#endregion
	protected abstract Vector3[][][] CalculateDrawPoints(Vector3 from, Vector3 to);
	#endregion
	
	#region Vectrosity Methods
	/// <summary>Returns an array of Vector3 points ready for use with Vectrosity.</summary>
	/// <returns>Array of points in local space.</returns>
	/// <param name="from">Lower limit for the points.</param>
	/// <param name="to">Upper limit for the points.</param>
	/// 
	/// Returns an array of Vector3 containing the points for a discrete vector line in Vectrosity. One entry is the starting point, the next entry is the end point, the
	/// next entry is the starting point of the next line and so on.
	public Vector3[] GetVectrosityPoints(Vector3 from, Vector3 to){
		Vector3[][] seperatePoints = GetVectrosityPointsSeparate(from, to); 
		Vector3[] returnedPoints = new Vector3[seperatePoints[0].Length + seperatePoints[1].Length + seperatePoints[2].Length];
		seperatePoints[0].CopyTo(returnedPoints, 0);
		seperatePoints[1].CopyTo(returnedPoints, seperatePoints[0].Length);
		seperatePoints[2].CopyTo(returnedPoints, seperatePoints[0].Length + seperatePoints[1].Length);
		return returnedPoints;
	}
		
	/// @overload
	/// Uses the grid's @c #size as limits, equal to
	/// <code>GetVectrosityPoints(-size, size);</code>
	public Vector3[] GetVectrosityPoints(){
		return GetVectrosityPoints(-size, size);
	}

	/// <summary>Returns an array of arrays of Vector3 points ready for use with Vectrosity.</summary>
	/// <returns>Jagged array of three arrays, each containing the points of a single axis.</returns>
	/// <param name="from">Lower limit for the points.</param>
	/// <param name="to">Upper limit for the points.</param>
	/// 
	/// This method is very similar to @c #GetVectrosityPoints, except that the points are in separate arrays for each axis. This is useful if you want to treat the lines
	/// of each axis differently, like having different colours.
	public Vector3[][] GetVectrosityPointsSeparate(Vector3 from, Vector3 to){
		CalculateDrawPoints(from, to);
		int[] lengths = new int[3];
		for(int i = 0; i < 3; i++){
			lengths[i] = _drawPoints[i].Count(line => line != null)*2; // <- ! watch out, using LINQ !
		}
		
		Vector3[][] returnedPoints = new Vector3[3][];
		for(int i = 0; i < 3; i++){
			returnedPoints[i] = new Vector3[lengths[i]];
			// Debug.Log(lengths[i]);
		}
		int iterator = 0;
		for(int i = 0; i < 3; i++){
			iterator = 0;
			foreach(Vector3[] line in _drawPoints[i]){
				if(line == null)
					continue;
				returnedPoints[i][iterator] = line[0];
				iterator++;
				returnedPoints[i][iterator] = line[1];
				iterator++;
			}
		}
		
		return returnedPoints;
	}
		
	/// @overload
	/// Uses the grid's @c #size as limits, equal to
	/// <code>GetVectrosityPointsSeparate(-size, size);</code>
	public Vector3[][] GetVectrosityPointsSeparate(){
		return GetVectrosityPointsSeparate(-size, size);
	}
	#endregion
	
	#region Runtime Methods
	void Awake(){
		if(hideOnPlay)
			hideGrid = true;
		
		if(renderMaterial == null)
			renderMaterial = defaultRenderMaterial;
		
		GFGridRenderManager.AddGrid(this);
	}
	
	void OnDestroy(){
	    GFGridRenderManager.RemoveGrid(GetComponent<GFGrid>());
	}
	#endregion
	
	#region helper methods
	// swaps two variables, useful for swapping quasi-X and quasi-Y to keep the same formula for pointy sides and flat sides
	protected static void Swap<T>(ref T a, ref T b, bool condition = true){
		if(condition){
			T temp = b; b = a; a = temp;
		}
	}
	
	// returns the a number rounded to the nearest multiple of anothr number (rounds up)
	protected static float RoundCeil (float number, float multiple){
		return Mathf.Ceil(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}
	// returns the a number rounded to the nearest multiple of anothr number (rounds up or down)
	protected static float RoundMultiple (float number, float multiple){
		return Mathf.Round(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}
	// returns the a number rounded to the nearest multiple of anothr number (rounds down)
	protected static float RoundFloor (float number, float multiple){
		return Mathf.Floor(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}
	#endregion
	
	#region deprecated methods (used for backwards compatibility with older releases)
	///@cond EXCLUDE_THIS
	// world space
	[System.Obsolete("Deprecated, please use NearestVertexW instead",false)]
	public Vector3 FindNearestVertex(Vector3 fromPoint, bool doDebug = false) {return NearestVertexW(fromPoint, doDebug);}
	[System.Obsolete("Deprecated, please use NearestFaceW instead")]
	public Vector3 FindNearestFace(Vector3 fromPoint, GridPlane thePlane, bool doDebug = false) {return NearestFaceW(fromPoint, thePlane, doDebug);}
	[System.Obsolete("Deprecated, please use NearestBoxW instead",false)]
	public Vector3 FindNearestBox(Vector3 fromPoint, bool doDebug = false) {return NearestBoxW(fromPoint, doDebug);}
	// grid space
	[System.Obsolete("Deprecated, please use NearestVertexG instead",false)]
	public Vector3 GetVertexCoordinates(Vector3 world) {return NearestVertexG(world);}
	[System.Obsolete("Deprecated, please use NearestFaceG instead",false)]
	public Vector3 GetFaceCoordinates(Vector3 world, GridPlane thePlane) {return NearestFaceG(world, thePlane);}
	[System.Obsolete("Deprecated, please use NearestBoxG instead",false)]
	public Vector3 GetBoxCoordinates(Vector3 world) {return NearestBoxG(world);}
	///@endcond
	#endregion
}

/// <summary>Radians or degrees.</summary>
/// 
/// This is a simple enum for specifying whether an angle is given in radians for degrees.
/// This enum is so far only used in methods of GFPolarGrid, but I decided to make it global in case other grids in the future will use it was well.
public enum GFAngleMode {radians = 0, degrees};