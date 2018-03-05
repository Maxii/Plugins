using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeCostEditor : SubEditor
{
    Vector2 scroll;

    float maxDuration = 120;
    
    public TechTreeCostEditor (BlueprintModelEditor editor) : base(editor)
    {

    }
    
    public override void Draw ()
    {
        //GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        //var area = GUILayoutUtility.GetLastRect();
        GUILayout.Label("Construction Times and Resource Costs");
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.BeginHorizontal();
        foreach(var i in Component.blueprints) {

            if(!i.group.visible) continue;
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUI.color = styles.TIME_COLOR;
            EditorGUI.BeginChangeCheck();
            var constructTime = GUILayout.VerticalSlider(i.constructTime, maxDuration, 0, styles.graphSlider, styles.graphThumb);
            if(EditorGUI.EndChangeCheck()) {
                var so = new SerializedObject(i);
                so.Update();
                so.FindProperty("constructTime").floatValue = ((int)(constructTime * 10)) / 10f;
                so.ApplyModifiedProperties();
            }
            foreach(var r in i.costs) {
                GUI.color = r.resource.color;
                EditorGUI.BeginChangeCheck();
                var qty = GUILayout.VerticalSlider(r.qty, r.resource.maxPossibleCost, 0, styles.graphSlider, styles.graphThumb);
                if(EditorGUI.EndChangeCheck()) {
                    var cso = new SerializedObject(r);
                    cso.Update();
                    cso.FindProperty("qty").floatValue = ((int)(qty * 10)) / 10f;
                    cso.ApplyModifiedProperties();
                }

            }
            GUILayout.EndHorizontal();
            GUI.color = i.group.color;
            GUILayout.BeginHorizontal(GUILayout.Width(128));
            GUILayout.BeginVertical("box", GUILayout.Height(128));
            GUILayout.Label(i.ID);
            GUILayout.Label("T: " + i.constructTime.ToString());
            foreach(var r in i.costs) {
                GUILayout.Label(r.resource.ID + ": " + r.qty);
            }
            GUILayout.EndVertical();
            GUI.color = styles.NORMAL_COLOR;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView ();
        
    }
    
   
}

