using System;
using UnityEngine;
using Zenject;

public class _TestWiki : MonoBehaviour
{
    public ISightService SightService;

    [Inject]
    public void Costruct(ISightService sightService)
    {
        SightService = sightService;
    }
    public async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var sights = await SightService.LoadNearestSightsAsync(new Vector2(55.7558f, 37.6173f));
            Debug.Log(sights);
        }
    }
}
