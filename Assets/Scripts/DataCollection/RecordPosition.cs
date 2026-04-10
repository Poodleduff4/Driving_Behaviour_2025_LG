using UnityEngine;
using System;
using System.IO;

public class RecordNPCPosition : MonoBehaviour
{
    private string fileName;
    private FileStream fileStream;
    SessionManager sessionManager;

    void Start()
    {
        sessionManager = GameObject.FindFirstObjectByType<SessionManager>();
        fileName = "Agent_"+gameObject.name+".csv";
        fileStream = new FileStream(Path.Combine(Application.dataPath + "/Data_Outputs/" + sessionManager.GetSessionID().ToString() + "/", fileName), FileMode.Create);

        var header = "UTC_Time,Agent_Position";
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(header + ",\n");
        fileStream.Write(headerBytes, 0, headerBytes.Length);
        fileStream.Flush();
    }
    void Update()
    {
        var current_time = DateTime.UtcNow;
        var position = transform.position;
        var line = current_time.ToString("o") + $",\"({position.x},{position.y},{position.z})\"\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(line);
        fileStream.Write(bytes, 0, bytes.Length);
        // fileStream.Flush();
    }
    private void OnApplicationQuit()
    {
        fileStream.Close();
    }
}