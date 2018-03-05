using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;



public class SubEditor
{
    public readonly BlueprintModelEditor editor;
    public readonly TechTreeSimpleEditorWidgets widget;
    public readonly TechTreeEditorStyles styles;

    public SubEditor(BlueprintModelEditor editor) {
        this.editor = editor;
        widget = new TechTreeSimpleEditorWidgets();
        styles = new TechTreeEditorStyles();
    }

    public BlueprintModel Component {
        get {
            return editor.Component;
        }
    }
    
    public Blueprint HotBP {
        get {
            return editor.HotBP;
        }
        set {
            editor.HotBP = value;
        }
    }

    public BlueprintGroup ActiveGroup {
        get {
            return editor.ActiveGroup;
        }
        set {
            editor.ActiveGroup = value;
        }
    }

    public virtual void Draw ()
    {
        GUILayout.Label("Editor is unimplemented");
    }

}
