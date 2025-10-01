using System;
using UnityEngine;
using Zenject;

public class LocationService : ITickable, IInitializable, IDisposable
{
    private readonly ILocationProvider _provider;

    public LocationService(ILocationProvider provider)
    {
        _provider = provider;
    }

    public void Initialize()
    {
        _provider.Start();
    }

    public void Tick()
    {
        _provider.Update();
    }

    public void Dispose()
    {
        Input.location.Stop();
    }

    public Vector2 GetCoordinates() => _provider.GetCoordinates();
}