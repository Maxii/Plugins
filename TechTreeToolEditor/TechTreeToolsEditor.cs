using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeToolsEditor : SubEditor {

    AccordionWidget container;

    public TechTreeToolsEditor(BlueprintModelEditor editor) : base(editor) {
        container = new AccordionWidget("Tools", DrawTools);
    }

    public override void Draw() {
        container.Draw();
    }

    void DrawTools() {
        var resourceNames = (from i in Component.resources select i.ID).ToArray();
        EditorGUILayout.HelpBox("Operations effect the selected BP and it's children, if they are in a visible group.", MessageType.Info);

        GUILayout.Label("Choose a Resource to Operate with.");
        resourceIndex = EditorGUILayout.Popup(resourceIndex, resourceNames);
        GUILayout.Label("Bump the cost up or down.");
        widget.DrawBoxedRow(() => {
            bumpAmount = EditorGUILayout.FloatField(bumpAmount);
            if (GUILayout.Button("Bump Cost")) {
                Apply(HotBP, 0, (b) => {
                    var rc = editor.GetCost(b.costs, Component.resources[resourceIndex]);
                    rc.qty += bumpAmount;
                });
                Component.SetDirty();
            }
        });
        GUILayout.Label("Scale the cost.");
        widget.DrawBoxedRow(() => {
            scaleAmount = EditorGUILayout.FloatField(scaleAmount);
            if (GUILayout.Button("Scale Cost")) {
                Apply(HotBP, 0, (b) => {
                    var rc = editor.GetCost(b.costs, Component.resources[resourceIndex]);
                    rc.qty *= scaleAmount;
                });
                Component.SetDirty();
            }
        });
        GUILayout.Label("Adjust Build Times");
        GUILayout.Label("Bump Build Time");
        widget.DrawBoxedRow(() => {
            bumpTimeAmount = EditorGUILayout.FloatField(bumpTimeAmount);
            if (GUILayout.Button("Bump Time")) {
                Apply(HotBP, 0, (b) => {
                    b.constructTime += bumpTimeAmount;
                });
                Component.SetDirty();
            }
        });
        GUILayout.Label("Scale Build Time");
        widget.DrawBoxedRow(() => {
            scaleTimeAmount = EditorGUILayout.FloatField(scaleAmount);
            if (GUILayout.Button("Scale Build Time")) {
                Apply(HotBP, 0, (b) => {
                    b.constructTime *= scaleTimeAmount;
                });
                Component.SetDirty();
            }
        });


    }

    float bumpAmount, scaleAmount, bumpTimeAmount, scaleTimeAmount;
    int resourceIndex;

    void Apply(Blueprint b, int depth, System.Action<Blueprint> fn) {
        if (b.group.visible) fn(b);
        foreach (var i in Component.GetDependentBlueprints(b)) {
            Apply(i, depth + 1, fn);
        }
    }

}
