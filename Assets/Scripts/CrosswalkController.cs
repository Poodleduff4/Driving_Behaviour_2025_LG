using UnityEngine;

public class CrosswalkController : MonoBehaviour
{
    public int pedestrianCount = 0; // Count of pedestrians in the crosswalk
    public TrafficDirection direction; // Direction of the crosswalk, if needed
    public Intersection2 intersectionController; // Reference to the intersection controller
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

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is a pedestrian
        if (other.CompareTag("pedestrian"))
        {
            // Logic for when a pedestrian enters the crosswalk
            Debug.Log("Pedestrian entered the crosswalk.");
            pedestrianCount++;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger is a pedestrian
        if (other.CompareTag("pedestrian"))
        {
            // Logic for when a pedestrian exits the crosswalk
            Debug.Log("Pedestrian exited the crosswalk.");
            pedestrianCount--;
        }
    }

    public bool IsCrosswalkClear() // for cars
    {
        // Check if the crosswalk is clear of pedestrians
        return pedestrianCount == 0;
    }

    public bool isSafeToCross() // for pedestrians
    {
        return intersectionController.direction == direction && intersectionController.pedestriansHaveTimeToCross;
    }
    
}
