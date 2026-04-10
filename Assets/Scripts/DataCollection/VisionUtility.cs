using Mono.Cecil.Cil;
using UnityEngine;

public class VisionUtility : MonoBehaviour
{
    Vector3 position;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        position = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        position = gameObject.transform.position;
    }
    
    public bool IsObjectInView(Transform objectTransform)
    {
        RaycastHit hit;
        Vector3 directionToObject = (objectTransform.position - position).normalized;
        if (Physics.Raycast(position + Vector3.up * 1.5f, directionToObject, out hit))
        {
            if (hit.transform == objectTransform)
            {
                return true;
            }
        }
        return false;
    }
}
