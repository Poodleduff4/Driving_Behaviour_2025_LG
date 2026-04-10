using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent
using System.Collections.Generic;
using Unity.AI.Navigation;

// Placeholder for IntersectionController is removed.
// We will now use the actual Intersection class.

public class BicycleController : MonoBehaviour
{
    private NavMeshAgent agent;
    public float raycastDistance = 20f;
    public LayerMask intersectionLayerMask; // Set this in the Inspector to filter raycasts
    // public float walkRadius = 20f; // Radius for sampling new random destinations // Removed
    public Vector3 navMeshWorldBoundsCenter = Vector3.zero; // Center of the area to sample NavMesh points from
    public Vector3 navMeshWorldBoundsSize = new Vector3(200f, 20f, 200f); // Size of the area

    private const float NAVMESH_SAMPLE_PROXIMITY = 20.0f; // How far from a random point to search for NavMesh
    private const int MAX_SAMPLE_ATTEMPTS = 30; // Max attempts to find a random NavMesh point

    private bool isWaitingAtIntersection = false;
    private bool isProceedingThroughIntersection = false;
    private bool isApproachingIntersection = false;
    private TrafficDirection requiredDirectionToProceed; // This will now use the external enum
    public Intersection2 currentIntersection;
    public BikeLaneController bikeLaneController;
    public TurnDirection turnDirection; // Assuming TurnDirection is an enum defined elsewhere
    public Queue<Vector3> waypointQueue = new Queue<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            enabled = false; // Disable script if no agent
            return;
        }
        Debug.Log("Agent Mask: " + agent.areaMask, this);

        SetNewRandomDestination();
    }

    // Update is called once per frame
    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // Raycast to detect intersections ahead
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Raycast from a bit above ground
        Debug.DrawRay(rayOrigin, transform.forward * raycastDistance, Color.yellow);

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, raycastDistance, intersectionLayerMask))
        {
            if (hit.collider.CompareTag("intersection") && !isApproachingIntersection && !isWaitingAtIntersection && !isProceedingThroughIntersection)
            {
                currentIntersection = hit.collider.GetComponent<Intersection2>();
                if (currentIntersection == null)
                {
                    Debug.LogError("No Intersection2 component found on " + hit.collider.name, this);
                    return; // Exit if no intersection controller is found
                }
                if (currentIntersection.direction == getGeneralMovementDirection()) //  && currentTurnDirection != TurnDirection.Right
                {
                    // Debug.Log("Adding: " + gameObject.GetInstanceID());
                    currentIntersection.incomingBikesFromActiveDirection[bikeLaneController.gameObject.name[10]].Add(gameObject.GetInstanceID()); // Mark incoming car from the current direction
                }
                if (bikeLaneController == null || bikeLaneController.gameObject != hit.collider.gameObject)
                {
                    // Debug.Log($"Pedestrian {gameObject.name} detected intersection: {hit.collider.name}", this);
                    // On first intersection detection, set the turn destination point
                    NavMeshHit navMeshHitOnIntersection;
                    if (NavMesh.SamplePosition(hit.point, out navMeshHitOnIntersection, 10f, agent.areaMask))
                    {
                        // Debug.Log($"Pedestrian {gameObject.name} sampled NavMesh point on intersection: {navMeshHitOnIntersection.position}", this);
                        bikeLaneController = currentIntersection.GetBikeLaneController(GetDetailedMovementDirection());
                        turnDirection = bikeLaneController.permittedTurnDirections[Random.Range(0, bikeLaneController.permittedTurnDirections.Count)];
                        // Insert intersection waypoint at front of queue
                        Queue<Vector3> tempQueue = new Queue<Vector3>();
                        tempQueue.Enqueue(navMeshHitOnIntersection.position);
                        tempQueue.Enqueue(bikeLaneController.getDestinationForDirection(turnDirection));
                        waypointQueue = tempQueue;
                        isApproachingIntersection = true;

                        SetNextWaypointFromQueue();
                    }
                }
            }
        }

        if (isApproachingIntersection)
        {
            Debug.Log($"Pedestrian {gameObject.name} is approaching intersection: {currentIntersection.name}", this);
            agent.isStopped = false; // Ensure the agent is moving towards the intersection
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Debug.Log("Distance: " + agent.remainingDistance + ", Stopping Distance: " + agent.stoppingDistance, this);
                // GREEN LIGHT
                if (currentIntersection != null && currentIntersection.direction == getGeneralMovementDirection())
                {
                    if (bikeLaneController.isSafeToTurn(turnDirection))
                    {
                        Debug.Log($"Pedestrian {gameObject.name} is proceeding through intersection: {currentIntersection.name}", this);
                        isWaitingAtIntersection = false;
                        isProceedingThroughIntersection = true;
                        isApproachingIntersection = false;
                        agent.isStopped = false;
                        SetNextWaypointFromQueue();
                    }
                    else
                    {
                        agent.isStopped = true;
                    }
                }
                // RED LIGHT
                else if (currentIntersection != null && currentIntersection.direction != getGeneralMovementDirection())
                {
                    isWaitingAtIntersection = true;
                    isApproachingIntersection = false;
                    isProceedingThroughIntersection = false;
                    agent.isStopped = true;
                }
            }
        }

        else if (isWaitingAtIntersection)
        {
            Debug.Log($"Pedestrian {gameObject.name} is waiting at intersection: {currentIntersection.name}", this);
            if (currentIntersection.direction == getGeneralMovementDirection() && bikeLaneController.isSafeToTurn(turnDirection))
            {
                isWaitingAtIntersection = false;
                isProceedingThroughIntersection = true;
                agent.isStopped = false;
                SetNextWaypointFromQueue();
            }
            else
            {
                agent.isStopped = true;
            }
        }

        else if (isProceedingThroughIntersection)
        {
            Debug.Log($"Pedestrian {gameObject.name} is proceeding through intersection: {currentIntersection.name}", this);
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isProceedingThroughIntersection = false;
                isWaitingAtIntersection = false;
                isApproachingIntersection = false;
                agent.isStopped = false;
                if (currentIntersection != null && currentIntersection.incomingBikesFromActiveDirection.ContainsKey(bikeLaneController.gameObject.name[10]))
                {
                    currentIntersection.incomingBikesFromActiveDirection[bikeLaneController.gameObject.name[10]].Remove(gameObject.GetInstanceID());
                }
                SetNextWaypointFromQueue();
        
            }
        }

        // Check if agent has reached its destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isApproachingIntersection && !isWaitingAtIntersection && !isProceedingThroughIntersection)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                SetNextWaypointFromQueue();
            }
        }
    }

    void SetNewRandomDestination()
    {
        for (int i = 0; i < MAX_SAMPLE_ATTEMPTS; i++)
        {
            float randomX = Random.Range(
                navMeshWorldBoundsCenter.x - navMeshWorldBoundsSize.x / 2,
                navMeshWorldBoundsCenter.x + navMeshWorldBoundsSize.x / 2
            );
            // Y-value sampling might need adjustment based on your NavMesh verticality.
            // For a mostly flat NavMesh, a smaller Y range or fixed Y might be better.
            float randomY = Random.Range(
                navMeshWorldBoundsCenter.y - navMeshWorldBoundsSize.y / 2,
                navMeshWorldBoundsCenter.y + navMeshWorldBoundsSize.y / 2
            );
            float randomZ = Random.Range(
                navMeshWorldBoundsCenter.z - navMeshWorldBoundsSize.z / 2,
                navMeshWorldBoundsCenter.z + navMeshWorldBoundsSize.z / 2
            );

            Vector3 randomPointSource = new Vector3(randomX, randomY, randomZ);
            NavMeshHit navHit;

            if (NavMesh.SamplePosition(randomPointSource, out navHit, NAVMESH_SAMPLE_PROXIMITY, agent.areaMask))
            {
                // agent.SetDestination(navHit.position);
                waypointQueue.Enqueue(navHit.position);
                SetNextWaypointFromQueue();
                if (agent.isStopped) // Ensure agent is not stopped if it was previously
                {
                    agent.isStopped = false;
                }
                return; // Successfully set a new destination
            }
        }
        Debug.LogWarning($"Pedestrian {gameObject.name} could not find a valid NavMesh point after {MAX_SAMPLE_ATTEMPTS} attempts within the defined bounds. It might stop or continue its current path.", this);
    }

    void SetNextWaypointFromQueue()
    {
        Debug.Log("Next Waypoint");
        if (waypointQueue.Count > 0)
        {
            Vector3 nextWaypoint = waypointQueue.Dequeue();
            agent.SetDestination(nextWaypoint);
            agent.stoppingDistance = 1f;
            agent.isStopped = false;
        }
        else
        {
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
}
