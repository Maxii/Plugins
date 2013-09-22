// Version 2.3
// ©2013 Starscene Software. All rights reserved. Redistribution of source code without permission not allowed.

#if UNITY_3_4 || UNITY_3_5
#define UNITY_3
#endif

using UnityEngine;
using System.Collections.Generic;

namespace Vectrosity {

public enum LineType {Continuous, Discrete}
public enum Joins {Fill, Weld, None}
public enum EndCap {Front, Both, Mirror, Back, None}
public enum Visibility {Dynamic, Static, Always, None}
public enum Brightness {Fog, None}

[System.Serializable]
public class VectorLine {
	GameObject m_vectorObject;
	public GameObject vectorObject {
		get {
			if (m_vectorObject != null) {
				return m_vectorObject;
			}
			else {
				LogError ("Vector object not set up");
				return null;
			}
		}
	}
	MeshFilter m_meshFilter;
	Mesh m_mesh;
	public Mesh mesh {
		get {return m_mesh;}
	}
	Vector3[] m_lineVertices;
	Vector2[] m_lineUVs;
#if UNITY_3
	Color[] m_lineColors;
#else
	Color32[] m_lineColors;
#endif
	public Color color {
		get {return m_lineColors[0];}
	}
	public Vector2[] points2;
	public Vector3[] points3;
	int m_pointsLength;
	int pointsLength {
		get {
			if ( (m_is2D && m_pointsLength != points2.Length) || (!m_is2D && m_pointsLength != points3.Length) ) {
				LogError ("The points array for \"" + name + "\" must not be resized. Use Resize if you need to change the length of the points array");
				return 0;
			}
			return m_pointsLength;
		}
	}
	bool m_is2D;
	Vector3[] m_screenPoints;
	float[] m_lineWidths;
	public float lineWidth {
		get {return m_lineWidths[0] * 2;}
		set {
			if (m_lineWidths.Length == 1) {
				m_lineWidths[0] = value * .5f;
			}
			else {
				float thisWidth = value * .5f;
				for (int i = 0; i < m_lineWidths.Length; i++) {
					m_lineWidths[i] = thisWidth;
				}
			}
			m_maxWeldDistance = (value*2) * (value*2);
#if !UNITY_3
			if (!m_1pixelLine && value == 1.0f) {
				RedoLine (true);
			}
			else if (m_1pixelLine && value != 1.0f) {
				RedoLine (false);
			}
#endif
		}
	}
	float m_maxWeldDistance;
	public float maxWeldDistance {
		get {return Mathf.Sqrt(m_maxWeldDistance);}
		set {m_maxWeldDistance = value * value;}
	}
	float[] m_distances;
	string m_name;
	public string name {
		get {return m_name;}
		set {
			m_name = value;
			if (m_vectorObject != null) {
				m_vectorObject.name = "Vector " + value;
			}
			if (m_mesh != null) {
				m_mesh.name = value;
			}
		}
	}
	Material m_material;
	public Material material {
		get {return m_material;}
		set {
			m_material = value;
			if (m_vectorObject != null) {
				m_vectorObject.renderer.material = m_material;
			}
		}
	}
	bool m_active = true;
	public bool active {
		get {return m_active;}
		set {
			m_active = value;
			if (m_vectorObject != null) {
				m_vectorObject.renderer.enabled = m_active;
			}
		}
	}
	public float capLength = 0.0f;
	int m_depth = 0;
	public int depth {
		get {return m_depth;}
		set {m_depth = Mathf.Clamp(value, 0, 100);}
	}
	public bool smoothWidth = false;
	int m_layer = -1;
	public int layer {
		get {return m_layer;}
		set {
			m_layer = value;
			if (m_layer < 0) m_layer = 0;
			else if (m_layer > 31) m_layer = 31;
			if (m_vectorObject != null) {
				m_vectorObject.layer = m_layer;
			}
		}
	}
	bool m_continuous;
	public bool continuous {
		get {return m_continuous;}
	}
	Joins m_joins;
	public Joins joins {
		get {return m_joins;}
		set {
			if (m_isPoints) return;
			if (!m_continuous && value == Joins.Fill) return;
			m_joins = value;
		}
	}
	bool m_isPoints;
	bool m_isAutoDrawing = false;
	public bool isAutoDrawing {
		get {return m_isAutoDrawing;}	
	}
	int m_minDrawIndex = 0;
	public int minDrawIndex {
		get {return m_minDrawIndex;}
		set {
			m_minDrawIndex = value;
			if (!m_continuous && (m_minDrawIndex & 1) != 0) {	// No odd numbers for discrete lines
				m_minDrawIndex++;
			}
			m_minDrawIndex = Mathf.Clamp (m_minDrawIndex, 0, pointsLength-1);
		}
	}
	int m_maxDrawIndex = 0;
	public int maxDrawIndex {
		get {return m_maxDrawIndex;}
		set {
			m_maxDrawIndex = value;
			m_minDrawIndex = Mathf.Clamp (m_minDrawIndex, 0, pointsLength-1);
		}
	}
	int m_drawStart = 0;
	public int drawStart {
		get {return m_drawStart;}
		set {
			if (!m_continuous && (value & 1) != 0) {	// No odd numbers for discrete lines
				value++;
			}
			m_drawStart = Mathf.Clamp (value, 0, pointsLength-1);
		}
	}
	int m_drawEnd = 0;
	public int drawEnd {
		get {return m_drawEnd;}
		set {
			if (!m_continuous && (value & 1) == 0) {	// No odd numbers for discrete lines
				value++;
			}
			m_drawEnd = Mathf.Clamp (value, 0, pointsLength-1);
		}
	}
	bool m_useNormals = false;
	bool m_useTangents = false;
	bool m_normalsCalculated = false;
	bool m_tangentsCalculated = false;
	int m_triangleCount;
	int m_vertexCount;
	EndCap m_capType = EndCap.None;
	string m_endCap;
	public string endCap {
		get {return m_endCap;}
		set {
			if (m_isPoints) {
				LogError ("VectorPoints can't use end caps");
				return;
			}
			if (value == null || value == "") {
				m_endCap = null;
				m_capType = EndCap.None;
				RemoveEndCapVertices();
				return;				
			}
			if (capDictionary == null || !capDictionary.ContainsKey (value)) {
				LogError ("End cap \"" + value + "\" is not set up");
				return;
			}
			m_endCap = value;
			m_capType = capDictionary[value].capType;
			if (m_capType != EndCap.None) {
				AddEndCap();
			}
		}
	}
	bool m_continuousTexture = false;
	public bool continuousTexture {
		get {return m_continuousTexture;}
		set {
			m_continuousTexture = value;
			if (value == false) {
				ResetTextureScale();
			}
		}
	}
	Transform m_useTransform;
#if !UNITY_3
	bool m_1pixelLine = false;
	static bool m_useMeshQuads = false;
	public static bool useMeshQuads {
		get {return m_useMeshQuads;}
		set {
			if (!m_meshRenderMethodSet) {
				m_useMeshQuads = value;
			}
			else {
				Debug.LogWarning ("useMeshQuads not changed, since a VectorLine has already been created");
			}
		}
	}
	static bool m_useMeshLines = false;
	public static bool useMeshLines {
		get {return m_useMeshLines;}
		set {
			if (!m_meshRenderMethodSet) {
				m_useMeshLines = value;
			}
			else {
				Debug.LogWarning ("useMeshLines not changed, since a VectorLine has already been created");
			}
		}
	}
	static bool m_useMeshPoints = false;
	public static bool useMeshPoints {
		get {return m_useMeshPoints;}
		set {
			if (!m_meshRenderMethodSet) {
				m_useMeshPoints = value;
			}
			else {
				Debug.LogWarning ("useMeshPoints not changed, since a VectorLine has already been created");
			}
		}
	}
	static bool m_meshRenderMethodSet = false;
#endif
	
	// Vector3 constructors
	public VectorLine (string lineName, Vector3[] linePoints, Material lineMaterial, float width) {
		points3 = linePoints;
		Color[] colors = SetColor(Color.white, LineType.Discrete, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color color, Material lineMaterial, float width) {
		points3 = linePoints;
		Color[] colors = SetColor(color, LineType.Discrete, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color[] colors, Material lineMaterial, float width) {
		points3 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, false, false);
	}

	public VectorLine (string lineName, Vector3[] linePoints, Material lineMaterial, float width, LineType lineType) {
		points3 = linePoints;
		Color[] colors = SetColor(Color.white, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color color, Material lineMaterial, float width, LineType lineType) {
		points3 = linePoints;
		Color[] colors = SetColor(color, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color[] colors, Material lineMaterial, float width, LineType lineType) {
		points3 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, false, false);
	}

	public VectorLine (string lineName, Vector3[] linePoints, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points3 = linePoints;
		Color[] colors = SetColor(Color.white, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color color, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points3 = linePoints;
		Color[] colors = SetColor(color, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, false, false);
	}
	public VectorLine (string lineName, Vector3[] linePoints, Color[] colors, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points3 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, false, false);
	}

	// Vector2 constructors
	public VectorLine (string lineName, Vector2[] linePoints, Material lineMaterial, float width) {
		points2 = linePoints;
		Color[] colors = SetColor(Color.white, LineType.Discrete, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color color, Material lineMaterial, float width) {
		points2 = linePoints;
		Color[] colors = SetColor(color, LineType.Discrete, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color[] colors, Material lineMaterial, float width) {
		points2 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Discrete, Joins.None, true, false);
	}

	public VectorLine (string lineName, Vector2[] linePoints, Material lineMaterial, float width, LineType lineType) {
		points2 = linePoints;
		Color[] colors = SetColor(Color.white, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color color, Material lineMaterial, float width, LineType lineType) {
		points2 = linePoints;
		Color[] colors = SetColor(color, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color[] colors, Material lineMaterial, float width, LineType lineType) {
		points2 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, Joins.None, true, false);
	}

	public VectorLine (string lineName, Vector2[] linePoints, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points2 = linePoints;
		Color[] colors = SetColor(Color.white, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color color, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points2 = linePoints;
		Color[] colors = SetColor(color, lineType, linePoints.Length, false);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, true, false);
	}
	public VectorLine (string lineName, Vector2[] linePoints, Color[] colors, Material lineMaterial, float width, LineType lineType, Joins joins) {
		points2 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, lineType, joins, true, false);
	}

	// Points constructors
	protected VectorLine (bool usePoints, string lineName, Vector2[] linePoints, Material lineMaterial, float width) {
		points2 = linePoints;
		Color[] colors = SetColor(Color.white, LineType.Continuous, linePoints.Length, true);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, true, true);
	}
	protected VectorLine (bool usePoints, string lineName, Vector2[] linePoints, Color color, Material lineMaterial, float width) {
		points2 = linePoints;
		Color[] colors = SetColor(color, LineType.Continuous, linePoints.Length, true);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, true, true);
	}
	protected VectorLine (bool usePoints, string lineName, Vector2[] linePoints, Color[] colors, Material lineMaterial, float width) {
		points2 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, true, true);
	}

