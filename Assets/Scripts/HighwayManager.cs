using System;
using System.Collections.Generic;
using UnityEngine;

public class HighwayManager : MonoBehaviour
{
    [Header("Highway Settings")]
    public float highwayLength = 1000f;
    public int numberOfLanes = 3;
    public float laneWidth = 4f;
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("Traffic Settings")]
    public GameObject[] vehiclePrefabs;
    public int maxVehicles = 20;
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 5f;
    
    [Header("Events")]
    public float eventChance = 0.05f; // Chance per second of triggering a traffic event
    public string[] possibleEvents = { "TrafficJam", "HighwayExit", "EmergencyVehicleApproaching" };
    
    private Vector3 highwayDirection;
    private Vector3 laneDirection;
    private List<CarNavigationController> activeVehicles = new List<CarNavigationController>();
    private float nextSpawnTime;

    public Vector3 HighwayDirection => highwayDirection;
    public Vector3 LaneDirection => laneDirection;
    
    void Start()
    {
        // Calculate highway direction from start to end
        if (startPoint != null && endPoint != null)
        {
            highwayDirection = (endPoint.position - startPoint.position).normalized;
            // laneDirection is calculated as the cross product of Vector3.up and highwayDirection.
            // This results in a vector that is perpendicular to both the up direction 
            // (assuming a flat highway on the XZ plane) and the highway's forward direction.
            // It effectively points "to the right" across the lanes if highwayDirection is "forward".
            laneDirection = Vector3.Cross(Vector3.up, highwayDirection).normalized;
        }
        else
        {
            highwayDirection = Vector3.forward;
            laneDirection = Vector3.right;
            Debug.LogWarning("Start or end point not set for highway. Using default directions.");
        }
        
        nextSpawnTime = Time.time + UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
        
        // Set up periodic event checking
        // InvokeRepeating("CheckForTrafficEvent", 10f, 1f);
    }
    
    void Update()
    {
        // Check if we need to spawn a new vehicle
        if (Time.time >= nextSpawnTime && activeVehicles.Count < maxVehicles)
        {
            SpawnVehicle();
            nextSpawnTime = Time.time + UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
        }
        
        // Clean up any null vehicles from list
        activeVehicles.RemoveAll(item => item == null);
    }
    
    void SpawnVehicle()
    {
        if (vehiclePrefabs.Length == 0)
            return;
            
        // Choose a random lane
        // int lane = UnityEngine.Random.Range(1, numberOfLanes + 1);
        // Start in right lane, left lane is for passing
        int lane = 2;
        
        // Calculate spawn position (this still needs to be in the correct starting lane)
        float initialLaneOffset = (lane - (numberOfLanes / 2f) - 0.5f) * laneWidth;
        Vector3 spawnPosition = startPoint.position + (laneDirection * initialLaneOffset);
        
        // Debug.Log($"Spawning vehicle in lane {lane} at position {spawnPosition}");        
        // Choose a random vehicle prefab
        GameObject vehiclePrefab = vehiclePrefabs[UnityEngine.Random.Range(0, vehiclePrefabs.Length)];
        
        // Spawn vehicle
        GameObject vehicle = Instantiate(vehiclePrefab, spawnPosition, Quaternion.LookRotation(highwayDirection));
        
        // Set up its navigation controller
        CarNavigationController controller = vehicle.GetComponent<CarNavigationController>();
        if (controller == null)
        {
            controller = vehicle.AddComponent<CarNavigationController>();
        }
        
        // Configure controller
        controller.currentLane = lane;
        controller.totalLanes = numberOfLanes;
        controller.laneWidth = laneWidth;
        controller.speed = UnityEngine.Random.Range(12f, 25f); // UnityEngine.Random speed for variety
        controller.highwayDirection = this.highwayDirection; // Pass highway direction
        controller.laneDirection = this.laneDirection;     // Pass lane direction
        
        // Create waypoints along the highway
        Debug.Log("Startpoint: " + controller.transform.position);
        SetupHighwayWaypoints(controller);
        
        // Add to active vehicles list
        activeVehicles.Add(controller);
    }
    
    void SetupHighwayWaypoints(CarNavigationController controller)
    {
        // Create waypoints along the highway for this vehicle
        List<Transform> waypoints = new List<Transform>();
        
        // // Create an intermediate waypoint (centerline)
        // GameObject intermediateWaypoint = new GameObject(controller.gameObject.name + "_IntermediateWaypoint");
        // intermediateWaypoint.transform.position = startPoint.position + (highwayDirection * (highwayLength * 0.5f));
        // intermediateWaypoint.transform.parent = transform; // Optional: organize under HighwayManager
        // waypoints.Add(intermediateWaypoint.transform);
        
        // Create end waypoint, offset into the vehicle's initial lane
        GameObject endWaypointGameObject = new GameObject(controller.gameObject.name + "_EndWaypoint");
        
        // Calculate the offset for the controller's current (initial) lane
        float laneOffsetValue = (controller.currentLane - (numberOfLanes / 2f) - 0.5f) * laneWidth;
        Vector3 offset = laneDirection * laneOffsetValue;
        
        // Position the waypoint at the highway's end point, plus the lane offset
        endWaypointGameObject.transform.position = endPoint.position + offset; 
        endWaypointGameObject.transform.parent = transform; // Optional: organize under HighwayManager
        waypoints.Add(endWaypointGameObject.transform);

        Debug.Log("Endpoint for " + controller.gameObject.name + " in lane " + controller.currentLane + ": " + endWaypointGameObject.transform.position);
        
        // Assign waypoints to the controller
        controller.waypoints = waypoints.ToArray();
    }
    
    void CheckForTrafficEvent()
    {
        // Randomly decide if a traffic event occurs
        if (UnityEngine.Random.value < eventChance)
        {
            // Choose a random event
            string eventType = possibleEvents[UnityEngine.Random.Range(0, possibleEvents.Length)];
            
            // Notify a random vehicle or multiple vehicles
            int vehiclesToNotify = UnityEngine.Random.Range(1, Mathf.Min(4, activeVehicles.Count + 1));
            
            for (int i = 0; i < vehiclesToNotify && i < activeVehicles.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, activeVehicles.Count);
                if (activeVehicles[randomIndex] != null)
                {
                    activeVehicles[randomIndex].ChangeLaneBasedOnEvent(eventType);
                }
            }
            
            // Debug.Log($"Traffic event triggered: {eventType} affecting {vehiclesToNotify} vehicles");
        }
    }
    
    // Get the position for a specific lane at a given progress along the highway (0 to 1)
    public Vector3 GetLanePosition(int lane, float progressAlongHighway)
    {
        Vector3 positionAlongHighway = Vector3.Lerp(startPoint.position, endPoint.position, progressAlongHighway);
        float laneOffset = (lane - (numberOfLanes / 2f) - 0.5f) * laneWidth;
        return positionAlongHighway + (laneDirection * laneOffset);
    }
    
    // Utility method to destroy vehicles once they reach the end of the highway
    public void RemoveVehicle(CarNavigationController vehicle)
    {
        activeVehicles.Remove(vehicle);
        Destroy(vehicle.gameObject);
    }
}
