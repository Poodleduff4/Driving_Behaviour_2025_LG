// using System;
// using UnityEngine;
// using UnityEngine.AI;

// public class Downtown_Car_Controller : MonoBehaviour
// {
//     [Header("Navigation")]
//     public float normalSpeed = 8f;
//     public float rotationSpeed = 120f;
//     public float acceleration = 8f;
//     public Transform[] waypoints; // Optional: for simple path following if not using a more complex destination system
//     private int currentWaypointIndex = 0;

//     [Header("Detection")]
//     public float forwardDetectionDistance = 40f;
//     public LayerMask vehicleLayerMask; // Set this in the inspector to the layer your vehicles are on
//     public string intersectionTag = "intersection"; // Ensure your intersection trigger colliders have this tag
//     public float vehicleAvoidanceDistance = 10f; // Distance to start slowing down for other vehicles
//     public float minDistanceToVehicle = 10f; // Minimum desired distance to keep from vehicle ahead (Changed to 10f)
//     // Removed: public float intersectionStoppingDistance = 5f; 

//     private NavMeshAgent navMeshAgent;
//     private bool isApproachingIntersection = false;
//     private Vector3 intersectionStopPosition;
//     private bool hasTargetDestination = false; // To ensure we have a primary goal
//     private ObstacleAvoidanceType originalObstacleAvoidanceType; // Added to store original setting
//     private bool isFollowingVehicle = false; // New flag for following behavior
//     private bool isProceedingThroughIntersection = false; // New flag for proceeding through intersection
//     private bool isStoppedAtIntersection = false; // New flag for stopped at intersection

//     private Intersection currentIntersection; // Optional: reference to the current intersection if needed

//     private Vector3 intersectionDestination;



//     void Start()
//     {
//         navMeshAgent = GetComponent<NavMeshAgent>();
//         if (navMeshAgent == null)
//         {
//             navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
//         }

//         navMeshAgent.speed = normalSpeed;
//         navMeshAgent.angularSpeed = rotationSpeed;
//         navMeshAgent.acceleration = acceleration;
//         navMeshAgent.stoppingDistance = 5f; // Default stopping distance for waypoints
//         originalObstacleAvoidanceType = navMeshAgent.obstacleAvoidanceType; // Store original avoidance type

//         // Example: Set initial destination
//         if (waypoints == null || waypoints.Length == 0)
//         {
//             Debug.Log($"{gameObject.name}: No waypoints assigned. Creating an initial forward waypoint.");
//             Vector3 forwardPoint = transform.position + transform.forward * 100f;
//             NavMeshHit navMeshHit;
//             if (NavMesh.SamplePosition(forwardPoint, out navMeshHit, 10.0f, NavMesh.AllAreas)) // Search within 5 units for a valid NavMesh point
//             {
//                 GameObject initialWaypointGO = new GameObject($"{gameObject.name}_InitialForwardWaypoint");
//                 initialWaypointGO.transform.position = navMeshHit.position;
//                 // Optional: Parent it to this car or a general waypoints holder for scene organization
//                 // initialWaypointGO.transform.SetParent(this.transform); 
                
//                 waypoints = new Transform[1];
//                 waypoints[0] = initialWaypointGO.transform;
//                 currentWaypointIndex = -1; // So SetNextWaypointDestination starts with index 0
//                 SetNextWaypointDestination();
//                 hasTargetDestination = true;
//             }
//             else
//             {
//                 Debug.LogWarning($"{gameObject.name}: Could not find a NavMesh point 100 units in front. Vehicle will need a destination set externally.");
//                 hasTargetDestination = false;
//                 // navMeshAgent.isStopped = true; // Optionally stop it
//             }
//         }
//         else if (waypoints != null && waypoints.Length > 0)
//         {
//             currentWaypointIndex = -1; // Ensure SetNextWaypointDestination starts with the first assigned waypoint
//             SetNextWaypointDestination();
//             hasTargetDestination = true;
//         }
//         else
//         {
//             Debug.LogWarning($"{gameObject.name}: No waypoints assigned and could not create initial. Vehicle will need a destination set externally.");
//             hasTargetDestination = false;
//             // navMeshAgent.isStopped = true; // Optionally stop it
//         }
//     }

//     void Update()
//     {
//         if (!navMeshAgent.isOnNavMesh || !hasTargetDestination) 
//         {
//             if (!hasTargetDestination && navMeshAgent.hasPath) navMeshAgent.ResetPath();
//             return;
//         }

//         HandleRaycastDetection();

