using UnityEngine;

public class TrafficLightSet : MonoBehaviour
{
    public TrafficLightState state;
    
    public TrafficDirection direction;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Update code here
    }

    public void setState(TrafficLightState newState)
    {
        state = newState;
        updateLights();
    }

    void updateLights()
    {
        foreach (var light in gameObject.GetComponentsInChildren<Transform>())
        {
            if (light.gameObject.tag.Equals("TrafficLight"))
            {
                if (state == TrafficLightState.Red)
                {
                    if (light.gameObject.name.Equals("red"))
                    {
                        light.GetComponent<Renderer>().enabled = true;
                    }
                    else
                    {
                        light.GetComponent<Renderer>().enabled = false;
                    }
                }
                else if (state == TrafficLightState.Green)
                {
                    if (light.gameObject.name.Equals("green"))
                    {
                        light.GetComponent<Renderer>().enabled = true;
                    }
                    else
                    {
                        light.GetComponent<Renderer>().enabled = false;
                    }
                }
                else if (state == TrafficLightState.Orange)
                {
                    if (light.gameObject.name.Equals("orange"))
                    {
                        light.GetComponent<Renderer>().enabled = true;
                    }
                    else
                    {
                        light.GetComponent<Renderer>().enabled = false;
                    }
                }
            }
        }
    }
}