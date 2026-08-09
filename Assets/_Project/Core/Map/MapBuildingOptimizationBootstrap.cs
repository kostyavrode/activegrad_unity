using System;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using Zenject;

public class MapBuildingOptimizationBootstrap : IInitializable, IDisposable
{
    private readonly AbstractMap _map;
    private MapMeshOptimizeModifier _optimizeModifier;

    public MapBuildingOptimizationBootstrap(AbstractMap map)
    {
        _map = map;
    }

    public void Initialize()
    {
        _map.OnInitialized += OnMapInitialized;
    }

    public void Dispose()
    {
        _map.OnInitialized -= OnMapInitialized;
    }

    private void OnMapInitialized()
    {
        if (_map.VectorData == null)
            return;

        foreach (var subLayer in _map.VectorData.GetAllFeatureSubLayers())
        {
            if (subLayer.coreOptions.layerName != "building")
                continue;

            subLayer.Modeling.EnableCombiningMeshes(true);
            subLayer.Modeling.ColliderOptions.SetFeatureCollider(ColliderType.None);
            EnsureOptimizeModifier(subLayer);
        }
    }

    private void EnsureOptimizeModifier(VectorSubLayerProperties subLayer)
    {
        _optimizeModifier ??= ScriptableObject.CreateInstance<MapMeshOptimizeModifier>();

        var alreadyAdded = subLayer.BehaviorModifiers
            .GetGameObjectModifier(modifier => modifier is MapMeshOptimizeModifier)
            .Any();

        if (!alreadyAdded)
            subLayer.BehaviorModifiers.AddGameObjectModifier(_optimizeModifier);
    }
}