	protected VectorLine (bool usePoints, string lineName, Vector3[] linePoints, Material lineMaterial, float width) {
		points3 = linePoints;
		Color[] colors = SetColor(Color.white, LineType.Continuous, linePoints.Length, true);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, false, true);
	}
	protected VectorLine (bool usePoints, string lineName, Vector3[] linePoints, Color[] colors, Material lineMaterial, float width) {
		points3 = linePoints;
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, false, true);
	}
	protected VectorLine (bool usePoints, string lineName, Vector3[] linePoints, Color color, Material lineMaterial, float width) {
		points3 = linePoints;
		Color[] colors = SetColor(color, LineType.Continuous, linePoints.Length, true);
		SetupMesh (ref lineName, lineMaterial, colors, ref width, LineType.Continuous, Joins.None, false, true);
	}
		
	Color[] SetColor (Color color, LineType lineType, int size, bool usePoints) {
		if (size == 0) {
			LogError ("VectorLine: Must use a points array with more than 0 entries");
			return null;
		}
		if (!usePoints) {
			size = lineType == LineType.Continuous? size-1 : size/2;
		}
		Color[] colors = new Color[size];
		for (int i = 0; i < size; i++) {
			colors[i] = color;
		}
		return colors;
	}

	protected void SetupMesh (ref string lineName, Material useMaterial, Color[] colors, ref float width, LineType lineType, Joins joins, bool use2Dlines, bool usePoints) {
		m_continuous = (lineType == LineType.Continuous);
		m_is2D = use2Dlines;
		if (joins == Joins.Fill && !m_continuous) {
			LogError ("VectorLine: Must use LineType.Continuous if using Joins.Fill for \"" + lineName + "\"");
			return;
		}
		if ( (m_is2D && points2 == null) || (!m_is2D && points3 == null) ) {
			LogError ("VectorLine: the points array is null for \"" + lineName + "\"");
			return;
		}
		if (colors == null) {
			LogError ("Vectorline: the colors array is null for \"" + lineName + "\"");
			return;
		}
		m_pointsLength = m_is2D? points2.Length : points3.Length;
		if (!usePoints && m_pointsLength < 2) {
			LogError ("The points array must contain at least two points");
			return;
		}
		if (!m_continuous && m_pointsLength%2 != 0) {
			LogError ("VectorLine: Must have an even points array length for \"" + lineName + "\" when using LineType.Discrete");
			return;
		}
		
		m_maxWeldDistance = (width*2) * (width*2);
		m_drawEnd = m_pointsLength;
		m_lineWidths = new float[1];
		m_lineWidths[0] = width * .5f;
		m_isPoints = usePoints;
		m_joins = joins;
		bool useSegmentColors = true;
		int colorsLength = 0;
#if !UNITY_3
		if (width == 1.0f && ( (m_isPoints && m_useMeshPoints) || (!m_isPoints && m_useMeshLines) ) ) {
			m_1pixelLine = true;
		}
#endif
		
		if (!usePoints) {
			if (m_continuous) {
				if (colors.Length != m_pointsLength-1) {
					Debug.LogWarning ("VectorLine: Length of color array for \"" + lineName + "\" must be length of points array minus one");
					useSegmentColors = false;
					colorsLength = m_pointsLength-1;
				}
			}
			else {
				if (colors.Length != m_pointsLength/2) {
					Debug.LogWarning ("VectorLine: Length of color array for \"" + lineName + "\" must be exactly half the length of points array");
					useSegmentColors = false;
					colorsLength = m_pointsLength/2;
				}
			}
		}
		else {
			if (colors.Length != m_pointsLength) {
				Debug.LogWarning ("VectorLine: Length of color array for \"" + lineName + "\" must be the same length as the points array");
				useSegmentColors = false;
				colorsLength = m_pointsLength;
			}
		}
		if (!useSegmentColors) {
			colors = new Color[colorsLength];
			for (int i = 0; i < colorsLength; i++) {
				colors[i] = Color.white;
			}
		}
		
		if (useMaterial == null) {
			if (defaultMaterial == null) {
				defaultMaterial = new Material("Shader \"Vertex Colors/Alpha\" {Category{Tags {\"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\"}SubShader {Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha Pass {BindChannels {Bind \"Color\", color Bind \"Vertex\", vertex}}}}}");
			}
			m_material = defaultMaterial;
		}
		else {
			m_material = useMaterial;
		}
	
		m_vectorObject = new GameObject("Vector "+lineName, typeof(MeshRenderer));
		m_vectorObject.layer = vectorLayer;
		m_vectorObject.renderer.material = m_material;
		m_mesh = new Mesh();
		m_mesh.name = lineName;
		m_meshFilter = (MeshFilter)m_vectorObject.AddComponent(typeof(MeshFilter));
		m_meshFilter.mesh = m_mesh;		
		name = lineName;
#if !UNITY_3
		m_meshRenderMethodSet = true;
#endif
		BuildMesh (colors);
	}
	
	public void Resize (Vector3[] linePoints) {
		if (m_is2D) {
			LogError ("Must supply a Vector2 array instead of a Vector3 array for \"" + name + "\"");
			return;
		}
		points3 = linePoints;
		m_pointsLength = linePoints.Length;
		RebuildMesh();
	}

	public void Resize (Vector2[] linePoints) {
		if (!m_is2D) {
			LogError ("Must supply a Vector3 array instead of a Vector2 array for \"" + name + "\"");
			return;
		}
		points2 = linePoints;
		m_pointsLength = linePoints.Length;
		RebuildMesh();
	}
	
	public void Resize (int newSize) {
		if (m_is2D) {
			points2 = new Vector2[newSize];
		}
		else {
			points3 = new Vector3[newSize];
		}
		m_pointsLength = newSize;
		RebuildMesh();
	}
	
	private void RebuildMesh () {
		if (!m_continuous && m_pointsLength%2 != 0) {
			LogError ("VectorLine.Resize: Must have an even points array length for \"" + name + "\" when using LineType.Discrete");
			return;
		}
		
		m_mesh.Clear();

		Color[] colors = SetColor (m_lineColors[0], m_continuous? LineType.Continuous : LineType.Discrete, m_pointsLength, m_isPoints);
		if (m_lineWidths.Length > 1) {
			float thisWidth = lineWidth;
			m_lineWidths = new float[m_pointsLength];
			lineWidth = thisWidth;
		}

		BuildMesh (colors);
		m_minDrawIndex = 0;
		m_maxDrawIndex = 0;
		m_drawStart = 0;
		m_drawEnd = m_pointsLength;
	}
	
	private void BuildMesh (Color[] colors) {
#if UNITY_3
		if (m_isPoints) {
			m_vertexCount = m_pointsLength*4;
		}
		else {
			m_vertexCount = m_continuous? (m_pointsLength-1)*4 : m_pointsLength*2;
		}
#else
		if (m_1pixelLine) {
			m_vertexCount = (!m_continuous || m_isPoints)? m_pointsLength : (m_pointsLength-1)*2;
		}
		else {
			if (m_isPoints) {
				m_vertexCount = m_pointsLength*4;
			}
			else {
				m_vertexCount = m_continuous? (m_pointsLength-1)*4 : m_pointsLength*2;
			}
		}
#endif
		if (m_vertexCount > 65534) {
			LogError ("VectorLine: exceeded maximum vertex count of 65534 for \"" + name + "\"...use fewer points (maximum is approximately 16000 points for continuous lines and points, and approximately 32000 points for discrete lines)");
			return;
		}
		
		m_lineVertices = new Vector3[m_vertexCount];
		m_lineUVs = new Vector2[m_vertexCount];
#if UNITY_3
		m_lineColors = new Color[m_vertexCount];
#else
		m_lineColors = new Color32[m_vertexCount];
#endif
		
		int idx = 0, end = 0;
#if !UNITY_3
		if (m_1pixelLine) {
			end = colors.Length;
			if (m_isPoints) {
				for (int i = 0; i < end; i++) {
					m_lineColors[i] = colors[i];
				}
			}
			else {
				for (int i = 0; i < end; i++) {
					m_lineColors[idx  ] = colors[i];
					m_lineColors[idx+1] = colors[i];
					idx += 2;
				}
			}
		}
		else {
#endif
			if (!m_isPoints) {
				end = m_continuous? m_pointsLength-1 : m_pointsLength/2;
			}
			else {
				end = m_pointsLength;
			}
			for (int i = 0; i < end; i++) {
				m_lineUVs[idx  ] = new Vector2(0.0f, 1.0f);
				m_lineUVs[idx+1] = new Vector2(0.0f, 0.0f);
				m_lineUVs[idx+2] = new Vector2(1.0f, 1.0f);
				m_lineUVs[idx+3] = new Vector2(1.0f, 0.0f);
				idx += 4;
			}
			
			idx = 0;
			for (int i = 0; i < end; i++) {
				m_lineColors[idx  ] = colors[i];
				m_lineColors[idx+1] = colors[i];
				m_lineColors[idx+2] = colors[i];
				m_lineColors[idx+3] = colors[i];
				idx += 4;
			}
#if !UNITY_3
		}
#endif
		
#if !UNITY_3
		m_mesh.MarkDynamic();
#endif
		m_mesh.vertices = m_lineVertices;
		m_mesh.uv = m_lineUVs;
#if UNITY_3
		m_mesh.colors = m_lineColors;
#else
		m_mesh.colors32 = m_lineColors;
#endif
		SetupTriangles();
						
		if (!m_is2D) {
			m_screenPoints = new Vector3[m_lineVertices.Length];
		}
		if (m_useNormals) {
			m_normalsCalculated = false;
		}
		if (m_useTangents) {
			m_tangentsCalculated = false;
		}
		
		if (m_capType != EndCap.None) {
			AddEndCap();
		}
	}

	private void SetupTriangles () {
		bool addPoint = false;
		
#if !UNITY_3	// Lines/points/quads
		if (m_1pixelLine) {
			if (m_continuous) {
				m_triangleCount = m_isPoints? m_pointsLength : (m_pointsLength-1)*2;
			}
			else {
				m_triangleCount = m_pointsLength;
			}
		}
		else {
			int vertNumber = m_useMeshQuads? 4 : 6;
#endif
#if UNITY_3
			int vertNumber = 6;
#endif
			if (m_continuous) {
				m_triangleCount = m_isPoints? m_triangleCount = (m_pointsLength)*vertNumber : m_triangleCount = (m_pointsLength-1)*vertNumber;
				if (m_joins == Joins.Fill) {
					m_triangleCount += (m_pointsLength-2)*vertNumber;
					// Add another join fill if the first point equals the last point (like with a square)
					if ( (m_is2D && points2[0] == points2[points2.Length-1]) || (!m_is2D && points3[0] == points3[points3.Length-1]) ) {
						m_triangleCount += vertNumber;
						addPoint = true;
					}
				}
			}
			else {
				m_triangleCount = m_pointsLength/2 * vertNumber;
			}
#if !UNITY_3
		}
#endif

		int[] newTriangles = new int[m_triangleCount];
		
		int end = 0, i = 0;
		if (!m_isPoints) {
			end = m_continuous? (m_pointsLength-1)*4 : m_pointsLength*2;
		}
		else {
			end = m_pointsLength*4;
		}
		
#if !UNITY_3
		if (m_1pixelLine) {
			if (!m_isPoints) {
				end = m_continuous? (m_pointsLength-1)*2 : m_pointsLength;
			}
			else {
				end = m_pointsLength;
			}
			if (m_continuous) {
				int idx = 0;
				if (!m_isPoints) {
					for (i = 0; i < end; i++) {
						newTriangles[idx] = i;
						newTriangles[idx++] = i;
					}
				}
				else {
					for (i = 0; i < end; i++) {
						newTriangles[i] = i;
					}
				}
			}
			else {
				for (i = 0; i < end; i++) {
					newTriangles[i] = i;
				}
			}
			m_mesh.SetIndices (newTriangles, m_isPoints? MeshTopology.Points : MeshTopology.Lines, 0);
		}
		else {
			if (m_useMeshQuads) {
				for (i = 0; i < end; i += 4) {
					newTriangles[i  ] = i+2;
					newTriangles[i+1] = i+3;
					newTriangles[i+2] = i+1;
					newTriangles[i+3] = i;
				}
				if (m_joins == Joins.Fill) {
					end -= 2;
					int idx = i;
					for (i = 2; i < end; i += 4) {
						newTriangles[idx  ] = i+2;
						newTriangles[idx+1] = i+3;
						newTriangles[idx+2] = i+1;
						newTriangles[idx+3] = i;
						idx += 4;
					}
					if (addPoint) {
						newTriangles[idx  ] = i;
						newTriangles[idx+1] = 0;
						newTriangles[idx+2] = 1;
						newTriangles[idx+3] = i+1;
					}
				}
				m_mesh.SetIndices (newTriangles, MeshTopology.Quads, 0);
			}
			else {
#endif
				int idx = 0;
				for (i = 0; i < end; i += 4) {
					newTriangles[idx  ] = i;
					newTriangles[idx+1] = i+2;
					newTriangles[idx+2] = i+1;
				
					newTriangles[idx+3] = i+2;
					newTriangles[idx+4] = i+3;
					newTriangles[idx+5] = i+1;
					idx += 6;
				}
				if (m_joins == Joins.Fill) {
					end -= 2;
					for (i = 2; i < end; i += 4) {
						newTriangles[idx  ] = i;
						newTriangles[idx+1] = i+2;
						newTriangles[idx+2] = i+1;
				
						newTriangles[idx+3] = i+2;
						newTriangles[idx+4] = i+3;
						newTriangles[idx+5] = i+1;
						idx += 6;
					}
					if (addPoint) {
						newTriangles[idx  ] = i;
						newTriangles[idx+1] = 0;
						newTriangles[idx+2] = i+1;
				
						newTriangles[idx+3] = 0;
						newTriangles[idx+4] = 1;
						newTriangles[idx+5] = i+1;
					}
				}
				m_mesh.triangles = newTriangles;
#if !UNITY_3
			}
		}
#endif
	}
	
	public void AddNormals () {
		m_useNormals = true;
		m_normalsCalculated = false;
	}
	
	public void AddTangents () {
		m_useTangents = true;
		m_tangentsCalculated = false;
	}
	
	private void CalculateTangents () {
		if (!m_useNormals) {
			m_useNormals = true;
			m_mesh.RecalculateNormals();
		}
		Vector3[] tan1 = new Vector3[m_lineVertices.Length];
		Vector3[] tan2 = new Vector3[m_lineVertices.Length];
		Vector4[] tangents = new Vector4[m_lineVertices.Length];
		var triangles = m_mesh.triangles;
		var uv = m_mesh.uv;
		var normals = m_mesh.normals;
		int triCount = triangles.Length;
		int tangentCount = m_lineVertices.Length;
		
		for (int i = 0; i < triCount; i += 3) {
			int i1 = triangles[i];
			int i2 = triangles[i+1];
			int i3 = triangles[i+2];
			
			Vector3 v1 = m_lineVertices[i1];
			Vector3 v2 = m_lineVertices[i2];
			Vector3 v3 = m_lineVertices[i3];
			
			Vector2 w1 = uv[i1];
			Vector2 w2 = uv[i2];
			Vector2 w3 = uv[i3];
			
			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;
			
			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;
	
			float r = 1.0f / (s1 * t2 - s2 * t1);
			Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
			Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
			
			tan1[i1] += sdir;
			tan1[i2] += sdir;
			tan1[i3] += sdir;
			
			tan2[i1] += tdir;
			tan2[i2] += tdir;
			tan2[i3] += tdir;
		}
	
		for (int i = 0; i < tangentCount; i++) {
			Vector3 n = normals[i];
			Vector3 t = tan1[i];
			tangents[i] = (t - n * Vector3.Dot(n, t)).normalized;
			tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
		}
		
		m_mesh.tangents = tangents;
	}
	
	private void AddEndCap () {
#if !UNITY_3
		if (m_1pixelLine) return;
#endif
		int newVertexCount = m_vertexCount + 8;
		if (newVertexCount > 65534) {
			LogError ("VectorLine: exceeded maximum vertex count of 65534 for \"" + m_name + "\"...use fewer points");
			return;
		}
		
		System.Array.Resize (ref m_lineVertices, newVertexCount);
		System.Array.Resize (ref m_lineUVs, newVertexCount);
		System.Array.Resize (ref m_lineColors, newVertexCount);
		var capType = capDictionary[m_endCap].capType;
		
		int[] triangles;
#if !UNITY_3
		if (m_useMeshQuads) {
			triangles = new int[8];
			int idx = 0;
			for (int i = newVertexCount-8; i < newVertexCount; i += 4) {
				triangles[idx] = i+2; triangles[idx+1] = i; triangles[idx+2] = i+1; triangles[idx+3] = i+3;
				idx += 4;
			}
		}
		else {
#endif
			triangles = new int[12];
			int idx = 0;
			for (int i = newVertexCount-8; i < newVertexCount; i += 4) {
				triangles[idx  ] = i+2; triangles[idx+1] = i+1; triangles[idx+2] = i;
				triangles[idx+3] = i+2; triangles[idx+4] = i+3; triangles[idx+5] = i+1;
				idx += 6;
			}
#if !UNITY_3
		}
#endif

		for (int i = newVertexCount-8; i < newVertexCount-4; i++) {
			m_lineColors[i] = m_lineColors[0];
			m_lineColors[i+4] = m_lineColors[newVertexCount-12];
		}
		
		m_lineUVs[newVertexCount - 8] = new Vector2 (0.0f, .25f);
		m_lineUVs[newVertexCount - 7] = new Vector2 (0.0f, 0.0f);
		m_lineUVs[newVertexCount - 6] = new Vector2 (1.0f, .25f);
		m_lineUVs[newVertexCount - 5] = new Vector2 (1.0f, 0.0f);
		if (capType == EndCap.Mirror) {
			m_lineUVs[newVertexCount - 4] = new Vector2 (1.0f, .25f);
			m_lineUVs[newVertexCount - 3] = new Vector2 (1.0f, 0.0f);
			m_lineUVs[newVertexCount - 2] = new Vector2 (0.0f, .25f);
			m_lineUVs[newVertexCount - 1] = new Vector2 (0.0f, 0.0f);
		}
		else {
			m_lineUVs[newVertexCount - 4] = new Vector2 (0.0f, 1.0f);
			m_lineUVs[newVertexCount - 3] = new Vector2 (0.0f, .75f);
			m_lineUVs[newVertexCount - 2] = new Vector2 (1.0f, 1.0f);
			m_lineUVs[newVertexCount - 1] = new Vector2 (1.0f, .75f);
		}
		
		m_mesh.vertices = m_lineVertices;
		m_mesh.uv = m_lineUVs;
#if UNITY_3
		m_mesh.colors = m_lineColors;
#else
		m_mesh.colors32 = m_lineColors;
#endif
		
		m_mesh.subMeshCount = 2;
#if UNITY_3
		m_mesh.SetTriangles (triangles, 1);
#else
		if (m_useMeshQuads) {
			m_mesh.SetIndices (triangles, MeshTopology.Quads, 1);
		}
		else {
			m_mesh.SetTriangles (triangles, 1);
		}
#endif
		var materials = new Material[2];
		materials[0] = m_material;
		materials[1] = capDictionary[m_endCap].material;
		m_vectorObject.renderer.sharedMaterials = materials;
	}
	
	private void RemoveEndCapVertices () {
		System.Array.Resize (ref m_lineVertices, m_vertexCount);
		System.Array.Resize (ref m_lineUVs, m_vertexCount);
		System.Array.Resize (ref m_lineColors, m_vertexCount);
		m_mesh.subMeshCount = 1;
		var materials = new Material[1];
		materials[0] = m_vectorObject.renderer.materials[0];
		m_vectorObject.renderer.materials = materials;
	}

	static Material defaultMaterial;
	static Camera cam;
	static Transform camTransform;
	static Camera cam3D;
	static Vector3 oldPosition;
	static Vector3 oldRotation;
	public static Vector3 camTransformPosition {
		get {return camTransform.position;}
	}
	public static bool camTransformExists {
		get {return camTransform != null;}
	}
	static int _vectorLayer = 31;
	public static int vectorLayer {
		get {
			return _vectorLayer;
		}
		set {
			_vectorLayer = value;
			if (_vectorLayer > 31) _vectorLayer = 31;
			else if (_vectorLayer < 0) _vectorLayer = 0;
		}
	}
	static int _vectorLayer3D = 0;
	public static int vectorLayer3D {
		get {
			return _vectorLayer3D;
		}
		set {
			_vectorLayer3D = value;
			if (_vectorLayer > 31) _vectorLayer3D = 31;
			else if (_vectorLayer < 0) _vectorLayer3D = 0;
		}
	}
	static float zDist;
	static bool useOrthoCam;
	const float cutoff = .15f;
	static bool error = false;
	static bool lineManagerCreated = false; 
	static LineManager _lineManager;
	public static LineManager lineManager {
		get {
			// This prevents OnDestroy functions that reference VectorManager from creating LineManager again when editor play mode is stopped
			// Checking _lineManager == null can randomly fail, since the order of objects being Destroyed is undefined
			if (!lineManagerCreated) {
				lineManagerCreated = true;
				var lineManagerGO = new GameObject("LineManager");
				_lineManager = lineManagerGO.AddComponent(typeof(LineManager)) as LineManager;
				_lineManager.enabled = false;
				MonoBehaviour.DontDestroyOnLoad(_lineManager);
			}
			return _lineManager;
		}
	}
	static int widthIdxAdd;
	static int m_screenWidth = 0;
	static int m_screenHeight = 0;
	static int screenWidth {
		get {
			if (m_screenWidth == 0) {
				return Screen.width;
			}
			return m_screenWidth;
		}
	}
	static int screenHeight {
		get {
			if (m_screenHeight == 0) {
				return Screen.height;
			}
			return m_screenHeight;
		}
	}
	static Dictionary<string, CapInfo> capDictionary;
	
	private static void LogError (string errorString) {
		Debug.LogError (errorString);
		error = true;
	}
	
	public static Camera SetCameraRenderTexture (RenderTexture renderTexture) {
		return SetCameraRenderTexture (renderTexture, Color.black, false);
	}
	
	public static Camera SetCameraRenderTexture (RenderTexture renderTexture, bool useOrtho) {
		return SetCameraRenderTexture (renderTexture, Color.black, useOrtho);
	}
	
	public static Camera SetCameraRenderTexture (RenderTexture renderTexture, Color color, bool useOrtho) {
		Camera vCam;
		if (renderTexture == null) {
			m_screenWidth = 0;
			m_screenHeight = 0;
			vCam = SetCamera (useOrtho);
			vCam.aspect = screenWidth/(float)screenHeight;
			vCam.targetTexture = null;
			return vCam;
		}
		
		int width = renderTexture.width;
		int height = renderTexture.height;
		m_screenWidth = width;
		m_screenHeight = height;
		vCam = SetCamera (CameraClearFlags.SolidColor, useOrtho);
		vCam.aspect = width/(float)height;
		vCam.backgroundColor = color;
		vCam.targetTexture = renderTexture;
		return vCam;
	}

	public static Camera SetCamera () {
		return SetCamera (CameraClearFlags.Depth, false);
	}
	
	public static Camera SetCamera (bool useOrtho) {
		return SetCamera (CameraClearFlags.Depth, useOrtho);
	}
	
	public static Camera SetCamera (CameraClearFlags clearFlags) {
		return SetCamera (clearFlags, false);
	}
	
	public static Camera SetCamera (CameraClearFlags clearFlags, bool useOrtho) {
		if (Camera.main == null) {
			LogError ("VectorLine.SetCamera: no camera tagged \"Main Camera\" found");
			return null;
		}
		return SetCamera (Camera.main, clearFlags, useOrtho);
	}
	
	public static Camera SetCamera (Camera thisCamera) {
		return SetCamera (thisCamera, CameraClearFlags.Depth, false);
	}
	
	public static Camera SetCamera (Camera thisCamera, bool useOrtho) {
		return SetCamera (thisCamera, CameraClearFlags.Depth, useOrtho);
	}
	
	public static Camera SetCamera (Camera thisCamera, CameraClearFlags clearFlags) {
		return SetCamera (thisCamera, clearFlags, false);
	}
	
	public static Camera SetCamera (Camera thisCamera, CameraClearFlags clearFlags, bool useOrtho) {
		if (!cam) {
			cam = new GameObject("VectorCam", typeof(Camera)).camera;
			MonoBehaviour.DontDestroyOnLoad(cam);
		}
		cam.depth = thisCamera.depth+1;
		cam.clearFlags = clearFlags;
		cam.orthographic = useOrtho;
		useOrthoCam = useOrtho;
		if (useOrtho) {
			cam.orthographicSize = screenHeight/2;
			cam.farClipPlane = 101.1f;
			cam.nearClipPlane = .9f;
		}
		else {
			cam.fieldOfView = 90.0f;
			cam.farClipPlane = screenHeight/2 + .0101f;
			cam.nearClipPlane = screenHeight/2 - .0001f;
		}
		cam.transform.position = new Vector3(screenWidth/2 - .5f, screenHeight/2 - .5f, 0.0f);
		cam.transform.eulerAngles = Vector3.zero;
		cam.cullingMask = 1 << _vectorLayer;	// Turn on only the vector layer on the Vectrosity camera
		cam.backgroundColor = thisCamera.backgroundColor;
#if !UNITY_3
		cam.hdr = thisCamera.hdr;
#endif
		
		thisCamera.cullingMask &= ~(1 << _vectorLayer);	// Turn off the vector layer on the non-Vectrosity camera
		camTransform = thisCamera.transform;
		cam3D = thisCamera;
		oldPosition = camTransform.position + Vector3.one;
		oldRotation = camTransform.eulerAngles + Vector3.one;
		return cam;
	}
	
	public static void SetCamera3D () {
		if (Camera.main == null) {
			LogError ("VectorLine.SetCamera3D: no camera tagged \"Main Camera\" found. Please call SetCamera3D with a specific camera instead.");
			return;
		}
		SetCamera3D (Camera.main);
	}
	
	public static void SetCamera3D (Camera thisCamera) {
		camTransform = thisCamera.transform;
		cam3D = thisCamera;
		oldPosition = camTransform.position + Vector3.one;
		oldRotation = camTransform.eulerAngles + Vector3.one;
	}
	
	public static bool CameraHasMoved () {
		return oldPosition != camTransform.position || oldRotation != camTransform.eulerAngles;
	}
	
	public static void UpdateCameraInfo () {
		oldPosition = camTransform.position;
		oldRotation = camTransform.eulerAngles;	
	}
	
	public static Camera GetCamera () {
		if (!cam) {
			LogError ("The vector cam has not been set up");
			return null;
		}
		return cam;
	}
	
	public static void SetVectorCamDepth (int depth) {
		if (!cam) {
			LogError ("The vector cam has not been set up");
			return;
		}
		cam.depth = depth;
	}
	
	public int GetSegmentNumber () {
		if (m_continuous) {
			return pointsLength-1;
		}
		else {
			return pointsLength/2;
		}
	}

	static string[] functionNames = {"VectorLine.SetColors: Length of color", "VectorLine.SetColorsSmooth: Length of color", "VectorLine.SetWidths: Length of line widths", "MakeCurve", "MakeSpline", "MakeEllipse"};
	enum FunctionName {SetColors, SetColorsSmooth, SetWidths, MakeCurve, MakeSpline, MakeEllipse}
	
	bool WrongArrayLength (int arrayLength, FunctionName functionName) {
		if (m_continuous) {
			if (arrayLength != m_pointsLength-1) {
				LogError (functionNames[(int)functionName] + " array for \"" + name + "\" must be length of points array minus one for a continuous line (one entry per line segment)");
				return true;
			}
		}
		else if (arrayLength != m_pointsLength/2) {
			LogError (functionNames[(int)functionName] + " array in \"" + name + "\" must be exactly half the length of points array for a discrete line (one entry per line segment)");
			return true;
		}
		return false;
	}
	
	bool CheckArrayLength (FunctionName functionName, int segments, int index) {
		if (segments < 1) {
			LogError ("VectorLine." + functionNames[(int)functionName] + " needs at least 1 segment");
			return false;
		}

		if (m_isPoints) {
			if (index + segments > m_pointsLength) {
				if (index == 0) {
					LogError ("VectorLine." + functionNames[(int)functionName] + ": The number of segments cannot exceed the number of points in the array for \"" + name + "\"");
					return false;
				}
				LogError ("VectorLine: Calling " + functionNames[(int)functionName] + " with an index of " + index + " would exceed the length of the Vector array for \"" + name + "\"");
				return false;				
			}
			return true;
		}

		if (m_continuous) {
			if (index + (segments+1) > m_pointsLength) {
				if (index == 0) {
					LogError ("VectorLine." + functionNames[(int)functionName] + ": The length of the array for continuous lines needs to be at least the number of segments plus one for \"" + name + "\"");
					return false;
				}
				LogError ("VectorLine: Calling " + functionNames[(int)functionName] + " with an index of " + index + " would exceed the length of the Vector array for \"" + name + "\"");
				return false;
			}
		}
		else {
			if (index + segments*2 > m_pointsLength) {
				if (index == 0) {
					LogError ("VectorLine." + functionNames[(int)functionName] + ": The length of the array for discrete lines needs to be at least twice the number of segments for \"" + name + "\"");
					return false;
				}
				LogError ("VectorLine: Calling " + functionNames[(int)functionName] + " with an index of " + index + " would exceed the length of the Vector array for \"" + name + "\"");
				return false;
			}	
		}
		return true;
	}
	
	private void SetEndCapColors () {
#if !UNITY_3
		if (m_1pixelLine) return;
#endif
		
		if (m_capType <= EndCap.Mirror) {
			int vIndex = m_continuous? m_drawStart * 4 : m_drawStart * 2;
			for (int i = 0; i < 4; i++) {
				m_lineColors[i + m_vertexCount] = m_lineColors[i + vIndex];
			}
		}
		if (m_capType >= EndCap.Both) {
			int end = m_drawEnd;
			if (m_continuous) {
				if (m_drawEnd == pointsLength) end--;
			}
			else {
				if (end < pointsLength) end++;
			}
			int vIndex = end * (m_continuous? 4 : 2) - 8;
			if (vIndex < -4) {
				vIndex = -4;
			}
			for (int i = 4; i < 8; i++) {
				m_lineColors[i + m_vertexCount] = m_lineColors[i + vIndex];
			}
		}
	}
	
	public void SetColor (Color color) {
		SetColor (color, 0, m_pointsLength);
	}
	
	public void SetColor (Color color, int index) {
		SetColor (color, index, index);
	}
	
	public void SetColor (Color color, int startIndex, int endIndex) {
		int max;
		if (m_isPoints) {
			max = pointsLength;
		}
		else {
			max = m_continuous? pointsLength-1 : pointsLength/2;
		}
		int linetypeMultiplier;
#if !UNITY_3
		if (m_1pixelLine) {
			linetypeMultiplier = m_isPoints? 1 : 2;
		}
		else {
#endif
			linetypeMultiplier = 4;
#if !UNITY_3
		}
#endif
		startIndex = Mathf.Clamp (startIndex, 0, max) * linetypeMultiplier;
		endIndex = Mathf.Clamp (endIndex + 1, 1, max) * linetypeMultiplier;
		for (int i = startIndex; i < endIndex; i++) {
			m_lineColors[i] = color;
		}
#if UNITY_3
		m_mesh.colors = m_lineColors;
#else
		m_mesh.colors32 = m_lineColors;
#endif
	}

	public void SetColors (Color[] lineColors) {
		if (lineColors == null) {
			LogError ("VectorLine.SetColors: line colors array must not be null");
			return;
		}
		if (!m_isPoints) {
			if (WrongArrayLength (lineColors.Length, FunctionName.SetColors)) {
				return;
			}
		}
		else if (lineColors.Length != pointsLength) {
			LogError ("VectorLine.SetColors: Length of lineColors array in \"" + name + "\" must be same length as points array");
			return;
		}
		
		int start = 0;
		int end = lineColors.Length;
		SetStartAndEnd (ref start, ref end);
		int idx = start*4;
		
#if !UNITY_3
		if (m_1pixelLine) {
			if (m_isPoints) {
				for (int i = start; i < end; i++) {
					m_lineColors[i] = lineColors[i];
				}
			}
			else {
				idx = start*2;
				for (int i = start; i < end; i++) {
					m_lineColors[idx  ] = lineColors[i];
					m_lineColors[idx+1] = lineColors[i];
					idx += 2;
				}
			}
		}
		else {
#endif
			for (int i = start; i < end; i++) {
				m_lineColors[idx  ] = lineColors[i];
				m_lineColors[idx+1] = lineColors[i];
				m_lineColors[idx+2] = lineColors[i];
				m_lineColors[idx+3] = lineColors[i];
				idx += 4;
			}
#if !UNITY_3
		}
#endif
		
		if (m_capType != EndCap.None) {
			SetEndCapColors();
		}
#if UNITY_3
		m_mesh.colors = m_lineColors;
#else
		m_mesh.colors32 = m_lineColors;
#endif
	}
	
	public void SetColorsSmooth (Color[] lineColors) {
		if (lineColors == null) {
			LogError ("VectorLine.SetColors: line colors array must not be null");
			return;
		}
		if (m_isPoints) {
			LogError ("VectorLine.SetColorsSmooth must be used with a line rather than points");
			return;
		}
		if (WrongArrayLength (lineColors.Length, FunctionName.SetColorsSmooth)) {
			return;
		}
		
		int start = 0;
		int end = lineColors.Length;
		SetStartAndEnd (ref start, ref end);
		int idx = start*4;
		
#if !UNITY_3
		if (m_1pixelLine) {
			idx = start*2;
			m_lineColors[idx  ] = lineColors[start];
			m_lineColors[idx+1] = lineColors[start];
			idx += 2;
			for (int i = start+1; i < end; i++) {
				m_lineColors[idx  ] = lineColors[i-1];
				m_lineColors[idx+1] = lineColors[i];
				idx += 2;
			}
		}
		else {
#endif
			m_lineColors[idx  ] = lineColors[start];
			m_lineColors[idx+1] = lineColors[start];
			m_lineColors[idx+2] = lineColors[start];
			m_lineColors[idx+3] = lineColors[start];
			idx += 4;
			for (int i = start+1; i < end; i++) {
				m_lineColors[idx  ] = lineColors[i-1];
				m_lineColors[idx+1] = lineColors[i-1];
				m_lineColors[idx+2] = lineColors[i];
				m_lineColors[idx+3] = lineColors[i];
				idx += 4;
			}
#if !UNITY_3
		}
		m_mesh.colors32 = m_lineColors;
#else
		m_mesh.colors = m_lineColors;		
#endif
	}

	private void SetStartAndEnd (ref int start, ref int end) {
		start = (m_minDrawIndex == 0)? 0 : (m_continuous)? m_minDrawIndex : m_minDrawIndex/2;
		if (m_maxDrawIndex > 0) {
			if (m_continuous) {
				end = m_maxDrawIndex;
			}
			else {
				end = m_maxDrawIndex/2;
				if (m_maxDrawIndex%2 != 0) {
					end++;
				}
			}
		}
	}

	public void SetWidths (float[] lineWidths) {
		SetWidths (lineWidths, null, lineWidths.Length, true);
	}
	
	public void SetWidths (int[] lineWidths) {
		SetWidths (null, lineWidths, lineWidths.Length, false);
	}
	
	private void SetWidths (float[] lineWidthsFloat, int[] lineWidthsInt, int arrayLength, bool doFloat) {
		if ((doFloat && lineWidthsFloat == null) || (!doFloat && lineWidthsInt == null)) {
			LogError ("VectorLine.SetWidths: line widths array must not be null");
			return;
		}
		if (m_isPoints) {
			if (arrayLength != pointsLength) {
				LogError ("VectorLine.SetWidths: line widths array must be the same length as the points array for \"" + name + "\"");
				return;
			}
		}
		else if (WrongArrayLength (arrayLength, FunctionName.SetWidths)) {
			return;
		}
#if !UNITY_3
		if (m_1pixelLine) {
			RedoLine (false);
		}
#endif
		
		m_lineWidths = new float[arrayLength];
		if (doFloat) {
			for (int i = 0; i < arrayLength; i++) {
				m_lineWidths[i] = lineWidthsFloat[i] * .5f;
			}
		}
		else {
			for (int i = 0; i < arrayLength; i++) {
				m_lineWidths[i] = (float)lineWidthsInt[i] * .5f;
			}
		}
	}

