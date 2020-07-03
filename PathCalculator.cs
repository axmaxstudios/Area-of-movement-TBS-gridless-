///Alessio Pagano, 03 July 2020
///Area of Movement for Turn Based Strategic in a gridless map

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class PathCalculator : MonoBehaviour
{
    public static PlayerController instance;
    RaycastHit hitInfo;
    NavMeshPath virtualPath;
    public NavMeshAgent controlledCharacter;

    //max distance a path could be navigated to
    public float maxDistance = 5;

    //subdivisions of the circle, Higher values is more precise but less efficient, 25 is a compromise value
    public int radiusSlices = 12;

    //value to calculate cavities, lower values result in a more precise calculation
    public float pathBoundariesPrecision = 0.5f;
    bool isDestinationReachable = false;
    public float movementPoints;
    Vector3 lastPosition;
    bool wasMovingBefore = false;
    GameManager gameManager;

    private void Awake()
    {
        instance = this;
    }

    //Initialization
    void Start()
    {
        //navmesh path used for calculations
        virtualPath = new NavMeshPath();
        controlledCharacter.SetDestination(controlledCharacter.transform.position);
        movementPoints = maxDistance;
    }

    //
    void Update()
    {

        //Tries to get the mouse raycast on map as a candidate to move agent
        bool isPointingSomething = IsCharacterStill() && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);

        if (isPointingSomething)
        //check if path is inside max distance and actually on the navmesh baked area
        {
            float distanceToTravel = GetPathDistance(virtualPath);
            isDestinationReachable = IsPathOnNavMesh(hitInfo.point, virtualPath) && distanceToTravel <= maxDistance;
            
            //if the point is actually on a navmesh area and in range and Left mouse button is clicked
            if (isDestinationReachable && Input.GetMouseButtonDown(0))
            {
                //set the path directly to the agent

                controlledCharacter.SetPath(virtualPath);
            }
        }

        //DEBUG Press SPACE to start the render of the area
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CalculateBoundaries(radiusSlices);

        }
    }

    //Checks if on a correctly baked navmesh or on obstacle
    bool IsPathOnNavMesh(Vector3 virtualDestination, NavMeshPath outPath)
    {
        return controlledCharacter.CalculatePath(virtualDestination, outPath);
    }

    //This Method returns the sum of the distance between every corner of a path
    float GetPathDistance(NavMeshPath path)
    {
        float totalLength = 0;
        List<Vector3> waypoints = new List<Vector3>();

        waypoints.Add(controlledCharacter.transform.position);
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

    //Method to check if the agent is still or moving, it could be improved using events (but I'm not very confident with these so I used the easy way)
    bool IsCharacterStill()
    {
        Vector3 characterPosition = controlledCharacter.transform.position;

        if (wasMovingBefore && characterPosition == lastPosition)
        {

            OnBecomeStill();
            wasMovingBefore = false;
        }
        else if (!wasMovingBefore && characterPosition != lastPosition)
        {
            OnStartMoving();
            wasMovingBefore = true;
        }

        bool isStill = lastPosition == characterPosition;

        lastPosition = characterPosition;
        return isStill;
    }

    //Here happens the magic
    private void CalculateBoundaries(NavMeshAgent agent, int subdivisions)
    {
        List<Vector3> waypoints = new List<Vector3>();
        //position of the target agent
        Vector3 center = agent.transform.position;

        //possible optimization using the previously created navmeshpath: virtualPath
        NavMeshPath controlPath = new NavMeshPath();
        Vector3 finalPoint = new Vector3();

        //it will check the path in every point in a circle. For example, using a subdivison of 12 it will check the path every 30 degrees in the radius (360d / 12 = 30d)
        for (int i = 0; i < subdivisions; i++)
        {
            float angle = 360 / subdivisions * i;

            //a point at the edge of agent possible movement
            finalPoint = center + GetPointOnRadiusCorrected(angle, maxDistance);

            bool isValid = IsPathOnNavMesh(finalPoint, controlPath) && GetPathDistance(controlPath) < maxDistance;

            if (isValid)
            {
                waypoints.Add(finalPoint);
            }
            else
            {
                //This while tries to rebuild the cavity part, there is a big margin of improvement here
                float cycle = pathBoundariesPrecision;

                while (!isValid && cycle < maxDistance)
                {
                    finalPoint = center + GetPointOnRadiusCorrected(angle, maxDistance - cycle);

                    waypoints.Add(finalPoint);

                    isValid = IsPathOnNavMesh(finalPoint, controlPath) && GetPathDistance(controlPath) < maxDistance;
                    //if path is not available it keeps recalculating the point until is available, but only if this point doesn't overlapp with the agent
                    Vector3 nextPoint = center + GetPointOnRadiusCorrected(angle - cycle * 5, maxDistance - cycle);

                    if (!isValid)
                    {
                        waypoints.Remove(finalPoint);
                    }

                    cycle += pathBoundariesPrecision;
                }
            }
        }

        //Actual call to render the calculated area
        RadiusDrawer.SetLinePoints(waypoints.ToArray());
    }

    //Convert a degree angle in a Vector2 with magnitude set to 1
    Vector3 GetRadiusPointFromAngle(float angle)
    {
        Vector3 point = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), 0, Mathf.Cos(Mathf.Deg2Rad * angle));
        return point;
    }

    //Multiplies the maximum distance the agent can reach to the point on a circle with magnitude of 1
    Vector3 GetPointOnRadiusCorrected(float angle, float maxDistance)
    {
        Vector3 circlePoint = GetRadiusPointFromAngle(angle);
        Vector3 finalPoint = (circlePoint * maxDistance) - circlePoint;
        return finalPoint;
    }

    void OnStartMoving()
    {
        //not implemented
        //follow player
    }

    void OnBecomeStill()
    {

        if (movementPoints <= 0)
        {
            //not implemented
            //pass turn
        }
        else
        {
            CalculateBoundaries(radiusSlices);
        }
    }


}