//         if (isApproachingIntersection)
//         {
//             // do a last check for green light state to go through the intersection, otherwise stop at red light
//             if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * 1.5 && !navMeshAgent.pathPending && currentIntersection.direction == getGeneralMovementDirection())
//             {
//                     isProceedingThroughIntersection = true;
//                     isApproachingIntersection = false;
//                     navMeshAgent.isStopped = false; // Proceed through the intersection
//                     navMeshAgent.stoppingDistance = 2f;
//                     // SetIntersectionTurnDestination((TurnDirection)UnityEngine.Random.Range(0, 3)); // Proceed random direction through the intersection
//                     SetIntersectionTurnDestination(TurnDirection.Straight); // Proceed straight through the intersection
//             }
//             if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
//             {
//                 isProceedingThroughIntersection = false;
//                 isApproachingIntersection = false;
//                 isStoppedAtIntersection = true;
//                 navMeshAgent.isStopped = true;
//             }
//         }
//         else if (isStoppedAtIntersection)
//         {
//             // Handle logic for waiting at the intersection
//             // This could involve checking traffic lights, other vehicles, etc.
//             // For now, we just keep the vehicle stopped
//             if (currentIntersection.direction == getGeneralMovementDirection())
//             {
//                 // navMeshAgent.SetDestination(intersectionDestination);
//                 navMeshAgent.isStopped = false;
//                 isStoppedAtIntersection = false;
//                 isApproachingIntersection = false;
//                 isProceedingThroughIntersection = true;
//                 navMeshAgent.speed = normalSpeed; // Reset speed to normal
//             }
//             else
//             {
//                 navMeshAgent.isStopped = true;
//             }
//         }
//         else if (isProceedingThroughIntersection)
//         {
//             // Handle logic for proceeding through the intersection
//             // This could involve checking traffic lights, other vehicles, etc.
//             // For now, we just keep the vehicle moving
//             if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
//             {
//                 isProceedingThroughIntersection = false;
//                 isApproachingIntersection = false;
//                 navMeshAgent.isStopped = false;
//                 ResetAgentToDefaults();

//                 // Clean up any temporary waypoints to prevent memory leaks
//                 if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null && 
//                     waypoints[0].name.Contains("InitialForwardWaypoint"))
//                 {
//                     Destroy(waypoints[0].gameObject);
//                 }

//                 Vector3 forward = transform.forward;
//                 Vector3 snappedForward = new Vector3(
//                     Mathf.Abs(forward.x) > Mathf.Abs(forward.z) ? Mathf.Sign(forward.x) : 0f,
//                     0f,
//                     Mathf.Abs(forward.z) > Mathf.Abs(forward.x) ? Mathf.Sign(forward.z) : 0f
//                 );
//                 Vector3 nextClearPoint = transform.position + snappedForward * 20f;
//                 NavMeshHit hitInfo;
//                 if (NavMesh.SamplePosition(nextClearPoint, out hitInfo, 10.0f, NavMesh.AllAreas))
//                 {
//                     GameObject tempWaypoint = new GameObject($"{gameObject.name}_PostIntersectionWaypoint");
//                     tempWaypoint.transform.position = hitInfo.position;
                    
//                     // Create a new waypoints array with just this temporary waypoint
//                     Transform[] newWaypoints = new Transform[1];
//                     newWaypoints[0] = tempWaypoint.transform;
//                     waypoints = newWaypoints;
//                     currentWaypointIndex = 0;
                    
//                     navMeshAgent.SetDestination(hitInfo.position);
//                     navMeshAgent.stoppingDistance = 1f; // Standard stopping distance for a point
//                     if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//                     navMeshAgent.speed = normalSpeed; // Ensure normal speed
//                     isFollowingVehicle = false; // Ensure not in following mode
//                     hasTargetDestination = true; 
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"{gameObject.name}: Could not find NavMesh point 20 units ahead post-intersection. Attempting to resume original waypoint path or stopping.");
//                     // Fallback to original path logic. SetNextWaypointDestination will handle
//                     // cases where waypoints might be null or empty.
//                     // SetNextWaypointDestination(); 
//                 }
//                 currentIntersection = null; // Clear the intersection reference
//             }
//         }
//         else if (isFollowingVehicle)
//         {
//             // Vehicle following logic is already handled in HandleRaycastDetection()
//             // No additional action needed here
//         }
//         // Ensure waypoint progression only if not following a vehicle and not approaching an intersection
//         else if (!isFollowingVehicle && !isApproachingIntersection && waypoints != null && waypoints.Length > 0 &&
//                  navMeshAgent.hasPath && !navMeshAgent.pathPending &&
//                  navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
//         {
//             SetNextWaypointDestination();
//         }
        