#if !UNITY_3	
	private void RedoLine (bool use1Pixel) {
		m_1pixelLine = use1Pixel;
		int start, add, arraySize;
		
		if (m_isPoints) {
			start = 0;
			add = 1;
			arraySize = m_vertexCount;
		}
		else {
			if (use1Pixel) {
				start = 2;
				add = 4;
				arraySize = m_vertexCount/4;
			}
			else {
				start = 1;
				add = 2;
				arraySize = m_vertexCount/2;
			}
		}
		
		var cols = new Color[arraySize];
		int end = m_vertexCount;
		int idx = 0;
		for (int i = start; i < end; i += add) {
			cols[idx++] = m_lineColors[i];
		}
		m_mesh.Clear();
		BuildMesh (cols);
	}
#endif
	
	static Material defaultLineMaterial;
	static float defaultLineWidth;
	static int defaultLineDepth;
	static float defaultCapLength;
	static Color defaultLineColor;
	static LineType defaultLineType;
	static Joins defaultJoins;
	static bool defaultsSet = false;
	static Vector3 v1;
	static Vector3 v2;
	static Vector3 v3;
	
	public static void SetLineParameters (Color color, Material material, float width, float capLength, int depth, LineType lineType, Joins joins) {
		defaultLineColor = color;
		defaultLineMaterial = material;
		defaultLineWidth = width;
		defaultLineDepth = depth;
		defaultCapLength = capLength;
		defaultLineType = lineType;
		defaultJoins = joins;
		defaultsSet = true;
	}
	
	private static void PrintMakeLineError () {
		LogError ("VectorLine.MakeLine: Must call SetLineParameters before using MakeLine with these parameters");
	}
	
	public static VectorLine MakeLine (string name, Vector3[] points, Color[] colors) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, colors, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine MakeLine (string name, Vector2[] points, Color[] colors) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, colors, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine MakeLine (string name, Vector3[] points, Color color) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, color, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine MakeLine (string name, Vector2[] points, Color color) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, color, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine MakeLine (string name, Vector3[] points) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, defaultLineColor, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine MakeLine (string name, Vector2[] points) {
		if (!defaultsSet) {
			PrintMakeLineError();
			return null;
		}
		var line = new VectorLine(name, points, defaultLineColor, defaultLineMaterial, defaultLineWidth, defaultLineType, defaultJoins);
		line.capLength = defaultCapLength;
		line.depth = defaultLineDepth;
		return line;
	}

	public static VectorLine SetLine (Color color, params Vector2[] points) {
		return SetLine (color, 0.0f, points);
	}

	public static VectorLine SetLine (Color color, float time, params Vector2[] points) {
		if (points.Length < 2) {
			LogError ("VectorLine.SetLine needs at least two points");
			return null;
		}
		var line = new VectorLine("Line", points, color, null, 1.0f, LineType.Continuous, Joins.None);
		if (time > 0.0f) {
			lineManager.DisableLine(line, time);
		}
		line.Draw();
		return line;
	}

	public static VectorLine SetLine (Color color, params Vector3[] points) {
		return SetLine (color, 0.0f, points);
	}
	
	public static VectorLine SetLine (Color color, float time, params Vector3[] points) {
		if (points.Length < 2) {
			LogError ("VectorLine.SetLine needs at least two points");
			return null;
		}
		var line = new VectorLine("SetLine", points, color, null, 1.0f, LineType.Continuous, Joins.None);
		if (time > 0.0f) {
			lineManager.DisableLine(line, time);
		}
		line.Draw();
		return line;
	}

	public static VectorLine SetLine3D (Color color, params Vector3[] points) {
		return SetLine3D (color, 0.0f, points);
	}
		
	public static VectorLine SetLine3D (Color color, float time, params Vector3[] points) {
		if (points.Length < 2) {
			LogError ("VectorLine.SetLine3D needs at least two points");
			return null;
		}
		var line = new VectorLine("SetLine3D", points, color, null, 1.0f, LineType.Continuous, Joins.None);
		line.Draw3DAuto (time);
		return line;
	}

	public static VectorLine SetRay (Color color, Vector3 origin, Vector3 direction) {
		return SetRay (color, 0.0f, origin, direction);
	}

	public static VectorLine SetRay (Color color, float time, Vector3 origin, Vector3 direction) {
		var line = new VectorLine("SetRay", new Vector3[] {origin, new Ray(origin, direction).GetPoint(direction.magnitude)}, color, null, 1.0f, LineType.Continuous, Joins.None);
		if (time > 0.0f) {
			lineManager.DisableLine(line, time);
		}
		line.Draw();
		return line;
	}

	public static VectorLine SetRay3D (Color color, Vector3 origin, Vector3 direction) {
		return SetRay3D (color, 0.0f, origin, direction);
	}

	public static VectorLine SetRay3D (Color color, float time, Vector3 origin, Vector3 direction) {
		var line = new VectorLine("SetRay3D", new Vector3[] {origin, new Ray(origin, direction).GetPoint (direction.magnitude)}, color, null, 1.0f, LineType.Continuous, Joins.None);
		line.Draw3DAuto (time);
		return line;
	}
	
	private bool CheckLine () {
		if (m_mesh == null) {
			LogError ("VectorLine \"" + m_name + "\" seems to have been destroyed. If you have used ObjectSetup, the way to remove the VectorLine is to destroy the GameObject passed into ObjectSetup.");
			return false;
		}
		
#if !UNITY_3
		if (m_1pixelLine) return true;
		
		if (m_useMeshQuads) {
			if (m_joins != Joins.Fill) {
				if (m_triangleCount != m_vertexCount) {
					SetupTriangles();
				}
			}
			else {
				if (m_is2D) {
					if ( (points2[0] != points2[m_pointsLength-1] && m_triangleCount != m_vertexCount*2 - 4) ||
						 (points2[0] == points2[m_pointsLength-1] && m_triangleCount != m_vertexCount*2) ) {
						SetupTriangles();
					}
				}
				else {
					if ( (points3[0] != points3[m_pointsLength-1] && m_triangleCount != m_vertexCount*2 - 4) ||
						 (points3[0] == points3[m_pointsLength-1] && m_triangleCount != m_vertexCount*2) ) {
						SetupTriangles();
					}
				}
				
				// Prevent extraneous quad with Joins.Fill
				if (m_drawStart > 0) {
					m_lineVertices[m_drawStart*4 - 1] = m_lineVertices[m_drawStart*4];
					m_lineVertices[m_drawStart*4 - 2] = m_lineVertices[m_drawStart*4];
				}
				if (m_drawEnd > 0 && m_drawEnd < m_pointsLength - 1) {
					m_lineVertices[m_drawEnd*4    ] = m_lineVertices[m_drawEnd*4 - 1];
					m_lineVertices[m_drawEnd*4 + 1] = m_lineVertices[m_drawEnd*4 - 1];
				}
				
				if (m_minDrawIndex > 0) {
					m_lineVertices[m_minDrawIndex*4 - 1] = m_lineVertices[m_minDrawIndex*4];
					m_lineVertices[m_minDrawIndex*4 - 2] = m_lineVertices[m_minDrawIndex*4];
				}
				if (m_maxDrawIndex > 0 && m_maxDrawIndex < m_pointsLength - 1) {
					m_lineVertices[m_maxDrawIndex*4    ] = m_lineVertices[m_maxDrawIndex*4 - 1];
					m_lineVertices[m_maxDrawIndex*4 + 1] = m_lineVertices[m_maxDrawIndex*4 - 1];
				}
			}
		}
		else {
#endif
			if (m_joins != Joins.Fill) {
				if (m_triangleCount != m_vertexCount + m_vertexCount/2) {
					SetupTriangles();
				}
			}
			else {
				if (m_is2D) {
					if ( (points2[0] != points2[m_pointsLength-1] && m_triangleCount != m_vertexCount*3 - 6) ||
						 (points2[0] == points2[m_pointsLength-1] && m_triangleCount != m_vertexCount*3) ) {
						SetupTriangles();
					}
				}
				else {
					if ( (points3[0] != points3[m_pointsLength-1] && m_triangleCount != m_vertexCount*3 - 6) ||
						 (points3[0] == points3[m_pointsLength-1] && m_triangleCount != m_vertexCount*3) ) {
						SetupTriangles();
					}
				}
				
				// Prevent extraneous triangle with Joins.Fill
				if (m_drawStart > 0) {
					m_lineVertices[m_drawStart*4 - 1] = m_lineVertices[m_drawStart*4];
				}
				if (m_drawEnd > 0 && m_drawEnd < m_pointsLength - 1) {
					m_lineVertices[m_drawEnd*4] = m_lineVertices[m_drawEnd*4 - 1];
				}
				
				if (m_minDrawIndex > 0) {
					m_lineVertices[m_minDrawIndex*4 - 1] = m_lineVertices[m_minDrawIndex*4];
				}
				if (m_maxDrawIndex > 0 && m_maxDrawIndex < m_pointsLength - 1) {
					m_lineVertices[m_maxDrawIndex*4] = m_lineVertices[m_maxDrawIndex*4 - 1];
				}
			}
#if !UNITY_3
		}
#endif
		if (m_capType != EndCap.None) {
			if (m_capType <= EndCap.Mirror) {
				int vIndex = m_drawStart * 4;
				int widthIndex = (m_lineWidths.Length > 1)? m_drawStart : 0;
				if (!m_continuous) {
					widthIndex /= 2;
					vIndex /= 2;
				}
				if (m_is2D) {
					var d = (m_lineVertices[vIndex] - m_lineVertices[vIndex+2]).normalized *
							m_lineWidths[widthIndex] * 2.0f * capDictionary[m_endCap].ratio1;
					m_lineVertices[m_vertexCount  ] = m_lineVertices[vIndex] + d;
					m_lineVertices[m_vertexCount+1] = m_lineVertices[vIndex+1] + d;
				}
				else {
					var v1 = cam3D.WorldToScreenPoint(m_lineVertices[vIndex]);
					var d = (v1 - cam3D.WorldToScreenPoint(m_lineVertices[vIndex+2])).normalized *
							m_lineWidths[widthIndex] * 2.0f * capDictionary[m_endCap].ratio1;
					m_lineVertices[m_vertexCount  ] = cam3D.ScreenToWorldPoint (v1 + d);
					m_lineVertices[m_vertexCount+1] = cam3D.ScreenToWorldPoint (cam3D.WorldToScreenPoint (m_lineVertices[vIndex+1]) + d);
				}
				m_lineVertices[m_vertexCount+2] = m_lineVertices[vIndex];
				m_lineVertices[m_vertexCount+3] = m_lineVertices[vIndex+1];
			}
			if (m_capType >= EndCap.Both) {
				int end = m_drawEnd;
				if (m_continuous) {
					if (m_drawEnd == m_pointsLength) end--;
				}
				else {
					if (end < m_pointsLength) end++;
				}
				int vIndex = end * 4;
				int widthIndex = (m_lineWidths.Length > 1)? end-1 : 0;
				if (widthIndex < 0) {
					widthIndex = 0;
				}
				if (!m_continuous) {
					widthIndex /= 2;
					vIndex /= 2;
				}
				if (vIndex < 4) {
					vIndex = 4;
				}
				m_lineVertices[m_vertexCount+4] = m_lineVertices[vIndex-2];
				m_lineVertices[m_vertexCount+5] = m_lineVertices[vIndex-1];
				if (m_is2D) {
					var d = (m_lineVertices[vIndex-1] - m_lineVertices[vIndex-3]).normalized *
							m_lineWidths[widthIndex] * 2.0f * capDictionary[m_endCap].ratio2;
					m_lineVertices[m_vertexCount+6] = m_lineVertices[vIndex-2] + d;
					m_lineVertices[m_vertexCount+7] = m_lineVertices[vIndex-1] + d;
				}
				else {
					var v1 = cam3D.WorldToScreenPoint(m_lineVertices[vIndex-1]);
					var d = (v1 - cam3D.WorldToScreenPoint(m_lineVertices[vIndex-3])).normalized *
							m_lineWidths[widthIndex] * 2.0f * capDictionary[m_endCap].ratio2;
					m_lineVertices[m_vertexCount+6] = cam3D.ScreenToWorldPoint (cam3D.WorldToScreenPoint (m_lineVertices[vIndex-2]) + d);
					m_lineVertices[m_vertexCount+7] = cam3D.ScreenToWorldPoint (v1 + d);
				}
			}
			
			if (m_drawStart > 0 || m_drawEnd < m_pointsLength) {
				SetEndCapColors();
#if UNITY_3
				m_mesh.colors = m_lineColors;
#else
				m_mesh.colors32 = m_lineColors;
#endif
			}
		}

		if (m_continuousTexture) {
			int idx = 0;
			float offset = 0.0f;
			SetDistances();
			int end = m_distances.Length-1;
			float totalDistance = m_distances[end];
			
			for (int i = 0; i < end; i++) {
				m_lineUVs[idx  ].x = offset;
				m_lineUVs[idx+1].x = offset;
				offset = 1.0f / (totalDistance / m_distances[i+1]);
				m_lineUVs[idx+2].x = offset;
				m_lineUVs[idx+3].x = offset;
				idx += 4;
			}
			
			m_mesh.uv = m_lineUVs;
		}
		
		return true;
	}
	
	private void CheckNormals () {
		if (m_useNormals && !m_normalsCalculated) {
			m_mesh.RecalculateNormals();
			m_normalsCalculated = true;
		}
		if (m_useTangents && !m_tangentsCalculated) {
			CalculateTangents();
			m_tangentsCalculated = true;
		}
	}
	
	public void Draw () {
		Draw (null);
	}
	
	public void Draw (Transform thisTransform) {
		if (error || !m_active) return;
		if (!cam) {
			SetCamera();
			if (!cam) {	// If that didn't work (no camera tagged "Main Camera")
				LogError ("VectorLine.Draw: You must call SetCamera before calling Draw for \"" + name + "\"");
				return;
			}
		}
		if (thisTransform != null) {
			m_useTransform = thisTransform;
		}
		if (m_isPoints) {
			DrawPoints (thisTransform);
			return;
		}
		 
		if (smoothWidth && m_lineWidths.Length == 1 && pointsLength > 2) {
			LogError ("VectorLine.Draw called with smooth line widths for \"" + name + "\", but VectorLine.SetWidths has not been used");
			return;
		}
	
		var useTransformMatrix = (thisTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
		zDist = useOrthoCam? 101-m_depth : screenHeight/2 + ((100.0f - m_depth) * .0001f);
		
		int start, end = 0;
		SetupDrawStartEnd (out start, out end);
		
		if (m_is2D) {
			Line2D (start, end, thisMatrix, useTransformMatrix);
		}
		else {
			if (m_continuous) {
				Line3DContinuous (start, end, thisMatrix, useTransformMatrix);
			}
			else {
				Line3DDiscrete (start, end, thisMatrix, useTransformMatrix);
			}
		}
		
		if (!CheckLine()) return;
		m_mesh.vertices = m_lineVertices;
		CheckNormals();
		if (m_mesh.bounds.center.x != screenWidth/2) {
			SetLineMeshBounds();
		}
	}

	private void Line2D (int start, int end, Matrix4x4 thisMatrix, bool useTransformMatrix) {
		Vector3 p1, p2;
#if !UNITY_3
		if (m_1pixelLine) {
			if (m_continuous) {
				int index = start*2;
				for (int i = start; i < end; i++) {
					if (useTransformMatrix) {
						p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
						p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
					}
					else {
						p1 = points2[i];
						p2 = points2[i+1];
					}
					p1.z = zDist; p2.z = zDist;
					m_lineVertices[index  ] = p1;
					m_lineVertices[index+1] = p2;
					index += 2;
				}
			}
			else {
				for (int i = start; i <= end; i++) {
					if (useTransformMatrix) {
						p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					}
					else {
						p1 = points2[i];
					}
					p1.z = zDist;
					m_lineVertices[i] = p1;
				}
			}
			return;
		}
#endif
		int add, idx, widthIdx = 0;
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		if (m_continuous) {
			idx = start*4;
			add = 1;
		}
		else {
			idx = start*2;
			add = 2;
			widthIdx /= 2;
		}
		
		if (capLength == 0.0f) {
			var perpendicular = new Vector3(0.0f, 0.0f, 0.0f);
			for (int i = start; i < end; i += add) {
				if (useTransformMatrix) {
					p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
				}
				else {
					p1 = points2[i];
					p2 = points2[i+1];
				}
				p1.z = zDist;
				if (p1.x == p2.x && p1.y == p2.y) {Skip (ref idx, ref widthIdx, ref p1); continue;}
				p2.z = zDist;
				
				v1.x = p2.y; v1.y = p1.x;
				v2.x = p1.y; v2.y = p2.x;
				perpendicular = v1 - v2;
				float normalizedDistance = ( 1.0f / Mathf.Sqrt ((perpendicular.x * perpendicular.x) + (perpendicular.y * perpendicular.y)) );
				perpendicular *= normalizedDistance * m_lineWidths[widthIdx];
				m_lineVertices[idx]   = p1 - perpendicular;
				m_lineVertices[idx+1] = p1 + perpendicular;
				if (smoothWidth && i < end-add) {
					perpendicular = v1 - v2;
					perpendicular *= normalizedDistance * m_lineWidths[widthIdx+1];
				}
				m_lineVertices[idx+2] = p2 - perpendicular;
				m_lineVertices[idx+3] = p2 + perpendicular;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
			if (m_joins == Joins.Weld) {
				if (m_continuous) {
					WeldJoins (start*4 + (start == 0? 4 : 0), end*4, Approximately2 (points2[0], points2[points2.Length-1])
						&& m_minDrawIndex == 0 && (m_maxDrawIndex == points2.Length-1 || m_maxDrawIndex == 0));
				}
				else {
					WeldJoinsDiscrete (start + 1, end, Approximately2 (points2[0], points2[points2.Length-1])
						&& m_minDrawIndex == 0 && (m_maxDrawIndex == points2.Length-1 || m_maxDrawIndex == 0));
				}
			}
		}
		else {
			var thisLine = new Vector3(0.0f, 0.0f, 0.0f);
			for (int i = m_minDrawIndex; i < end; i += add) {
				if (useTransformMatrix) {
					p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
				}
				else {
					p1 = points2[i];
					p2 = points2[i+1];
				}
				p1.z = zDist;
				if (p1.x == p2.x && p1.y == p2.y) {Skip (ref idx, ref widthIdx, ref p1); continue;}
				p2.z = zDist;
				
				thisLine = p2 - p1;
				thisLine *= ( 1.0f / Mathf.Sqrt ((thisLine.x * thisLine.x) + (thisLine.y * thisLine.y)) );
				p1 -= thisLine * capLength;
				p2 += thisLine * capLength;
				
				v1.x = thisLine.y; v1.y = -thisLine.x;
				thisLine = v1 * m_lineWidths[widthIdx];
				m_lineVertices[idx]   = p1 - thisLine;
				m_lineVertices[idx+1] = p1 + thisLine;
				if (smoothWidth && i < end-add) {
					thisLine = v1 * m_lineWidths[widthIdx+1];
				}
				m_lineVertices[idx+2] = p2 - thisLine;
				m_lineVertices[idx+3] = p2 + thisLine;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
		}
	}

	private void Line3DContinuous (int start, int end, Matrix4x4 thisMatrix, bool useTransformMatrix) {
		if (!cam3D) {
			LogError ("The 3D camera no longer exists...if you have changed scenes, ensure that SetCamera3D is called in order to set it up.");
			return;
		}
#if !UNITY_3
		if (m_1pixelLine) {
			Vector3 p1;
			Vector3 p2 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[start])) :
											 cam3D.WorldToScreenPoint (points3[start]);
			p2.z = p2.z < cutoff? -zDist : zDist;
			int index = start*2;
			for (int i = start; i < end; i++) {
				p1 = p2;
				p2 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1])) :
										 cam3D.WorldToScreenPoint (points3[i+1]);
				p2.z = p2.z < cutoff? -zDist : zDist;
				
				m_lineVertices[index  ] = p1;
				m_lineVertices[index+1] = p2;
				index += 2;
			}
			return;
		}
