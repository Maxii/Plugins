using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TechTreeEditorStyles
{

    public GUIStyle nodeWindow;
    public GUIStyle controlButton;
    public GUIStyle controlToggle;
    public GUIStyle graphSlider;
    public GUIStyle graphThumb;
    public Color MISSING_COLOR = new Color (1, 0.5f, 0.5f, 0.5f);
    public Color DELETE_COLOR = Color.red;
    public Color CREATE_COLOR = Color.green;
    public Color INFO_COLOR = Color.yellow;
    public Color NORMAL_COLOR = Color.white;
    public Color TIME_COLOR = Color.blue;



    public TechTreeEditorStyles ()
    {
        var skin = EditorGUIUtility.GetBuiltinSkin (EditorSkin.Inspector);
        
        nodeWindow = new GUIStyle (skin.window);
        nodeWindow.alignment = TextAnchor.UpperLeft;
        
        controlButton = new GUIStyle (skin.button);
        controlButton.fixedWidth = 12;
        controlButton.fixedHeight = 12;

        controlToggle = new GUIStyle(controlButton);


        graphThumb = new GUIStyle(skin.box);
        graphSlider = new GUIStyle(skin.verticalSlider);
        graphSlider.normal.background = null;
        graphSlider.fixedWidth = 0;
        graphSlider.stretchWidth = true;
    }
}
