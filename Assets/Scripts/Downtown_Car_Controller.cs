using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Downtown_Car_Controller : MonoBehaviour
{
    [Header("Navigation")]
    public float normalSpeed = 12f;
    public float rotationSpeed = 120f;
    public float acceleration = 8f;
    public Transform[] waypoints; // Optional: for simple path following if not using a more complex destination system
    private int currentWaypointIndex = 0;

    [Header("Detection")]
    public float forwardDetectionDistance = 40f;
    public LayerMask vehicleLayerMask; // Set this in the inspector to the layer your vehicles are on
    public string intersectionTag = "intersection"; // Ensure your intersection trigger colliders have this tag
    public float vehicleAvoidanceDistance = 30f; // Distance to start slowing down for other vehicles
    public float vehicleStopDistance = 15f; // Distance at which speed reaches 0
    // Removed: public float intersectionStoppingDistance = 5f; 

    private NavMeshAgent navMeshAgent;
    private bool isApproachingIntersection = false;
    private Vector3 intersectionStopPosition;
    private bool hasTargetDestination = false; // To ensure we have a primary goal
    private ObstacleAvoidanceType originalObstacleAvoidanceType; // Added to store original setting
    private bool isProceedingThroughIntersection = false; // New flag for proceeding through intersection
    private bool isStoppedAtIntersection = false; // New flag for stopped at intersection

    private Intersection currentIntersection; // Optional: reference to the current intersection if needed

    private Vector3 intersectionDestination;

    // New waypoint queue system
    private Queue<Vector3> waypointQueue = new Queue<Vector3>();
    private Vector3 originalDestination;
    private bool hasOriginalDestination = false;

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
        Vector3 forwardPoint = transform.position + transform.forward * 50f;
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
            Debug.LogWarning($"{gameObject.name}: Could not find a NavMesh point 50 units in front. Vehicle will need a destination set externally.");
            hasTargetDestination = false;
        }
    }

    void Update()
    {
        if (!navMeshAgent.isOnNavMesh || !hasTargetDestination) 
        {
            if (!hasTargetDestination && navMeshAgent.hasPath) navMeshAgent.ResetPath();
            return;
        }

        HandleRaycastDetection();

        if (isApproachingIntersection)
        {
            // Check if we're close to the intersection waypoint and can proceed
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * navMeshAgent.speed/3 && !navMeshAgent.pathPending && currentIntersection.direction == getGeneralMovementDirection())
            {
                Debug.Log($"{gameObject.name} is proceeding through intersection at {currentIntersection.transform.position}");
                isProceedingThroughIntersection = true;
                isApproachingIntersection = false;
                navMeshAgent.isStopped = false; // Proceed through the intersection
                navMeshAgent.stoppingDistance = 2f;
                // SetIntersectionTurnDestination((TurnDirection)UnityEngine.Random.Range(0, 3)); // Proceed random direction through the intersection
                SetIntersectionTurnDestination((TurnDirection)UnityEngine.Random.Range(0, 3)); // Proceed random direction through the intersection
            }
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
            {
                isProceedingThroughIntersection = false;
                isApproachingIntersection = false;
                isStoppedAtIntersection = true;
                navMeshAgent.isStopped = true;
            }
        }
        else if (isStoppedAtIntersection)
        {
            // Handle logic for waiting at the intersection
            if (currentIntersection.direction == getGeneralMovementDirection())
            {
                navMeshAgent.isStopped = false;
                isStoppedAtIntersection = false;
                isApproachingIntersection = false;
                isProceedingThroughIntersection = true;
                navMeshAgent.speed = normalSpeed; // Reset speed to normal
            }
            else
            {
                navMeshAgent.isStopped = true;
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

                // Set new destination 50 units forward after exiting intersection
                Vector3 forwardPoint = transform.position + transform.forward * 20f;
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, NavMesh.AllAreas))
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
                        SetNextWaypointFromQueue();
                    }
                    else if (hasOriginalDestination)
                    {
                        // Re-add original destination if queue is empty
                        waypointQueue.Enqueue(originalDestination);
                        SetNextWaypointFromQueue();
                    }
                }
            }
        }
        // Check if reached current waypoint and move to next
        else if (navMeshAgent.hasPath && !navMeshAgent.pathPending &&
                 navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (waypointQueue.Count > 0)
            {
                SetNextWaypointFromQueue();
            }
            else if (hasOriginalDestination)
            {
                // Re-add original destination if queue is empty
                waypointQueue.Enqueue(originalDestination);
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
            if (hit.collider.CompareTag(intersectionTag))
            {
                currentIntersection = hit.collider.GetComponent<Intersection>();
                if (currentIntersection == null)
                {
                    Debug.LogWarning($"{gameObject.name} hit an intersection but no Intersection component found.");
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
                        navMeshAgent.stoppingDistance = 10f;
                        
                        // Add intersection exit waypoint
                        Vector3 exitWaypoint = GetIntersectionExitWaypoint(TurnDirection.Straight);
                        if (exitWaypoint != Vector3.zero)
                        {
                            tempQueue.Enqueue(exitWaypoint);
                        }
                        
                        // Re-add original destination
                        if (hasOriginalDestination)
                        {
                            tempQueue.Enqueue(originalDestination);
                        }
                        
                        // Add any remaining waypoints from current queue
                        while (waypointQueue.Count > 0)
                        {
                            tempQueue.Enqueue(waypointQueue.Dequeue());
                        }
                        
                        waypointQueue = tempQueue;
                        
                        isApproachingIntersection = true;
                        navMeshAgent.isStopped = false;
                        SetNextWaypointFromQueue();
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name} could not find a valid NavMesh point near intersection hit at {hit.point}. Proceeding with caution.");
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
        }
        
        // No obstacles detected, maintain normal speed
        if (!isApproachingIntersection && !isProceedingThroughIntersection && !isStoppedAtIntersection)
        {
            navMeshAgent.speed = normalSpeed;
            navMeshAgent.isStopped = false;
            if(navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
        }
    }

    Vector3 GetIntersectionExitWaypoint(TurnDirection turnDirection)
    {
        if (currentIntersection != null)
        {
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

            // Find the target lane marker
            foreach (GameObject item in currentIntersection.laneMarkers)
            {
                if (item.name == targetLane)
                {
                    return item.transform.position;
                }
            }
            return GetIntersectionExitWaypoint((TurnDirection)UnityEngine.Random.Range(0, 3)); // Fallback to random turn if no lane found
        }
        return Vector3.zero;
    }

    void SetNextWaypointFromQueue()
    {
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
            hasTargetDestination = false;
            if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
        }
    }

    TrafficDirection getGeneralMovementDirection()
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
                    Debug.Log($"{gameObject.name} moving to intersection lane marker: {item.name} at {intersectionDestination}");
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
        
        // Reset intersection state
        currentIntersection = null;
        
        Debug.Log($"{gameObject.name}: Agent reset to default properties");
    }

    void SetNextWaypointDestination()
    {
        // This method is now replaced by SetNextWaypointFromQueue()
        // Keeping for backward compatibility but redirecting to new system
        if (waypointQueue.Count > 0)
        {
            SetNextWaypointFromQueue();
        }
        else if (hasOriginalDestination)
        {
            waypointQueue.Enqueue(originalDestination);
            SetNextWaypointFromQueue();
        }
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
