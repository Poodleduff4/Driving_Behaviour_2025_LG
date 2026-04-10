using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    public SceneVariables sceneVariables;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sceneVariables = new SceneVariables();
        sceneVariables.timeOfDay = TimeOfDay.Night;
        sceneVariables.weatherCondition = WeatherCondition.Sunny;

        SetSceneLighting(sceneVariables.timeOfDay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetSceneLighting(TimeOfDay timeOfDay)
    {
        // Placeholder for setting scene lighting based on time of day
        if (timeOfDay == TimeOfDay.Day)
        {
            RenderSettings.skybox = Resources.Load<Material>("ReversedAssets/City Builder Urban/URP/Materials/Skybox_day");
            GameObject.FindGameObjectsWithTag("streetlight").ToList().ForEach(light => light.GetComponentInChildren<Light>().enabled = false);
        }
        else
        {
            RenderSettings.skybox = Resources.Load<Material>("ReversedAssets/City Builder Urban/URP/Materials/Skybox_night");
            GameObject.FindGameObjectsWithTag("streetlight").ToList().ForEach(light => light.GetComponentInChildren<Light>().enabled = true);
            GameObject.FindGameObjectWithTag("dayLight").GetComponentInChildren<Transform>().gameObject.SetActive(false);
        }
    }
}
