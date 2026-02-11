using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ISightService
{
    Task<List<SightShortInfo>> LoadNearestSightsAsync(Vector2 coordinates, int radius = 50);
    Task<SightFullInfo> LoadSightDetailsAsync(int pageId);
    Task<List<SightFullInfo>> LoadSightDetailsBatchAsync(List<int> pageIds);
    Task<Texture2D> LoadImageAsync(string url);
}