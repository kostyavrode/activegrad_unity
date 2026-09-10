using UnityEngine;

public interface ILocationProvider
{
    Vector2 GetCoordinates();
    void GetCoordinatesPrecise(out double longitude, out double latitude);
}
