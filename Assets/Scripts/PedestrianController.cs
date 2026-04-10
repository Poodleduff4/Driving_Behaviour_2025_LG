using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

// Placeholder for IntersectionController is removed.
// We will now use the actual Intersection class.

public class PedestrianController : MonoBehaviour
{
    private NavMeshAgent agent;
    public float raycastDistance = 3f;
    public LayerMask intersectionLayerMask; // Set this in the Inspector to filter raycasts
    // public float walkRadius = 20f; // Radius for sampling new random destinations // Removed
    public Vector3 navMeshWorldBoundsCenter = Vector3.zero; // Center of the area to sample NavMesh points from
    public Vector3 navMeshWorldBoundsSize = new Vector3(200f, 20f, 200f); // Size of the area
    
    private const float NAVMESH_SAMPLE_PROXIMITY = 10.0f; // How far from a random point to search for NavMesh
    private const int MAX_SAMPLE_ATTEMPTS = 30; // Max attempts to find a random NavMesh point

    private bool isWaitingAtIntersection = false;
    private CrosswalkController currentCrosswalkController; // Changed type to CrosswalkController
    private TrafficDirection requiredDirectionToProceed; // This will now use the external enum
    
    Animator animator; // Animator for pedestrian animations (if needed)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Assuming you have an Animator component for animations
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            enabled = false; // Disable script if no agent
            return;
        }

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
            Debug.Log($"Pedestrian {gameObject.name} detected intersection: {hit.collider.name}", this);
            if (hit.collider.CompareTag("Crosswalk") && !isWaitingAtIntersection)
            {
                CrosswalkController crosswalkController = hit.collider.GetComponent<CrosswalkController>(); // Changed to GetComponent<Intersection2>()
                if (crosswalkController != null)
                {
                    // Check if the crosswalk is clear before proceeding
                    if (crosswalkController.isSafeToCross())
                    {
                        Debug.Log($"Pedestrian {gameObject.name} proceeding through crosswalk: {crosswalkController.name}", this);
                        // Proceed through the crosswalk
                        isWaitingAtIntersection = false;
                        agent.isStopped = false;
                        animator.SetBool("isStopped", false); // Assuming you have an animation for walking
                        currentCrosswalkController = null; // Reset controller
                    }
                    else
                    {
                        Debug.Log($"Pedestrian {gameObject.name} waiting at crosswalk: {crosswalkController.name}", this);
                        // Wait at the intersection
                        isWaitingAtIntersection = true;
                        agent.isStopped = true;
                        animator.SetBool("isStopped", true);
                        currentCrosswalkController = crosswalkController; // Set the current controller
                                                                          // requiredDirectionToProceed = getGeneralMovementDirection(); // Determine direction to proceed
                                                                          // Debug.Log("Waiting at crosswalk.", this);
                    }

                }
            }
        }

        // Movement logic if not waiting
        if (!isWaitingAtIntersection)
        {
            // Check if agent has reached its destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    SetNewRandomDestination();
                }
            }
        }




        if (isWaitingAtIntersection)
        {
            if (currentCrosswalkController != null)
            {
                // Check if the intersection allows proceeding
                // Changed to use currentIntersectionController.direction
                if (currentCrosswalkController.isSafeToCross())
                {
                    isWaitingAtIntersection = false;
                    agent.isStopped = false;

                    currentCrosswalkController = null;
                    // Debug.Log("Proceeding through intersection.", this);
                }
                else
                {
                    return; // Still waiting
                }
            }
            else
            {
                // Safety: if controller is lost, resume (or handle error)
                isWaitingAtIntersection = false;
                agent.isStopped = false;
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

            if (NavMesh.SamplePosition(randomPointSource, out navHit, NAVMESH_SAMPLE_PROXIMITY, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
                if (agent.isStopped) // Ensure agent is not stopped if it was previously
                {
                    agent.isStopped = false;
                }
                return; // Successfully set a new destination
            }
        }
        Debug.LogWarning($"Pedestrian {gameObject.name} could not find a valid NavMesh point after {MAX_SAMPLE_ATTEMPTS} attempts within the defined bounds. It might stop or continue its current path.", this);
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
}
