using System.Collections.Generic;
using UnityEngine;

public class BikeLaneController : MonoBehaviour
{
    Intersection2 intersectionController;
    public List<TurnDirection> permittedTurnDirections; // List of possible turn directions for the bike lane
    public List<GameObject> laneMarkers;
    public CrosswalkController crosswalkControllerInFront;
    public List<CrosswalkController> crosswalkControllers;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        intersectionController = GetComponentInParent<Intersection2>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Vector3 getDestinationForDirection(TurnDirection direction)
    {
        // Implement logic to get the destination point for the bike lane based on the direction
        // This is a placeholder implementation and should be replaced with your actual logic
        int index = permittedTurnDirections.IndexOf(direction);
        if (index >= 0 && index < laneMarkers.Count)
        {
            return laneMarkers[index].transform.position;
        }
        return Vector3.zero;
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
                if (crosswalk != null && !crosswalk.IsCrosswalkClear())
                {
                    Debug.Log(2);
                    return false; // Not safe to turn if the crosswalk is active or not clear
                }
                
            }
        }
        // only same direction as turn needs to be clear for a right turn 
        // if (direction == TurnDirection.Right && intersectionController.incomingCarsFromActiveDirection[laneMarkers[index].name[5] == '+' ? '+' : '-'].Count > 0)
        // {
        //     Debug.Log(3);
        //     return false; // Not safe to turn if there are incoming cars from the active direction
        // }
        // else if (direction == TurnDirection.Straight)
        // {
        //     if (intersectionController.incomingCarsFromActiveDirection['+'].Count > 0 || intersectionController.incomingCarsFromActiveDirection['-'].Count > 0)
        //     {
        //         Debug.Log(6);
        //         return false;
        //     }
        // }
        // both directions need to be clear for a left turn or to go straight through
        // check for vehicle count (above 1 because of self)
        // else if (direction == TurnDirection.Left)
        // {
        //     // turning left into yielding direction of traffic from main road
        //     if (!yield && intersectionController.incomingCarsFromActiveDirection[gameObject.name[5] == '+' ? '-' : '+'].Count > 0)
        //     {
        //         Debug.Log(4);
        //         return false; // Not safe to turn if there are incoming cars from the active direction
        //     }
        //     // turning left into main road from side street, check both directions of traffic
        //     else if (yield && (intersectionController.incomingCarsFromActiveDirection['+'].Count > 0 || intersectionController.incomingCarsFromActiveDirection['-'].Count > 0))
        //     {
        //         Debug.Log(5);
        //         return false;
        //     }
        // }

        return true;
    }
}