#endif
		Vector3 pos1, perpendicular;
		Vector3 pos2 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[start])) :
										   cam3D.WorldToScreenPoint (points3[start]);
		pos2.z = pos2.z < cutoff? -zDist : zDist;
		float normalizedDistance = 0.0f;
		int widthIdx = 0;
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		int idx = start*4;
		
		for (int i = start; i < end; i++) {
			pos1 = pos2;
			pos2 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1])) :
									   cam3D.WorldToScreenPoint (points3[i+1]);
			if (pos1.x == pos2.x && pos1.y == pos2.y) {Skip (ref idx, ref widthIdx, ref pos1); continue;}
			pos2.z = pos2.z < cutoff? -zDist : zDist;
			
			v1.x = pos2.y; v1.y = pos1.x;
			v2.x = pos1.y; v2.y = pos2.x;
			perpendicular = v1 - v2;
			normalizedDistance = 1.0f / Mathf.Sqrt ((perpendicular.x * perpendicular.x) + (perpendicular.y * perpendicular.y));
			perpendicular *= normalizedDistance * m_lineWidths[widthIdx];
			m_lineVertices[idx]   = pos1 - perpendicular;
			m_lineVertices[idx+1] = pos1 + perpendicular;
			if (smoothWidth && i < end-1) {
				perpendicular = v1 - v2;
				perpendicular *= normalizedDistance * m_lineWidths[widthIdx+1];
			}
			m_lineVertices[idx+2] = pos2 - perpendicular;
			m_lineVertices[idx+3] = pos2 + perpendicular;
			idx += 4;
			widthIdx += widthIdxAdd;
		}
		
		if (m_joins == Joins.Weld) {
			WeldJoins (start*4 + 4, end*4, Approximately3 (points3[0], points3[points3.Length-1])
				&& m_minDrawIndex == 0 && (m_maxDrawIndex == points3.Length-1 || m_maxDrawIndex == 0));
		}
	}

	private void Line3DDiscrete (int start, int end, Matrix4x4 thisMatrix, bool useTransformMatrix) {
		if (!cam3D) {
			LogError ("The 3D camera no longer exists...if you have changed scenes, ensure that SetCamera3D is called in order to set it up.");
			return;
		}
#if !UNITY_3
		if (m_1pixelLine) {
			Vector3 p1;
			for (int i = start; i <= end; i++) {
				p1 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i])) :
										 cam3D.WorldToScreenPoint (points3[i]);
				p1.z = p1.z < cutoff? -zDist : zDist;
				m_lineVertices[i] = p1;
			}
			return;
		}
