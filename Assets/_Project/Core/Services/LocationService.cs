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

    public Vector2 GetCoordinates() => _provider.GetCoordinates();
}