using UnityEngine;
using System.Collections.Generic;
using System;

namespace Forge3D
{
    [Serializable]
    public class F3DPoolManagerDB : ScriptableObject
    { 
        public List<string> poolsName; 
        public List<F3DPoolContainer> pools; 
        public string namer = "default";
    }
}
