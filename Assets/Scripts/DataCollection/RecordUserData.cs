using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class RecordUserData : MonoBehaviour
{
    private string fileName;
    private FileStream fileStream;
    private List<Transform> significantObjects = new List<Transform>();

    public VisionUtility visionUtility;


    private CarInputActions inputActions;
    public string steerAxis = "Horizontal";
    public string throttleAndBrakeAxis = "Vertical";
    public string throttleAxis = "Fire2";
    public string brakeAxis = "Fire3";
    public string handbrakeAxis = "Jump";

    SessionManager sessionManager;

    void Start()
    {
        sessionManager = GameObject.FindFirstObjectByType<SessionManager>();

        inputActions = new CarInputActions();
        inputActions.Driving.Enable();

        fileName = "UserCar.csv";
        fileStream = new FileStream(Path.Combine(Application.dataPath + "/Data_Outputs/" + sessionManager.GetSessionID().ToString() + "/", fileName), FileMode.Create);
        significantObjects.AddRange(GameObject.FindGameObjectsWithTag("sign").ToList().Select(go => go.transform));
        var header = "UTC_Time,User_Position,Steer,Gas,Brake" + significantObjects.Aggregate("", (acc, obj) => acc + $",{obj.name}");
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(header + ",\n");
        fileStream.Write(headerBytes, 0, headerBytes.Length);
        fileStream.Flush();
    }

    // Update is called once per frame
    void Update()
    {
        var current_time = DateTime.UtcNow;
        var position = transform.position;

        float steerInput = inputActions.Driving.Steering.ReadValue<float>();
        float forwardInput = inputActions.Driving.Throttle.ReadValue<float>();
        float reverseInput = inputActions.Driving.Brake.ReadValue<float>();

        // Debug.Log($"Steer: {steerInput}, Throttle: {forwardInput}, Brake: {reverseInput}");

        var line = current_time.ToString("o") + $",\"({position.x},{position.y},{position.z})\",{steerInput},{forwardInput},{reverseInput}";
        foreach (var obj in significantObjects)
        {
            var v = visionUtility.IsObjectInView(obj) ? ",1" : ",0";
            line += v;
        }
        line += "\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(line);
        fileStream.Write(bytes, 0, bytes.Length);
        fileStream.Flush();
    }
    private void OnApplicationQuit()
    {
        fileStream.Close();
    }
}
