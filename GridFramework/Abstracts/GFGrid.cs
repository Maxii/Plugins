#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define UNITY_3_API
#endif

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6
#define UNITY_3_API
#endif

using UnityEngine;
using System;
using GridFramework;
using GridFramework.Vectors;

/// <summary>Abstract base class for all Grid Framework grids.</summary>
/// 
/// This is the standard class all grids are based on. Aside from providing a
/// common set of variables and a template for what methods to use, this class
/// has no practical meaning for end users. Use this as reference for what can
/// be done without having to specify which type of grid you are using. For
/// anything more specific you have to look at the child classes.
[System.Serializable] public abstract class GFGrid : MonoBehaviour {
	#region nested classes and enums

	// instead of always aligning the centre we can use this to align other regions (WIP)
	private enum AlignReference {
		Center,
		RightUpBack,
		RightUpFront,
		RightDownBack,
		RightDownFront,
		LeftUpBack,
		LeftUpFront,
		LeftDownBack,
		LeftDownFront}
	;
	#endregion

	#region Caching
	#region Matrices
	/// @internal
	/// <summary>
	///   Flag that tells us when we need to update the matrices before
	///   carrying out matrix operations.
	/// </summary>
	///
	/// <para>
	///   Whenever something is done to the grid that changes the coordinate
	///   conversion matrices set this flag to <c>true</c>. After the matrices
	///   have been updated set it back to <c>false</c>.
	/// </para>
	protected bool _matricesMustUpdate = true;
	
	/// <summary>Update all the coordinate conversion matrices of the grid.</summary>
	protected abstract void MatricesUpdate();
	#endregion
	#region Transform
	/// @internal <summary>Caching the transform for performance.</summary>
	private Transform  _transform; //this is the real cache
	private Vector3    _position;
	private Quaternion _rotation;
	//private Transform _parent = null; // <-- Do I need this?

	/// @internal<summary>Whether the _Transform has been changed since the
	/// last time this was checked.</summary>
	/// <returns>
	///   <c>true</c>, if the <c>_Transform</c> has been changed since the last
	///   time, <c>false</c> otherwise.
	/// </returns>
	protected bool _TransformNeedsUpdate() {
		bool alteredTransform = false;
		if (_position != _Transform.position) {
			alteredTransform = true;
			_position = _Transform.position;
		}
		if (_rotation != _Transform.rotation) {
			alteredTransform = true;
			_rotation = _Transform.rotation;
		}
		/*if (_parent != _transform.parent) {
					alteredTransform = true;
					_parent = _transform.parent;
		}*/
		if (alteredTransform) {
			_matricesMustUpdate = true;
			_drawPointsMustUpdate = true;
		}
		return alteredTransform;
	}

	/// @internal <summary>
	///   This is used for access, if there is nothing cached it performs the
	///   cache first, then return the component.
	/// </summary>
	protected Transform _Transform {
		get {
			if (!_transform) {
				_transform = transform;
				_position = _transform.position;
				_rotation = _transform.rotation;
				//_parent    = _Transform.parent;
			}
			return _transform;
		}
	}
	#endregion
	#region Draw points
	/// @internal <summary>Amount of draw points.</summary>
	/// Each of the three entries stands for the amount of *lines* to draw per corresponding axis.
	private int[] _drawPointsCount = {0, 0, 0};

	/// @internal
	/// <summary>We store the draw points here for re-use.</summary>
	///
	/// I should consider switching to a 3-dimensional array instead of a
	/// jagged one.  The other dimension is always 3 and stands for the three
	/// axes. The middle dimension is the amount of lines per axis and it's
	/// always different. The inner dimension is always 2 and contains the two
	/// end points of each line.
	protected Vector3[][][] _drawPoints = new Vector3[3][][];

	/// @internal <summary>Flag that forces the draw points to update.</summary>
	protected bool _drawPointsMustUpdate = true;
	/// @internal <summary>Flag that forces the number of draw points to update.</summary>
	protected bool _drawPointsCountMustUpdate = true;

	/// <summary>Updates the draw points array.</summary>
	/// <param name="from">Lower limit of the points.</param>
	/// <param name="to">Upper limit of the points.</param>
	///
	/// This method updates the draw points if necessary.
	protected void DrawPointsUpdate(Vector3 from, Vector3 to) {
		//Debug.Log("_drawPointsMustUpdate: " + _drawPointsMustUpdate + ", _TransformNeedsUpdate(): " + _TransformNeedsUpdate());
		bool changeRequired = _drawPointsMustUpdate || _TransformNeedsUpdate();
		if (!changeRequired && _drawPoints[0] != null && _drawPoints[1] != null && _drawPoints[2] != null) {
			//Debug.Log("No change.");
			return;
		}
		//Debug.Log("Change");
		// calcuclate how many draw points we need
		int sizeX = 0, sizeY = 0, sizeZ = 0;
		DrawPointsCount(ref sizeX, ref sizeY, ref sizeZ, ref from, ref to);
		// allocate them
		if (DrawPointsAllocate(sizeX, sizeY, sizeZ) || changeRequired) {
			// calculate the points
			DrawPointsCalculate(_drawPoints, ref _drawPointsCount, from, to);
			_drawPointsMustUpdate = false;
		}
	}

	/// <summary>Computes the amount of draw points.</summary>
	/// <param name="countX">Amount of "x" (red) draw lines.</param>
	/// <param name="countY">Amount of "y" (green) draw lines.</param>
	/// <param name="countZ">Amount of "z" (blue) draw lines.</param>
	/// <param name="from">Lower limit of the points.</param>
	/// <param name="to">Upper limit of the points.</param>
	/// <param name="condition">If the condition evaluates to <c>true</c> the
	/// computation is carried out, otherwise nothing happens.</param>
	///
	/// This is an abstract method, the implementations are up to the
	/// subclasses. Despite its name, the amount is for the *lines* per axis,
	/// not individual points (two per line). This method should be called
	/// before the array is allocated. note that both <c>from</c> and <c>to</c>
	/// are references, this allows us to convert them into a common format
	/// that can be used in the subsequent calculations. For example, absolute
	/// world dimensions could be converted to relative grid dimensions and all
	/// other calculations would only need to be implemented for relative grid
	/// dimensions.
	protected abstract void DrawPointsCount(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool condition = true);

	/// <summary>Allocates a memory array for new draw points when needed.</summary>
	/// <returns><c>true</c>, if points a new array needed to be allocated, <c>false</c> otherwise.</returns>
	/// <param name="sizeX">Size of the "x" (red) line set.</param>
	/// <param name="sizeY">Size of the "y" (green) line set.</param>
	/// <param name="sizeZ">Size of the "z" (blue) line set.</param>
	///
	/// This method first checks is the size of the individual line sets has
	/// changed or if they even exist. If so, then it simply returns false.
	/// Otherwise the size array is updated and then the line arrays are
	/// created and all vector set to (0, 0, 0).
	protected bool DrawPointsAllocate(int sizeX, int sizeY, int sizeZ) {
		if (_drawPoints[2] != null && _drawPointsCount[0] == sizeX && _drawPointsCount[1] == sizeY && _drawPointsCount[2] == sizeZ && _drawPoints[0] != null && _drawPoints[1] != null) {
			//Debug.Log("Discrepancy X: " + (_drawPointsCount[0] - sizeX) + ", Y: " + (_drawPointsCount[1] - sizeY) + ", Z: " + (_drawPointsCount[2] - sizeZ));
			//Debug.Log("No alloc");
			return false;
		}
		//Debug.Log("Alloc");
		// update the size array
		_drawPointsCount[0] = sizeX; _drawPointsCount[1] = sizeY; _drawPointsCount[2] = sizeZ;

		for (int i = 0; i < 3; ++i) {
			_drawPoints[i] = new Vector3[_drawPointsCount[i]][];
			for (int j = 0; j < _drawPointsCount[i]; ++j) {
				_drawPoints[i][j] = new Vector3[2];
				for (int k = 0; k < 2; ++k) {
					_drawPoints[i][j][k] = Vector3.zero;
				}
			}
		}
		_drawPointsCountMustUpdate = false;
		return true;
	}

	/// <summary>Computes the coordinates of the draw points.</summary>
	/// <param name="points">Array that stores the points.</param>
	/// <param name="amount">array containing the amount of *lines* per axis.</param>
	/// <param name="from">Lower limit of the points.</param>
	/// <param name="to">Upper limit if the points.</param>
	///
	/// This is an abstract method, the implementations are up to the
	/// subclasses. Call the method after the amount is known and the
	/// <c>points</c> array is allocated.
	protected abstract void DrawPointsCalculate(Vector3[][][] points, ref int[] amount, Vector3 from, Vector3 to);
	#endregion
	#endregion
	
	#region class members
	#region size and range

	#region Protected
	///<summary> Whether rendering depends on "spacing" or not. </summary>
	[SerializeField] private bool _relativeSize = true;
	
	///<summary> Combined upper and lower rendering range bound. </summary>
	[SerializeField] protected Vector3 _size = 5.0f * Vector3.one;
	
	///<summary> Lower rendering range bound. </summary>
	[SerializeField] protected Vector3 _renderFrom = -5 * Vector3.one;
	
	///<summary> Upper rendering range bound. </summary>
	[SerializeField] protected Vector3 _renderTo = 5 * Vector3.one;
	#endregion

	#region Accessors
	/// <summary>Whether the drawing/rendering will scale with spacing.</summary>
	/// <value><c>true</c> if the grid's size is in grid coordinates; <c>false</c> if it's in world coordinates.</value>
	/// Set this to <c>true</c> if you want the drawing to have relative size,
	/// i.e. to scale with the spacing/radius or whatever the specific grid
	/// uses.  Otherwise set it to <c>false</c>.
	/// 
	/// See also: <see cref="size"/>, <see cref="renderFrom"/>, <see cref="renderTo"/>
	public bool relativeSize {
		get {return _relativeSize;}
		set {SetMember<bool>(value, ref _relativeSize, false);}
	}

	/// <summary>The size of the visual representation of the grid.</summary>
	/// <value>The size of the grid's visual representation.</value>
	/// Defines the size of the drawing and rendering of the grid. Keep in mind
	/// that the grid is infinitely large, the drawing is just a visual
	/// representation, stretching on all three directions from the origin. The
	/// size is either absolute or relative to the grid's other parameters,
	/// depending on the value of <see cref="relativeSize"/>.
	/// 
	/// If you set <see cref="useCustomRenderRange"/> to <c>true</c> that range
	/// will override this member. The size is either absolute or relative to
	/// the grid's other parameters, depending on the value of <see cref="relativeSize"/>.
	/// 
	/// See also: <see cref="relativeSize"/>, <see cref="useCustomRenderRange"/>
	public virtual Vector3 size {
		get {return _size;}
		set {
			SetMember<Vector3>(value, ref _size, restrictor: Vector3.Max, limit: Vector3.zero, updateMatrix: false);
		}
	}

	/// <summary>Custom lower limit for drawing and rendering.</summary>
	/// <value>Custom lower limit for drawing and rendering.</value>
	/// When using a custom rendering range this is the lower left backward
	/// limit of the rendering and drawing.
	/// 
	/// See also: <see cref="relativeSize"/>, <see cref="useCustomRenderRange"/>, <see cref="renderTo"/>
	public virtual Vector3 renderFrom {
		get {return _renderFrom;}
		set {
			SetMember<Vector3>(value, ref _renderFrom, restrictor: Vector3.Min, limit: renderTo, updateMatrix: false);
		}
	}

	/// <summary>Custom upper limit for drawing and rendering.</summary>
	/// <value>Custom upper limit for drawing and rendering.</value>
	/// When using a custom rendering range this is the upper right forward limit of the rendering and drawing.
	/// 
	/// See also: <see cref="relativeSize"/>, <see cref="useCustomRenderRange"/>, <see cref="renderFrom"/>
	public virtual Vector3 renderTo {
		get {return _renderTo;}
		set {
			SetMember<Vector3>(value, ref _renderTo, restrictor: Vector3.Max, limit: renderFrom, updateMatrix: false);
		}
	}
	#endregion
	#endregion

	#region Offset
	[SerializeField]
	protected Vector3
		_originOffset = Vector3.zero;

	/// <summary>Offset to add to the origin</summary>
	/// 
	/// By default the origin of grids is at the world position of their
	/// gameObject (the position of the Transform), this offset allows you to
	/// move the grid's pivot point by adding a value to it.  Keep in mind how
	/// this will affect the various grid coordinate systems, they are still
	/// relative to the grid's origin, not the Transform.
	/// 
	/// In other words, if a point at grid position (1, 2, 0) is at world
	/// position (4, 5, 0) and you add an offset of (1, 1, 0), then point's
	/// grid position will still be (1, 2, 0), but its world position will be
	/// (5, 6, 0). Here is an example:
	/// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	/// GFGrid myGrid;
	/// Vector3 gPos = new Vector3 (1, 2, 3);
	/// Vector3 wPos = myGrid.GridToWorld (gPos); 
	/// Debug.Log (wPos); // prints (4, 5, 0)
	/// 
	/// myGrid.pivotOffset = new Vector3 (1, 1, 0);
	/// wPos = myGrid.GridToWorld (gPos); 
	/// Debug.Log (wPos); // prints (5, 6, 0)
	/// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	public Vector3 originOffset {
		get { return _originOffset;}
		set {
			SetMember<Vector3>(value, ref _originOffset);
		}
	}
	#endregion

	#region colours
	protected const string DefaultShader = "Shader \"Lines/Colored Blended\" {" +
		"SubShader { Pass { " +
		"	Blend SrcAlpha OneMinusSrcAlpha " +
		"	ZWrite Off Cull Off Fog { Mode Off } " +
		"	BindChannels {" +
		"	Bind \"vertex\", vertex Bind \"color\", color }" +
		"} } }";

	/// <summary>Colours of the axes when drawing and rendering.</summary>
	/// 
	/// The colours are stored as three separate entries, corresponding to the
	/// three separate axes. They will be used for both drawing an rendering,
	/// unless <see cref="useSeparateRenderColor"/> is set to <c>true</c>.
	public ColorVector3 axisColors = new ColorVector3();

	/// <summary>Whether to use the same colours for rendering as for drawing.</summary>
	/// 
	/// If you set this flag to <c>true</c> the rendering will use the colours
	/// of <see cref="renderAxisColors"/>, otherwise it will default to <see
	/// cref="axisColors"/>.  This is useful if you want to have different
	/// colours for rendering and drawing. For example, you could have a
	/// clearly visible grid in the editor to work with and a barely visible
	/// grid in the game while playing.
	public bool useSeparateRenderColor = false;

	/// <summary>Separate colours of the axes when rendering.</summary>
	/// 
	/// By default the colours of <see cref="axisColors"/> are used for
	/// rendering, however if you set <see cref="useSeparateRenderColor"/> to
	/// <c>true</c> these colours will be used instead. Otherwise this does
	/// nothing.
	public ColorVector3 renderAxisColors = new ColorVector3(Color.gray);
	#endregion
	
	#region Draw & Render Flags
	/// <summary>Whether to hide the grid completely.</summary>
	/// 
	/// <para>If set to <c>true</c> the grid will be neither drawn nor rendered
	/// at all, it then takes precedence over all the other flags.</para>
	public bool hideGrid = false;

	/// <summary>Whether to hide the grid in play mode.</summary>
	/// 
	/// This is similar to <see cref="hideGrid"/>, but only active while in play mode.
	public bool hideOnPlay = false;

	/// <summary>Whether to hide just individual axes.</summary>
	/// 
	/// This hides the individual axes rather than the whole grid.
	public BoolVector3 hideAxis = new BoolVector3();

	/// <summary>Whether to draw a little sphere at the origin of the grid.</summary>
	/// 
	/// If set to <c>true</c> a small gizmo sphere will be drawn at the origin
	/// of the grid. This is not a rendering, so it wil not appear in the game,
	/// it is intended to make selecting the grid in the editor easier.
	public bool drawOrigin = false;

	/// <summary>Whether to render the grid at runtime.</summary>
	/// 
	/// The grid will only be rendered if this flag is set to <c>true</c>,
	/// otherwise you won't be able to see the grid in the game.
	public bool renderGrid = true;

	[SerializeField] protected bool _useCustomRenderRange = true;
	/// <summary>Use your own values for the range of the rendering.</summary>
	/// <value><c>true</c> if using a custom range for rendering and drawing; otherwise, <c>false</c>.</value>
	///
	/// If this flag is set to <c>true</c> the grid rendering and drawing will
	/// use the values of <see cref="renderFrom"/> and <see cref="renderTo"/>
	/// as limits. Otherwise it will use the <see cref="size"/> instead.
	public bool useCustomRenderRange {
		get{ return _useCustomRenderRange;}
		set{ 
			SetMember<bool>(value, ref _useCustomRenderRange, false);
		}
	}

	[SerializeField]
	protected int _renderLineWidth = 1;
	/// <summary>The width of the lines used when rendering the grid.</summary>
	/// <value>The width of the render line.</value>
	/// The width of the rendered lines, if it is set to 1 all lines will be
	/// one pixel wide, otherwise they will have the specified width in world
	/// units.
	public int renderLineWidth {
		get{ return _renderLineWidth;}
		set{ _renderLineWidth = Mathf.Max(value, 1);}
	}

	/// <summary>The material for rendering, if none is given it uses a default material.</summary>
	///
	/// You can use you own material if you want control over the shader used,
	/// otherwise a default material with the following shader will be used:
	/// <code>
	/// Shader "GridFramework/DefaultShader" {
	///     SubShader {
	///         Pass {
	///             Blend SrcAlpha OneMinusSrcAlpha
	///             ZWrite Off Cull Off Fog {
	///                 Mode Off
	///             }
	///             BindChannels {
	///                 Bind "vertex", vertex Bind "color", color
	///             }
	///         }
	///     }
	/// }
	/// </code>
	public Material renderMaterial;
	protected static Material DefaultRenderMaterial {
		get {
			#if UNITY_2_6 || UNITY_2_6_1 ||  UNITY_3_API || UNITY_4_API
			return new Material(DefaultShader);
			#endif
			return new Material(Shader.Find("GridFramework/DefaultShader"));
		}
	}
	#endregion
	
	#region helper values (read only)
	/// The normal X-, Y- and Z- vectors in world-space.
	protected static Vector3[] Units {
		get {
			return new Vector3[]{Vector3.right, Vector3.up, Vector3.forward};
		}
	}
	#endregion

	#region Events
	/// <summary>A delegate for handling events when the grid has been changed
	/// in such a way that it requires a redraw</summary>
	/// 
	/// This is the delegate type for methods to be called when changes to the
	/// grid occur. It is best used together with the #GridChangedEvent event.
	public delegate void GridChangedDelegate(GFGrid grid);

	/// <summary>An even that gets fired </summary>
	/// 
	/// This is the event that gets fired when one of the grid's properties is
	/// changed. If the Transform (position or rotation) is changed this event
	/// will only be fired if there is a camera trying to render the grid or
	/// some other method tries to draw the gird (like drawing in the editor or
	/// calling #GetVectrosityPoints). You can learn more about events ont he
	/// @ref events page of the user manual.
	public event GridChangedDelegate GridChangedEvent;

	protected void GridChanged() {
		if (GridChangedEvent != null) // fire the event
			GridChangedEvent(this);
	}
	#endregion

	#region Set Members
	// These two methods should be combined if I can figure out how to make the
	// limit and restrictor optional for both value- and reference types.

	/// <summary>Sets a member to a value and applies additional routines in the process.</summary>
	/// <param name="value">        The value to assign.                   </param>
	/// <param name="member">       The member to be assigned to.          </param>
	/// <param name="updateMatrix"> Whether to update the grid's matrices. </param>
	///
	/// This method should be used for setters of grid members. The method will
	/// abort if the member is already equal to the value. Otherwise it will update
	/// matrices and draw points if necessary. When all that is done the grid
	/// change event is fired.
	protected void SetMember<T> (T value, ref T member, bool updateMatrix = true) {
			if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, member)) {
				return;
			}
			member = value;
			_matricesMustUpdate |= updateMatrix;
			_drawPointsMustUpdate = true;
			_drawPointsCountMustUpdate |= !relativeSize;
			GridChanged();
	}

	/// <summary>Sets a member to a value and applies additional routines in the process.</summary>
	/// <param name="value"       > The value to assign.                   </param>
	/// <param name="member"      > The member to be assigned to.          </param>
	/// <param name="limit"       > A limiting value.                      </param>
	/// <param name="restrictor"  > A function to apply the limit.         </param>
	/// <param name="updateMatrix"> Whether to update the grid's matrices. </param>
	///
	/// This method should be used for setters of grid members. The method will
	/// abort if the member is already equal to the value. Otherwise it will first
	/// apply the limit using a supplied function and then update matrices and
	/// draw points if necessary. When all that is done the grid change event is
	/// fired.
	protected void SetMember<T> (T value, ref T member, T limit, Func<T, T, T> restrictor, bool updateMatrix = true) {
			// needed because the editor fires the setter even if this wasn't changed
			if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, member)) {
				return;
			}
			member = restrictor(value, limit);
			_matricesMustUpdate |= updateMatrix;
			_drawPointsMustUpdate = true;
			_drawPointsCountMustUpdate |= !relativeSize;
			GridChanged();
	}
	
	#endregion
	#endregion
	
	#region Grid <-> World coordinate transformation
	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <returns>Grid coordinates of the world point.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Takes in a point in world space and converts it to grid space. Some
	/// grids have several coordinate system, so look into the specific class
	/// for conversion methods to other coordinate systems.
	public abstract Vector3 WorldToGrid(Vector3 worldPoint);

	/// <summary>Converts grid coordinates to world coordinates.</summary>
	/// <returns>World coordinates of the grid point.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// Takes in a point in grid space and converts it to world space. Some
	/// grids have several coordinate system, so look into the specific class
	/// for conversion methods from other coordinate systems.
	public abstract Vector3 GridToWorld(Vector3 gridPoint);
	#endregion
	
	#region nearest in world space
	/// <summary>Returns the world position of the nearest vertex.</summary>
	/// <returns>World position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Returns the world position of the nearest vertex from a given point in
	/// world space. If <paramref name="doDebug"/> is set a small gizmo sphere
	/// will be drawn at that position. This is just an abstract template for
	/// the method, look into the specific class for exact implementation.
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug);

	/// @overload
	public Vector3 NearestVertexW(Vector3 worldPoint) {
		return NearestVertexW(worldPoint, false);
	}

	/// <summary>Returns the world position of the nearest face.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world
	/// coordinates of a face on the grid. Since the face is enclosed by
	/// several vertices, the returned value is the point in between all of the
	/// vertices. You also need to specify on which plane the face lies
	/// (optional for hex- and polar grids). If <paramref name="doDebug"/> is
	/// set a small gizmo face will drawn there.
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug);

	/// @overload
	public Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane) {
		return NearestFaceW(worldPoint, plane, false);
	}

	/// <summary>Returns the world position of the nearest box.</summary>
	/// <returns>World position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world
	/// coordinates of a box in the grid. Since the box is enclosed by several
	/// vertices, the returned value is the point in between all of the
	/// vertices. If <paramref name="doDebug"/> is set a small gizmo box will
	/// drawn there.
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug);

	/// @overload
	public Vector3 NearestBoxW(Vector3 worldPoint) {
		return NearestBoxW(worldPoint, false);
	}
	#endregion
	
	#region nearest in grid space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Returns the position of the nearest vertex in grid coordinates from a
	/// given point in world space.
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestVertexG(Vector3 worldPoint);

	/// <summary>Returns the grid position of the nearest Face.</summary>
	/// <returns>Grid position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid
	/// coordinates of a face on the grid. Since the face is enclosed by
	/// several vertices, the returned value is the point in between all of the
	/// vertices. You also need to specify on which plane the face lies
	/// (optional for hex- and polar grids).
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane);

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <returns>Grid position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid
	/// coordinates of a box in the grid. Since the box is enclosed by several
	/// vertices, the returned value is the point in between all of the
	/// vertices.
	/// 
	/// This is just an abstract template for the method, look into the
	/// specific class for exact implementation.
	public abstract Vector3 NearestBoxG(Vector3 worldPoint);
	#endregion
	
	#region Align Methods
	/// <summary>Fits a position vector into the grid.</summary>
	/// <returns>Aligned position vector.</returns>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// 
	/// Fits a position inside the grid by using the object’s transform. The
	/// exact position depends on whether the components of <paramref
	/// name="scale"/> are even or odd and the exact implementation can be
	/// found in the subclasses. The parameter <paramref name="ignoreAxis"/>
	/// makes the function not touch the corresponding coordinate.
	public abstract Vector3 AlignVector3(Vector3 pos, Vector3 scale, BoolVector3 ignoreAxis);

	#region Overload
	/// @overload
	/// It aligns the position while respecting all axes and uses a default
	/// size of 1 x 1 x 1, it is equal to <code>AlignVector3(pos, Vector3.one,
	/// new BoolVector3(false));</code>
	public Vector3 AlignVector3(Vector3 pos) {
		return AlignVector3(pos, Vector3.one, new BoolVector3(false));
	}

	/// @overload
	/// It aligns the position and uses a default size of 1 x 1 x 1 while
	/// leaving the axes to the user, it is equal to
	/// <code>AlignVector3(pos, Vector3.one, lockAxis);</code>
	public Vector3 AlignVector3(Vector3 pos, BoolVector3 lockAxis) {
		return AlignVector3(pos, Vector3.one, lockAxis);
	}
		
	/// @overload
	/// It aligns the position and respects the axes while using a default size
	/// of 1 x 1 x 1, it is equal to <code>AlignVector3(pos, scale, new
	/// BoolVector3(false));</code>
	public Vector3 AlignVector3(Vector3 pos, Vector3 scale) {
		return AlignVector3(pos, scale, new BoolVector3(false));
	}
	#endregion

	/// <summary>Fits a Transform inside the grid (without scaling it).</summary>
	/// <param name="theTransform">The Transform to align.</param>
	/// <param name="rotate">Whether to rotate to the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// 
	/// Fits an object inside the grid by using the object’s Transform. Setting
	/// <c>doRotate</c> makes the object take on the grid’s rotation. The
	/// parameter <c>lockAxis</c> makes the function not touch the
	/// corresponding coordinate.
	/// 
	/// The resulting position depends on <cref name="AlignVector3"/>, so
	/// please look up how that method works.
	public void AlignTransform(Transform theTransform, bool rotate, BoolVector3 ignoreAxis) {
		theTransform.position = AlignVector3(theTransform.position, theTransform.lossyScale, ignoreAxis);
		if (rotate) {theTransform.rotation = transform.rotation;}
	}

	#region overload
	/// @overload
	/// It aligns and rotates the Transform while respecting all axes, it is equal to
	/// <code>AlignTransform(theTransform, true, new BoolVector3(false));</code>
	public void AlignTransform(Transform theTransform) {
		AlignTransform(theTransform, true, new BoolVector3(false));
	}

	/// @overload
	/// It aligns and rotates the Transform but leaves the axes to the user, it is equal to
	/// <code>AlignTransform(theTransform, true, lockAxis);</code>
	public void AlignTransform(Transform theTransform, BoolVector3 lockAxis) {
		AlignTransform(theTransform, true, lockAxis);
	}

	/// @overload
	/// It aligns and respects all axes to the user, but leaves the decision of rotation to the user, it is equal to
	/// <code>AlignTransform(theTransform, rotate, new BoolVector3(false));</code>
	public void AlignTransform(Transform theTransform, bool rotate) {
		AlignTransform(theTransform, rotate, new BoolVector3(false));
	}
	#endregion
	#endregion
	
	#region Scale Methods
	/// <summary>Scales a size vector to fit inside a grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// 
	/// This method takes in a vector representing a size and fits it inside
	/// the grid. The *ignoreAxis* parameter lets you ignore individual axes.
	public abstract Vector3 ScaleVector3(Vector3 scl, BoolVector3 ignoreAxis);

	#region overload
	/// @overload
	/// It scales the size while respecting all axes, it is equal to
	/// <code>ScaleVector3(scl, new BoolVector3(false));</code>
	public Vector3 ScaleVector3(Vector3 scl) {
		return ScaleVector3(scl, new BoolVector3(false));
	}
	#endregion
	/// <summary>Scales a Transform to fit the grid (without moving it).</summary>
	/// <param name="theTransform">The Transform to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	///
	/// Scales a Transform to fit inside a grid. The parameter *ignoreAxis*
	/// makes the function not touch the corresponding coordinate.
	/// 
	/// The resulting position depends on <see cref="ScaleVector3"/>, so please look up how that method works.
	public void ScaleTransform(Transform theTransform, BoolVector3 ignoreAxis) {
		theTransform.localScale = ScaleVector3(theTransform.localScale, ignoreAxis);
	}

	#region Overload
	/// @overload
	/// It scales the Transform while respecting all axes, it is equal to
	/// <code>ScaleTransform(theTransform, new BoolVector3(false));</code>
	public void ScaleTransform(Transform theTransform) {
		ScaleTransform(theTransform, new BoolVector3(false));
	}
	#endregion
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
	/// Renders the grid with lower and upper limit, a given line width and
	/// individual colours for the three axes.  If the lines have line width 1
	/// they will be exactly one pixel wide, and if they have a larger with
	/// they will be rendered as billboards (always facing the camera).  If
	/// there is no camera and camera Transform passed this won't be possible
	/// and the lines will default back to one pixel width.
	/// 
	/// It is not necessary to call this method manually, rather you should
	/// just set the @c #renderGrid flag to @c true and let Grid Framework take
	/// care of it.  However, if you want total control use this method,
	/// usually from within an <c><a
	/// href="http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnPostRender.html">OnPostRender</a></c>
	/// method.
	public void RenderGrid(Vector3 from, Vector3 to, ColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null) {
		if (!renderGrid) {
			return;
		}

		if (!renderMaterial) {
			renderMaterial = DefaultRenderMaterial;
		}
		
		DrawPointsUpdate(from, to);
				
		RenderGridLines(colors, width, cam, camTransform);
	}

	#region overload
	/// @overload
	/// Renders the grid using @c #size for lower and upper limits, equal to
	/// <code>RenderGrid(-size, size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);</code>
	public virtual void RenderGrid(int width = 0, Camera cam = null, Transform camTransform = null) {
		RenderGrid(-size, size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}

	/// @overload
	/// Renders the grid using @c #axisColors (or @c #renderAxisColors if @c
	/// #useSeparateRenderColor is <c>true</c>) as colours, equal to
	/// <code>RenderGrid(from, to, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);</code>
	public void RenderGrid(Vector3 from, Vector3 to, int width = 0, Camera cam = null, Transform camTransform = null) {
		RenderGrid(from, to, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}
	#endregion

	protected void RenderGridLines(ColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null) {
		renderMaterial.SetPass(0);
		
		if (width <= 1 || !cam || !camTransform) {// use simple lines for width 1 or if no camera was passed
			GL.Begin(GL.LINES);
			for (int i = 0; i < 3; i++) {
				if (hideAxis[i]) {
					continue;
				}
				GL.Color(colors[i]);
				foreach (Vector3[] line in _drawPoints[i]) {
					if (line == null) {
						continue;
					}
					GL.Vertex(line[0]);
					GL.Vertex(line[1]);
				}
			}
			GL.End();
		} else {// quads for "lines" with width
			GL.Begin(GL.QUADS);
			float mult = Mathf.Max(0, 0.5f * width); //the multiplier, half the desired width
			
			for (int i = 0; i < 3; i++) {
				GL.Color(colors[i]);
				if (hideAxis[i]) {
					continue;
				}
				
				//sample a direction vector, one per direction is enough (using
				//the first line of each line set (<- !!! ONLY TRUE FOR RECT
				//GRIDS !!!)
				var dir = new Vector3();
				if (_drawPoints[i].Length > 0) { //can't get a line if the set is empty
					dir = Vector3.Cross(
						_drawPoints[i][0][0] - _drawPoints[i][0][1],
						camTransform.forward
					).normalized;
				}
				//multiply dir with the world length of one pixel in distance
				#if UNITY_2_6 || UNITY_2_6_1
				if (cam.isOrthoGraphic) {
				#else
				if (cam.orthographic) {
				#endif
					dir *= (cam.orthographicSize * 2) / cam.pixelHeight;
				} else {// (the 50 below is just there to smooth things out)
					dir *= (cam.ScreenToWorldPoint(new Vector3(0, 0, 50)) - cam.ScreenToWorldPoint(new Vector3(20, 0, 50))).magnitude / 20;
				}
				
				foreach (Vector3[] line in _drawPoints[i]) {
					if (line == null) {
						continue;
					}
					// if the grid is not rectangular we need to change dir every time
					if (GetType() != typeof(GFRectGrid)) {
						dir = Vector3.Cross(line[0] - line[1], camTransform.forward).normalized;
						#if UNITY_2_6 || UNITY_2_6_1
						if (cam.isOrthoGraphic) {
						#else
						if (cam.orthographic) {
						#endif
							dir *= (cam.orthographicSize * 2) / cam.pixelHeight;
						} else {// (the 50 below is just there to smooth things out)
							dir *= (cam.ScreenToWorldPoint(new Vector3(0, 0, 50)) - cam.ScreenToWorldPoint(new Vector3(20, 0, 50))).magnitude / 20;
						}
					}
					GL.Vertex(line[0] - mult * dir);
					GL.Vertex(line[0] + mult * dir);
					GL.Vertex(line[1] + mult * dir);
					GL.Vertex(line[1] - mult * dir);
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
	/// This method draws the grid in the editor using gizmos. There is usually
	/// no reason to call this method manually, you should instead set the
	/// drawing flags of the grid itself. However, if you must, call this
	/// method from inside <c><a
	/// href="http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDrawGizmos.html">OnDrawGizmos</a></c>.
	public  void DrawGrid(Vector3 from, Vector3 to) {
		//don't draw if not supposed to
		if (hideGrid) {
			return;
		}
		
		//CalculateDrawPoints(from, to);
		DrawPointsUpdate(from, to);
		
		for (int i = 0; i < 3; i++) {
			if (hideAxis[i]) {
				continue;
			}
			Gizmos.color = axisColors[i];
			foreach (Vector3[] line in _drawPoints[i]) {
				if (line == null) {
					continue;
				}
				Gizmos.DrawLine(line[0], line[1]);
			}
		}
		
		//draw a sphere at the centre
		if (drawOrigin) {
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(_Transform.position, 0.3f);
		}
	}

	/// @overload
	/// Uses the size as limits, it is equivalent to
	/// <code>DrawGrid(-size, size);</code>
	public void DrawGrid() {
		DrawGrid(-size, size);
	}

	protected static void DrawSphere(Vector3 pos, float rad = 0.3f) {
		Gizmos.DrawSphere(pos, rad);
	}
	#endregion
	
	#region Vectrosity Methods
	/// <summary>Returns an array of Vector3 points ready for use with Vectrosity.</summary>
	/// <returns>Array of points in local space.</returns>
	/// <param name="from">Lower limit for the points.</param>
	/// <param name="to">Upper limit for the points.</param>
	/// 
	/// Returns an array of Vector3 containing the points for a discrete vector
	/// line in Vectrosity. One entry is the starting point, the next entry is
	/// the end point, the next entry is the starting point of the next line
	/// and so on.
	public Vector3[] GetVectrosityPoints(Vector3 from, Vector3 to) {
		Vector3[][] seperatePoints = GetVectrosityPointsSeparate(from, to); 
		var returnedPoints = new Vector3[seperatePoints[0].Length + seperatePoints[1].Length + seperatePoints[2].Length];
		seperatePoints[0].CopyTo(returnedPoints, 0);
		seperatePoints[1].CopyTo(returnedPoints, seperatePoints[0].Length);
		seperatePoints[2].CopyTo(returnedPoints, seperatePoints[0].Length + seperatePoints[1].Length);
		return returnedPoints;
	}
		
	/// @overload
	/// Uses the grid's @c #size or @c #renderFrom and @c #renderTo as limits, equal to
	/// <code>useCustomRenderRange ? GetVectrosityPoints(renderFrom, renderTo) : GetVectrosityPoints(-size, size);</code>
	public Vector3[] GetVectrosityPoints() {
		return useCustomRenderRange ? GetVectrosityPoints(renderFrom, renderTo) : GetVectrosityPoints(-size, size);
	}

	/// <summary>Returns an array of arrays of Vector3 points ready for use with Vectrosity.</summary>
	/// <returns>Jagged array of three arrays, each containing the points of a single axis.</returns>
	/// <param name="from">Lower limit for the points.</param>
	/// <param name="to">Upper limit for the points.</param>
	/// 
	/// This method is very similar to @c #GetVectrosityPoints, except that the
	/// points are in separate arrays for each axis. This is useful if you want
	/// to treat the lines of each axis differently, like having different
	/// colours.
	public Vector3[][] GetVectrosityPointsSeparate(Vector3 from, Vector3 to) {
		// Count the amount of points
		int lengthX = 0, lengthY = 0, lengthZ = 0;
		DrawPointsCount(ref lengthX, ref lengthY, ref lengthZ, ref from, ref to);
		var lengths = new [] {lengthX, lengthY, lengthZ};

		// Compute the lines
		var lines = new Vector3[3][][]; // allocate the arrays first
		for (int i = 0; i < 3; ++i) {
			lines[i] = new Vector3[lengths[i]][];
			for (int j = 0; j < lengths[i]; ++j) {
				lines[i][j] = new [] {Vector3.zero, Vector3.zero};
			}
		}
		DrawPointsCalculate(lines, ref lengths, from, to);

		// Make lines into points
		var points = new Vector3[3][];
		for (int i = 0; i < 3; ++i) {
			points[i] = new Vector3[2 * lengths[i]];
			for (int j = 0; j < lengths[i]; ++j) {
				points[i][2 * j + 0] = lines[i][j][0];
				points[i][2 * j + 1] = lines[i][j][1];
			}
		}
		
		//return returnedPoints;
		return points;
	}
		
	/// @overload
	/// Uses the grid's @c #size or @c #renderFrom and @c #renderTo as limits, equal to
	/// <code>useCustomRenderRange ? GetVectrosityPointsSeparate(renderFrom, renderTo) : GetVectrosityPointsSeparate(-size, size);</code>
	public Vector3[][] GetVectrosityPointsSeparate() {
		return useCustomRenderRange ? GetVectrosityPointsSeparate(renderFrom, renderTo) : GetVectrosityPointsSeparate(-size, size);
	}
	#endregion
	
	#region Runtime Methods
	void Awake() {
		hideGrid |= hideOnPlay;
		
		if (renderMaterial == null) {
			renderMaterial = DefaultRenderMaterial;
		}
		
		GFGridRenderManager.AddGrid(this);
	}
	
	void OnDestroy() {
		GFGridRenderManager.RemoveGrid(GetComponent<GFGrid>());
	}

	void OnDrawGizmos() {
		if (useCustomRenderRange) {
			DrawGrid(renderFrom, renderTo);
		} else {
			DrawGrid(-size, size);
		}
	}
	#endregion
	
	#region helper methods
	// swaps two variables, useful for swapping quasi-X and quasi-Y to keep the
	// same formula for pointy sides and flat sides
	protected static void Swap<T>(ref T a, ref T b) {
		Swap<T>(ref a, ref b, true);
	}
	protected static void Swap<T>(ref T a, ref T b, bool condition) {
		if (condition) {
			T temp = b;
			b = a;
			a = temp;
		}
	}
	
	// returns the a number rounded to the nearest multiple of anothr number (rounds up)
	protected static float RoundCeil(float number, float multiple) {
		return Mathf.Ceil(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}
	// returns the a number rounded to the nearest multiple of anothr number (rounds up or down)
	protected static float RoundMultiple(float number, float multiple) {
		return Mathf.Round(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}
	// returns the a number rounded to the nearest multiple of anothr number (rounds down)
	protected static float RoundFloor(float number, float multiple) {
		return Mathf.Floor(number / multiple) * multiple;// could use Ceil or Floor to always round up or down
	}

	/// <summary>Computes the remainder, but always as a positive number.</summary>
	/// <param name="a">Dividend.</param>
	/// <param name="b">Divisor.</param>
	/// <returns>Remainerder of Dividend / Divisor, always as a positive number.</returns>
	///
	/// The default % operator will return a negative number if Dividend is
	/// negative, this variant always returns a positive number.
	protected static int Mod(int a, int b) {
		return (a %= b) >= 0 ? a : -a;
	}
	#endregion
	
	#region deprecated methods (used for backwards compatibility with older releases)
	///@cond GF_DOXYGEN_EXCLUDE_THIS
	// world space
	[System.Obsolete("Deprecated, please use NearestVertexW instead",false)]
	public Vector3 FindNearestVertex(Vector3 fromPoint, bool doDebug = false) {
		return NearestVertexW(fromPoint, doDebug);
	}
	[System.Obsolete("Deprecated, please use NearestFaceW instead")]
	public Vector3 FindNearestFace(Vector3 fromPoint, GridPlane thePlane, bool doDebug = false) {
		return NearestFaceW(fromPoint, thePlane, doDebug);
	}
	[System.Obsolete("Deprecated, please use NearestBoxW instead",false)]
	public Vector3 FindNearestBox(Vector3 fromPoint, bool doDebug = false) {
		return NearestBoxW(fromPoint, doDebug);
	}
	// grid space
	[System.Obsolete("Deprecated, please use NearestVertexG instead",false)]
	public Vector3 GetVertexCoordinates(Vector3 world) {
		return NearestVertexG(world);
	}
	[System.Obsolete("Deprecated, please use NearestFaceG instead",false)]
	public Vector3 GetFaceCoordinates(Vector3 world, GridPlane thePlane) {
		return NearestFaceG(world, thePlane);
	}
	[System.Obsolete("Deprecated, please use NearestBoxG instead",false)]
	public Vector3 GetBoxCoordinates(Vector3 world) {
		return NearestBoxG(world);
	}
	///@endcond
	#endregion
}
