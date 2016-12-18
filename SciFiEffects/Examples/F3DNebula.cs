using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DNebula : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        { 
            transform.position -= Vector3.forward*Time.deltaTime*4000;

            if (transform.position.z < -9150)
            {
                Vector3 newPos = transform.position;
                newPos.z = 9150;
                transform.position = newPos;
                transform.rotation = Random.rotation;
                transform.localScale = new Vector3(1, 1, 1)*Random.Range(200, 800);
            }

        }
    }
}