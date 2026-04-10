using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Downtown_Car_Controller_2 : MonoBehaviour
{
    [Header("Navigation")]
    public float normalSpeed; // This is one declaration.
    public float rotationSpeed;
    public float acceleration;
    private Transform[] waypoints; // Optional: for simple path following if not using a more complex destination system
    private int currentWaypointIndex = 0;

    [Header("Detection")]
    public float forwardDetectionDistance;
    public LayerMask vehicleLayerMask; // Set this in the inspector to the layer your vehicles are on
    public string intersectionTag = "intersection"; // Ensure your intersection trigger colliders have this tag
    public string turnLaneTag = "TurnLane"; // Tag for turn lanes, if needed
    public float vehicleAvoidanceDistance; // Distance to start slowing down for other vehicles
    public float vehicleStopDistance; // Distance at which speed reaches 0
    // Removed: public float intersectionStoppingDistance = 5f; 

    private NavMeshAgent navMeshAgent;
    private bool isApproachingIntersection = false;
    private Vector3 intersectionStopPosition;
    private bool hasTargetDestination = false; // To ensure we have a primary goal
    private ObstacleAvoidanceType originalObstacleAvoidanceType; // Added to store original setting
    private bool isProceedingThroughIntersection = false; // New flag for proceeding through intersection
    private bool isStoppedAtIntersection = false; // New flag for stopped at intersection

    private Intersection2 currentIntersection; // Optional: reference to the current intersection if needed
    private TurnLaneController2 laneController;

    private CrosswalkController currentCrosswalk;

    public TurnDirection currentTurnDirection; // Current turn direction at intersection

    private bool isWaitingForLeftTurn = false; // Flag to indicate if a left turn is being performed
    private bool isWaitingForRightTurn = false; // Flag to indicate if a right turn is being performed

    private Vector3 intersectionDestination;

    // New waypoint queue system
    public Queue<Vector3> waypointQueue = new Queue<Vector3>();
    private Vector3 originalDestination;
    private bool hasOriginalDestination = false;

    public float laneWidth = 5f; // Width of the lane for intersection calculations

    public int currentLane; // Current lane index, used for intersection logic
    public int hitDistanceToRegisterIntersectionPresence = 10; // Distance to register intersection presence

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }

        navMeshAgent.speed = normalSpeed;
        navMeshAgent.angularSpeed = rotationSpeed;
        navMeshAgent.acceleration = acceleration;
        navMeshAgent.stoppingDistance = 1f; // Smaller stopping distance for waypoints
        originalObstacleAvoidanceType = navMeshAgent.obstacleAvoidanceType; // Store original avoidance type

        // Create initial destination 50 units forward
        Vector3 directionVectorStart;
        TrafficDirection generalDirStart = getGeneralMovementDirection();
        if (generalDirStart == TrafficDirection.X)
        {
            directionVectorStart = (transform.forward.x >= 0) ? Vector3.right : Vector3.left;
        }
        else // TrafficDirection.Z
        {
            directionVectorStart = (transform.forward.z >= 0) ? Vector3.forward : Vector3.back;
        }
        Vector3 forwardPoint = transform.position + directionVectorStart * 50f;
        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, NavMesh.AllAreas))
        {
            originalDestination = navMeshHit.position;
            hasOriginalDestination = true;
            waypointQueue.Enqueue(originalDestination);
            
            if (waypointQueue.Count > 0)
            {
                SetNextWaypointFromQueue();
            }
        }
        else
        {
            // Debug.LogWarning($"{gameObject.name}: Could not find a NavMesh point 50 units in front. Vehicle will need a destination set externally.");
            hasTargetDestination = false;
        }
    }

    void Update()
    {
        navMeshAgent.speed = normalSpeed; // Reset speed to normal speed at the start of each update
        if (!navMeshAgent.isOnNavMesh || !hasTargetDestination)
        {
            if (!hasTargetDestination && navMeshAgent.hasPath) navMeshAgent.ResetPath();
            return;
        }

        HandleRaycastDetection();

        if (isApproachingIntersection)
        {
            // Check if we're close to the intersection waypoint and can proceed with a green light
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * navMeshAgent.speed / 3 && !navMeshAgent.pathPending && currentIntersection.direction == getGeneralMovementDirection())
            {
                if (currentTurnDirection == TurnDirection.Left)
                {
                    isWaitingForLeftTurn = true; // Set flag for left turn
                    navMeshAgent.stoppingDistance = 2f;
                    // // Debug.Log("Waiting for Left turn set to true");
                    // navMeshAgent.speed *= 0.3f; // Reduce speed for left turn
                }
                else if (currentTurnDirection == TurnDirection.Right)
                {
                    isWaitingForRightTurn = true; // Set flag for right turn
                    navMeshAgent.stoppingDistance = 2f;
                    // navMeshAgent.speed *= 0.3f; // Reduce speed for right turn
                    // // Debug.Log("Waiting for Right turn");
                }
                else
                {
                    isWaitingForLeftTurn = false; // Reset flag for left turn
                    isWaitingForRightTurn = false; // Reset flag for right turn
                    isProceedingThroughIntersection = true;
                    // Debug.Log("Proceeding through intersection");
                }
                isApproachingIntersection = false;
                navMeshAgent.isStopped = false; // Proceed through the intersection
                navMeshAgent.stoppingDistance = 1f;

                SetNextWaypointFromQueue(); // Set next waypoint destination to after intersection
            }
            else if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
            {
                isProceedingThroughIntersection = false;
                isApproachingIntersection = false;
                isStoppedAtIntersection = true;
                navMeshAgent.isStopped = true;
                // Debug.Log("Stopped at intersection red light or stop sign");
            }
        }
        else if (isStoppedAtIntersection)
        {
            // Debug.Log($"{gameObject.name} is stopped at intersection {currentIntersection.name}");
            // Handle logic for waiting at the intersection
            // continue when the intersection is controlled and the light is green
            if (!laneController.yield && currentIntersection != null)
            {
                if (currentIntersection.direction == getGeneralMovementDirection())
                {
                    if (currentTurnDirection == TurnDirection.Left)
                    {
                        isWaitingForLeftTurn = true; // Set flag for left turn
                        navMeshAgent.stoppingDistance = 2f;
                        // // Debug.Log("Waiting for Left turn set to true");
                        // navMeshAgent.speed *= 0.3f; // Reduce speed for left turn
                    }
                    else if (currentTurnDirection == TurnDirection.Right)
                    {
                        isWaitingForRightTurn = true; // Set flag for right turn
                        navMeshAgent.stoppingDistance = 2f;
                        // navMeshAgent.speed *= 0.3f; // Reduce speed for right turn
                        // // Debug.Log("Waiting for Right turn");
                    }
                    else
                    {
                        isWaitingForLeftTurn = false; // Reset flag for left turn
                        isWaitingForRightTurn = false; // Reset flag for right turn
                        isProceedingThroughIntersection = true;
                        // Debug.Log("Proceeding through intersection");
                    }
                    isApproachingIntersection = false;
                    isStoppedAtIntersection = false;
                    navMeshAgent.isStopped = false; // Proceed through the intersection
                    navMeshAgent.stoppingDistance = 1f;

                    SetNextWaypointFromQueue(); // Set next waypoint destination to after intersection
                }
                else
                {
                    navMeshAgent.isStopped = true;
                    // Debug.Log($"{gameObject.name} is waiting at intersection {currentIntersection.name} for correct direction");
                }
            }
            else if(laneController.yield)// stopped at uncontrolled intersection
            {
                // leave turning to the turning state logic
                if (currentTurnDirection == TurnDirection.Left && laneController.isSafeToTurn(TurnDirection.Left))
                {
                    isWaitingForLeftTurn = true; // Set flag for left turn
                    navMeshAgent.stoppingDistance = 2f;
                    navMeshAgent.isStopped = false;
                    isStoppedAtIntersection = false;
                    // move to laneController position
                    SetNextWaypointFromQueue();
                    // // Debug.Log("Waiting for Left turn set to true");
                    // navMeshAgent.speed *= 0.3f; // Reduce speed for left turn
                }
                else if (currentTurnDirection == TurnDirection.Right && laneController.isSafeToTurn(TurnDirection.Right))
                {
                    isWaitingForRightTurn = true; // Set flag for right turn
                    navMeshAgent.stoppingDistance = 2f;
                    navMeshAgent.isStopped = false;
                    isStoppedAtIntersection = false;
                    // move to laneController position
                    SetNextWaypointFromQueue();
                    // navMeshAgent.speed *= 0.3f; // Reduce speed for right turn
                    // // Debug.Log("Waiting for Right turn");
                }
                else
                {
                    isWaitingForLeftTurn = false; // Reset flag for left turn
                    isWaitingForRightTurn = false; // Reset flag for right turn
                    if (laneController.isSafeToTurn(currentTurnDirection))
                    {
                        isProceedingThroughIntersection = true;
                        isStoppedAtIntersection = false;
                        navMeshAgent.isStopped = false; // Proceed through the intersection
                        navMeshAgent.stoppingDistance = 1f;
                        SetNextWaypointFromQueue(); // Set next waypoint destination to after intersection
                        // Debug.Log($"{gameObject.name} proceeding through uncontrolled intersection {currentIntersection.name}");
                    }
                }
            }
        }
        else if (isWaitingForLeftTurn || isWaitingForRightTurn)
        {
            // Handle logic for waiting for left/right turn once reaching turn stop point
            // Have to wait at intersection for left/right turn and at the intersection stop point
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
            {
                // // Debug.Log($"{gameObject.name} reached stop point for turn at intersection {currentIntersection.name}");
                // // Debug.Log("Distance to intersection: " + navMeshAgent.remainingDistance);
                // Stop the agent at the intersection stop point
                // navMeshAgent.isStopped = true;
                // Debug.Log(laneController.isSafeToTurn(currentTurnDirection));
                if (laneController.isSafeToTurn(currentTurnDirection))
                {
                    // Debug.Log($"{gameObject.name} proceeding through intersection {currentIntersection.name} to turn {currentTurnDirection.ToString()}");
                    // navMeshAgent.speed *= 0.5f;
                    navMeshAgent.isStopped = false;
                    isProceedingThroughIntersection = true;
                    isWaitingForLeftTurn = false;
                    isWaitingForRightTurn = false;
                    SetNextWaypointFromQueue();
                }
                else
                {
                    navMeshAgent.isStopped = true; // Stop if crosswalk is not clear
                }

                // if (isWaitingForLeftTurn && currentTurnDirection == TurnDirection.Left && false) // AND NOT SAFE TO PROCEED!!!
                // {
                //     // Wait for left turn
                //     // navMeshAgent.isStopped = true;

                //     // Debug.Log($"{gameObject.name} waiting for left turn at intersection {currentIntersection.name}");
                // }
                // else if (isWaitingForRightTurn && currentTurnDirection == TurnDirection.Right && false) // AND NOT SAFE TO PROCEED!!!
                // {
                //     // Wait for right turn
                //     // navMeshAgent.isStopped = true;
                //     // Debug.Log($"{gameObject.name} waiting for right turn at intersection {currentIntersection.name}");
                // }
                // else
                // {
                //     // Proceed through intersection if not waiting
                //     // Debug.Log($"{gameObject.name} proceeding through intersection {currentIntersection.name} to turn {currentTurnDirection.ToString()}");
                //     SetNextWaypointFromQueue();
                //     // navMeshAgent.speed *= 0.5f;
                //     navMeshAgent.isStopped = false;
                //     isProceedingThroughIntersection = true;
                //     isWaitingForLeftTurn = false;
                //     isWaitingForRightTurn = false;
                // }
            }
            else if (currentIntersection != null && currentIntersection.direction != getGeneralMovementDirection())
            {
                // Continue waiting at the intersection stop point
                navMeshAgent.isStopped = false; // Run red so that you're not in intersection
                isProceedingThroughIntersection = true;
                // Debug.Log($"{gameObject.name} is running the red light at intersection {currentIntersection.name} for turn direction");
            }
            else
            {
                // navMeshAgent.isStopped = true; // Stop if not in the correct direction
                // // Debug.Log("Stopped at intersection waiting for turn direction");
            }
        }
        else if (isProceedingThroughIntersection)
        {
            // Handle logic for proceeding through the intersection
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
            {
                isProceedingThroughIntersection = false;
                isApproachingIntersection = false;
                navMeshAgent.isStopped = false;
                navMeshAgent.stoppingDistance = 1f;
                ResetAgentToDefaults();
                currentIntersection = null; // Clear the intersection reference

                // Set new destination 20 units forward after exiting intersection
                Vector3 directionVectorProceed;
                TrafficDirection generalDirProceed = getGeneralMovementDirection();
                if (generalDirProceed == TrafficDirection.X)
                {
                    directionVectorProceed = (transform.forward.x >= 0) ? Vector3.right : Vector3.left;
                }
                else // TrafficDirection.Z
                {
                    directionVectorProceed = (transform.forward.z >= 0) ? Vector3.forward : Vector3.back;
                }
                Vector3 forwardPoint = transform.position + directionVectorProceed * 20f;
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, navMeshAgent.areaMask))
                {
                    originalDestination = navMeshHit.position;
                    hasOriginalDestination = true;
                    waypointQueue.Clear();
                    waypointQueue.Enqueue(originalDestination);

                    SetNextWaypointFromQueue();
                }
                else
                {
                    // Fallback: continue to next waypoint in queue
                    if (waypointQueue.Count > 0)
                    {

                        // SetNextWaypointFromQueue();
                    }
                    else if (hasOriginalDestination)
                    {
                        // Re-add original destination if queue is empty
                        // waypointQueue.Enqueue(originalDestination);

                        // SetNextWaypointFromQueue();
                    }
                }
            }
        }
        // Check if reached current waypoint and move to next
        else if (navMeshAgent.hasPath && !navMeshAgent.pathPending &&
                 navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * navMeshAgent.speed / 3)
        {
            if (waypointQueue.Count > 0)
            {
                SetNextWaypointFromQueue();
            }
            else
            {
                // Re-add original destination if queue is empty
                Vector3 directionVectorReached;
                TrafficDirection generalDirReached = getGeneralMovementDirection();
                if (generalDirReached == TrafficDirection.X)
                {
                    directionVectorReached = (transform.forward.x >= 0) ? Vector3.right : Vector3.left;
                }
                else // TrafficDirection.Z
                {
                    directionVectorReached = (transform.forward.z >= 0) ? Vector3.forward : Vector3.back;
                }
                Vector3 forwardPoint = transform.position + directionVectorReached * 30f;
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, NavMesh.AllAreas))
                {
                    waypointQueue.Enqueue(navMeshHit.position);
                }
                else
                {
                    // Fallback to original destination if a point in front cannot be found
                    // waypointQueue.Enqueue(originalDestination);
                    // Debug.LogWarning($"{gameObject.name}: Could not find a NavMesh point 30 units in front. Re-adding original destination.");
                }

                SetNextWaypointFromQueue();
            }
        }
        
        // Debug draw path
        if (navMeshAgent.hasPath)
        {
            Debug.DrawLine(transform.position, navMeshAgent.pathEndPosition, Color.yellow);
        }
    }

    void HandleRaycastDetection()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, forwardDetectionDistance))
        {
            Debug.DrawLine(rayOrigin, hit.point, Color.red);

            // Check for intersection first
            // Must be new intersection hit
            if (hit.collider.CompareTag(intersectionTag) && currentIntersection == null)
            {
                navMeshAgent.isStopped = true; // Stop the agent when approaching an intersection
                currentIntersection = hit.collider.GetComponentInParent<Intersection2>();
                laneController = currentIntersection.GetIntersectionLaneController(GetDetailedMovementDirection(), currentLane);
                // Debug.Log("LaneController: " + laneController.name);

                if (currentIntersection == null)
                {
                    // Debug.LogWarning($"{gameObject.name} hit an intersection but no Intersection component found.");
                    return;
                }

                if (!isApproachingIntersection && !isProceedingThroughIntersection)
                {
                    NavMeshHit navMeshHitOnIntersection;
                    if (NavMesh.SamplePosition(hit.point, out navMeshHitOnIntersection, 10f, NavMesh.AllAreas))
                    {
                        // Insert intersection waypoint at front of queue
                        Queue<Vector3> tempQueue = new Queue<Vector3>();
                        tempQueue.Enqueue(navMeshHitOnIntersection.position);
                        navMeshAgent.stoppingDistance = 1f;

                        // Add intersection exit waypoint
                        currentTurnDirection = laneController.GetPermittedTurnDirections()[UnityEngine.Random.Range(0, laneController.GetPermittedTurnDirections().Count)];

                        List<Vector3> intersectionWaypoints = laneController.GetIntersectionPositions(currentTurnDirection);

                        if (currentIntersection.direction == getGeneralMovementDirection()
                            && hit.distance < hitDistanceToRegisterIntersectionPresence && currentTurnDirection != TurnDirection.Right)
                        {
                            // Debug.Log("Adding: " + gameObject.GetInstanceID());
                            currentIntersection.incomingCarsFromActiveDirection[GetDetailedMovementDirection()[0]].Add(gameObject.GetInstanceID()); // Mark incoming car from the current direction
                        }

                        // add all waypoints for the intersection to the queue
                        foreach (Vector3 waypoint in intersectionWaypoints)
                        {
                            if (NavMesh.SamplePosition(waypoint, out NavMeshHit hitWaypoint, 10f, NavMesh.AllAreas))
                            {
                                tempQueue.Enqueue(hitWaypoint.position);
                            }
                            else
                            {
                                // Debug.LogWarning($"{gameObject.name} could not find a valid NavMesh point for intersection waypoint at {waypoint}. Skipping this waypoint.");
                            }
                        }

                        // Re-add original destination
                        if (hasOriginalDestination)
                        {
                            // tempQueue.Enqueue(originalDestination);
                        }

                        // Add any remaining waypoints from current queue
                        while (waypointQueue.Count > 0)
                        {
                            // tempQueue.Enqueue(waypointQueue.Dequeue());
                        }

                        waypointQueue = tempQueue;
                        if (currentTurnDirection == TurnDirection.Left)
                        {
                            // currentLane = 1; // Assuming left turn means moving to lane index 1, closest to center
                        }
                        else if (currentTurnDirection == TurnDirection.Right)
                        {
                            // currentLane = 2; // Assuming right turn means moving to lane index 2, farthest from center
                        }
                        else
                        {
                            // Straight or U-turn, stay in current lane index from center
                        }

                        isApproachingIntersection = true;
                        navMeshAgent.isStopped = false;

                        SetNextWaypointFromQueue();
                    }
                    else
                    {
                        // Debug.LogWarning($"{gameObject.name} could not find a valid NavMesh point near intersection hit at {hit.point}. Proceeding with caution.");
                        // navMeshAgent.speed = normalSpeed * 0.25f;
                    }
                }
            }

            // Check for other vehicles - use speed modulation instead of following
            if ((vehicleLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0)
            {
                float distanceToVehicle = hit.distance;

                if (distanceToVehicle <= vehicleStopDistance)
                {
                    // Calculate speed based on distance (speed reaches 0 at vehicleStopDistance)
                    float speedMultiplier = Mathf.Clamp01((distanceToVehicle - 2f) / (vehicleStopDistance - 2f));
                    navMeshAgent.speed = normalSpeed * speedMultiplier;

                    if (speedMultiplier <= 0.1f)
                    {
                        navMeshAgent.speed = 0f;
                        navMeshAgent.isStopped = true;
                    }
                    else
                    {
                        navMeshAgent.isStopped = false;
                    }
                }
                else
                {
                    // Vehicle is far enough, maintain normal speed
                    navMeshAgent.speed = normalSpeed;
                    navMeshAgent.isStopped = false;
                }
                return; // Vehicle detection handled
            }
            else
            {
                // No vehicles detected, reset speed to normal
                navMeshAgent.speed = normalSpeed;
            }
        }

        // No obstacles detected, maintain normal speed and travel smoothly through waypoints
        if (!isApproachingIntersection && !isProceedingThroughIntersection && !isStoppedAtIntersection && !isWaitingForLeftTurn && !isWaitingForRightTurn)
        {
            navMeshAgent.speed = normalSpeed;
            navMeshAgent.isStopped = false;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
            if (navMeshAgent.hasPath && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * navMeshAgent.speed / 3)
            {
                // If we have a path and are close to the destination, we can consider it reached
                if (waypointQueue.Count > 0)
                {
                    
                    SetNextWaypointFromQueue();
                }
            }
        }
    }

    Vector3 GetIntersectionExitWaypoint(TurnDirection turnDirection)
    {
        if (currentIntersection != null)
        {
            return currentIntersection.GetDestinationForTurn(transform.position, turnDirection);
        }
        return Vector3.zero;
    }

    void SetNextWaypointFromQueue()
    {
        // Debug.Log("Next Waypoint");
        if (waypointQueue.Count > 0)
        {
            Vector3 nextWaypoint = waypointQueue.Dequeue();
            navMeshAgent.SetDestination(nextWaypoint);
            navMeshAgent.stoppingDistance = 1f;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
            navMeshAgent.isStopped = false;
            hasTargetDestination = true;
        }
        else
        {
            // Re-add original destination if queue is empty
            Vector3 directionVectorReached;
            TrafficDirection generalDirReached = getGeneralMovementDirection();
            if (generalDirReached == TrafficDirection.X)
            {
                directionVectorReached = (transform.forward.x >= 0) ? Vector3.right : Vector3.left;
            }
            else // TrafficDirection.Z
            {
                directionVectorReached = (transform.forward.z >= 0) ? Vector3.forward : Vector3.back;
            }
            Vector3 forwardPoint = transform.position + directionVectorReached * 30f;
            NavMeshHit navMeshHit;
            if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, NavMesh.AllAreas))
            {
                waypointQueue.Enqueue(navMeshHit.position);
            }
            else
            {
                // Fallback to original destination if a point in front cannot be found
                // waypointQueue.Enqueue(originalDestination);
                // Debug.LogWarning($"{gameObject.name}: Could not find a NavMesh point 30 units in front. Re-adding original destination.");
            }

            SetNextWaypointFromQueue();
        }
    }

    public TrafficDirection getGeneralMovementDirection()
    {
        // Determine the dominant axis of the car's forward direction
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            return TrafficDirection.X;
        }
        else
        {
            return TrafficDirection.Z;
        }
    }

    public string GetDetailedMovementDirection()
    {
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            // Movement is primarily along the X-axis
            if (forward.x > 0)
            {
                return "+x";
            }
            else
            {
                return "-x";
            }
        }
        else
        {
            // Movement is primarily along the Z-axis
            if (forward.z > 0)
            {
                return "+z";
            }
            else
            {
                return "-z";
            }
        }
    }

    void SetIntersectionTurnDestination(TurnDirection turnDirection)
    {
        if (currentIntersection != null)
        {
            // Get current travel direction
            TrafficDirection currentTrafficDirection = getGeneralMovementDirection();
            string currentLane = null;
            string targetLane = null;

            // Determine current lane direction (+z, -z, +x, -x)
            if (currentTrafficDirection == TrafficDirection.Z)
            {
                currentLane = transform.forward.z > 0 ? "+z" : "-z";
            }
            else // TrafficDirection.X
            {
                currentLane = transform.forward.x > 0 ? "+x" : "-x";
            }

            // Determine resulting direction based on current direction and requested turn
            switch (currentLane)
            {
                case "+z": // Moving north
                    switch (turnDirection)
                    {
                        case TurnDirection.Right: targetLane = "+x"; break;
                        case TurnDirection.Left: targetLane = "-x"; break;
                        case TurnDirection.Straight: targetLane = "+z"; break;
                        case TurnDirection.UTurn: targetLane = "-z"; break;
                    }
                    break;

                case "-z": // Moving south
                    switch (turnDirection)
                    {
                        case TurnDirection.Right: targetLane = "-x"; break;
                        case TurnDirection.Left: targetLane = "+x"; break;
                        case TurnDirection.Straight: targetLane = "-z"; break;
                        case TurnDirection.UTurn: targetLane = "+z"; break;
                    }
                    break;

                case "+x": // Moving east
                    switch (turnDirection)
                    {
                        case TurnDirection.Right: targetLane = "-z"; break;
                        case TurnDirection.Left: targetLane = "+z"; break;
                        case TurnDirection.Straight: targetLane = "+x"; break;
                        case TurnDirection.UTurn: targetLane = "-x"; break;
                    }
                    break;

                case "-x": // Moving west
                    switch (turnDirection)
                    {
                        case TurnDirection.Right: targetLane = "+z"; break;
                        case TurnDirection.Left: targetLane = "-z"; break;
                        case TurnDirection.Straight: targetLane = "-x"; break;
                        case TurnDirection.UTurn: targetLane = "+x"; break;
                    }
                    break;
            }

            // Set the destination to the intersection lane marker
            foreach (GameObject item in currentIntersection.laneMarkers)
            {
                if (item.name == targetLane)
                {
                    intersectionDestination = item.transform.position;
                    navMeshAgent.SetDestination(intersectionDestination);
                    // Debug.Log($"{gameObject.name} moving to intersection lane marker: {item.name} at {intersectionDestination}");
                    return;
                }
            }
        }
    }

    public void ResetAgentToDefaults()
    {
        // Reset NavMesh properties
        navMeshAgent.speed = normalSpeed;
        navMeshAgent.angularSpeed = rotationSpeed;
        navMeshAgent.acceleration = acceleration;
        navMeshAgent.stoppingDistance = 1f;
        
        // Reset obstacle avoidance
        if (navMeshAgent.isOnNavMesh) 
            navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
        
        // Reset behavior flags
        isApproachingIntersection = false;
        isStoppedAtIntersection = false;
        isProceedingThroughIntersection = false;
        
        // Reset path if needed
        if (navMeshAgent.hasPath) 
            navMeshAgent.ResetPath();
        
        // Ensure agent can move
        navMeshAgent.isStopped = false;

        // Remove car agent from intersection presence tracking
        if (currentIntersection != null && currentIntersection.incomingCarsFromActiveDirection.ContainsKey(laneController.gameObject.name[5]))
        {
            currentIntersection.incomingCarsFromActiveDirection[laneController.gameObject.name[5]].Remove(gameObject.GetInstanceID());
        }
        
        // Reset intersection state
        currentIntersection = null;
        laneController = null;
        
        // Debug.Log($"{gameObject.name}: Agent reset to default properties");
    }

    // Call this method from an external script to assign a new primary destination
    public void SetPrimaryDestination(Vector3 destination)
    {
        originalDestination = destination;
        hasOriginalDestination = true;
        
        // Clear current queue and set new destination
        waypointQueue.Clear();
        waypointQueue.Enqueue(destination);
        
        navMeshAgent.SetDestination(destination);
        navMeshAgent.stoppingDistance = 1f;
        if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
        isApproachingIntersection = false;
        navMeshAgent.isStopped = false;
        hasTargetDestination = true;
        
        // Clear old waypoints
        waypoints = null; 
        currentWaypointIndex = 0;
    }
}
