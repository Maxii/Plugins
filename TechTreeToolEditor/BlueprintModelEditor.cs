using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree;
using TechTree.Model;

public class BlueprintModelEditor : EditorWindow
{

    public BlueprintModel Component { get; set; }

    public bool needRepaint = false;
    Blueprint _hotBP;

    public Blueprint HotBP { 
        get { 
            return _hotBP; 
        }
        set {
            _hotBP = value;
            if (value == null)
                ActiveGroup = null;
            else
                ActiveGroup = _hotBP.group;
        }
    }

    public string focusControl = null;

    public BlueprintGroup ActiveGroup {
        get;
        set;
    }

    List<System.Action> runNext = new List<System.Action>();

    public void RunNext(System.Action fn) {
        runNext.Add(fn);
    }

    void Update() {
        foreach(var rn in runNext.ToArray()) {
            rn();
        }
        runNext.Clear();
    }

    void OnEnable ()
    {
        widget = new TechTreeSimpleEditorWidgets ();
        inspector = new TechTreeInspector (this);
        groupPanel = new TechTreeGroupEditor (this);
        resources = new TechTreeResourcesEditor (this);
        graphEditor = new TechTreeGraphEditor (this);
        costEditor = new TechTreeCostEditor (this);
        statEditor = new TechTreeStatEditor (this);

        this.wantsMouseMove = true;
    }

    public new void SetDirty ()
    {
        if (Component != null)
            Component.SetDirty ();
    }

    void DrawEditorGUI ()
    {
        if (needRepaint) {
            needRepaint = false;
        }

        if (Component == null) {
            Component = Selection.activeObject as BlueprintModel;
            if (Component == null) {
                GUILayout.BeginHorizontal ();
                widget.DrawUsageMessage ();
                Component = EditorGUILayout.ObjectField (Component, typeof(BlueprintModel), false) as BlueprintModel;
                GUILayout.EndHorizontal ();
                return;
            }
        }
        //ValidateModel ();
        if (focusControl != null) {
            var focus = GUI.GetNameOfFocusedControl ();
            if (focus == focusControl) {
                GUI.FocusControl (focusControl);
                focusControl = null;
            } else {
                GUI.FocusControl (focusControl);
            }
        }
        GUILayout.Space (10);
        widget.DrawRow (() => {
            activeTab = GUILayout.Toolbar (activeTab, tabs, GUILayout.Width (512));
            GUILayout.FlexibleSpace ();
            Component = EditorGUILayout.ObjectField (Component, typeof(BlueprintModel), false) as BlueprintModel;
        });
        GUILayout.Space (20);
        switch (activeTab) {
        case 0:
            resources.Draw ();
            break;
        case 1:
            DrawEditingControls ();
            break;
        case 2:
            DrawCostControls ();
            break;
        case 3:
            statEditor.Draw ();
            break;
        }

        foreach (var i in removedBlueprints) {
            Component.Delete (i);
        }
        removedBlueprints.Clear ();
        foreach (var i in removedGroups) {
            Component.Delete (i);
        }
        removedGroups.Clear ();
    }

    void OnGUI ()
    {
        var e = Event.current;
        if (Event.current.type == EventType.ValidateCommand) {
            if (e.commandName == "UndoRedoPerformed") {
                e.Use ();
            }
        }
        if (e.type == EventType.ExecuteCommand) {
            if (e.commandName == "UndoRedoPerformed") {
                Repaint ();
            }
        }
        DrawEditorGUI ();
    }

    void DrawEditingControls ()
    {
        GUILayout.BeginHorizontal ();
        GUILayout.BeginVertical (GUILayout.Width (276));
        GUILayout.Space (3);
        groupPanel.Draw ();
        GUILayout.Space (3);
        inspector.Draw ();
        GUILayout.FlexibleSpace ();
        GUILayout.EndVertical ();
        GUILayout.BeginVertical (GUILayout.ExpandWidth (true));
        graphEditor.Draw ();
        GUILayout.EndVertical ();
        GUILayout.EndHorizontal ();
    }

    void DrawCostControls ()
    {
        GUILayout.BeginHorizontal ();
        GUILayout.BeginVertical (GUILayout.Width (276));
        GUILayout.Space (3);
        groupPanel.Draw ();
        GUILayout.FlexibleSpace ();
        GUILayout.EndVertical ();
        GUILayout.BeginVertical (GUILayout.ExpandWidth (true));
        costEditor.Draw ();
        GUILayout.EndVertical ();
        GUILayout.EndHorizontal ();
    }

