using System;
using System.IO;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    Guid Session_ID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Session_ID = Guid.NewGuid();
        System.IO.Directory.CreateDirectory(Path.Combine(Application.dataPath + "/Data_Outputs/", Session_ID.ToString()));
        Debug.Log("Session ID: " + Session_ID.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Guid GetSessionID()
    {
        return Session_ID;
    }
}
