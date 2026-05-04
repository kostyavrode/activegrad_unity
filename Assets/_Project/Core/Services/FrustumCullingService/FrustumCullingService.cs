using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class FrustumCullingService : IInitializable, IDisposable
{
    // Статический доступ — CullableObject использует его напрямую,
    // без необходимости Zenject-инжекта на каждом объекте сцены
    public static FrustumCullingService Instance { get; private set; }

    private readonly Camera _camera;
    private readonly CoroutineRunner _coroutineRunner;

    private readonly List<CullableObject> _registered = new();
    private readonly List<CullableObject> _pendingAdd = new();
    private readonly List<CullableObject> _pendingRemove = new();

    private readonly Plane[] _frustumPlanes = new Plane[6];
    private Coroutine _cullingCoroutine;

    // Буфер: объект отключается значительно позже, чем уходит с экрана.
    // Чем больше значение — тем дальше за границей камеры происходит отключение
    private const float CullMargin = 8f;
    private const float CheckInterval = 0.15f;

    public FrustumCullingService(Camera camera, CoroutineRunner coroutineRunner)
    {
        _camera = camera;
        _coroutineRunner = coroutineRunner;
    }

    public void Initialize()
    {
        Instance = this;
        _cullingCoroutine = _coroutineRunner.StartCoroutine(CullingLoop());
    }

    public void Dispose()
    {
        Instance = null;

        if (_cullingCoroutine != null)
            _coroutineRunner.StopCoroutine(_cullingCoroutine);

        _registered.Clear();
        _pendingAdd.Clear();
        _pendingRemove.Clear();
    }

    public void Register(CullableObject obj)
    {
        // Защита от дублей: объект может снова вызвать OnEnable
        // после того как мы сами его отключили через SetActive(false)
        if (!_registered.Contains(obj) && !_pendingAdd.Contains(obj))
            _pendingAdd.Add(obj);
    }

    public void Unregister(CullableObject obj)
    {
        _pendingRemove.Add(obj);
    }

    private IEnumerator CullingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(CheckInterval);

            // Применяем отложенные изменения списка, чтобы не модифицировать
            // коллекцию во время итерации
            if (_pendingRemove.Count > 0)
            {
                foreach (var obj in _pendingRemove)
                    _registered.Remove(obj);
                _pendingRemove.Clear();
            }

            if (_pendingAdd.Count > 0)
            {
                _registered.AddRange(_pendingAdd);
                _pendingAdd.Clear();
            }

            if (_registered.Count == 0 || _camera == null)
                continue;

            GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);

            foreach (var obj in _registered)
            {
                if (obj == null) continue;

                Bounds bounds = obj.GetBounds();
                bounds.Expand(CullMargin);

                bool visible = GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds);
                obj.SetVisible(visible);
            }
        }
    }
}
