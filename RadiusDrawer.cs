using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RadiusDrawer : MonoBehaviour
{

   static LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
    }

    //Render the array of Vectors, static to permit call to method without an instance (gotta go fast)
    public static void SetLinePoints(Vector3[] points) {
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }
}
