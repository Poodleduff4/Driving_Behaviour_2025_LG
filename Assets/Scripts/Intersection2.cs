using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using Unity.Collections;

public class Intersection2 : MonoBehaviour
{
    float startTime;
    float orangeTime;
    public bool changingState = false;
    public TrafficLightSet[] trafficLights;

    public TrafficDirection direction;
    public List<TrafficLightSet> X_lights;
    public List<TrafficLightSet> Z_lights;

    public List<GameObject> laneMarkers;

    public List<TurnLaneController2> turnLaneControllers;
    public List<BikeLaneController> bikeLaneControllers;

    public bool pedestrian_seperate = false;
    public bool pedestriansHaveTimeToCross = true;

    public bool controlled = true;
    // A hash map to track incoming cars from the active direction
    public Dictionary<char, List<int>> incomingCarsFromActiveDirection;
    public Dictionary<char, List<int>> incomingBikesFromActiveDirection;

    public bool mergeLane = false; // Whether to merge lanes at the intersection

    public int lightCycleTime = 10;
    
    // Start is called before the first frame update
    void Start()
    {
        // Initialization code here
        startTime = Time.time;
        incomingCarsFromActiveDirection = new Dictionary<char, List<int>>(); // Initialize the hash map
        incomingCarsFromActiveDirection['+'] = new List<int>();
        incomingCarsFromActiveDirection['-'] = new List<int>();

        incomingBikesFromActiveDirection = new Dictionary<char, List<int>>(); // Initialize the hash map
        incomingBikesFromActiveDirection['+'] = new List<int>();
        incomingBikesFromActiveDirection['-'] = new List<int>();

        if (controlled)
        {
            X_lights = new List<TrafficLightSet>();
            Z_lights = new List<TrafficLightSet>();

            trafficLights = gameObject.GetComponentsInChildren<TrafficLightSet>();
            foreach (var light in trafficLights)
            {
                if (light.direction == TrafficDirection.X)
                {
                    X_lights.Add(light);
                }
                else
                {
                    Z_lights.Add(light);
                }
            }

            setDirection((TrafficDirection)UnityEngine.Random.Range(0, 2));
        }

        turnLaneControllers = new List<TurnLaneController2>(GetComponentsInChildren<TurnLaneController2>());
        bikeLaneControllers = new List<BikeLaneController>(GetComponentsInChildren<BikeLaneController>());
    }

    // Update is called once per frame
    void Update()
    {
        if (controlled)
        {
            // Update code here
            if (Time.time - startTime > lightCycleTime/3)
            {
                pedestriansHaveTimeToCross = false;
            }
            if (Time.time - startTime > lightCycleTime && !changingState)
                {
                    // Debug.Log("Changing state");
                    orangeTime = Time.time;
                    changingState = true;

                    foreach (var light in direction == TrafficDirection.X ? X_lights : Z_lights)
                    {
                        light.setState(TrafficLightState.Orange);
                    }
                }

            if (Time.time - orangeTime > lightCycleTime/4 && changingState)
            {
                // Debug.Log("Changing state done");
                changingState = false;
                setDirection(direction == TrafficDirection.X ? TrafficDirection.Z : TrafficDirection.X);
                startTime = Time.time;
                pedestriansHaveTimeToCross = true;
            }
        }
    }

    void setDirection(TrafficDirection newDirection)
    {
        direction = newDirection;

        if (direction == TrafficDirection.Z)
        {
            foreach (var light in X_lights)
            {
                light.setState(TrafficLightState.Red);
            }
            foreach (var light in Z_lights)
            {
                light.setState(TrafficLightState.Green);
            }
        }
        else if (direction == TrafficDirection.X)
        {
            foreach (var light in X_lights)
            {
                light.setState(TrafficLightState.Green);

            }
            foreach (var light in Z_lights)
            {
                light.setState(TrafficLightState.Red);

            }
        }
        incomingCarsFromActiveDirection['+'].Clear();
        incomingCarsFromActiveDirection['-'].Clear();
        incomingBikesFromActiveDirection['+'].Clear();
        incomingBikesFromActiveDirection['-'].Clear();
    }