#endif
		Vector3 pos1, pos2, perpendicular;
		float normalizedDistance = 0.0f;
		int widthIdx = 0;
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		int idx = start*2;
		
		for (int i = start; i < end; i += 2) {
			if (useTransformMatrix) {
				pos1 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i]));
				pos2 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1]));
			}
			else {
				pos1 = cam3D.WorldToScreenPoint (points3[i]);
				pos2 = cam3D.WorldToScreenPoint (points3[i+1]);
			}
			pos1.z = pos1.z < cutoff? -zDist : zDist;
			if (pos1.x == pos2.x && pos1.y == pos2.y) {Skip (ref idx, ref widthIdx, ref pos1); continue;}
			pos2.z = pos2.z < cutoff? -zDist : zDist;
			
			v1.x = pos2.y; v1.y = pos1.x;
			v2.x = pos1.y; v2.y = pos2.x;
			perpendicular = v1 - v2;
			normalizedDistance = 1.0f / Mathf.Sqrt((perpendicular.x * perpendicular.x) + (perpendicular.y * perpendicular.y));
			perpendicular *= normalizedDistance * m_lineWidths[widthIdx];
			m_lineVertices[idx]   = pos1 - perpendicular;
			m_lineVertices[idx+1] = pos1 + perpendicular;
			if (smoothWidth && i < end-2) {
				perpendicular = v1 - v2;
				perpendicular *= normalizedDistance * m_lineWidths[widthIdx+1];
			}
			m_lineVertices[idx+2] = pos2 - perpendicular;
			m_lineVertices[idx+3] = pos2 + perpendicular;
			idx += 4;
			widthIdx += widthIdxAdd;
		}
		
		if (m_joins == Joins.Weld) {
			WeldJoinsDiscrete (start + 1, end, Approximately3 (points3[0], points3[points3.Length-1])
				&& m_minDrawIndex == 0 && (m_maxDrawIndex == points3.Length-1 || m_maxDrawIndex == 0));
		}
	}

	public void Draw3D () {
		Draw3D (null);
	}

	public void Draw3D (Transform thisTransform) {
		if (error || !m_active) return;
		if (!cam3D) {
			SetCamera3D();
			if (!cam3D) {
				LogError ("VectorLine.Draw3D: You must call SetCamera or SetCamera3D before calling Draw3D for \"" + name + "\"");
				return;
			}
		}
		if (m_is2D) {
			LogError ("VectorLine.Draw3D can only be used with a Vector3 array, which \"" + name + "\" doesn't have");
			return;
		}
		if (thisTransform != null) {
			m_useTransform = thisTransform;
		}
		if (m_isPoints) {
			DrawPoints3D (thisTransform);
			return;
		}

		if (smoothWidth && m_lineWidths.Length == 1 && pointsLength > 2) {
			LogError ("VectorLine.Draw3D called with smooth line widths for \"" + name + "\", but VectorLine.SetWidths has not been used");
			return;
		}
		
		if (layer == -1) {
			m_vectorObject.layer = _vectorLayer3D;
			layer = _vectorLayer3D;
		}
		
		int start, end, idx, add, widthIdx = 0;
		SetupDrawStartEnd (out start, out end);
		var useTransformMatrix = (thisTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
		
#if !UNITY_3
		if (m_1pixelLine) {
			if (m_continuous) {
				int index = start*2;
				if (useTransformMatrix) {
					for (int i = start; i < end; i++) {
						m_lineVertices[index  ] = thisMatrix.MultiplyPoint3x4 (points3[i]);
						m_lineVertices[index+1] = thisMatrix.MultiplyPoint3x4 (points3[i+1]);
						index += 2;
					}
				}
				else {
					for (int i = start; i < end; i++) {
						m_lineVertices[index  ] = points3[i];
						m_lineVertices[index+1] = points3[i+1];
						index += 2;
					}
				}
			}
			else {
				if (useTransformMatrix) {
					for (int i = start; i <= end; i++) {
						m_lineVertices[i] = thisMatrix.MultiplyPoint3x4 (points3[i]);
					}
				}
				else {
					for (int i = start; i <= end; i++) {
						m_lineVertices[i] = points3[i];
					}
				}
			}
			
			if (!CheckLine()) return;
			m_mesh.vertices = m_lineVertices;
			m_mesh.RecalculateBounds();
			return;
		}
#endif
		
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		if (m_continuous) {
			idx = start*4;
			add = 1;
		}
		else {
			idx = start*2;
			widthIdx /= 2;
			add = 2;
		}
		Vector3 pos1, pos2, thisLine, perpendicular;
		
		for (int i = start; i < end; i += add) {
			if (useTransformMatrix) {
				pos1 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i]));
				pos2 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1]));
			}
			else {
				pos1 = cam3D.WorldToScreenPoint (points3[i]);
				pos2 = cam3D.WorldToScreenPoint (points3[i+1]);
			}
			
			v1.x = pos2.y; v1.y = pos1.x;
			v2.x = pos1.y; v2.y = pos2.x;
			thisLine = (v1 - v2).normalized;
			perpendicular = thisLine * m_lineWidths[widthIdx];
			
			m_screenPoints[idx  ] = pos1 - perpendicular;	// Used for Joins.Weld
			m_screenPoints[idx+1] = pos1 + perpendicular;
			m_lineVertices[idx  ] = cam3D.ScreenToWorldPoint (m_screenPoints[idx]);
			m_lineVertices[idx+1] = cam3D.ScreenToWorldPoint (m_screenPoints[idx+1]);
			
			if (smoothWidth && i < end-add) {
				perpendicular = thisLine * m_lineWidths[widthIdx+1];
			}
			m_screenPoints[idx+2] = pos2 - perpendicular;
			m_screenPoints[idx+3] = pos2 + perpendicular;
			m_lineVertices[idx+2] = cam3D.ScreenToWorldPoint (m_screenPoints[idx+2]);
			m_lineVertices[idx+3] = cam3D.ScreenToWorldPoint (m_screenPoints[idx+3]);
			
			idx += 4;
			widthIdx += widthIdxAdd;
		}
		
		if (m_joins == Joins.Weld) {
			if (m_continuous) {
				WeldJoins3D (start*4 + 4, end*4, Approximately3 (points3[0], points3[m_pointsLength-1])
					&& m_minDrawIndex == 0 && (m_maxDrawIndex == points3.Length-1 || m_maxDrawIndex == 0));
			}
			else {
				WeldJoinsDiscrete3D (start + 1, end, Approximately3 (points3[0], points3[m_pointsLength-1])
					&& m_minDrawIndex == 0 && (m_maxDrawIndex == points3.Length-1 || m_maxDrawIndex == 0));
			}
		}
		
		if (!CheckLine()) return;
		m_mesh.vertices = m_lineVertices;
		m_mesh.RecalculateBounds();
		CheckNormals();
	}

	public void DrawViewport () {
		DrawViewport (null);
	}

	public void DrawViewport (Transform thisTransform) {
		if (error || !m_active) return;
		if (!cam) {
			SetCamera();
			if (!cam) {	// If that didn't work (no camera tagged "Main Camera")
				LogError ("VectorLine.DrawViewport: You must call SetCamera before calling DrawViewport for \"" + name + "\"");
				return;
			}
		}
		if (m_isPoints) {
			LogError ("VectorLine.DrawViewport can't be used with VectorPoints");
			return;
		}
		if (!m_is2D) {
			LogError ("VectorLine.DrawViewport can only be used with a Vector2 array, which \"" + name + "\" doesn't have");
			return;
		}
		if (smoothWidth && m_lineWidths.Length == 1 && pointsLength > 2) {
			LogError ("VectorLine.DrawViewport called with smooth line widths for \"" + name + "\", but SetWidths has not been used");
			return;
		}
		
		var useTransformMatrix = (thisTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
		zDist = useOrthoCam? 101-m_depth : screenHeight/2 + ((100.0f - m_depth) * .0001f);
		Vector3 p1, p2;
		int idx, add, start, end, widthIdx = 0;
		widthIdxAdd = 0;
		SetupDrawStartEnd (out start, out end);
		int sWidth = screenWidth;
		int sHeight = screenHeight;
		
#if !UNITY_3
		if (m_1pixelLine) {
			if (m_continuous) {
				int index = start*2;
				for (int i = start; i < end; i++) {
					if (useTransformMatrix) {
						p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
						p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
					}
					else {
						p1 = points2[i];
						p2 = points2[i+1];
					}
					p1.z = zDist; p2.z = zDist;
					p1.x *= sWidth; p1.y *= sHeight;
					p2.x *= sWidth; p2.y *= sHeight;
					m_lineVertices[index  ] = p1;
					m_lineVertices[index+1] = p2;
					index += 2;
				}
			}
			else {
				for (int i = start; i <= end; i++) {
					if (useTransformMatrix) {
						p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					}
					else {
						p1 = points2[i];
					}
					p1.x *= sWidth; p1.y *= sHeight;
					p1.z = zDist;
					m_lineVertices[i] = p1;
				}
			}
			
			if (!CheckLine()) return;
			m_mesh.vertices = m_lineVertices;
			if (m_mesh.bounds.center.x != sWidth/2) {
				SetLineMeshBounds();
			}
			return;
		}
#endif
		
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		if (m_continuous) {
			idx = start*4;
			add = 1;
		}
		else {
			idx = start*2;
			widthIdx /= 2;
			add = 2;
		}
		
		if (capLength == 0.0f) {
			Vector3 perpendicular;
			for (int i = start; i < end; i += add) {
				if (useTransformMatrix) {
					p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
				}
				else {
					p1 = points2[i];
					p2 = points2[i+1];
				}
				p1.z = zDist;
				if (p1.x == p2.x && p1.y == p2.y) {Skip (ref idx, ref widthIdx, ref p1); continue;}
				p2.z = zDist;
				p1.x *= sWidth; p1.y *= sHeight;
				p2.x *= sWidth; p2.y *= sHeight;
				
				v1.x = p2.y * sWidth; v1.y = p1.x * sHeight;
				v2.x = p1.y * sWidth; v2.y = p2.x * sHeight;
				perpendicular = v1 - v2;
				float normalizedDistance = ( 1.0f / Mathf.Sqrt ((perpendicular.x * perpendicular.x) + (perpendicular.y * perpendicular.y)) );
				perpendicular *= normalizedDistance * m_lineWidths[widthIdx];
				m_lineVertices[idx]   = p1 - perpendicular;
				m_lineVertices[idx+1] = p1 + perpendicular;
				if (smoothWidth && i < end-add) {
					perpendicular = v1 - v2;
					perpendicular *= normalizedDistance * m_lineWidths[widthIdx+1];
				}
				m_lineVertices[idx+2] = p2 - perpendicular;
				m_lineVertices[idx+3] = p2 + perpendicular;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
			if (m_joins == Joins.Weld) {
				if (m_continuous) {
					WeldJoins (start*4 + 4, end*4, Approximately2 (points2[0], points2[m_pointsLength-1])
						&& m_minDrawIndex == 0 && (m_maxDrawIndex == points2.Length-1 || m_maxDrawIndex == 0));
				}
				else {
					WeldJoinsDiscrete (start + 1, end, Approximately2 (points2[0], points2[m_pointsLength-1])
						&& m_minDrawIndex == 0 && (m_maxDrawIndex == points2.Length-1 || m_maxDrawIndex == 0));
				}
			}
		}
		else {
			Vector3 thisLine;
			for (int i = m_minDrawIndex; i < end; i += add) {
				if (useTransformMatrix) {
					p1 = thisMatrix.MultiplyPoint3x4(points2[i]);
					p2 = thisMatrix.MultiplyPoint3x4(points2[i+1]);
				}
				else {
					p1 = points2[i];
					p2 = points2[i+1];
				}
				p1.z = zDist;
				if (p1.x == p2.x && p1.y == p2.y) {Skip (ref idx, ref widthIdx, ref p1); continue;}
				p2.z = zDist;
				p1.x *= sWidth; p1.y *= sHeight;
				p2.x *= sWidth; p2.y *= sHeight;
				
				thisLine = p2 - p1;
				thisLine *= ( 1.0f / Mathf.Sqrt((thisLine.x * thisLine.x) + (thisLine.y * thisLine.y)) );
				p1 -= thisLine * capLength;
				p2 += thisLine * capLength;
				
				v1.x = thisLine.y; v1.y = -thisLine.x;
				thisLine = v1 * m_lineWidths[widthIdx];
				m_lineVertices[idx]   = p1 - thisLine;
				m_lineVertices[idx+1] = p1 + thisLine;
				if (smoothWidth && i < end-add) {
					thisLine = v1 * m_lineWidths[widthIdx+1];
				}
				m_lineVertices[idx+2] = p2 - thisLine;
				m_lineVertices[idx+3] = p2 + thisLine;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
		}
		
		if (!CheckLine()) return;
		m_mesh.vertices = m_lineVertices;
		if (m_mesh.bounds.center.x != sWidth/2) {
			SetLineMeshBounds();
		}
	}

	private void DrawPoints () {
		DrawPoints (null);
	}
	
	private void DrawPoints (Transform thisTransform) {
		var useTransformMatrix = (thisTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
		zDist = useOrthoCam? 101-m_depth : screenHeight/2 + ((100.0f - m_depth) * .0001f);

		int start, end, widthIdx = 0;
		SetupDrawStartEnd (out start, out end);

#if !UNITY_3
		if (m_1pixelLine) {
			if (!m_is2D) {
				for (int i = start; i <= end; i++) {
					m_lineVertices[i] = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4(points3[i])) :
															cam3D.WorldToScreenPoint (points3[i]);
					if (m_lineVertices[i].z < cutoff) {
						m_lineVertices[i] = Vector3.zero;
						continue;
					}
					m_lineVertices[i].z = zDist;
				}
			}
			else {
				for (int i = start; i <= end; i++) {
					m_lineVertices[i] = useTransformMatrix? thisMatrix.MultiplyPoint3x4 (points2[i]) : (Vector3)points2[i];
					m_lineVertices[i].z = zDist;
				}
			}
			
			m_mesh.vertices = m_lineVertices;
			if (m_mesh.bounds.center.x != screenWidth/2) {
				SetLineMeshBounds();
			}
			return;
		}
#endif
		
		Vector3 p1;
		int idx = start*4;
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		
		if (!m_is2D) {
			for (int i = start; i <= end; i++) {
				p1 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4(points3[i])) :
										 cam3D.WorldToScreenPoint (points3[i]);
				if (p1.z < cutoff) {
					Skip (ref idx, ref widthIdx, ref p1);
					continue;
				}
				p1.z = zDist;
				v1.x = v1.y = v2.y = m_lineWidths[widthIdx];
				v2.x = -m_lineWidths[widthIdx];				

				m_lineVertices[idx  ] = p1 + v2;
				m_lineVertices[idx+1] = p1 - v1;
				m_lineVertices[idx+2] = p1 + v1;
				m_lineVertices[idx+3] = p1 - v2;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
		}
		else {
			for (int i = start; i <= end; i++) {
				p1 = useTransformMatrix? thisMatrix.MultiplyPoint3x4 (points2[i]) : (Vector3)points2[i];
				p1.z = zDist;
				v1.x = v1.y = v2.y = m_lineWidths[widthIdx];
				v2.x = -m_lineWidths[widthIdx];
	
				m_lineVertices[idx  ] = p1 + v2;
				m_lineVertices[idx+1] = p1 - v1;
				m_lineVertices[idx+2] = p1 + v1;
				m_lineVertices[idx+3] = p1 - v2;
				idx += 4;
				widthIdx += widthIdxAdd;
			}
		}
		
		m_mesh.vertices = m_lineVertices;
		if (m_mesh.bounds.center.x != screenWidth/2) {
			SetLineMeshBounds();
		}
	}

	private void DrawPoints3D () {
		DrawPoints3D (null);
	}

	private void DrawPoints3D (Transform thisTransform) {
		if (layer == -1) {
			m_vectorObject.layer = _vectorLayer3D;
			layer = _vectorLayer3D;
		}
		var useTransformMatrix = (thisTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
		
		int start, end, widthIdx = 0;
		SetupDrawStartEnd (out start, out end);
		
#if !UNITY_3
		if (m_1pixelLine) {
			if (useTransformMatrix) {
				for (int i = start; i <= end; i++) {
					m_lineVertices[i] = thisMatrix.MultiplyPoint3x4 (points3[i]);
				}
			}
			else {
				for (int i = start; i <= end; i++) {
					m_lineVertices[i] = points3[i];
				}
			}
			
			m_mesh.vertices = m_lineVertices;
			m_mesh.RecalculateBounds();
			return;
		}
#endif
		int idx = m_minDrawIndex*4;
		widthIdxAdd = 0;
		if (m_lineWidths.Length > 1) {
			widthIdx = start;
			widthIdxAdd = 1;
		}
		Vector3 p1;
		for (int i = start; i <= end; i++) {
			p1 = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i])) :
									 cam3D.WorldToScreenPoint (points3[i]);
			if (p1.z < cutoff) {
				p1 = Vector3.zero;
				Skip (ref idx, ref widthIdx, ref p1);
				continue;
			}
			v1.x = v1.y = v2.y = m_lineWidths[widthIdx];
			v2.x = -m_lineWidths[widthIdx];
			
			m_lineVertices[idx  ] = cam3D.ScreenToWorldPoint (p1 + v2);
			m_lineVertices[idx+1] = cam3D.ScreenToWorldPoint (p1 - v1);
			m_lineVertices[idx+2] = cam3D.ScreenToWorldPoint (p1 + v1);
			m_lineVertices[idx+3] = cam3D.ScreenToWorldPoint (p1 - v2);
			idx += 4;
			widthIdx += widthIdxAdd;
		}
		
		m_mesh.vertices = m_lineVertices;
		m_mesh.RecalculateBounds();
		CheckNormals();
	}

	private void Skip (ref int idx, ref int widthIdx, ref Vector3 pos) {
		m_lineVertices[idx  ] = pos;
		m_lineVertices[idx+1] = pos;
		m_lineVertices[idx+2] = pos;
		m_lineVertices[idx+3] = pos;
		idx += 4;
		widthIdx += widthIdxAdd;
	}
	
	private void SetLineMeshBounds () {
		var bounds = new Bounds();
		if (!useOrthoCam) {
			bounds.center = new Vector3(screenWidth/2, screenHeight/2, screenHeight/2);
			bounds.extents = new Vector3(screenWidth*100, screenHeight*100, .1f);
		}
		else {
			bounds.center = new Vector3(screenWidth/2, screenHeight/2, 50.5f);
			bounds.extents = new Vector3(screenWidth*100, screenHeight*100, 51.0f);
		}
		m_mesh.bounds = bounds;
	}
	
	private void SetupDrawStartEnd (out int start, out int end) {
		start = m_minDrawIndex;
		end = (m_maxDrawIndex == 0)? m_pointsLength-1 : m_maxDrawIndex;
		if (m_drawStart > 0) {
			start = m_drawStart;
			ZeroVertices (0, m_drawStart);
		}
		if (m_drawEnd < m_pointsLength) {
			end = m_drawEnd;
			ZeroVertices (m_drawEnd, m_pointsLength);
		}
	}
	
	public static void LineManagerCheckDistance () {
		lineManager.StartCheckDistance();
	}
	
	public static void LineManagerDisable () {
		lineManager.DisableIfUnused();
	}
	
	public static void LineManagerEnable () {
		lineManager.EnableIfUsed();
	}

	public void Draw3DAuto () {
		Draw3DAuto (0.0f, null);
	}

	public void Draw3DAuto (float time) {
		Draw3DAuto (time, null);
	}

	public void Draw3DAuto (Transform thisTransform) {
		Draw3DAuto (0.0f, thisTransform);
	}
	
	public void Draw3DAuto (float time, Transform thisTransform) {
#if !UNITY_3
		if (m_1pixelLine) {
			Debug.LogWarning ("VectorLine: When using a 1 pixel line and useMeshLines=true (or 1 pixel points and useMeshPoints=true), Draw3DAuto is unnecessary. Use Draw3D instead for optimal performance.");
		}
#endif
		if (time < 0.0f) time = 0.0f;
		lineManager.AddLine (this, thisTransform, time);
		m_isAutoDrawing = true;
		Draw3D (thisTransform);
	}
	
	public void StopDrawing3DAuto () {
		lineManager.RemoveLine (this);
		m_isAutoDrawing = false;
	}
	
	private void WeldJoins (int start, int end, bool connectFirstAndLast) {
		if (connectFirstAndLast) {
			SetIntersectionPoint (m_vertexCount-4, m_vertexCount-2, 0, 2);
			SetIntersectionPoint (m_vertexCount-3, m_vertexCount-1, 1, 3);
		}
		for (int i = start; i < end; i+= 4) {
			SetIntersectionPoint (i-4, i-2, i, i+2);
			SetIntersectionPoint (i-3, i-1, i+1, i+3);
		}
	}

	private void WeldJoinsDiscrete (int start, int end, bool connectFirstAndLast) {
		if (connectFirstAndLast) {
			SetIntersectionPoint (m_vertexCount-4, m_vertexCount-2, 0, 2);
			SetIntersectionPoint (m_vertexCount-3, m_vertexCount-1, 1, 3);
		}
		int idx = (start+1) / 2 * 4;
		if (m_is2D) {
			for (int i = start; i < end; i+= 2) {
				if (points2[i] == points2[i+1]) {
					SetIntersectionPoint (idx-4, idx-2, idx,   idx+2);
					SetIntersectionPoint (idx-3, idx-1, idx+1, idx+3);
				}
				idx += 4;
			}
		}
		else {
			for (int i = start; i < end; i+= 2) {
				if (points3[i] == points3[i+1]) {
					SetIntersectionPoint (idx-4, idx-2, idx,   idx+2);
					SetIntersectionPoint (idx-3, idx-1, idx+1, idx+3);
				}
				idx += 4;
			}
		}
	}
	
	private void SetIntersectionPoint (int p1, int p2, int p3, int p4) {
		var l1a = m_lineVertices[p1]; var l1b = m_lineVertices[p2];
		var l2a = m_lineVertices[p3]; var l2b = m_lineVertices[p4];
		float d = (l2b.y - l2a.y)*(l1b.x - l1a.x) - (l2b.x - l2a.x)*(l1b.y - l1a.y);
		if (d == 0.0f) return;	// Parallel lines
		float n = ( (l2b.x - l2a.x)*(l1a.y - l2a.y) - (l2b.y - l2a.y)*(l1a.x - l2a.x) ) / d;
		
		v3.x = l1a.x + (n * (l1b.x - l1a.x));
		v3.y = l1a.y + (n * (l1b.y - l1a.y));
		v3.z = l1a.z;
		if ((v3 - l1b).sqrMagnitude > m_maxWeldDistance) return;
		m_lineVertices[p2] = v3;
		m_lineVertices[p3] = v3;
	}

	private void WeldJoins3D (int start, int end, bool connectFirstAndLast) {
		if (connectFirstAndLast) {
			SetIntersectionPoint3D (m_vertexCount-4, m_vertexCount-2, 0, 2);
			SetIntersectionPoint3D (m_vertexCount-3, m_vertexCount-1, 1, 3);
		}
		for (int i = start; i < end; i+= 4) {
			SetIntersectionPoint3D (i-4, i-2, i, i+2);
			SetIntersectionPoint3D (i-3, i-1, i+1, i+3);
		}
	}

	private void WeldJoinsDiscrete3D (int start, int end, bool connectFirstAndLast) {
		if (connectFirstAndLast) {
			SetIntersectionPoint3D (m_vertexCount-4, m_vertexCount-2, 0, 2);
			SetIntersectionPoint3D (m_vertexCount-3, m_vertexCount-1, 1, 3);
		}
		int idx = (start+1) / 2 * 4;
		for (int i = start; i < end; i+= 2) {
			if (points3[i] == points3[i+1]) {
				SetIntersectionPoint3D (idx-4, idx-2, idx,   idx+2);
				SetIntersectionPoint3D (idx-3, idx-1, idx+1, idx+3);
			}
			idx += 4;
		}
	}

	private void SetIntersectionPoint3D (int p1, int p2, int p3, int p4) {
		var l1a = m_screenPoints[p1]; var l1b = m_screenPoints[p2];
		var l2a = m_screenPoints[p3]; var l2b = m_screenPoints[p4];
		float d = (l2b.y - l2a.y)*(l1b.x - l1a.x) - (l2b.x - l2a.x)*(l1b.y - l1a.y);
		if (d == 0.0f) return;	// Parallel lines
		float n = ( (l2b.x - l2a.x)*(l1a.y - l2a.y) - (l2b.y - l2a.y)*(l1a.x - l2a.x) ) / d;
		
		v3.x = l1a.x + (n * (l1b.x - l1a.x));
		v3.y = l1a.y + (n * (l1b.y - l1a.y));
		v3.z = l1a.z;
		if ((v3 - l1b).sqrMagnitude > m_maxWeldDistance) return;
		m_lineVertices[p2] = cam3D.ScreenToWorldPoint(v3);
		m_lineVertices[p3] = m_lineVertices[p2];
	}
	
	public void SetTextureScale (float textureScale) {
		SetTextureScale (null, textureScale, 0.0f);
	}

	public void SetTextureScale (Transform thisTransform, float textureScale) {
		SetTextureScale (thisTransform, textureScale, 0.0f);
	}

	public void SetTextureScale (float textureScale, float offset) {
		SetTextureScale (null, textureScale, offset);
	}
	
	public void SetTextureScale (Transform thisTransform, float textureScale, float offset) {
#if !UNITY_3
		if (m_1pixelLine) return;
#endif
		int end = m_continuous? pointsLength-1 : pointsLength;
		int add = m_continuous? 1 : 2;
		int idx = 0;
		int widthIdx = 0;
		widthIdxAdd = m_lineWidths.Length == 1? 0 : 1;
		float thisScale = 1.0f / textureScale;
		
		if (m_is2D) {
			for (int i = 0; i < end; i += add) {
				float xPos = thisScale / (m_lineWidths[widthIdx]*2 / (points2[i] - points2[i+1]).magnitude);
				m_lineUVs[idx  ].x = offset;
				m_lineUVs[idx+1].x = offset;
				m_lineUVs[idx+2].x = xPos + offset;
				m_lineUVs[idx+3].x = xPos + offset;
				idx += 4;
				offset = (offset + xPos) % 1;
				widthIdx += widthIdxAdd;
			}
		}
		else {
			if (!cam3D) {
				SetCamera3D();
				if (!cam3D) {
					LogError ("VectorLine.SetTextureScale: You must call SetCamera3D before calling SetTextureScale");
					return;
				}
			}
			
			var useTransformMatrix = (thisTransform == null)? false : true;
			var thisMatrix = useTransformMatrix? thisTransform.localToWorldMatrix : Matrix4x4.identity;
			var p1 = Vector2.zero;
			var p2 = Vector2.zero;
			for (int i = 0; i < end; i += add) {
				if (useTransformMatrix) {
					p1 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i]));
					p2 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1]));					
				}
				else {
					p1 = cam3D.WorldToScreenPoint (points3[i]);
					p2 = cam3D.WorldToScreenPoint (points3[i+1]);
				}
				float xPos = thisScale / (m_lineWidths[widthIdx]*2 / (p1 - p2).magnitude);
				m_lineUVs[idx  ].x = offset;
				m_lineUVs[idx+1].x = offset;
				m_lineUVs[idx+2].x = xPos + offset;
				m_lineUVs[idx+3].x = xPos + offset;
				idx += 4;
				offset = (offset + xPos) % 1;
				widthIdx += widthIdxAdd;
			}
		}
		
		m_mesh.uv = m_lineUVs;
	}

	public void ResetTextureScale () {
#if !UNITY_3
		if (m_1pixelLine) return;
#endif
		int end = m_lineUVs.Length;
		
		for (int i = 0; i < end; i += 4) {
			m_lineUVs[i  ].x = 0.0f;
			m_lineUVs[i+1].x = 0.0f;
			m_lineUVs[i+2].x = 1.0f;
			m_lineUVs[i+3].x = 1.0f;
		}
		
		m_mesh.uv = m_lineUVs;
	}
	
	public static void SetDepth (Transform thisTransform, int depth) {
		depth = Mathf.Clamp(depth, 0, 100);
		thisTransform.position = new Vector3(thisTransform.position.x,
											 thisTransform.position.y,
											 useOrthoCam? 101-depth : screenHeight/2 + ((100.0f - depth) * .0001f));		
	}
	
	static int endianDiff1;
	static int endianDiff2;
	static byte[] byteBlock;
	
	public static Vector3[] BytesToVector3Array (byte[] lineBytes) {
		if (lineBytes.Length % 12 != 0) {
			LogError ("VectorLine.BytesToVector3Array: Incorrect input byte length...must be a multiple of 12");
			return null;
		}
		
		SetupByteBlock();
		Vector3[] points = new Vector3[lineBytes.Length/12];
		int idx = 0;
		for (int i = 0; i < lineBytes.Length; i += 12) {
			points[idx++] = new Vector3( ConvertToFloat (lineBytes, i),
										 ConvertToFloat (lineBytes, i+4),
										 ConvertToFloat (lineBytes, i+8) );
		}
		return points;
	}
	
	public static Vector2[] BytesToVector2Array (byte[] lineBytes) {
		if (lineBytes.Length % 8 != 0) {
			LogError ("VectorLine.BytesToVector2Array: Incorrect input byte length...must be a multiple of 8");
			return null;
		}
		
		SetupByteBlock();
		Vector2[] points = new Vector2[lineBytes.Length/8];
		int idx = 0;
		for (int i = 0; i < lineBytes.Length; i += 8) {
			points[idx++] = new Vector2( ConvertToFloat (lineBytes, i),
										 ConvertToFloat (lineBytes, i+4));
		}
		return points;
	}
	
	private static void SetupByteBlock () {
		if (byteBlock == null) {byteBlock = new byte[4];}
		if (System.BitConverter.IsLittleEndian) {endianDiff1 = 0; endianDiff2 = 0;}
		else {endianDiff1 = 3; endianDiff2 = 1;}	
	}
	
	// Unfortunately we can't just use System.BitConverter.ToSingle as-is...we need a function to handle both big-endian and little-endian systems
	private static float ConvertToFloat (byte[] bytes, int i) {
		byteBlock[    endianDiff1] = bytes[i];
		byteBlock[1 + endianDiff2] = bytes[i+1];
		byteBlock[2 - endianDiff2] = bytes[i+2];
		byteBlock[3 - endianDiff1] = bytes[i+3];
		return System.BitConverter.ToSingle (byteBlock, 0);
	}
	
	public static void Destroy (ref VectorLine line) {
		if (line != null) {
			Object.Destroy (line.m_mesh);
			Object.Destroy (line.m_meshFilter);
			Object.Destroy (line.m_vectorObject);
			if (line.isAutoDrawing) {
				line.StopDrawing3DAuto();
			}
			line = null;
		}
	}

	public static void Destroy (ref VectorPoints line) {
		if (line != null) {
			Object.Destroy (line.m_mesh);
			Object.Destroy (line.m_meshFilter);
			Object.Destroy (line.m_vectorObject);
			if (line.isAutoDrawing) {
				line.StopDrawing3DAuto();
			}
			line = null;
		}
	}
	
	public static void Destroy (ref VectorLine line, GameObject go) {
		Destroy (ref line);
		if (go != null) {
			Object.Destroy (go);
		}
	}

	public static void Destroy (ref VectorPoints line, GameObject go) {
		Destroy (ref line);
		if (go != null) {
			Object.Destroy (go);
		}
	}

	public void MakeRect (Rect rect) {
		MakeRect (new Vector2(rect.x, rect.y), new Vector2(rect.x+rect.width, rect.y-rect.height), 0);
	}

	public void MakeRect (Rect rect, int index) {
		MakeRect (new Vector2(rect.x, rect.y), new Vector2(rect.x+rect.width, rect.y-rect.height), index);
	}

	public void MakeRect (Vector3 topLeft, Vector3 bottomRight) {
		MakeRect (topLeft, bottomRight, 0);
	}

	public void MakeRect (Vector3 topLeft, Vector3 bottomRight, int index) {
		if (m_continuous) {
			if (index + 5 > pointsLength) {
				if (index == 0) {
					LogError ("VectorLine.MakeRect: The length of the array for continuous lines needs to be at least 5 for \"" + name + "\"");
					return;
				}
				LogError ("Calling VectorLine.MakeRect with an index of " + index + " would exceed the length of the Vector2 array for \"" + name + "\"");
				return;
			}
			if (m_is2D) {
				points2[index  ] = new Vector2(topLeft.x,     topLeft.y);
				points2[index+1] = new Vector2(bottomRight.x, topLeft.y);
				points2[index+2] = new Vector2(bottomRight.x, bottomRight.y);
				points2[index+3] = new Vector2(topLeft.x,	  bottomRight.y);
				points2[index+4] = new Vector2(topLeft.x,     topLeft.y);
			}
			else {
				points3[index  ] = new Vector3(topLeft.x,     topLeft.y, 	 topLeft.z);
				points3[index+1] = new Vector3(bottomRight.x, topLeft.y, 	 topLeft.z);
				points3[index+2] = new Vector3(bottomRight.x, bottomRight.y, bottomRight.z);
				points3[index+3] = new Vector3(topLeft.x,	  bottomRight.y, bottomRight.z);
				points3[index+4] = new Vector3(topLeft.x,     topLeft.y, 	 topLeft.z);
			}
		}
		else {
			if (index + 8 > pointsLength) {
				if (index == 0) {
					LogError ("VectorLine.MakeRect: The length of the array for discrete lines needs to be at least 8 for \"" + name + "\"");
					return;
				}
				LogError ("Calling VectorLine.MakeRect with an index of " + index + " would exceed the length of the Vector2 array for \"" + name + "\"");
				return;
			}
			if (m_is2D) {
				points2[index  ] = new Vector2(topLeft.x,     topLeft.y);
				points2[index+1] = new Vector2(bottomRight.x, topLeft.y);
				points2[index+2] = new Vector2(bottomRight.x, topLeft.y);
				points2[index+3] = new Vector2(bottomRight.x, bottomRight.y);
				points2[index+4] = new Vector2(bottomRight.x, bottomRight.y);
				points2[index+5] = new Vector2(topLeft.x,     bottomRight.y);
				points2[index+6] = new Vector2(topLeft.x,     bottomRight.y);
				points2[index+7] = new Vector2(topLeft.x,     topLeft.y);				
			}
			else {
				points3[index  ] = new Vector3(topLeft.x,     topLeft.y,	 topLeft.z);
				points3[index+1] = new Vector3(bottomRight.x, topLeft.y, 	 topLeft.z);
				points3[index+2] = new Vector3(bottomRight.x, topLeft.y, 	 topLeft.z);
				points3[index+3] = new Vector3(bottomRight.x, bottomRight.y, bottomRight.z);
				points3[index+4] = new Vector3(bottomRight.x, bottomRight.y, bottomRight.z);
				points3[index+5] = new Vector3(topLeft.x,     bottomRight.y, bottomRight.z);
				points3[index+6] = new Vector3(topLeft.x,     bottomRight.y, bottomRight.z);
				points3[index+7] = new Vector3(topLeft.x,     topLeft.y, 	 topLeft.z);				
			}
		}
	}

	public void MakeCircle (Vector3 origin, float radius) {
		MakeEllipse (origin, Vector3.forward, radius, radius, GetSegmentNumber(), 0.0f, 0);
	}
	
	public void MakeCircle (Vector3 origin, float radius, int segments) {
		MakeEllipse (origin, Vector3.forward, radius, radius, segments, 0.0f, 0);
	}

	public void MakeCircle (Vector3 origin, float radius, int segments, float pointRotation) {
		MakeEllipse (origin, Vector3.forward, radius, radius, segments, pointRotation, 0);
	}

	public void MakeCircle (Vector3 origin, float radius, int segments, int index) {
		MakeEllipse (origin, Vector3.forward, radius, radius, segments, 0.0f, index);
	}

	public void MakeCircle (Vector3 origin, float radius, int segments, float pointRotation, int index) {
		MakeEllipse (origin, Vector3.forward, radius, radius, segments, pointRotation, index);
	}

	public void MakeCircle (Vector3 origin, Vector3 upVector, float radius) {
		MakeEllipse (origin, upVector, radius, radius, GetSegmentNumber(), 0.0f, 0);
	}
	
	public void MakeCircle (Vector3 origin, Vector3 upVector, float radius, int segments) {
		MakeEllipse (origin, upVector, radius, radius, segments, 0.0f, 0);
	}

	public void MakeCircle (Vector3 origin, Vector3 upVector, float radius, int segments, float pointRotation) {
		MakeEllipse (origin, upVector, radius, radius, segments, pointRotation, 0);
	}

	public void MakeCircle (Vector3 origin, Vector3 upVector, float radius, int segments, int index) {
		MakeEllipse (origin, upVector, radius, radius, segments, 0.0f, index);
	}

	public void MakeCircle (Vector3 origin, Vector3 upVector, float radius, int segments, float pointRotation, int index) {
		MakeEllipse (origin, upVector, radius, radius, segments, pointRotation, index);
	}

	public void MakeEllipse (Vector3 origin, float xRadius, float yRadius) {
		MakeEllipse (origin, Vector3.forward, xRadius, yRadius, GetSegmentNumber(), 0.0f, 0);
	}
	
	public void MakeEllipse (Vector3 origin, float xRadius, float yRadius, int segments) {
		MakeEllipse (origin, Vector3.forward, xRadius, yRadius, segments, 0.0f, 0);
	}
	
	public void MakeEllipse (Vector3 origin, float xRadius, float yRadius, int segments, int index) {
		MakeEllipse (origin, Vector3.forward, xRadius, yRadius, segments, 0.0f, index);
	}

	public void MakeEllipse (Vector3 origin, float xRadius, float yRadius, int segments, float pointRotation) {
		MakeEllipse (origin, Vector3.forward, xRadius, yRadius, segments, pointRotation, 0);
	}

	public void MakeEllipse (Vector3 origin, Vector3 upVector, float xRadius, float yRadius) {
		MakeEllipse (origin, upVector, xRadius, yRadius, GetSegmentNumber(), 0.0f, 0);
	}

	public void MakeEllipse (Vector3 origin, Vector3 upVector, float xRadius, float yRadius, int segments) {
		MakeEllipse (origin, upVector, xRadius, yRadius, segments, 0.0f, 0);
	}
	
	public void MakeEllipse (Vector3 origin, Vector3 upVector, float xRadius, float yRadius, int segments, int index) {
		MakeEllipse (origin, upVector, xRadius, yRadius, segments, 0.0f, index);
	}

	public void MakeEllipse (Vector3 origin, Vector3 upVector, float xRadius, float yRadius, int segments, float pointRotation) {
		MakeEllipse (origin, upVector, xRadius, yRadius, segments, pointRotation, 0);
	}
	
	public void MakeEllipse (Vector3 origin, Vector3 upVector, float xRadius, float yRadius, int segments, float pointRotation, int index) {
		if (segments < 3) {
			LogError ("VectorLine.MakeEllipse needs at least 3 segments");
			return;
		}
		if (!CheckArrayLength (FunctionName.MakeEllipse, segments, index)) {
			return;
		}
		
		float radians = 360.0f / segments*Mathf.Deg2Rad;
		float p = -pointRotation*Mathf.Deg2Rad;
		
		if (m_continuous) {
			int i = 0;
			if (m_is2D) {
				Vector2 v2Origin = origin;
				for (i = 0; i < segments; i++) {
					points2[index+i] = v2Origin + new Vector2(.5f + Mathf.Cos(p)*xRadius, .5f + Mathf.Sin(p)*yRadius);
					p += radians;
				}
				if (!m_isPoints) {
					points2[index+i] = points2[index+(i-segments)];
				}
			}
			else {
				var thisMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(-upVector, upVector), Vector3.one);
				for (i = 0; i < segments; i++) {
					points3[index+i] = origin + thisMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(p)*xRadius, Mathf.Sin(p)*yRadius, 0.0f));
					p += radians;
				}
				if (!m_isPoints) {
					points3[index+i] = points3[index+(i-segments)];
				}
			}
		}
		
		else {
			if (m_is2D) {
				Vector2 v2Origin = origin;
				for (int i = 0; i < segments*2; i++) {
					points2[index+i] = v2Origin + new Vector2(.5f + Mathf.Cos(p)*xRadius, .5f + Mathf.Sin(p)*yRadius);
					p += radians;
					i++;
					points2[index+i] = v2Origin + new Vector2(.5f + Mathf.Cos(p)*xRadius, .5f + Mathf.Sin(p)*yRadius);
				}
			}
			else {
				var thisMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(-upVector, upVector), Vector3.one);
				for (int i = 0; i < segments*2; i++) {
					points3[index+i] = origin + thisMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(p)*xRadius, Mathf.Sin(p)*yRadius, 0.0f));
					p += radians;
					i++;
					points3[index+i] = origin + thisMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(p)*xRadius, Mathf.Sin(p)*yRadius, 0.0f));
				}
			}
		}
	}

	public void MakeCurve (Vector2[] curvePoints) {
		MakeCurve (curvePoints, GetSegmentNumber(), 0);
	}
	
	public void MakeCurve (Vector2[] curvePoints, int segments) {
		MakeCurve (curvePoints, segments, 0);
	}

	public void MakeCurve (Vector2[] curvePoints, int segments, int index) {
		if (curvePoints.Length != 4) {
			LogError ("VectorLine.MakeCurve needs exactly 4 points in the curve points array");
			return;
		}
		MakeCurve (curvePoints[0], curvePoints[1], curvePoints[2], curvePoints[3], segments, index);
	}

	public void MakeCurve (Vector3[] curvePoints) {
		MakeCurve (curvePoints, GetSegmentNumber(), 0);
	}
	
	public void MakeCurve (Vector3[] curvePoints, int segments) {
		MakeCurve (curvePoints, segments, 0);
	}
	
	public void MakeCurve (Vector3[] curvePoints, int segments, int index) {
		if (curvePoints.Length != 4) {
			LogError ("VectorLine.MakeCurve needs exactly 4 points in the curve points array");
			return;
		}
		MakeCurve (curvePoints[0], curvePoints[1], curvePoints[2], curvePoints[3], segments, index);
	}

	public void MakeCurve (Vector3 anchor1, Vector3 control1, Vector3 anchor2, Vector3 control2) {
		MakeCurve (anchor1, control1, anchor2, control2, GetSegmentNumber(), 0);
	}
	
	public void MakeCurve (Vector3 anchor1, Vector3 control1, Vector3 anchor2, Vector3 control2, int segments) {
		MakeCurve (anchor1, control1, anchor2, control2, segments, 0);
	}
	
	public void MakeCurve (Vector3 anchor1, Vector3 control1, Vector3 anchor2, Vector3 control2, int segments, int index) {
		if (!CheckArrayLength (FunctionName.MakeCurve, segments, index)) {
			return;
		}
		
		if (m_continuous) {
			int end = m_isPoints? segments : segments+1;
			if (m_is2D) {
				Vector2 anchor1a = anchor1; Vector2 anchor2a = anchor2;
				Vector2 control1a = control1; Vector2 control2a = control2;
				for (int i = 0; i < end; i++) {
					points2[index+i] = GetBezierPoint (ref anchor1a, ref control1a, ref anchor2a, ref control2a, (float)i/segments);
				}
			}
			else {
				for (int i = 0; i < end; i++) {
					points3[index+i] = GetBezierPoint3D (ref anchor1, ref control1, ref anchor2, ref control2, (float)i/segments);
				}
			}
		}
		
		else {
			int idx = 0;
			if (m_is2D) {
				Vector2 anchor1a = anchor1; Vector2 anchor2a = anchor2;
				Vector2 control1a = control1; Vector2 control2a = control2;
				for (int i = 0; i < segments; i++) {
					points2[index + idx++] = GetBezierPoint (ref anchor1a, ref control1a, ref anchor2a, ref control2a, (float)i/segments);
					points2[index + idx++] = GetBezierPoint (ref anchor1a, ref control1a, ref anchor2a, ref control2a, (float)(i+1)/segments);
				}
			}
			else {
				for (int i = 0; i < segments; i++) {
					points3[index + idx++] = GetBezierPoint3D (ref anchor1, ref control1, ref anchor2, ref control2, (float)i/segments);
					points3[index + idx++] = GetBezierPoint3D (ref anchor1, ref control1, ref anchor2, ref control2, (float)(i+1)/segments);
				}
			}
		}
	}
	
	private static Vector2 GetBezierPoint (ref Vector2 anchor1, ref Vector2 control1, ref Vector2 anchor2, ref Vector2 control2, float t) {
		float cx = 3 * (control1.x - anchor1.x);
		float bx = 3 * (control2.x - control1.x) - cx;
		float ax = anchor2.x - anchor1.x - cx - bx;
		float cy = 3 * (control1.y - anchor1.y);
		float by = 3 * (control2.y - control1.y) - cy;
		float ay = anchor2.y - anchor1.y - cy - by;
		
		return new Vector2( (ax * (t*t*t)) + (bx * (t*t)) + (cx * t) + anchor1.x,
						    (ay * (t*t*t)) + (by * (t*t)) + (cy * t) + anchor1.y );
	}

	private static Vector3 GetBezierPoint3D (ref Vector3 anchor1, ref Vector3 control1, ref Vector3 anchor2, ref Vector3 control2, float t) {
		float cx = 3 * (control1.x - anchor1.x);
		float bx = 3 * (control2.x - control1.x) - cx;
		float ax = anchor2.x - anchor1.x - cx - bx;
		float cy = 3 * (control1.y - anchor1.y);
		float by = 3 * (control2.y - control1.y) - cy;
		float ay = anchor2.y - anchor1.y - cy - by;
		float cz = 3 * (control1.z - anchor1.z);
		float bz = 3 * (control2.z - control1.z) - cz;
		float az = anchor2.z - anchor1.z - cz - bz;
		
		return new Vector3( (ax * (t*t*t)) + (bx * (t*t)) + (cx * t) + anchor1.x,
							(ay * (t*t*t)) + (by * (t*t)) + (cy * t) + anchor1.y,
							(az * (t*t*t)) + (bz * (t*t)) + (cz * t) + anchor1.z );
	}

	public void MakeSpline (Vector2[] splinePoints) {
		MakeSpline (splinePoints, null, GetSegmentNumber(), 0, false);
	}

	public void MakeSpline (Vector2[] splinePoints, bool loop) {
		MakeSpline (splinePoints, null, GetSegmentNumber(), 0, loop);
	}
	
	public void MakeSpline (Vector2[] splinePoints, int segments) {
		MakeSpline (splinePoints, null, segments, 0, false);
	}

	public void MakeSpline (Vector2[] splinePoints, int segments, bool loop) {
		MakeSpline (splinePoints, null, segments, 0, loop);
	}

	public void MakeSpline (Vector2[] splinePoints, int segments, int index) {
		MakeSpline (splinePoints, null, segments, index, false);
	}

	public void MakeSpline (Vector2[] splinePoints, int segments, int index, bool loop) {
		MakeSpline (splinePoints, null, segments, index, loop);
	}

	public void MakeSpline (Vector3[] splinePoints) {
		MakeSpline (null, splinePoints, GetSegmentNumber(), 0, false);
	}

	public void MakeSpline (Vector3[] splinePoints, bool loop) {
		MakeSpline (null, splinePoints, GetSegmentNumber(), 0, loop);
	}
	
	public void MakeSpline (Vector3[] splinePoints, int segments) {
		MakeSpline (null, splinePoints, segments, 0, false);
	}

	public void MakeSpline (Vector3[] splinePoints, int segments, bool loop) {
		MakeSpline (null, splinePoints, segments, 0, loop);
	}

	public void MakeSpline (Vector3[] splinePoints, int segments, int index) {
		MakeSpline (null, splinePoints, segments, index, false);
	}

	public void MakeSpline (Vector3[] splinePoints, int segments, int index, bool loop) {
		MakeSpline (null, splinePoints, segments, index, loop);
	}
		
	private void MakeSpline (Vector2[] splinePoints2, Vector3[] splinePoints3, int segments, int index, bool loop) {
		int pointsLength = (splinePoints2 != null)? splinePoints2.Length : splinePoints3.Length;		
		if (pointsLength < 2) {
			LogError ("VectorLine.MakeSpline needs at least 2 spline points");
			return;
		}
		if (splinePoints2 != null && !m_is2D) {
			LogError ("VectorLine.MakeSpline was called with a Vector2 spline points array, but the line uses Vector3 points");
			return;
		}
		if (splinePoints3 != null && m_is2D) {
			LogError ("VectorLine.MakeSpline was called with a Vector3 spline points array, but the line uses Vector2 points");
			return;
		}
		if (!CheckArrayLength (FunctionName.MakeSpline, segments, index)) {
			return;
		}

		var pointCount = index;
		var numberOfPoints = loop? pointsLength : pointsLength-1;
		var add = 1.0f / segments * numberOfPoints;
		float i, start = 0.0f;
		int j, p0 = 0, p2 = 0, p3 = 0;
		
		for (j = 0; j < numberOfPoints; j++) {
			p0 = j-1;
			p2 = j+1;
			p3 = j+2;
			if (p0 < 0) {
				p0 = loop? numberOfPoints-1 : 0;
			}
			if (loop && p2 > numberOfPoints-1) {
				p2 -= numberOfPoints;
			}
			if (p3 > numberOfPoints-1) {
				p3 = loop? p3-numberOfPoints : numberOfPoints;
			}
			if (m_continuous) {
				if (m_is2D) {
					for (i = start; i <= 1.0f; i += add) {
						points2[pointCount++] = GetSplinePoint (ref splinePoints2[p0], ref splinePoints2[j], ref splinePoints2[p2], ref splinePoints2[p3], i);
					}
				}
				else {
					for (i = start; i <= 1.0f; i += add) {
						points3[pointCount++] = GetSplinePoint3D (ref splinePoints3[p0], ref splinePoints3[j], ref splinePoints3[p2], ref splinePoints3[p3], i);
					}
				}
			}
			else {
				if (m_is2D) {
					for (i = start; i <= 1.0f; i += add) {
						points2[pointCount++] = GetSplinePoint (ref splinePoints2[p0], ref splinePoints2[j], ref splinePoints2[p2], ref splinePoints2[p3], i);
						if (pointCount > index+1 && pointCount < index + (segments*2)) {
							points2[pointCount++] = points2[pointCount-2];
						}
					}
				}
				else {
					for (i = start; i <= 1.0f; i += add) {
						points3[pointCount++] = GetSplinePoint3D (ref splinePoints3[p0], ref splinePoints3[j], ref splinePoints3[p2], ref splinePoints3[p3], i);
						if (pointCount > index+1 && pointCount < index + (segments*2)) {
							points3[pointCount++] = points3[pointCount-2];
						}
					}
				}
			}
			start = i - 1.0f;
		}
		// The last point might not get done depending on number of splinePoints and segments, so ensure that it's done here
		if ( (m_continuous && pointCount < index + (segments+1)) || (!m_continuous && pointCount < index + (segments*2)) ) {
			if (m_is2D) {
				points2[pointCount] = GetSplinePoint (ref splinePoints2[p0], ref splinePoints2[j-1], ref splinePoints2[p2], ref splinePoints2[p3], 1.0f);
			}
			else {
				points3[pointCount] = GetSplinePoint3D (ref splinePoints3[p0], ref splinePoints3[j-1], ref splinePoints3[p2], ref splinePoints3[p3], 1.0f);
			}
		}
	}

	private static Vector2 GetSplinePoint (ref Vector2 p0, ref Vector2 p1, ref Vector2 p2, ref Vector2 p3, float t) {
		float t2 = t*t;
		float t3 = t2*t;
		return new Vector2 (0.5f * ((2.0f*p1.x) + (-p0.x + p2.x)*t + (2.0f*p0.x - 5.0f*p1.x + 4.0f*p2.x - p3.x)*t2 + (-p0.x + 3.0f*p1.x- 3.0f*p2.x + p3.x)*t3),
							0.5f * ((2.0f*p1.y) + (-p0.y + p2.y)*t + (2.0f*p0.y - 5.0f*p1.y + 4.0f*p2.y - p3.y)*t2 + (-p0.y + 3.0f*p1.y- 3.0f*p2.y + p3.y)*t3));
	}
	
	private static Vector3 GetSplinePoint3D (ref Vector3 p0, ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, float t) {
		float t2 = t*t;
		float t3 = t2*t;
		return new Vector3 (0.5f * ((2.0f*p1.x) + (-p0.x + p2.x)*t + (2.0f*p0.x - 5.0f*p1.x + 4.0f*p2.x - p3.x)*t2 + (-p0.x + 3.0f*p1.x- 3.0f*p2.x + p3.x)*t3),
							0.5f * ((2.0f*p1.y) + (-p0.y + p2.y)*t + (2.0f*p0.y - 5.0f*p1.y + 4.0f*p2.y - p3.y)*t2 + (-p0.y + 3.0f*p1.y- 3.0f*p2.y + p3.y)*t3),
							0.5f * ((2.0f*p1.z) + (-p0.z + p2.z)*t + (2.0f*p0.z - 5.0f*p1.z + 4.0f*p2.z - p3.z)*t2 + (-p0.z + 3.0f*p1.z- 3.0f*p2.z + p3.z)*t3));
	}
	
	public void MakeText (string text, Vector3 startPos, float size) {
		MakeText (text, startPos, size, 1.0f, 1.5f, true);
	}
	
	public void MakeText (string text, Vector3 startPos, float size, bool uppercaseOnly) {
		MakeText (text, startPos, size, 1.0f, 1.5f, uppercaseOnly);
	}
	
	public void MakeText (string text, Vector3 startPos, float size, float charSpacing, float lineSpacing) {
		MakeText (text, startPos, size, charSpacing, lineSpacing, true);
	}
	
	public void MakeText (string text, Vector3 startPos, float size, float charSpacing, float lineSpacing, bool uppercaseOnly) {
		if (m_continuous) {
			LogError ("VectorLine.MakeText can only be used with a discrete line");
			return;
		}
		int charPointsLength = 0;
		
		// Get total number of points needed for all characters in the string
		for (int i = 0; i < text.Length; i++) {
			int charNum = System.Convert.ToInt32(text[i]);
			if (charNum < 0 || charNum > VectorChar.numberOfCharacters) {
				LogError ("VectorLine.MakeText: Character '" + text[i] + "' is not valid");
				return;
			}
			if (uppercaseOnly && charNum >= 97 && charNum <= 122) {
				charNum -= 32;
			}
			if (VectorChar.data[charNum] != null) {
				charPointsLength += VectorChar.data[charNum].Length;
			}
		}
		if (charPointsLength > pointsLength) {
			Resize (charPointsLength);
		}
		else if (charPointsLength < pointsLength) {
			ZeroPoints (charPointsLength);
		}
		
		float charPos = 0.0f, linePos = 0.0f;
		int idx = 0;
		var scaleVector = new Vector2(size, size);

		for (int i = 0; i < text.Length; i++) {
			int charNum = System.Convert.ToInt32(text[i]);
			// Newline
			if (charNum == 10) {
				linePos -= lineSpacing;
				charPos = 0.0f;
			}
			// Space
			else if (charNum == 32) {
				charPos += charSpacing;
			}
			// Character
			else {
				if (uppercaseOnly && charNum >= 97 && charNum <= 122) {
					charNum -= 32;
				}
				int end = 0;
				if (VectorChar.data[charNum] != null) {
					end = VectorChar.data[charNum].Length;
				}
				else {
					charPos += charSpacing;
					continue;
				}
				if (m_is2D) {
					for (int j = 0; j < end; j++) {
						points2[idx++] = Vector2.Scale(VectorChar.data[charNum][j] + new Vector2(charPos, linePos), scaleVector) + (Vector2)startPos;
					}
				}
				else {
					for (int j = 0; j < end; j++) {
						points3[idx++] = Vector3.Scale((Vector3)VectorChar.data[charNum][j] + new Vector3(charPos, linePos, 0.0f), scaleVector) + startPos;
					}
				}
				charPos += charSpacing;
			}
		}
	}
	
	public void MakeWireframe (Mesh mesh) {
		if (m_continuous) {
			LogError ("VectorLine.MakeWireframe only works with a discrete line");
			return;
		}
		if (m_is2D) {
			LogError ("VectorLine.MakeWireframe can only be used with a Vector3 array, which \"" + name + "\" doesn't have");
			return;
		}
		if (mesh == null) {
			LogError ("VectorLine.MakeWireframe can't use a null mesh");
			return;
		}
		var meshTris = mesh.triangles;
		var meshVertices = mesh.vertices;
		var pairs = new Dictionary<Vector3Pair, bool>();
		var linePoints = new List<Vector3>();
		
		for (int i = 0; i < meshTris.Length; i += 3) {
			CheckPairPoints (pairs, meshVertices[meshTris[i]],   meshVertices[meshTris[i+1]], linePoints);
			CheckPairPoints (pairs, meshVertices[meshTris[i+1]], meshVertices[meshTris[i+2]], linePoints);
			CheckPairPoints (pairs, meshVertices[meshTris[i+2]], meshVertices[meshTris[i]],   linePoints);
		}
		
		if (linePoints.Count > points3.Length) {
			System.Array.Resize (ref points3, linePoints.Count);
			Resize (linePoints.Count);
		}
		else if (linePoints.Count < points3.Length) {
			ZeroPoints (linePoints.Count);
		}
		System.Array.Copy (linePoints.ToArray(), points3, linePoints.Count);
	}

	private static void CheckPairPoints (Dictionary<Vector3Pair, bool> pairs, Vector3 p1, Vector3 p2, List<Vector3> linePoints) {
		var pair1 = new Vector3Pair(p1, p2);
		var pair2 = new Vector3Pair(p2, p1);
		if (!pairs.ContainsKey(pair1) && !pairs.ContainsKey(pair2)) {
			pairs[pair1] = true;
			pairs[pair2] = true;
			linePoints.Add(p1);
			linePoints.Add(p2);
		}
	}
	
	public void MakeCube (Vector3 position, float xSize, float ySize, float zSize) {
		MakeCube (position, xSize, ySize, zSize, 0);
	}
	
	public void MakeCube (Vector3 position, float xSize, float ySize, float zSize, int index) {
		if (m_continuous) {
			LogError ("VectorLine.MakeCube only works with a discrete line");
			return;
		}
		if (m_is2D) {
			LogError ("VectorLine.MakeCube can only be used with a Vector3 array, which \"" + name + "\" doesn't have");
			return;
		}
		if (index + 24 > points3.Length) {
			if (index == 0) {
				LogError ("VectorLine.MakeCube: The length of the Vector3 array needs to be at least 24 for \"" + name + "\"");
				return;
			}
			LogError ("Calling VectorLine.MakeCube with an index of " + index + " would exceed the length of the Vector3 array for \"" + name + "\"");
			return;
		}
		
		xSize /= 2;
		ySize /= 2;
		zSize /= 2;
		// Top
		points3[index   ] = position + new Vector3(-xSize, ySize, -zSize);
		points3[index+1 ] = position + new Vector3(xSize, ySize, -zSize);
		points3[index+2 ] = position + new Vector3(xSize, ySize, -zSize);
		points3[index+3 ] = position + new Vector3(xSize, ySize, zSize);
		points3[index+4 ] = position + new Vector3(xSize, ySize, zSize);
		points3[index+5 ] = position + new Vector3(-xSize, ySize, zSize);
		points3[index+6 ] = position + new Vector3(-xSize, ySize, zSize);
		points3[index+7 ] = position + new Vector3(-xSize, ySize, -zSize);
		// Middle
		points3[index+8 ] = position + new Vector3(-xSize, -ySize, -zSize);
		points3[index+9 ] = position + new Vector3(-xSize, ySize, -zSize);
		points3[index+10] = position + new Vector3(xSize, -ySize, -zSize);
		points3[index+11] = position + new Vector3(xSize, ySize, -zSize);
		points3[index+12] = position + new Vector3(-xSize, -ySize, zSize);
		points3[index+13] = position + new Vector3(-xSize, ySize, zSize);
		points3[index+14] = position + new Vector3(xSize, -ySize, zSize);
		points3[index+15] = position + new Vector3(xSize, ySize, zSize);
		// Bottom
		points3[index+16] = position + new Vector3(-xSize, -ySize, -zSize);
		points3[index+17] = position + new Vector3(xSize, -ySize, -zSize);
		points3[index+18] = position + new Vector3(xSize, -ySize, -zSize);
		points3[index+19] = position + new Vector3(xSize, -ySize, zSize);
		points3[index+20] = position + new Vector3(xSize, -ySize, zSize);
		points3[index+21] = position + new Vector3(-xSize, -ySize, zSize);
		points3[index+22] = position + new Vector3(-xSize, -ySize, zSize);
		points3[index+23] = position + new Vector3(-xSize, -ySize, -zSize);
	}

	public void SetDistances () {
		if (m_distances == null || m_distances.Length != (m_continuous? m_pointsLength : m_pointsLength/2 + 1)) {
			m_distances = new float[m_continuous? m_pointsLength : m_pointsLength/2 + 1];
		}

		var totalDistance = 0.0d;
		int thisPointsLength = pointsLength-1;
		
		if (points3 != null) {
			if (m_continuous) {
				for (int i = 0; i < thisPointsLength; i++) {
					Vector3 diff = points3[i] - points3[i+1];
					totalDistance += System.Math.Sqrt (diff.x*diff.x + diff.y*diff.y + diff.z*diff.z); // Same as Vector3.Distance, but with double instead of float
					m_distances[i+1] = (float)totalDistance;
				}
			}
			else {
				int count = 1;
				for (int i = 0; i < thisPointsLength; i += 2) {
					Vector3 diff = points3[i] - points3[i+1];
					totalDistance += System.Math.Sqrt (diff.x*diff.x + diff.y*diff.y + diff.z*diff.z);
					m_distances[count++] = (float)totalDistance;
				}
			}
		}
		else {
			if (m_continuous) {
				for (int i = 0; i < thisPointsLength; i++) {
					Vector2 diff = points2[i] - points2[i+1];
					totalDistance += System.Math.Sqrt (diff.x*diff.x + diff.y*diff.y); // Same as Vector2.Distance, but with double instead of float
					m_distances[i+1] = (float)totalDistance;
				}
			}
			else {
				int count = 1;
				for (int i = 0; i < thisPointsLength; i += 2) {
					Vector2 diff = points2[i] - points2[i+1];
					totalDistance += System.Math.Sqrt (diff.x*diff.x + diff.y*diff.y);
					m_distances[count++] = (float)totalDistance;
				}
			}
		}
	}
	
	public float GetLength () {
		if (m_distances == null || m_distances.Length != (m_continuous? pointsLength : pointsLength/2 + 1)) {
			SetDistances();
		}
		return m_distances[m_distances.Length-1];
	}

	public Vector2 GetPoint01 (float distance) {
		return GetPoint (Mathf.Lerp(0.0f, GetLength(), distance) );
	}

	public Vector2 GetPoint (float distance) {
		if (!m_is2D) {
			LogError ("VectorLine.GetPoint only works with Vector2 points");
			return Vector2.zero;
		}
		if (points2.Length < 2) {
			LogError ("VectorLine.GetPoint needs at least 2 points in the points2 array");
			return Vector2.zero;
		}
		if (m_distances == null) {
			SetDistances();
		}
		int i = m_drawStart + 1;
		if (!m_continuous) {
			i++;
			i /= 2;
		}
		if (i >= m_distances.Length) {
			i = m_distances.Length - 1;
		}
		int end = m_continuous? m_drawEnd : (m_drawEnd + 1) / 2;
		while (distance > m_distances[i] && i < end) {
			i++;
		}
		if (m_continuous) {
			return Vector2.Lerp(points2[i-1], points2[i], Mathf.InverseLerp(m_distances[i-1], m_distances[i], distance));
		}
		return Vector2.Lerp(points2[(i-1)*2], points2[(i-1)*2+1], Mathf.InverseLerp(m_distances[i-1], m_distances[i], distance));
	}

	public Vector3 GetPoint3D01 (float distance) {
		return GetPoint3D (Mathf.Lerp(0.0f, GetLength(), distance) );
	}
	
	public Vector3 GetPoint3D (float distance) {
		if (m_is2D) {
			LogError ("VectorLine.GetPoint3D only works with Vector3 points");
			return Vector3.zero;
		}
		if (points3.Length < 2) {
			LogError ("VectorLine.GetPoint3D needs at least 2 points in the points3 array");
			return Vector3.zero;
		}
		if (m_distances == null) {
			SetDistances();
		}
		int i = m_drawStart + 1;
		if (!m_continuous) {
			i++;
			i /= 2;
		}
		if (i >= m_distances.Length) {
			i = m_distances.Length - 1;
		}
		int end = m_continuous? m_drawEnd : (m_drawEnd + 1) / 2;
		while (distance > m_distances[i] && i < end) {
			i++;
		}
		if (m_continuous) {
			return Vector3.Lerp (points3[i-1], points3[i], Mathf.InverseLerp (m_distances[i-1], m_distances[i], distance));			
		}
		return Vector3.Lerp (points3[(i-1)*2], points3[(i-1)*2+1], Mathf.InverseLerp (m_distances[i-1], m_distances[i], distance));
	}

	public static void SetEndCap (string name, EndCap capType) {
		SetEndCap (name, capType, null, null);
	}
	
	public static void SetEndCap (string name, EndCap capType, Material material, params Texture2D[] textures) {
		if (capDictionary == null) {
			capDictionary = new Dictionary<string, CapInfo>();
		}
		if (name == null || name == "") {
			LogError ("VectorLine: must supply a name for SetEndCap");
			return;
		}
		if (capDictionary.ContainsKey (name) && capType != EndCap.None) {
			LogError ("VectorLine: end cap \"" + name + "\" has already been set up");
			return;
		}
		
		if (capType == EndCap.Both) {
			if (textures.Length < 2) {
				LogError ("VectorLine: must supply two textures when using SetEndCap with EndCap.Both");
				return;
			}
			if (textures[0].width != textures[1].width || textures[0].height != textures[1].height) {
				LogError ("VectorLine: when using SetEndCap with EndCap.Both, both textures must have the same width and height");
				return;
			}
		}
		if ( (capType == EndCap.Front || capType == EndCap.Back || capType == EndCap.Mirror) && textures.Length < 1) {
			LogError ("VectorLine: must supply a texture when using SetEndCap with EndCap.Front, EndCap.Back, or EndCap.Mirror");
			return;
		}
		if (capType == EndCap.None) {
			if (!capDictionary.ContainsKey (name)) {
				return;
			}
			RemoveEndCap (name);
			return;
		}
		if (material == null) {
			LogError ("VectorLine: must supply a material when using SetEndCap with any EndCap type except EndCap.None");
			return;
		}
		if (!material.HasProperty ("_MainTex")) {
			LogError ("VectorLine: the material supplied when using SetEndCap must contain a shader that has a \"_MainTex\" property");
			return;
		}
		
		int width = textures[0].width;
		int height = textures[0].height;
		float ratio1 = 0.0f, ratio2 = 0.0f;
		Color[] cols1 = null, cols2 = null;
		if (capType == EndCap.Front) {
			cols1 = textures[0].GetPixels();
			cols2 = new Color[width * height];
			ratio1 = textures[0].width / (float)textures[0].height;
		}
		else if (capType == EndCap.Back) {
			cols1 = new Color[width * height];
			cols2 = textures[0].GetPixels();
			ratio2 = textures[0].width / (float)textures[0].height;
		}
		else if (capType == EndCap.Both) {
			cols1 = textures[0].GetPixels();
			cols2 = textures[1].GetPixels();
			ratio1 = textures[0].width / (float)textures[0].height;
			ratio2 = textures[1].width / (float)textures[1].height;
		}
		else if (capType == EndCap.Mirror) {
			cols1 = textures[0].GetPixels();
			cols2 = new Color[width * height];
			ratio1 = textures[0].width / (float)textures[0].height;
			ratio2 = ratio1;
		}
		
		var tex = new Texture2D(width, height*4, TextureFormat.ARGB32, false);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.filterMode = textures[0].filterMode;
		tex.SetPixels (0, 0, width, height, cols1);
		tex.SetPixels (0, height*3, width, height, cols2);
		// Add space to prevent top/bottom textures from potentially bleeding into each other when using bilinear filtering
		tex.SetPixels (0, height, width, height*2, new Color[width * (height*2)]);
		tex.Apply (false, true);
		var capMaterial = (Material)MonoBehaviour.Instantiate(material);
		capMaterial.name = material.name + " EndCap";
		capMaterial.mainTexture = tex;
		
		capDictionary.Add (name, new CapInfo(capType, capMaterial, tex, ratio1, ratio2));
	}
	
	public static void RemoveEndCap (string name) {
		if (!capDictionary.ContainsKey (name)) {
			LogError ("VectorLine: RemoveEndCap: \"" + name + "\" has not been set up");
			return;
		}
		MonoBehaviour.Destroy (capDictionary[name].texture);
		MonoBehaviour.Destroy (capDictionary[name].material);
		capDictionary.Remove (name);
	}

	public void ZeroPoints () {
		ZeroPoints (0, m_pointsLength);
	}
	
	public void ZeroPoints (int startIndex) {
		ZeroPoints (startIndex, m_pointsLength);
	}

	public void ZeroPoints (int startIndex, int endIndex) {
		if (endIndex < 0 || endIndex > pointsLength || startIndex < 0 || startIndex > pointsLength || startIndex > endIndex) {
			LogError ("VectorLine: index out of range for \"" + name + "\" when calling ZeroPoints. StartIndex: " + startIndex + ", EndIndex: " + endIndex + ", array length: " + m_pointsLength);
			return;
		}
		
		if (m_is2D) {
			var v2zero = Vector2.zero;	// Making a local variable is at least twice as fast for some reason
			for (int i = startIndex; i < endIndex; i++) {
				points2[i] = v2zero;
			}
		}
		else {
			var v3zero = Vector3.zero;
			for (int i = startIndex; i < endIndex; i++) {
				points3[i] = v3zero;
			}
		}
	}
	
	private void ZeroVertices (int startIndex, int endIndex) {
		var v3zero = Vector3.zero;
#if !UNITY_3
		if (m_1pixelLine) {
			for (int i = startIndex; i < endIndex; i++) {
				m_lineVertices[i] = v3zero;
			}
			return;
		}
#endif
		if (m_continuous) {
			startIndex *= 4;
			endIndex *= 4;
			if (endIndex > m_vertexCount) {
				endIndex -= 4;
			}
			for (int i = startIndex; i < endIndex; i += 4) {
				m_lineVertices[i  ] = v3zero;
				m_lineVertices[i+1] = v3zero;
				m_lineVertices[i+2] = v3zero;
				m_lineVertices[i+3] = v3zero;
			}
		}
		else {
			startIndex *= 2;
			endIndex *= 2;
			for (int i = startIndex; i < endIndex; i += 2) {
				m_lineVertices[i  ] = v3zero;
				m_lineVertices[i+1] = v3zero;
			}
		}
	}

	public bool Selected (Vector2 p) {
		int temp;
		return Selected (p, 0, out temp);
	}

	public bool Selected (Vector2 p, out int index) {
		return Selected (p, 0, out index);
	}
	
	public bool Selected (Vector2 p, int extraDistance, out int index) {
		int wAdd = m_lineWidths.Length == 1? 0 : 1;
		int wIdx = m_continuous? m_drawStart - wAdd : m_drawStart/2 - wAdd;
		int end = m_drawEnd;
		var useTransformMatrix = (m_useTransform == null)? false : true;
		var thisMatrix = useTransformMatrix? m_useTransform.localToWorldMatrix : Matrix4x4.identity;
		
		if (m_isPoints) {
			if (end == pointsLength) {
				end--;
			}
			Vector2 thisPoint;
			
			if (m_is2D) {
				for (int i = m_drawStart; i <= end; i++) {
					wIdx += wAdd;
					float size = m_lineWidths[wIdx] + extraDistance;
					if (useTransformMatrix) {
						thisPoint = thisMatrix.MultiplyPoint3x4 (points2[i]);
					}
					else {
						thisPoint = points2[i];
					}
					if (p.x >= thisPoint.x - size && p.x <= thisPoint.x + size && p.y >= thisPoint.y - size && p.y <= thisPoint.y + size) {
						index = i;
						return true;
					}
				}
				index = -1;
				return false;
			}
			
			for (int i = m_drawStart; i <= end; i++) {
				wIdx += wAdd;
				float size = m_lineWidths[wIdx] + extraDistance;
				thisPoint = useTransformMatrix? cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i])) : cam3D.WorldToScreenPoint (points3[i]);
				if (p.x >= thisPoint.x - size && p.x <= thisPoint.x + size && p.y >= thisPoint.y - size && p.y <= thisPoint.y + size) {
					index = i;
					return true;
				}
			}
			index = -1;
			return false;
		}
		
		float t = 0.0f;
		int add = m_continuous? 1 : 2;
		Vector2 p1, p2 = Vector2.zero;
		if (m_continuous && m_drawEnd == pointsLength) {
			end--;
		}
		
		if (m_is2D) {
			for (int i = m_drawStart; i < end; i += add) {
				wIdx += wAdd;
				if (points2[i].x == points2[i+1].x && points2[i].y == points2[i+1].y) {
					continue;
				}
				if (useTransformMatrix) {
					p1 = thisMatrix.MultiplyPoint3x4 (points2[i]);
					p2 = thisMatrix.MultiplyPoint3x4 (points2[i+1]);
				}
				else {
					p1 = points2[i];
					p2 = points2[i+1];
				}
				
				// Do nothing if the point is beyond the line segment end points
				t = Vector2.Dot(p - p1, p2 - p1) / (p2 - p1).sqrMagnitude;
				if (t < 0.0f || t > 1.0f) {
					continue;
				}
				
				// If the distance of the point to the line is <= the line width
				if ((p - (p1 + t * (p2 - p1))).sqrMagnitude <= (m_lineWidths[wIdx] + extraDistance) * (m_lineWidths[wIdx] + extraDistance)) {
					index = m_continuous? i : i/2;
					return true;
				}
			}
			index = -1;
			return false;
		}
		
		Vector3 screenPoint1, screenPoint2 = Vector3.zero;
		for (int i = m_drawStart; i < end; i += add) {
			wIdx += wAdd;
			if (points3[i].x == points3[i+1].x && points3[i].y == points3[i+1].y && points3[i].z == points3[i+1].z) {
				continue;
			}
			if (useTransformMatrix) {
				screenPoint1 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i]));
				screenPoint2 = cam3D.WorldToScreenPoint (thisMatrix.MultiplyPoint3x4 (points3[i+1]));
			}
			else {
				screenPoint1 = cam3D.WorldToScreenPoint (points3[i]);
				screenPoint2 = cam3D.WorldToScreenPoint (points3[i+1]);
			}
			if (screenPoint1.z < cutoff || screenPoint2.z < cutoff) {
				continue;
			}
			p1.x = (int)screenPoint1.x; p2.x = (int)screenPoint2.x;
			p1.y = (int)screenPoint1.y; p2.y = (int)screenPoint2.y;
			if (p1.x == p2.x && p1.y == p2.y) {
				continue;
			}
			
			// Do nothing if the point is beyond the line segment end points
			t = Vector2.Dot (p - p1, p2 - p1) / (p2 - p1).sqrMagnitude;
			if (t < 0.0f || t > 1.0f) {
				continue;
			}
			
			// If the distance of the point to the line is <= the line width
			if ((p - (p1 + t * (p2 - p1))).sqrMagnitude <= (m_lineWidths[wIdx] + extraDistance) * (m_lineWidths[wIdx] + extraDistance)) {
				index = m_continuous? i : i/2;
				return true;
			}
		}
		index = -1;
		return false;
	}
	
	bool Approximately2 (Vector2 p1, Vector2 p2) {
		return Approximately(p1.x, p2.x) && Approximately(p1.y, p2.y);
	}

	bool Approximately3 (Vector3 p1, Vector3 p2) {
		return Approximately(p1.x, p2.x) && Approximately(p1.y, p2.y) && Approximately(p1.z, p2.z);
	}
	
	bool Approximately (float a, float b) {
		return Mathf.Round(a*100)/100 == Mathf.Round(b*100)/100;
	}
	
	public static string Version () {
		return "Vectrosity version 2.3";
	}
}

