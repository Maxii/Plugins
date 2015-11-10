using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class F3DCurvedBeam : MonoBehaviour
{


    public Transform dest;
    
  

    public float beamScale;         // Default beam scale to be kept over distance
   

 
    public float UVTime;            // UV Animation speed
        
 
    LineRenderer lineRenderer;      // Line rendered component

    public int curvePoints;
    public float curveHeight;
   
    
    float initialBeamOffset;        // Initial UV offset 

    void Start()
    {
        // Get line renderer component
        lineRenderer = GetComponent<LineRenderer>();

        // Randomize uv offset
        initialBeamOffset = Random.Range(0f, 5f);

        lineRenderer.SetVertexCount(curvePoints);
    }
    
    void Update()
    {
    
               
        lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(Time.time * UVTime + initialBeamOffset, 0f));

        


        float distToDest = Vector3.Distance(transform.position, dest.position);
     
        // Get current beam length and update line renderer accordingly
      //  beamLength = Vector3.Distance(transform.position, hitPoint.point);

        lineRenderer.SetPosition(0, transform.position);

        float piRate = Mathf.PI / (curvePoints - 1);

        for (int i = 1; i < curvePoints - 1; i++ )
        {
            float distRatio = (distToDest / (curvePoints - 1)) * i;
            Vector3 midPos = Vector3.Normalize(dest.position - transform.position) * distRatio;

            float cHeight = Mathf.Sin(piRate * i) * curveHeight; 
            midPos += transform.up * cHeight;

            lineRenderer.SetPosition(i, transform.position + midPos);
        }


        lineRenderer.SetPosition(curvePoints - 1, dest.position);

        float propMult = distToDest * (beamScale / 10f);

        // Calculate default beam proportion multiplier based on default scale and current length
      //  propMult = beamLength * (beamScale / 10f);
                    
        // Set beam to maximum length
      //  beamLength = MaxBeamLength;
     //   lineRenderer.SetPosition(1, new Vector3(0f, 0f, beamLength));

            // Adjust impact effect position

        // Set beam scaling according to its length
        lineRenderer.material.SetTextureScale("_MainTex", new Vector2(propMult, 1f));        
          
    }
}
