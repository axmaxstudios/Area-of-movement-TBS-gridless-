///Alessio Pagano, 03 July 2020
///Area of Movement for Turn Based Strategic in a gridless map

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class PathCalculator : MonoBehaviour
{
     RaycastHit hitInfo;
    NavMeshPath virtualPath;
    internal WorldCharacter controlledCharacter;
    public NavMeshAgent controlledAgent;
    //public float maxDistance = 1;
    // public float stillDistanceThreeshold = 0.5f;
    public int radiusSlices = 12;
    public float pathBoundariesPrecision = 0.5f;
    bool isDestinationReachable = false;
    public float movementPoints;
    Vector3 lastPosition;
    bool wasMovingBefore = false;
    IInteractable pendingInteraction = null;
    bool isInteracting = false;
    static bool isAlive = false;

    

    void Start()
    {
        virtualPath = new NavMeshPath();
    }

    internal static void Setup() {
        isAlive = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (!GameManager.IsInteracting)
        {
            bool isPointingSomething = isAlive && IsCharacterStill() && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);

            if (isPointingSomething)
            {
                float distanceToTravel = GetPathDistance(virtualPath);
                isDestinationReachable = IsPointOnNavMesh(hitInfo.point, virtualPath) && distanceToTravel <= movementPoints;


                IInteractable interactable = hitInfo.transform.GetComponent<IInteractable>();
                
                //print(isDestinationReachable);

                if (isDestinationReachable && Input.GetMouseButtonDown(0))
                {
                    MoveCharacter(virtualPath, distanceToTravel);

                    if (interactable != null)
                    {
                        SetPendingInteraction(interactable);
                    }
                }

                if (pendingInteraction == null)
                {
                    if (isDestinationReachable && interactable != null)
                    {
                        CursorManager.SetCursorImage(CursorEnum.INTERACT);
                    }
                    else
                    {

                        CursorManager.SetCursorImage(!isDestinationReachable);

                    }
                }

            }
        }


        void SetPendingInteraction(IInteractable interaction)
        {
            pendingInteraction = interaction;
        }

        void MoveCharacter(NavMeshPath virtualPath, float distance)
        {
            controlledAgent.SetPath(virtualPath);
            movementPoints -= distance;

        }

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    CalculateBoundaries(radiusSlices);

        //}

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

        //RadiusDrawer.SetLinePoints(waypoints.ToArray());
        return totalLength;

    }

    bool IsCharacterStill()
    {

        Vector3 characterPosition = controlledAgent.transform.position;

        if (wasMovingBefore && characterPosition == lastPosition && controlledAgent.velocity.magnitude < 0.1f)
        {
            wasMovingBefore = false;
            OnBecomeStill();
        }
        else if (!wasMovingBefore && characterPosition != lastPosition && controlledAgent.velocity.magnitude > 0.1f)
        {
            wasMovingBefore = true;
            OnStartMoving();
        }

        bool isStill = lastPosition == characterPosition;

        lastPosition = characterPosition;
        return isStill;
    }
    
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
     
    void OnStartMoving()
    {
        CameraManager.TrackTarget(controlledAgent.transform);
    }

    void OnBecomeStill()
    {


        if (pendingInteraction != null)
        {
            ExecuteInteraction();
        }
        else
        {
            CalculateMovementPoints();
        }
        CameraManager.StopTracking();

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
