using System.Collections.Generic;
using UnityEngine;

public class Intersection : MonoBehaviour
{
    float startTime;
    float orangeTime;
    bool changingState = false;
    public TrafficLightSet[] trafficLights;

    public TrafficDirection direction;
    public List<TrafficLightSet> X_lights;
    public List<TrafficLightSet> Z_lights;

    public List<GameObject> laneMarkers;

    public bool pedestrian_seperate = false;

    public bool controlled = true;
    // Start is called before the first frame update
    void Start()
    {
        // Initialization code here
        startTime = Time.time;
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
    }

    // Update is called once per frame
    void Update()
    {
        if (controlled)
        {
            // Update code here
            if (Time.time - startTime > Random.Range(5, 7) && !changingState)
            {
                Debug.Log("Changing state");
                orangeTime = Time.time;
                changingState = true;

                foreach (var light in direction == TrafficDirection.X ? X_lights : Z_lights)
                {
                    light.setState(TrafficLightState.Orange);
                }
            }

            if (Time.time - orangeTime > 2 && changingState)
            {
                Debug.Log("Changing state done");
                changingState = false;
                setDirection(direction == TrafficDirection.X ? TrafficDirection.Z : TrafficDirection.X);
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
        startTime = Time.time;
    }
}