///Alessio Pagano, 22 September 2020
///Contact me for any inconvenience: axmaxstudios@gmail.com
///Area of Movement for Turn Based Strategic in a gridless map

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class PathCalculator : MonoBehaviour
{
    static bool isAlive = false;

    RaycastHit hitInfo;
    NavMeshPath virtualPath;
    internal WorldCharacter controlledCharacter;
    public NavMeshAgent controlledAgent;

    //Controls the number of raycast used for precision, around 25 is a good amount, values higher than 50 may cause slower calculation
    public int radiusSlices = 12;

    //meter value used to analyze obstacles or unreachable areas, lower means more precise but also slower. I suggest 0.25 or 0.5
    public float pathBoundariesPrecision = 0.5f;

    bool isDestinationReachable = false;
    
    //change this value to widen the area circle
    public float movementPoints;
    
    Vector3 lastPosition;
    bool wasMovingBefore = false;
    IInteractable pendingInteraction = null;
    bool isInteracting = false;

    void Start()
    {
        virtualPath = new NavMeshPath();
        isAlive = true;
    }

//FOR DEBUG ONLY, Use mouse click to set the destination of the designed navmesh agent
void Update()
    {
        
            bool isPointingSomething = isAlive && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);

            if (isPointingSomething)
            {
                float distanceToTravel = GetPathDistance(virtualPath);
                isDestinationReachable = IsPointOnNavMesh(hitInfo.point, virtualPath) && distanceToTravel <= movementPoints;

                IInteractable interactable = hitInfo.transform.GetComponent<IInteractable>();

                if (isDestinationReachable && Input.GetMouseButtonDown(0))
                {
                    MoveCharacter(virtualPath, distanceToTravel);

                }
            }
    }

    void MoveCharacter(NavMeshPath virtualPath, float distance)
    {
        controlledAgent.SetPath(virtualPath);
        movementPoints -= distance;
    }
    
    public RaycastHit GetRaycastHit()
    {
        return hitInfo;
    }
    
    public bool IsPointOnNavMesh(Vector3 virtualDestination, NavMeshPath outPath)
    {
        return controlledAgent.CalculatePath(virtualDestination, outPath);
    }

    public bool IsPointOnNavMesh()
    {
        NavMeshPath path = new NavMeshPath();
        return controlledAgent.CalculatePath(hitInfo.point, path);
    }

    float GetPathDistance(NavMeshPath path)
    {
        float totalLength = 0;
        List<Vector3> waypoints = new List<Vector3>();

        waypoints.Add(controlledAgent.transform.position);
        foreach (Vector3 v in path.corners)
        {
            waypoints.Add(v);
        }

        for (int i = 0; i < waypoints.Count - 1; i++)
        {

            float partial = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            totalLength += partial;
        }

        return totalLength;
    }

//Main calculations here
    private void CalculateBoundaries(int subdivisions)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector3 center = controlledAgent.transform.position;
        NavMeshPath controlPath = new NavMeshPath();
        Vector3 finalPoint = new Vector3();
        RaycastHit hit;

        for (int i = 0; i < subdivisions; i++)
        {
            float angle = 360 / subdivisions * i;

            bool isValid = false;
            float decrement = 0;
            do
            {
                finalPoint = center + GetPointOnRadiusCorrected(angle, movementPoints + decrement);
                Vector3 rayOrigin = finalPoint + Vector3.up * 15;
                Physics.Raycast(rayOrigin, -Vector3.up, out hit);
                isValid = IsPointOnNavMesh(hit.point, controlPath) && GetPathDistance(controlPath) < movementPoints;
                decrement -= pathBoundariesPrecision;
            }
            while (!isValid);
            finalPoint = controlPath.corners.LastOrDefault();
            waypoints.Add(finalPoint);

        }
        RadiusDrawer.SetLinePoints(waypoints.ToArray());
    }


    Vector3 GetPointOnRadiusCorrected(float angle, float maxDistance)
    {
        Vector3 circlePoint = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), 0, Mathf.Cos(Mathf.Deg2Rad * angle));
        Vector3 finalPoint = (circlePoint * maxDistance);
        return finalPoint;
    }

    internal void CalculateMovementPoints()
    {
        if (movementPoints <= 1.5f)
        {
            GameManager.CharacterFinishedTurn();
            movementPoints = 15;
        }
        else
        {
            CalculateBoundaries(radiusSlices);
        }
    }
}
