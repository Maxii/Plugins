using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeGroupEditor : SubEditor
{
 


    public TechTreeGroupEditor (BlueprintModelEditor editor) : base(editor)
    {

    }


    public override void Draw ()
    {
        var color = GUI.color;
		if(Component == null) return;
        foreach (var g in Component.groups.ToArray ()) {
            GUILayout.Space (1);
            widget.DrawBoxed (() => {
                widget.DrawRow (() => {


                });
                GUI.color = color;
                var so = new SerializedObject (g);
                so.Update ();
                var labelWidth = GUILayout.Width (16);
                var widgetWidth = GUILayout.Width (160);
                widget.DrawRow (() => {
                    var dirty = widget.DrawIDWidget (g, so, labelWidth, widgetWidth);
                    EditorGUILayout.PropertyField (so.FindProperty ("color"), GUIContent.none, GUILayout.Width(48));
                    GUILayout.FlexibleSpace();
                    g.visible = widget.ControlToggle(g.visible, styles.INFO_COLOR, "Toggle Visibility");
                    if(dirty) Component.SetDirty();

                    if (widget.ControlButton (styles.DELETE_COLOR, "Delete")) {
                        var performDelete = true;
                        if(!(g.ID == null || g.ID == string.Empty)) {
                            if (!EditorUtility.DisplayDialog ("This operation can not be undone!", "Are you sure you want to remove this Blueprint Group?", "Yes", "Cancel")) {
                                performDelete = false;
                            }
                        }
                        if(performDelete) {
                            editor.Delete(g);
                        }
                    }
                    if (widget.ControlButton (styles.CREATE_COLOR, "Add a new Blueprint")) {
                        editor.CreateNewBlueprint (g);
                    }
                });

                so.ApplyModifiedProperties ();
            });
            g.rect = GUILayoutUtility.GetLastRect ();
            if (ActiveGroup == g) {
                widget.DrawRect (g.rect, Color.blue, Color.clear, -1);
            }
        }

        
    }



}
