using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeResourcesEditor : SubEditor
{
    
    public TechTreeResourcesEditor (BlueprintModelEditor editor) : base(editor)
    {
    }

    void DrawHelp() {
        EditorGUILayout.HelpBox("MaxCost is only used to allow you to scale the resource costs in the Costs window.", MessageType.Info);
    }

    public override void Draw ()
    {
        DrawHelp();
        widget.DrawRow (() => {
            GUILayout.Label ("ID", GUILayout.Width (64));
            GUILayout.Label ("Qty", GUILayout.Width (64));
            GUILayout.Label ("MaxCost", GUILayout.Width (64));
            GUILayout.Label ("Regen?", GUILayout.Width (64));
            GUILayout.Label ("Rate", GUILayout.Width (64));
            GUILayout.Label ("Capped?", GUILayout.Width (64));
            GUILayout.Label ("Max", GUILayout.Width (64));
            GUILayout.Label ("Prefab", GUILayout.Width (128));
            GUILayout.Label ("Color", GUILayout.Width (128));
        });
        var deleteId = -1;
        var background = GUI.backgroundColor;
        for (var i=0; i<Component.resources.Count; i++) {
            var r = Component.resources [i];
            widget.DrawBoxedRow (() => {
                GUI.backgroundColor = r.ID == null || r.ID == "" ? styles.MISSING_COLOR : background;
                var so = new SerializedObject (r);
                so.Update ();
                var idWidth = GUILayout.Width (64);
                    
                EditorGUI.BeginChangeCheck ();
                var idProp = so.FindProperty ("ID");
                EditorGUILayout.PropertyField (idProp, GUIContent.none, idWidth);
                if (EditorGUI.EndChangeCheck ()) {
                    so.ApplyModifiedProperties ();
                    BlueprintModelAsset.Rename (r, idProp.stringValue);
                    Component.SetDirty ();
                }
                    
                EditorGUILayout.PropertyField (so.FindProperty ("qty"), GUIContent.none, idWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("maxPossibleCost"), GUIContent.none, idWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("autoReplenish"), GUIContent.none, GUILayout.Width (64));
                GUI.enabled = r.autoReplenish;
                EditorGUILayout.PropertyField (so.FindProperty ("autoReplenishRate"), GUIContent.none, GUILayout.Width (64));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (so.FindProperty ("hasMaximumCapacity"), GUIContent.none, GUILayout.Width (64));
                GUI.enabled = r.hasMaximumCapacity;
                EditorGUILayout.PropertyField (so.FindProperty ("maximumCapacity"), GUIContent.none, GUILayout.Width (64));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (so.FindProperty ("gameObject"), GUIContent.none, GUILayout.Width (128));
                EditorGUILayout.PropertyField (so.FindProperty ("color"), GUIContent.none, GUILayout.Width (128));
                so.ApplyModifiedProperties ();
                GUILayout.Space (10);
                if (widget.ControlButton(styles.DELETE_COLOR, "Delete Resource")) {
                    deleteId = i;
                }
            });
            GUILayout.Space (10);
        }
        GUI.backgroundColor = background;
        if (widget.ControlButton(styles.CREATE_COLOR, "Add a new Resouce")) {
            editor.RunNext(() => {
                Component.resources.Add (BlueprintModelAsset.Create<Resource> (Component));
                Component.SetDirty ();
                editor.needRepaint = true;
                editor.ValidateModel();
            });
        }
        if (deleteId >= 0) {
            var performDelete = true;
            var ID = Component.resources [deleteId].ID;
            if(!(ID == null || ID == "")) {
                performDelete = EditorUtility.DisplayDialog ("This operation can not be undone!", "Are you sure you want to remove this Resource?", "Yes", "Cancel");
            }
            if(performDelete) {
                Component.Delete (Component.resources [deleteId]);
                Component.SetDirty ();
                editor.needRepaint = true;
            }
        }
            
        
    }
}