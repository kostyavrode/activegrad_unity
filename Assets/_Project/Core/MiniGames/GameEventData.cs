using System;
using UnityEngine;

[Serializable]
public class GameEventData
{
    public string EventId;
    public GameEventType EventType;
    public Vector2 Coordinates;
    public float SpawnTime;
    public float Lifetime;
    public bool IsActive;
}



