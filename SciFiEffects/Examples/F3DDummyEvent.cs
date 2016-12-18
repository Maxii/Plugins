using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DDummyEvent : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            // Used in the example scene to fire up initialization routines
            BroadcastMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);
        }
    }
}