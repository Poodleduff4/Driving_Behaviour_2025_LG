using UnityEngine;

public class Main : MonoBehaviour
{
    public GameObject car;

    public float Roll, Pitch;

    public float x, y, z, w;
    public float euler_x, euler_y, euler_z;
    public Vector3 euler;

    // Update is called once per frame
    void Update()
    {
        Transform t = car.transform;

        //Pitch = Vector3.Angle(new Vector3(t.forward.x, 0, t.forward.z), t.forward);
        //Pitch = - t.rotation.x;
        Pitch = Mathf.DeltaAngle(0, transform.rotation.eulerAngles.x);


        Roll = Mathf.Asin(2f * t.rotation.x * t.rotation.y + 2f * t.rotation.z * t.rotation.w);
        //Roll = Mathf.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z);
        //Pitch = Mathf.Atan2(2 * x * w + 2 * y * z, 1 - 2 * x * x - 2 * z * z);
        //Yaw = Mathf.Asin(2 * x * y + 2 * z * w);
        QuPlaySimtools.QuSimtools_SendTelemetry(Roll, Pitch, 0, 0, 0, 0, 0, 0, 0);
    }
}