//         // Debug draw path
//         if (navMeshAgent.hasPath)
//         {
//             Debug.DrawLine(transform.position, navMeshAgent.pathEndPosition, Color.yellow);
//         }
//     }

//     void HandleRaycastDetection()
//     {
//         RaycastHit hit;
//         Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; 

//         if (Physics.Raycast(rayOrigin, transform.forward, out hit, forwardDetectionDistance))
//         {
//             Debug.DrawLine(rayOrigin, hit.point, Color.red);

//             // Check for intersection first
//             if (hit.collider.CompareTag(intersectionTag))
//             {
                
//                 currentIntersection = hit.collider.GetComponent<Intersection>();
//                 if (currentIntersection == null)
//                 {
//                     Debug.LogWarning($"{gameObject.name} hit an intersection but no Intersection component found.");
//                     return;
//                 }
//                 // Debug.Log($"{gameObject.name} detected intersection: {hit.collider.name} with direction {trafficDirection}");
//                 // if (isFollowingVehicle)
//                 // {
//                 //     Debug.Log($"{gameObject.name} was following, but intersection detected. Prioritizing intersection.");
//                 //     isFollowingVehicle = false; // Cancel following
//                 //     // Restore typical non-following agent settings, intersection logic will override as needed
//                 //     if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//                 //     navMeshAgent.speed = normalSpeed; 
//                 // }


//                 // float distanceToIntersection = Vector3.Distance(transform.position, hit.point);
//                 // Debug.Log($"{gameObject.name} distance to intersection: {distanceToIntersection:F2}m");

//                 if (!isApproachingIntersection && !isProceedingThroughIntersection)
//                 {
//                     //Debug.Log($"{gameObject.name} detected intersection: {hit.collider.name} at {hit.point}");
//                     NavMeshHit navMeshHitOnIntersection;
//                     if (NavMesh.SamplePosition(hit.point, out navMeshHitOnIntersection, 1.0f, NavMesh.AllAreas))
//                     {
//                         intersectionStopPosition = navMeshHitOnIntersection.position;
//                         navMeshAgent.SetDestination(intersectionStopPosition);
//                         navMeshAgent.stoppingDistance = 5f; // Set a larger stopping distance for intersections
//                         isApproachingIntersection = true;
//                         navMeshAgent.isStopped = false; // Ensure the agent can move
//                         //Debug.Log($"{gameObject.name} approaching intersection stop point: {intersectionStopPosition}, agent stopping distance: {navMeshAgent.stoppingDistance}");
//                     }
//                     else
//                     {
//                         Debug.LogWarning($"{gameObject.name} could not find a valid NavMesh point near intersection hit at {hit.point}. Proceeding with caution.");
//                         navMeshAgent.speed = normalSpeed * 0.25f;
//                     }
//                 }
//                 return; // Intersection handled, priority
//             }

//             // Check for other vehicles if not an intersection
//             if ((vehicleLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0)
//             {
//                 float distanceToHit = hit.distance;
//                 //Debug.Log($"{gameObject.name} detected vehicle: {hit.collider.name}, Distance: {distanceToHit:F2}m");

//                 if (distanceToHit < vehicleAvoidanceDistance) // Start following if vehicle is within avoidance distance
//                 {
//                     // Calculate temporary destination minDistanceToVehicle behind the hit point
//                     Vector3 directionToHit = (hit.point - rayOrigin).normalized;
//                     Vector3 targetFollowPosition = hit.point - directionToHit * minDistanceToVehicle;

