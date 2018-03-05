using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AccordionWidget
{

    public bool expand = false;
    public string title;
    Vector2 scroll;

    public AccordionWidget (string title, System.Action fn)
    {
        this.title = title;
        this.fn = fn;
    }

    public void Draw ()
    {
        this.expand = GUILayout.Toggle (this.expand, this.title + (this.expand ? " <" : " >"), "button");

        if (this.expand) {
            scroll = GUILayout.BeginScrollView (scroll);
            GUILayout.BeginVertical ("box");
            this.fn ();
            GUILayout.EndVertical ();
            GUILayout.EndScrollView ();
        }


    }

    System.Action fn;

}
