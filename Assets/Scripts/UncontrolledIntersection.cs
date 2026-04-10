using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UncontrolledIntersection : MonoBehaviour
{
    public ArrayList laneMarkers;
    public float laneWidth = 5f; // Width of each lane
    public TrafficDirection trafficDirection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        laneMarkers = new ArrayList();
        foreach (Transform child in transform)
        {
            if (child.CompareTag("LaneMarker"))
            {
                laneMarkers.Add(child.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameObject laneMarker in laneMarkers)
        {
            // Update each lane marker's position or behavior
        }
    }

    /// <summary>
    /// Gets the destination position for a vehicle wanting to make a turn.
    /// </summary>
    /// <param name="vehiclePosition">The current world position of the vehicle.</param>
    /// <param name="desiredTurn">The desired turn direction (enum TurnDirection).</param>
    /// <returns>The world position of the target LaneMarker, or Vector3.zero if not found.</returns>
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
}
