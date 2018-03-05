using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeStatEditor : SubEditor
{
    
    public TechTreeStatEditor (BlueprintModelEditor editor) : base(editor)
    {
    }

    void DrawHelp ()
    {
        //EditorGUILayout.HelpBox ("All Statistics", MessageType.Info);
    }

    public override void Draw ()
    {
        GUILayout.BeginHorizontal ();
        GUILayout.BeginVertical (GUILayout.Width (128));
        DrawStats ();
        GUILayout.EndVertical ();
        GUILayout.Space (10);
        GUILayout.BeginVertical ();
        DrawBlueprints ();
        GUILayout.EndVertical ();
        GUILayout.EndHorizontal ();
    }

    int groupIndex;
    int bpIndex;
    int levelIndex;
    int statIndex;

    public void DrawBlueprints ()
    {
        GUILayout.Label ("Statistics per Blueprint Level");
        GUILayout.BeginHorizontal ("box");
        var groupList = (from i in Component.groups select i.ID).ToArray ();
        var bpList = (from i in Component.blueprints where i.@group == Component.groups [groupIndex] select i.ID).ToArray ();
        if(bpList.Length == 0)
            return;
        var bp = Component.FindBlueprint (bpList [bpIndex]);
        var levelList = (from i in Enumerable.Range (0, bp.upgradeLevels.Count + 1) select i.ToString ()).ToArray ();
        var statList = (from i in Component.stats select i.ID).ToArray ();

        GUILayout.Label ("Group:");
        groupIndex = EditorGUILayout.Popup (groupIndex, groupList);
        GUILayout.Label ("Blueprint");
        bpIndex = EditorGUILayout.Popup (bpIndex, bpList);
        GUILayout.Label ("Level");
        levelIndex = EditorGUILayout.Popup (levelIndex, levelList);


        GUILayout.EndHorizontal ();

        var statValues = (from i in bp.statValues where i.level == levelIndex select i).ToArray ();
        var headWidth = GUILayout.Width (128);
        var colWidth = GUILayout.Width (96);
        GUILayout.Space (10);
        GUILayout.BeginHorizontal ();
        GUILayout.Label ("Statistic", headWidth);
        GUILayout.Label ("Start Value", colWidth);
        GUILayout.Label ("Max Value", colWidth);
        GUILayout.Label ("Regen?", colWidth);
        GUILayout.Label ("Regen Rate", colWidth);
        GUILayout.Label ("Notify If Zero?", colWidth);
        GUILayout.Space (10);
        GUILayout.EndHorizontal ();

        foreach (var statValue in statValues) {
            var so = new SerializedObject (statValue);
            so.Update ();
            if (statValue.stat == null)
                continue;
            GUILayout.BeginHorizontal ("box");
            GUILayout.Label (statValue.stat.ID, headWidth);
            EditorGUILayout.PropertyField (so.FindProperty ("startValue"), GUIContent.none, colWidth);
            EditorGUILayout.PropertyField (so.FindProperty ("maxValue"), GUIContent.none, colWidth);
            EditorGUILayout.PropertyField (so.FindProperty ("regen"), GUIContent.none, colWidth);
            EditorGUILayout.PropertyField (so.FindProperty ("regenRate"), GUIContent.none, colWidth);
            EditorGUILayout.PropertyField (so.FindProperty ("notifyIfZero"), GUIContent.none, colWidth);
            GUILayout.Space (10);
            so.ApplyModifiedProperties ();
            if (widget.ControlButton (styles.DELETE_COLOR, "Delete Stat")) {
                bp.statValues.Remove (statValue);
                BlueprintModelAsset.Remove (statValue);
            }
            GUILayout.EndHorizontal ();

        }
        if (Component.stats.Count > 0) {
            GUILayout.BeginHorizontal ();
            statIndex = EditorGUILayout.Popup (statIndex, statList, headWidth);
            if (statIndex > Component.stats.Count)
                statIndex = 0;
            var statToAdd = Component.stats [statIndex];
            var enabled = GUI.enabled;
            GUI.enabled = (from i in statValues where i.stat == statToAdd select i).Count () == 0;
            if (widget.ControlButton (styles.CREATE_COLOR, "Add Stat")) {
                var newStatValue = BlueprintModelAsset.Create<UnitStatValue> (Component);
                newStatValue.stat = statToAdd;
                newStatValue.level = levelIndex;
                BlueprintModelAsset.Rename (newStatValue, bp.ID + "-" + statToAdd.ID);
                bp.statValues.Add (newStatValue);
                Component.SetDirty ();
                editor.needRepaint = true;
            }
            GUI.enabled = enabled;
            GUILayout.EndHorizontal ();
        }



    }

    public void DrawStats ()
    {
        DrawHelp ();
        GUILayout.Label ("Statistics");
        widget.DrawRow (() => {
            GUILayout.Label ("ID", GUILayout.Width (64));
            GUILayout.Label ("Color", GUILayout.Width (128));
        });
        var deleteId = -1;
        var background = GUI.backgroundColor;
        for (var i=0; i<Component.stats.Count; i++) {
            var r = Component.stats [i];
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
                EditorGUILayout.PropertyField (so.FindProperty ("color"), GUIContent.none, GUILayout.Width (128));
                so.ApplyModifiedProperties ();
                GUILayout.Space (10);
                if (widget.ControlButton (styles.DELETE_COLOR, "Delete Stat")) {
                    deleteId = i;
                }
            });
            GUILayout.Space (10);
        }
        GUI.backgroundColor = background;
        if (widget.ControlButton (styles.CREATE_COLOR, "Add a new Stat")) {
            Component.stats.Add (BlueprintModelAsset.Create<UnitStat> (Component));
            Component.SetDirty ();
            editor.needRepaint = true;
        }
        if (deleteId >= 0) {
            var performDelete = true;
            var ID = Component.stats [deleteId].ID;
            if (!(ID == null || ID == "")) {
                performDelete = EditorUtility.DisplayDialog ("This operation can not be undone!", "Are you sure you want to remove this Stat?", "Yes", "Cancel");
            }
            if (performDelete) {
                Component.Delete (Component.stats [deleteId]);
                Component.SetDirty ();
                editor.needRepaint = true;
            }
        }
            
        
    }
}