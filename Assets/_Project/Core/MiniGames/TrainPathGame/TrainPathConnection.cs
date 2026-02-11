using UnityEngine;

public class TrainPathConnection : MonoBehaviour
{
    private Station _from;
    private Station _to;
    private float _travelTime;

    public Station From => _from;
    public Station To => _to;
    public float TravelTime => _travelTime;

    public void Initialize(Station from, Station to, float travelTime)
    {
        _from = from;
        _to = to;
        _travelTime = travelTime;
    }

    public Station GetOtherStation(Station station)
    {
        if (_from == station)
            return _to;
        if (_to == station)
            return _from;
        return null;
    }
}
