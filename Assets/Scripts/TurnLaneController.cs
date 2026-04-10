using System.Collections.Generic;
using UnityEngine;

public class TurnLaneController : MonoBehaviour
{
    public List<TurnDirection> permittedTurnDirections;
    public CrosswalkController crosswalkControllerInFront;
    public List<CrosswalkController> crosswalkControllers;
    public List<GameObject> laneMarkers;
    public Intersection2 intersectionController;
    public bool yield;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        intersectionController = GetComponentInParent<Intersection2>();
        if (intersectionController == null)
        {
            Debug.LogError("No Intersection2 controller found in parent for " + gameObject.name);
            enabled = false; // Disable script if no controller
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<TurnDirection> GetPermittedTurnDirections()
    {
        return permittedTurnDirections;
    }

    public Vector3 GetTurnLanePosition(TurnDirection direction)
    {
        int index = permittedTurnDirections.IndexOf(direction);
        if (index >= 0 && index < laneMarkers.Count)
        {
            return laneMarkers[index].transform.position;
        }
        return Vector3.zero; // Return zero vector if no matching turn lane is found
    }


    public List<Vector3> GetIntersectionPositions(TurnDirection direction)
    {
        List<Vector3> positions = new List<Vector3>();
        if (direction != TurnDirection.Straight)
        {
            positions.Add(transform.position); // Add a waypoint to the lane controller position, 
            // halt at right and left turns for incoming cars and crossing pedestrians
        }
        // Add the position of the lane marker corresponding to the turn direction
        int index = permittedTurnDirections.IndexOf(direction);
        if (index >= 0 && index < laneMarkers.Count)
        {
            positions.Add(laneMarkers[index].transform.position);
        }
        return positions;
    }

    public bool isSafeToTurn(TurnDirection direction)
    {
        int index = permittedTurnDirections.IndexOf(direction);
        if (crosswalkControllers.Count > 0)
        {
            if (index >= 0 && index < crosswalkControllers.Count)
            {
                // check both crosswalk right in front of the car, and the one corresponding to their turn direction
                CrosswalkController crosswalk_front = crosswalkControllers[0];
                if (crosswalkControllerInFront != null && !crosswalkControllerInFront.IsCrosswalkClear())
                {
                    Debug.Log(1);
                    return false; // Not safe to turn if the crosswalk is active or not clear
                }
                CrosswalkController crosswalk = crosswalkControllers[index];
                if (crosswalkControllers.Count > 0 && crosswalk != null && !crosswalk.IsCrosswalkClear())
                {
                    Debug.Log(2);
                    return false; // Not safe to turn if the crosswalk is active or not clear
                }
                
            }
        }
        // only same direction as turn needs to be clear for a right turn 
        if (direction == TurnDirection.Right && yield && intersectionController.incomingCarsFromActiveDirection[laneMarkers[index].name[0] == '+' ? '+' : '-'].Count > 0)
        {
            Debug.Log(3);
            return false; // Not safe to turn if there are incoming cars from the active direction
        }
        // both directions need to be clear for a left turn or to go straight through
        // check for vehicle count (above 1 because of self)
        else if (direction == TurnDirection.Left)
        {
            // turning left into yielding direction of traffic from main road
            if (!yield && intersectionController.incomingCarsFromActiveDirection[gameObject.name[5] == '+' ? '-' : '+'].Count > 0)
            {
                Debug.Log(4);
                return false; // Not safe to turn if there are incoming cars from the active direction
            }
            // turning left into main road from side street, check both directions of traffic
            else if (yield && (intersectionController.incomingCarsFromActiveDirection['+'].Count > 0 || intersectionController.incomingCarsFromActiveDirection['-'].Count > 0))
            {
                Debug.Log(5);
                return false;
            }
        }
        else if (direction == TurnDirection.Straight)
        {
            if (yield && (intersectionController.incomingCarsFromActiveDirection['+'].Count > 0 || intersectionController.incomingCarsFromActiveDirection['-'].Count > 0))
            {
                Debug.Log(6);
                return false;
            }
        }

        return true;
    }
}
