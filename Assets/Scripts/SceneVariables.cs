using UnityEngine;

public class SceneVariables
{
    public TimeOfDay timeOfDay;
    public WeatherCondition weatherCondition;
}

public enum TimeOfDay
{
    Day,
    Night
}

public enum WeatherCondition
{
    Sunny,
    Rainy,
    Snowy
}

public enum TrafficFlow
{
    Light,
    Heavy
}

public enum VRUDensity
{
    None,
    Low,
    High
}

public enum VehicleBehaviour
{
    Safe,
    Aggressive
}