    public Vector3 GetDestinationForTurn(Vector3 vehiclePosition, TurnDirection desiredTurn)
    {
        Vector3 intersectionCenter = transform.position;
        Vector3 relativePos = vehiclePosition - intersectionCenter;
        string approachDirection = "";

        // Determine approach direction based on which axis the vehicle is further along
        if (Mathf.Abs(relativePos.x) > Mathf.Abs(relativePos.z))
        {
            // Approaching along X-axis
            if (relativePos.x > 0) approachDirection = "+x"; // Approaching from East (vehicle moving towards -X)
            else approachDirection = "-x"; // Approaching from West (vehicle moving towards +X)
        }
        else
        {
            // Approaching along Z-axis
            if (relativePos.z > 0) approachDirection = "+z"; // Approaching from North (vehicle moving towards -Z)
            else approachDirection = "-z"; // Approaching from South (vehicle moving towards +Z)
        }

        string targetLaneMarkerName = "";

        // Determine target LaneMarker name based on approach direction and desired turn
        // LaneMarker names (-x, +x, -z, +z) indicate the direction of travel *after* the turn.
        if (approachDirection == "+x") // Approaching from East
        {
            if (desiredTurn == TurnDirection.Straight) targetLaneMarkerName = "-x";
            else if (desiredTurn == TurnDirection.Left) targetLaneMarkerName = "+z";
            else if (desiredTurn == TurnDirection.Right) targetLaneMarkerName = "-z";
        }
        else if (approachDirection == "-x") // Approaching from West
        {
            if (desiredTurn == TurnDirection.Straight) targetLaneMarkerName = "+x";
            else if (desiredTurn == TurnDirection.Left) targetLaneMarkerName = "-z";
            else if (desiredTurn == TurnDirection.Right) targetLaneMarkerName = "+z";
        }
        else if (approachDirection == "+z") // Approaching from North
        {
            if (desiredTurn == TurnDirection.Straight) targetLaneMarkerName = "-z";
            else if (desiredTurn == TurnDirection.Left) targetLaneMarkerName = "-x";
            else if (desiredTurn == TurnDirection.Right) targetLaneMarkerName = "+x";
        }
        else if (approachDirection == "-z") // Approaching from South
        {
            if (desiredTurn == TurnDirection.Straight) targetLaneMarkerName = "+z";
            else if (desiredTurn == TurnDirection.Left) targetLaneMarkerName = "+x";
            else if (desiredTurn == TurnDirection.Right) targetLaneMarkerName = "-x";
        }

        if (string.IsNullOrEmpty(targetLaneMarkerName))
        {
            Debug.LogError($"Could not determine target lane marker for vehicle at {vehiclePosition} (approach {approachDirection}) and turn '{desiredTurn}'.");
            return Vector3.zero;
        }

        if (laneMarkers == null)
        {
            Debug.LogError("LaneMarkers collection is null. Ensure Start() has run and LaneMarkers are tagged correctly.");
            return Vector3.zero;
        }

        foreach (GameObject laneMarkerGO in laneMarkers)
        {
            if (laneMarkerGO != null && laneMarkerGO.name == targetLaneMarkerName) 
            {
                return laneMarkerGO.transform.position;
            }
        }

        Debug.LogError($"LaneMarker named '{targetLaneMarkerName}' not found in the intersection's children.");
        return Vector3.zero;
    }

    public TurnLaneController2 GetIntersectionLaneController(string movementDirection, int lane)
    {
        if (turnLaneControllers == null || turnLaneControllers.Count == 0)
        {
            Debug.LogError("TurnLaneControllers collection is null or empty. Ensure it is populated in the inspector.");
            return null;
        }

        return turnLaneControllers.Find(t => t.gameObject.name == "lane_" + movementDirection + "_" + lane.ToString());
    }

    public BikeLaneController GetBikeLaneController(string movementDirection)
    {
        if (bikeLaneControllers == null || bikeLaneControllers.Count == 0)
        {
            Debug.LogError("BikeLaneControllers collection is null or empty. Ensure it is populated in the inspector.");
            return null;
        }

        return bikeLaneControllers.Find(t => t.gameObject.name == "bike_lane_" + movementDirection);
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     Debug.Log("Removing: " + other.gameObject.GetInstanceID());
    //     incomingCarsFromActiveDirection[other.gameObject.GetComponent<Downtown_Car_Controller_2>().GetDetailedMovementDirection()[0]].Remove(other.gameObject.GetInstanceID());

    // }

}
