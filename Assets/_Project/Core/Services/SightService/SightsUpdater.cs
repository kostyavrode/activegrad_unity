using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class SightsUpdater : IInitializable, IDisposable
{
    public List<SightShortInfo> CachedNearestSights { get; private set; }
    public List<SightFullInfo> CachedSights { get; private set; }

    private readonly ISightService _sightService;
    private readonly LocationService _locationService;

    private bool _isRunning;
    private readonly float _interval = 5f;

    public SightsUpdater(ISightService sightService, LocationService locationService)
    {
        _sightService = sightService;
        _locationService = locationService;
    }

    public void Initialize()
    {
        _isRunning = true;
        RunUpdateLoop();
    }

    public void Dispose()
    {
        _isRunning = false;
    }

    private async void RunUpdateLoop()
    {
        while (_isRunning)
        {
            await UpdateSights();
            await Task.Delay((int)(_interval * 1000));
        }
    }

    private async Task UpdateSights()
    {
        Vector2 coords = _locationService.GetCoordinates();
        Debug.Log("Coords:"+coords.ToString());

        CachedNearestSights = await _sightService.LoadNearestSightsAsync(coords,5000);
        
        var tasks = new List<Task<SightFullInfo>>();

        foreach (var s in CachedNearestSights)
        {
            tasks.Add(_sightService.LoadSightDetailsAsync(s.PageId));
        }

        var results = await Task.WhenAll(tasks);
        
        CachedSights = new List<SightFullInfo>();

        CachedSights.AddRange(results);

        Debug.Log($"Sights updated: {CachedNearestSights.Count}");
    }
    
}