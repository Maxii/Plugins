using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeInspector : SubEditor
{

    Vector2 scroll;
    int tab = 0;

    public TechTreeInspector (BlueprintModelEditor editor) : base(editor)
    {

    }

    public override void Draw ()
    {

        if (HotBP != null) {
            DrawBlueprintInspector ();
        }
    }

    void DrawUnitDetails (Blueprint unit, SerializedObject so, GUILayoutOption labelWidth, GUILayoutOption widgetWidth)
    {

        widget.DrawBoxed (() => {
            widget.DrawRow (() => {
                var dirty = widget.DrawIDWidget (unit, so, labelWidth, widgetWidth, "blueprintID");
                if (dirty)
                    this.editor.SetDirty ();
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Time to Build", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("constructTime"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Allow Multiple", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("allowMultiple"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Upgradeable", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("isUpgradeable"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Pre-built", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("prebuilt"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Mutex", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("mutex"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Req Fac Level", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("requiredFactoryLevel"), GUIContent.none, widgetWidth);
            });
            var builders = GetBuilderFactories (unit);

            if (builders.Length == 0 || unit.requiredFactoryLevel > (from x in builders select x.upgradeLevels.Count).Max ()) {
                EditorGUILayout.HelpBox ("There is no factory upgradable to this level", MessageType.Error);
            }

            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Inherit Fac Level", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("inheritFactoryLevel"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Prefab", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("gameObject"), GUIContent.none, widgetWidth);
            });
            widget.DrawRow (() => {
                EditorGUILayout.LabelField ("Sprite", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("sprite"), GUIContent.none, widgetWidth);
            });
        });
    }

    Blueprint[] GetBuilderFactories (Blueprint bp)
    {
        return (from i in Component.blueprints where i.isFactory && i.factory.blueprints.Contains (bp) select i).ToArray ();
    }
    
    void DrawUnitPrerequisites (Blueprint unit, SerializedObject so, GUILayoutOption labelWidth)
    {
        widget.DrawBoxed (() => {
            EditorGUILayout.LabelField ("Prerequisites");

            var back = GUI.backgroundColor;
            var deleteIndex = -1;
            for (var i = 0; i < unit.prerequisites.Count; i++) {
                var prerequisite = unit.prerequisites [i];
                var rso = new SerializedObject (prerequisite);
                rso.Update ();

                widget.DrawBoxedRow (() => {
                    GUILayout.Label (prerequisite.blueprint.ID, labelWidth);
                    GUILayout.FlexibleSpace ();
                    GUILayout.Label ("Level:");
                    EditorGUILayout.PropertyField (rso.FindProperty ("level"), GUIContent.none, GUILayout.Width (32));
                    GUILayout.FlexibleSpace ();
                    if (widget.ControlButton (styles.DELETE_COLOR, "Delete")) {
                        deleteIndex = i;
                    }
                });
                if (prerequisite.level > prerequisite.blueprint.upgradeLevels.Count) {
                    EditorGUILayout.HelpBox ("The pre-requisite is not upgradeable to this level", MessageType.Error);
                }
                rso.ApplyModifiedProperties ();
                GUI.backgroundColor = back;
            }
            if (deleteIndex >= 0) {
                unit.prerequisites.RemoveAt (deleteIndex);
                editor.needRepaint = true;
            }
            
        });
        
    }

    void DrawResourceCostWidget (List<ResourceCost> costs, GUILayoutOption labelWidth)
    {
        for (var i = 0; i < Component.resources.Count; i++) {
            var cost = editor.GetCost (costs, Component.resources [i]);
            widget.DrawBoxedRow (() => {
                GUILayout.Label (cost.resource.ID, labelWidth);
                cost.qty = EditorGUILayout.FloatField (cost.qty, GUILayout.ExpandWidth (true));
            });
        }
    }

    void DrawUnitResourceCosts (Blueprint unit, GUILayoutOption labelWidth)
    {
        widget.DrawBoxed (() => 
        {
            EditorGUILayout.LabelField ("Resource Costs (to build a unit)");
            DrawResourceCostWidget (unit.costs, labelWidth);
 
        });

        if (unit.isUpgradeable) {
            GUILayout.Space (10);
            widget.DrawBoxed (() => 
            {
                EditorGUILayout.LabelField ("Upgrade Costs");
                if (GUILayout.Button ("Add Upgrade Level")) {
                    var rc = BlueprintModelAsset.Create<UpgradeLevel> (Component);
                    BlueprintModelAsset.Rename (rc, unit.ID);
                    unit.upgradeLevels.Add (rc);
                }
            });

            var deleteIndex = -1;
            for (int i = 0; i < unit.upgradeLevels.Count; i++) {
                var uc = unit.upgradeLevels [i];
                if (uc == null || uc.costs == null) {
                    deleteIndex = i;
                    continue;
                }

                widget.DrawBoxed (() => 
                {
                    GUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField ("Level " + (i + 1));
                    if (widget.ControlButton (styles.DELETE_COLOR, "Delete")) {
                        deleteIndex = i;
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField ("Upgrade Time");
                    uc.constructTime = EditorGUILayout.FloatField (uc.constructTime);
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField ("Prefab");
                    uc.gameObject = EditorGUILayout.ObjectField (uc.gameObject, typeof(GameObject), false) as GameObject;
                    GUILayout.EndHorizontal ();

                    DrawResourceCostWidget (uc.costs, labelWidth);
                });
            }
            if (deleteIndex >= 0) {
                BlueprintModelAsset.Remove (unit.upgradeLevels [deleteIndex]);
                unit.upgradeLevels.RemoveAt (deleteIndex);
                Component.SetDirty ();
                editor.needRepaint = true;
            }
        }
        
    }

    void DrawResourceProductionWidget (List<ResourceProductionRate> rates, GUILayoutOption labelWidth)
    {
        for (var i = 0; i < Component.resources.Count; i++) {
            var rpr = editor.GetProducer (rates, Component.resources [i]);
            widget.DrawBoxedRow (() => {
                GUILayout.Label (rpr.resource.ID, labelWidth);
                rpr.qtyPerSecond = EditorGUILayout.FloatField (rpr.qtyPerSecond, GUILayout.ExpandWidth (true));
            });
        }

    }

    void DrawUnitResourceProduction (Blueprint unit, GUILayoutOption labelWidth)
    {
        widget.DrawBoxed (() => 
        {
            EditorGUILayout.LabelField ("Resource Production (after unit is built)");           
            DrawResourceProductionWidget (unit.productionRates, labelWidth);
        });
        if (unit.isUpgradeable) {
            GUILayout.Space (10);
            widget.DrawBoxed (() => 
            {
                EditorGUILayout.LabelField ("Upgraded Production Rates");
            });
            

            for (int i = 0; i < unit.upgradeLevels.Count; i++) {
                var uc = unit.upgradeLevels [i];

                widget.DrawBoxed (() => 
                {
                    GUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField ("Level " + (i + 1));
                    GUILayout.EndHorizontal ();
                    DrawResourceProductionWidget (uc.productionRates, labelWidth);
                });
            }
        }
    }

    void DrawResourceConsumptionWidget (List<ResourceConsumptionRate> rates, GUILayoutOption labelWidth)
    {
        for (var i = 0; i < Component.resources.Count; i++) {
            var rcr = editor.GetConsumer (rates, Component.resources [i]);
            widget.DrawBoxedRow (() => {
                GUILayout.Label (rcr.resource.ID, labelWidth);
                rcr.qtyPerSecond = EditorGUILayout.FloatField (rcr.qtyPerSecond, GUILayout.ExpandWidth (true));
            });
        }

    }

    void DrawUnitResourceConsumption (Blueprint unit, GUILayoutOption labelWidth)
    {
        widget.DrawBoxed (() => 
        {
            EditorGUILayout.LabelField ("Resource Consumption (after unit is built)");
            DrawResourceConsumptionWidget (unit.consumptionRates, labelWidth);

        });
        if (unit.isUpgradeable) {
            GUILayout.Space (10);
            widget.DrawBoxed (() => 
            {
                EditorGUILayout.LabelField ("Upgraded Consumption Rates");
            });
            
            
            for (int i = 0; i < unit.upgradeLevels.Count; i++) {
                var uc = unit.upgradeLevels [i];
                
                widget.DrawBoxed (() => 
                {
                    GUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField ("Level " + (i + 1));
                    GUILayout.EndHorizontal ();
                    DrawResourceConsumptionWidget (uc.consumptionRates, labelWidth);
                });
            }
        }
        
        
    }

    void DrawBlueprintInspector ()
    {

        if (Component == null)
            return;

        var unit = HotBP;
        var spacing = 3;
        var labelWidth = GUILayout.Width (96);
        var widgetWidth = GUILayout.Width (145);

        GUILayout.Label ("Blueprint: " + unit.ID, "box", GUILayout.ExpandWidth (true));
        widget.DrawBoxed (() => {
            var groups = (from i in Component.groups select i.ID).ToArray ();

            GUILayout.BeginHorizontal ();
            var moveToIndex = System.Array.IndexOf<string> (groups, unit.group.ID);
            GUILayout.Label ("Group:");
            var newMoveToIndex = EditorGUILayout.Popup (moveToIndex, groups);
            if (newMoveToIndex != moveToIndex) {
                unit.group = Component.groups [newMoveToIndex];
            }
            GUILayout.EndHorizontal ();
        });
        tab = GUILayout.Toolbar (tab, new string[] {
            "Base",
            "Cost",
            "Produce",
            "Consume",
            "Factory"
        }, GUILayout.Width (296));
        scroll = GUILayout.BeginScrollView (scroll);
        var so = new SerializedObject (unit);
        so.Update ();           
        var background = GUI.backgroundColor;

        switch (tab) {
        case 0:
            GUI.backgroundColor = unit.ID == null || unit.ID == "" ? styles.MISSING_COLOR : background;
            
            DrawUnitDetails (unit, so, labelWidth, widgetWidth);
            GUILayout.Space (spacing);
            DrawUnitPrerequisites (unit, so, labelWidth);
            GUILayout.Space (spacing);
            break;
        case 1:
            DrawUnitResourceCosts (unit, labelWidth);
            GUILayout.Space (spacing);
            break;
        case 2:
            DrawUnitResourceProduction (unit, labelWidth);
            GUILayout.Space (spacing);
            break;
        case 3:
            DrawUnitResourceConsumption (unit, labelWidth);
            GUILayout.Space (spacing);
            break;
        case 4:
            widget.DrawRow (() => {
                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.LabelField ("Is factory?", labelWidth);
                EditorGUILayout.PropertyField (so.FindProperty ("isFactory"), GUIContent.none, widgetWidth);
                if (EditorGUI.EndChangeCheck ())
                    editor.needRepaint = true;
            });
            if (unit.isFactory) {
                GUILayout.Space (spacing);
                DrawFactory (unit, labelWidth);
            }
            break;
        }


        so.ApplyModifiedProperties ();

        
        
        GUI.backgroundColor = background;
        GUILayout.Space (spacing);

        GUILayout.EndScrollView ();
        
    }
    
    void DrawFactory (Blueprint bp, GUILayoutOption labelWidth)
    {
        if (bp.factory == null) {
            bp.factory = BlueprintModelAsset.Create<Factory> (Component);
            BlueprintModelAsset.Rename (bp.factory, bp.ID);
            Component.SetDirty ();
        }
        var so = new SerializedObject (bp.factory);
        so.Update ();
        widget.DrawBoxed (() => {
            EditorGUILayout.LabelField ("Factory Settings");
            EditorGUILayout.PropertyField (so.FindProperty ("type"));
            foreach (var g in Component.groups) {

                var factoryChoices = new List<string> ();
                foreach (var i in Component.blueprints) {
                    if (i.group == g)
                        factoryChoices.Add (i.ID);
                }
                factoryChoices.Insert (0, "** ALL **");
                var addUnitIndex = widget.ControlPopup ("Add from " + g.ID + " group", factoryChoices.ToArray ());
                
                if (addUnitIndex >= 0) {
                    editor.needRepaint = true;
                    if (addUnitIndex == 0) {
                        foreach (var i in Component.blueprints) {
                            if (i.group == g) {
                                bp.factory.AddBlueprint (i);
                            }
                        }
                    } else {
                        var factoryUnit = factoryChoices [addUnitIndex];
                        bp.factory.AddBlueprint (Component.FindBlueprint (factoryUnit));
                    }
                    
                }
                so.ApplyModifiedProperties ();
            }
            
            var deleteIndex = -1;
            factoryScroll = GUILayout.BeginScrollView (factoryScroll);
            for (var i = 0; i < bp.factory.blueprints.Count; i++) {
                var back = GUI.backgroundColor;
                var factoryUnit = bp.factory.blueprints [i];

                widget.DrawBoxedRow (() => {
                    GUILayout.Label (factoryUnit.ID);
                    GUILayout.FlexibleSpace ();
                    if (widget.ControlButton (styles.DELETE_COLOR, "Delete")) {
                        deleteIndex = i;
                    }
                });
                GUI.backgroundColor = back;
            }
            GUILayout.EndScrollView ();
            if (deleteIndex >= 0) {
                bp.factory.blueprints.RemoveAt (deleteIndex);
                editor.needRepaint = true;
            }
            
        });
    }
    
    Vector2 factoryScroll;
}
