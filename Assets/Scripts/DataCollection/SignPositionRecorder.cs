using System.IO;
using UnityEngine;
using System;

public class SignPositionRecorder : MonoBehaviour
{
    GameObject[] signs;
    FileStream fileStream;
    SessionManager sessionManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sessionManager = GameObject.FindFirstObjectByType<SessionManager>();
        signs = GameObject.FindGameObjectsWithTag("sign");
        string fileName = "SignPositions.csv";
        fileStream = new FileStream(Path.Combine(Application.dataPath + "/Data_Outputs/" + sessionManager.GetSessionID().ToString() + "/", fileName), FileMode.Create);

        var line = "Sign_Name,Position\n";
        foreach (var sign in signs)
        {
            var pos = sign.transform.position;
            line += $"{sign.name},\"({pos.x},{pos.y},{pos.z})\"\n";
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(line);
        fileStream.Write(bytes, 0, bytes.Length);
        fileStream.Flush();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
