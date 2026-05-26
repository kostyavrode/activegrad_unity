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

    private readonly List<CullableObject> _registered    = new();
    private readonly List<CullableObject> _pendingAdd    = new();
    private readonly List<CullableObject> _pendingRemove = new();

    private readonly Plane[] _frustumPlanes = new Plane[6];
    private Coroutine _frustumCoroutine;
    private Coroutine _distanceCoroutine;

    // Frustum: проверяем часто — нужна плавность появления/скрытия
    private const float FrustumCheckInterval  = 0.15f;
    // Distance: проверяем редко — игрок не перемещается мгновенно
    private const float DistanceCheckInterval = 0.5f;
    // Буфер: объект отключается чуть позже, чем уходит с экрана
    private const float CullMargin = 8f;

    public FrustumCullingService(Camera camera, CoroutineRunner coroutineRunner)
    {
        _camera = camera;
        _coroutineRunner = coroutineRunner;
    }

    public void Initialize()
    {
        Instance = this;
        _frustumCoroutine  = _coroutineRunner.StartCoroutine(FrustumLoop());
        _distanceCoroutine = _coroutineRunner.StartCoroutine(DistanceLoop());
    }

    public void Dispose()
    {
        Instance = null;

        if (_frustumCoroutine  != null) _coroutineRunner.StopCoroutine(_frustumCoroutine);
        if (_distanceCoroutine != null) _coroutineRunner.StopCoroutine(_distanceCoroutine);

        _registered.Clear();
        _pendingAdd.Clear();
        _pendingRemove.Clear();
    }

    // ── регистрация ───────────────────────────────────────────────────────────

    public void Register(CullableObject obj)
    {
        if (!_registered.Contains(obj) && !_pendingAdd.Contains(obj))
            _pendingAdd.Add(obj);
    }

    public void Unregister(CullableObject obj)
    {
        _pendingRemove.Add(obj);
    }

    // ── frustum loop (0.15 с) ─────────────────────────────────────────────────

    private IEnumerator FrustumLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(FrustumCheckInterval);

            FlushPending();

            if (_registered.Count == 0 || _camera == null) continue;

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

    // ── distance loop (0.5 с) ─────────────────────────────────────────────────

    private IEnumerator DistanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(DistanceCheckInterval);

            if (_registered.Count == 0 || _camera == null) continue;

            Vector3 playerPos = _camera.transform.position;

            foreach (var obj in _registered)
            {
                if (obj == null) continue;

                float threshold = obj.DistanceCullDistance;
                bool near = (obj.transform.position - playerPos).sqrMagnitude
                            <= threshold * threshold;

                obj.SetNear(near);
            }
        }
    }

    // ── общий хелпер ─────────────────────────────────────────────────────────

    /// <summary>
    /// Применяет отложенные добавления/удаления из списка.
    /// Вызывается только из FrustumLoop, т.к. оба цикла читают один и тот же список —
    /// структурные изменения делаем один раз, в более частом цикле.
    /// </summary>
    private void FlushPending()
    {
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
    }
}
