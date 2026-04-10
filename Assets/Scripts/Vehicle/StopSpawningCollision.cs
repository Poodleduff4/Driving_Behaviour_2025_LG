using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopSpawningCollision : MonoBehaviour {

    public bool isUserNear = false;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider other)
    {
        // if the user car is on top of this object; send the current node/link to the road network script
        if (other.tag == "UserCar")
        {
            isUserNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // if the user car is on top of this object; send the current node/link to the road network script
        if (other.tag == "UserCar")
        {
            isUserNear = false;
        }
    }
}
