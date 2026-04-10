using System;
using Unity.XR.CoreUtils;
using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    int config;

    GameObject red_1;
    GameObject red_2;
    GameObject green_1;
    GameObject green_2;
    float startTime;

    TrafficLightState state1;
    TrafficLightState state2;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        config = 0;
        foreach (var child in gameObject.GetComponentsInChildren<Transform>()) {
            if (child.gameObject.tag.Equals("TrafficLight")) {
                config++;
            }
        }
        {
            
        }
        red_1 = transform.Find("red_1").gameObject;
        green_1 = transform.Find("green_1").gameObject;

        if (config > 2) {
            red_2 = transform.Find("red_2").gameObject;
            green_2 = transform.Find("green_2").gameObject;
        }
        else {
            red_2 = null;
            green_2 = null;
        }

        startTime = Time.time;

        state1 = (TrafficLightState)UnityEngine.Random.Range(0, 2);
        state2 = state1.Equals(TrafficLightState.Red) ? TrafficLightState.Green : TrafficLightState.Red;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - startTime > UnityEngine.Random.Range(5, 7)) {
            if (state1 == TrafficLightState.Red) {
                red_1.SetActive(false);
                green_1.SetActive(true);
                if (red_2 != null) {
                    red_2.SetActive(true);
                    green_2.SetActive(false);
                    state2 = TrafficLightState.Red;
                }
                state1 = TrafficLightState.Green;
            }
            else {
                red_1.SetActive(true);
                green_1.SetActive(false);
                if (red_2 != null) {
                    red_2.SetActive(false);
                    green_2.SetActive(true);
                    state2 = TrafficLightState.Green;
                }
                state1 = TrafficLightState.Red;
            }
            Debug.Log("State 1: " + state1);
            startTime = Time.time;
        }
        
    }
}