//                     NavMeshHit navMeshHitFollow;
//                     if (NavMesh.SamplePosition(targetFollowPosition, out navMeshHitFollow, 2.0f, NavMesh.AllAreas))
//                     {
//                         if (!isFollowingVehicle)
//                         {
//                              //Debug.Log($"{gameObject.name} started following {hit.collider.name}.");
//                         }
//                         isFollowingVehicle = true;
//                         navMeshAgent.SetDestination(navMeshHitFollow.position);
//                         navMeshAgent.stoppingDistance = 1.0f; // Stop reasonably close to this temp point
//                         navMeshAgent.speed = normalSpeed; // Move at normal speed towards it
//                         navMeshAgent.isStopped = false;
//                         if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        
//                         //Debug.Log($"{gameObject.name} following {hit.collider.name}. Temp Dest: {navMeshHitFollow.position}");
//                     }
//                     else
//                     {
//                         Debug.LogWarning($"{gameObject.name} could not sample NavMesh for follow position behind {hit.collider.name}. Slowing down as fallback.");
//                         navMeshAgent.speed = normalSpeed * 0.25f; // Fallback if cannot find valid follow point
//                         if (isFollowingVehicle) // If was following, but now can't find spot, revert to not following
//                         {
//                             isFollowingVehicle = false;
//                             if(navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//                             // Consider SetNextWaypointDestination() here if appropriate
//                         }
//                     }
//                 }
//                 else if (isFollowingVehicle) // Vehicle detected, but too far to continue following actively, revert.
//                 {
//                     //Debug.Log($"{gameObject.name} vehicle {hit.collider.name} too far ({distanceToHit:F2}m). Stopping follow. Resuming original path.");
//                     isFollowingVehicle = false;
//                     if(navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//                     navMeshAgent.speed = normalSpeed;
//                     navMeshAgent.isStopped = false;
//                     SetNextWaypointDestination(); // Resume normal path
//                 }
//                 return; // Vehicle detection handled
//             }
//         }
        
//         // No hit from forward raycast, or hit was not an intersection or vehicle
//         if (isFollowingVehicle) // Was following, but raycast no longer hits a vehicle in front
//         {
//             //Debug.Log($"{gameObject.name} lost sight of followed vehicle. Resuming original path.");
//             isFollowingVehicle = false;
//             if(navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//             navMeshAgent.speed = normalSpeed;
//             navMeshAgent.isStopped = false;
            
//             // Clean up any temporary waypoints before setting a new destination
//             if (waypoints != null && waypoints.Length > 0)
//             {
//                 foreach (Transform waypoint in waypoints)
//                 {
//                     if (waypoint != null && 
//                         (waypoint.name.Contains("InitialForwardWaypoint") || 
//                          waypoint.name.Contains("PostIntersectionWaypoint")))
//                     {
//                         Destroy(waypoint.gameObject);
//                     }
//                 }
//             }
            
//             SetNextWaypointDestination(); // Resume normal path
//         }
//         else if (!isApproachingIntersection) // Not following, not approaching intersection, clear path
//         {
//             navMeshAgent.speed = normalSpeed;
//             navMeshAgent.isStopped = false;
//             if(navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//         }
//     }

//     TrafficDirection getGeneralMovementDirection()
//     {
//         // Determine the dominant axis of the car's forward direction
//         Vector3 forward = transform.forward;
//         if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
//         {
//             return TrafficDirection.X;
//         }
//         else
//         {
//             return TrafficDirection.Z;
//         }
//     }

//     void SetIntersectionTurnDestination(TurnDirection turnDirection)
//     {
//         if (currentIntersection != null)
//         {
//             // Get current travel direction
//             TrafficDirection currentTrafficDirection = getGeneralMovementDirection();
//             string currentLane = null;
//             string targetLane = null;

//             // Determine current lane direction (+z, -z, +x, -x)
//             if (currentTrafficDirection == TrafficDirection.Z)
//             {
//                 currentLane = transform.forward.z > 0 ? "+z" : "-z";
//             }
//             else // TrafficDirection.X
//             {
//                 currentLane = transform.forward.x > 0 ? "+x" : "-x";
//             }

//             // Determine resulting direction based on current direction and requested turn
//             switch (currentLane)
//             {
//                 case "+z": // Moving north
//                     switch (turnDirection)
//                     {
//                         case TurnDirection.Right: targetLane = "+x"; break;
//                         case TurnDirection.Left: targetLane = "-x"; break;
//                         case TurnDirection.Straight: targetLane = "+z"; break;
//                         case TurnDirection.UTurn: targetLane = "-z"; break;
//                     }
//                     break;

//                 case "-z": // Moving south
//                     switch (turnDirection)
//                     {
//                         case TurnDirection.Right: targetLane = "-x"; break;
//                         case TurnDirection.Left: targetLane = "+x"; break;
//                         case TurnDirection.Straight: targetLane = "-z"; break;
//                         case TurnDirection.UTurn: targetLane = "+z"; break;
//                     }
//                     break;

//                 case "+x": // Moving east
//                     switch (turnDirection)
//                     {
//                         case TurnDirection.Right: targetLane = "-z"; break;
//                         case TurnDirection.Left: targetLane = "+z"; break;
//                         case TurnDirection.Straight: targetLane = "+x"; break;
//                         case TurnDirection.UTurn: targetLane = "-x"; break;
//                     }
//                     break;

