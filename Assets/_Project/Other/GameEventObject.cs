using UnityEngine;

public class GameEventObject : MonoBehaviour
{
    private string _eventId;
    private GameEventType _eventType;

    public void SetEventData(string eventId, GameEventType eventType)
    {
        _eventId = eventId;
        _eventType = eventType;
    }

    public string GetEventId() => _eventId;
    public GameEventType GetEventType() => _eventType;
}

