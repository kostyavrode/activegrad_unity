using System;
using UnityEngine;
using Zenject;

public class LocationService : IDisposable
{
    private readonly ILocationProvider _provider;

    public LocationService(ILocationProvider provider)
    {
        _provider = provider;
    }

    public void Dispose()
    {
        Input.location.Stop();
    }

    public Vector2 GetCoordinates()
    {
        var coords = _provider.GetCoordinates();
        return coords;
    }

    public void GetCoordinatesPrecise(out double longitude, out double latitude)
    {
        _provider.GetCoordinatesPrecise(out longitude, out latitude);
    }
}