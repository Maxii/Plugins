using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeSimpleEditorWidgets
{


    TechTreeEditorStyles styles;

    public TechTreeSimpleEditorWidgets ()
    {
        styles = new TechTreeEditorStyles ();
    }

    public bool ControlButton (Color c, string tip)
    {
        var back = GUI.backgroundColor;
        GUI.backgroundColor = c;
        var result = GUILayout.Button (new GUIContent ("", tip), styles.controlButton);
        GUI.backgroundColor = back;
        return result;
    }

    public bool ControlToggle (bool flag, Color c, string tip)
    {
        var back = GUI.backgroundColor;
        GUI.backgroundColor = c;
        var result = GUILayout.Toggle (flag, new GUIContent ("", tip), styles.controlToggle);
        GUI.backgroundColor = back;
        return result;
    }
    
    public int ControlPopup (string label, string[] choices)
    {
        var back = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        var selected = 0;
        var items = new List<string> ();
        items.Add (label);
        items.AddRange (choices);
        selected = EditorGUILayout.Popup (selected, items.ToArray ());
        selected -= 1;
        GUI.backgroundColor = back;
        return selected;
    }
    
    public void DrawUsageMessage ()
    {
        GUILayout.BeginHorizontal ("box");
        GUILayout.FlexibleSpace ();
        GUILayout.Label ("Please select a BlueprintModel game object to enable the Editor.");
        GUILayout.FlexibleSpace ();
        GUILayout.EndHorizontal ();
    }

    public void DrawRect (Rect r, Color color, Color internalColor, float expand=0)
    {
        var verts = new Vector3[] {
            new Vector3 (r.xMin - expand - 0, r.yMin - expand, 0),
            new Vector3 (r.xMin - expand - 0, r.yMax + expand+0.5f, 0),
            new Vector3 (r.xMax + expand + 1.5f, r.yMax + expand+0.5f, 0),
            new Vector3 (r.xMax + expand + 1.5f, r.yMin - expand, 0),
        };
        Handles.DrawSolidRectangleWithOutline (verts, internalColor, color);
    }

    public void DrawConnectionCurve (Rect start, Rect end)
    {
        var depth = GUI.depth;
        GUI.depth = depth - 1;
        Vector3 startPos = new Vector3 (start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3 (end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Color shadowCol = new Color (0, 0, 0, 0.06f);
        for (int i = 0; i < 3; i++) // Draw a shadow
            Handles.DrawBezier (startPos, endPos, startTan, endTan, shadowCol, null, (i + 3) * 5);
        Handles.DrawBezier (startPos, endPos, startTan, endTan, Color.white, null, 3);
        
        GUI.depth = depth;
    }

    public void DrawBoxed(System.Action fn) {
        GUILayout.BeginVertical("box");
        fn();
        GUILayout.EndVertical();
    }

    public void DrawRow(System.Action fn) {
        GUILayout.BeginHorizontal();
        fn();
        GUILayout.EndHorizontal();
    }

    public void DrawBoxedRow(System.Action fn) {
        GUILayout.BeginHorizontal("box");
        fn();
        GUILayout.EndHorizontal();
    }

    public bool DrawIDWidget (BlueprintModelAsset asset, SerializedObject so, GUILayoutOption labelWidth, GUILayoutOption widgetWidth, string widgetID=null)
    {
        EditorGUI.BeginChangeCheck ();
        EditorGUILayout.LabelField ("ID", labelWidth);
        var idProp = so.FindProperty ("ID");
        if (widgetID != null) {
            GUI.SetNextControlName (widgetID);
        }
        EditorGUILayout.PropertyField (idProp, GUIContent.none, widgetWidth);
        if (EditorGUI.EndChangeCheck ()) {
            so.ApplyModifiedProperties ();
            BlueprintModelAsset.Rename (asset, idProp.stringValue);
            return true;
        }
        return false;
    }

}