public class VectorPoints : VectorLine {
	public VectorPoints (string name, Vector2[] points, Material material, float width) : base (true, name, points, material, width) {}
	public VectorPoints (string name, Vector2[] points, Color[] colors, Material material, float width) : base (true, name, points, colors, material, width) {}
	public VectorPoints (string name, Vector2[] points, Color color, Material material, float width) : base (true, name, points, color, material, width) {}

	public VectorPoints (string name, Vector3[] points, Material material, float width) : base (true, name, points, material, width) {}
	public VectorPoints (string name, Vector3[] points, Color[] colors, Material material, float width) : base (true, name, points, colors, material, width) {}
	public VectorPoints (string name, Vector3[] points, Color color, Material material, float width) : base (true, name, points, color, material, width) {}
}

public struct Vector3Pair {
	public Vector3 p1;
	public Vector3 p2;
	public Vector3Pair (Vector3 point1, Vector3 point2) {
		p1 = point1;
		p2 = point2;
	}
}

public class CapInfo {
	public EndCap capType;
	public Material material;
	public Texture2D texture;
	public float ratio1;
	public float ratio2;
	
	public CapInfo (EndCap capType, Material material, Texture2D texture, float ratio1, float ratio2) {
		this.capType = capType;
		this.material = material;
		this.texture = texture;
		this.ratio1 = ratio1;
		this.ratio2 = ratio2;
	}
}

}