    public Blueprint CreateNewBlueprint (BlueprintGroup g)
    {
        var c = BlueprintModelAsset.Create<Blueprint> (Component);
        c.rect = new Rect (100, 100, 128, 128);
        if (HotBP != null) {
            c.rect.x = HotBP.rect.x + HotBP.rect.width + 50;
            c.rect.y = HotBP.rect.y + 50;
        }
        Component.blueprints.Add (c);
        c.group = g;
        ActiveGroup = g;
        HotBP = c;
        focusControl = "blueprintID";
        Component.SetDirty ();
        return c;

    }

    public void Delete (Blueprint b)
    {
        removedBlueprints.Add (b);
    }

    public void Delete (BlueprintGroup g)
    {
        removedGroups.Add (g);
    }

    public ResourceCost GetCost (List<ResourceCost> costs, Resource newResource)
    {
        var recosts = (from i in costs select i.resource).ToList ();
        var index = recosts.IndexOf (newResource);
        if (index < 0) {

            var newCost = BlueprintModelAsset.Create<ResourceCost> (Component);
            newCost.resource = newResource;
            costs.Add (newCost);
            BlueprintModelAsset.Rename (newCost, "" + "-" + newResource.ID);
            Component.SetDirty ();
            needRepaint = true;
            return newCost;
        } else {
            return costs [index];
        }
    }

    public ResourceProductionRate GetProducer (List<ResourceProductionRate> rates, Resource newResource)
    {
        var costs = (from i in rates select i.resource).ToList ();
        var index = costs.IndexOf (newResource);
        if (index < 0) {

            var newCost = BlueprintModelAsset.Create<ResourceProductionRate> (Component);
            newCost.resource = newResource;
            rates.Add (newCost);
            BlueprintModelAsset.Rename (newCost, "" + "-" + newResource.ID);
            Component.SetDirty ();
            needRepaint = true;
            return newCost;
        } else {
            return rates [index];
        }
    }

    public ResourceConsumptionRate GetConsumer (List<ResourceConsumptionRate> rates, Resource newResource)
    {
        var costs = (from i in rates select i.resource).ToList ();
        var index = costs.IndexOf (newResource);
        if (index < 0) {

            var newCost = BlueprintModelAsset.Create<ResourceConsumptionRate> (Component);
            newCost.resource = newResource;
            rates.Add (newCost);
            BlueprintModelAsset.Rename (newCost, "" + "-" + newResource.ID);
            Component.SetDirty ();
            needRepaint = true;
            return newCost;
        } else {
            return rates [index];
        }
    }

    public void ValidateModel ()
    {
        if(Component == null) {
            Debug.Log("NULL");
            return;
        }
        Component.groups.RemoveAll ((i) => i == null);
        Component.blueprints.RemoveAll ((i) => i == null);
        foreach (var b in Component.blueprints) {
            b.prerequisites.RemoveAll ((i) => i == null);
            if (b.isFactory) {
                if (b.factory == null) {
                    b.factory = BlueprintModelAsset.Create<Factory> (Component);
                    BlueprintModelAsset.Rename (b.factory, b.ID);
                    Component.SetDirty ();
                }
                b.factory.blueprints.RemoveAll ((i) => i == null);
            }

            foreach (var r in Component.resources) {
                GetCost (b.costs, r);
                GetConsumer (b.consumptionRates, r);
                GetProducer (b.productionRates, r);
                foreach (var u in b.upgradeLevels) {
                    GetCost (u.costs, r);
                    GetConsumer (u.consumptionRates, r);
                    GetProducer (u.productionRates, r);
                }
            }

        }

    }

    [MenuItem ("Window/TechTree Manager")]
    static void Init ()
    {
        var window = (BlueprintModelEditor)EditorWindow.GetWindow (typeof(BlueprintModelEditor));
        window.Show ();
    }
    
    [MenuItem("Assets/Create/New TechTree Model")]
    static public void CreateNew ()
    {
        var root = "Assets/";
        var instance = ScriptableObject.CreateInstance<BlueprintModel> ();
        var path = AssetDatabase.GenerateUniqueAssetPath (root + "/New TechTree Model.asset");
        AssetDatabase.CreateAsset (instance, path);
        Selection.activeObject = instance;
        Init ();
    }

    TechTreeSimpleEditorWidgets widget;
    TechTreeGroupEditor groupPanel;
    TechTreeInspector inspector;
    TechTreeResourcesEditor resources;
    TechTreeGraphEditor graphEditor;
    TechTreeCostEditor costEditor;
    TechTreeStatEditor statEditor;
    string[] tabs = new string[] {
        "Resources",
        "Blueprints",
        "Costs",
        "Stats"
    };
    int activeTab;
    List<Blueprint> removedBlueprints = new List<Blueprint> ();
    List<BlueprintGroup> removedGroups = new List<BlueprintGroup> ();

}