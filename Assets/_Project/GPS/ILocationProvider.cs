using UnityEngine;

public interface ILocationProvider
{
    void Start();
    void Update();
    Vector2 GetCoordinates();
}