//                 case "-x": // Moving west
//                     switch (turnDirection)
//                     {
//                         case TurnDirection.Right: targetLane = "+z"; break;
//                         case TurnDirection.Left: targetLane = "-z"; break;
//                         case TurnDirection.Straight: targetLane = "-x"; break;
//                         case TurnDirection.UTurn: targetLane = "+x"; break;
//                     }
//                     break;
//             }

//             // Debug.Log($"{gameObject.name} is on {currentLane}, turning {turnDirection}, will exit on {targetLane}");

//             // if (currentIntersection.direction == currentTrafficDirection)
//             // {
//             //     isProceedingThroughIntersection = true;
//             //     isStoppedAtIntersection = false;
//             //     navMeshAgent.isStopped = false; // Ensure the agent can move
//             //     navMeshAgent.speed = normalSpeed; // Reset speed to normal
//             //     navMeshAgent.stoppingDistance = 1f; // Set a smaller stopping distance for turns
//             // }
//             // else
//             // {
//             //     isStoppedAtIntersection = true;
//             //     isApproachingIntersection = false;
//             //     navMeshAgent.isStopped = true;
//             // }

//             // Set the destination to the intersection lane marker
//             foreach (GameObject item in currentIntersection.laneMarkers)
//             {
//                 if (item.name == targetLane)
//                 {
//                     intersectionDestination = item.transform.position;
//                     navMeshAgent.ResetPath();
//                     navMeshAgent.SetDestination(intersectionDestination);
//                     Debug.Log($"{gameObject.name} moving to intersection lane marker: {item.name} at {intersectionDestination}");
//                     return;
//                 }
//             }
//             //SetIntersectionTurnDestination((TurnDirection)UnityEngine.Random.Range(0, 3));
//         }
//     }

//     public void ResetAgentToDefaults()
//     {
//         // Reset NavMesh properties
//         navMeshAgent.speed = normalSpeed;
//         navMeshAgent.angularSpeed = rotationSpeed;
//         navMeshAgent.acceleration = acceleration;
//         navMeshAgent.stoppingDistance = 1f;
        
//         // Reset obstacle avoidance
//         if (navMeshAgent.isOnNavMesh) 
//             navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
        
//         // Reset behavior flags
//         isApproachingIntersection = false;
//         isStoppedAtIntersection = false;
//         isProceedingThroughIntersection = false;
//         isFollowingVehicle = false;
        
//         // Reset path if needed
//         if (navMeshAgent.hasPath) 
//             navMeshAgent.ResetPath();
        
//         // Ensure agent can move
//         navMeshAgent.isStopped = false;
        
//         // Reset intersection state
//         currentIntersection = null;
        
//         Debug.Log($"{gameObject.name}: Agent reset to default properties");
//     }

//     void SetNextWaypointDestination()
//     {
//         if (waypoints == null || waypoints.Length == 0)
//         {
//             hasTargetDestination = false;
//             if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
//             Debug.LogWarning($"{gameObject.name}: No waypoints to set destination.");
//             return;
//         }

//         currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
//         navMeshAgent.SetDestination(waypoints[currentWaypointIndex].position);
//         navMeshAgent.stoppingDistance = 1f; // Reset to default stopping distance for waypoints
//         if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//         // isApproachingIntersection = false; // This is usually set when intersection is cleared or handled by other logic
//                                         // For SetNextWaypointDestination, it implies we are moving to a general waypoint, so clear it.
//         isApproachingIntersection = false; 
//         isFollowingVehicle = false; // Ensure not in following mode when setting a new general waypoint
//         navMeshAgent.isStopped = false; // Ensure agent can move
//         hasTargetDestination = true;
//         //Debug.Log($"{gameObject.name} heading to waypoint {currentWaypointIndex}: {waypoints[currentWaypointIndex].position}");
//     }

//     // Call this method from an external script to assign a new primary destination
//     public void SetPrimaryDestination(Vector3 destination)
//     {
//         navMeshAgent.SetDestination(destination);
//         navMeshAgent.stoppingDistance = 1f; // Default stopping distance
//         if (navMeshAgent.isOnNavMesh) navMeshAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
//         isApproachingIntersection = false;
//         navMeshAgent.isStopped = false;
//         hasTargetDestination = true;
//         // If using waypoints, you might want to clear them or find the closest one
//         waypoints = null; 
//         currentWaypointIndex = 0;
//         //Debug.Log($"{gameObject.name} new primary destination set to: {destination}");
//     }
// }
