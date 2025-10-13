using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class UIManager : IInitializable, IDisposable
{
    private readonly Dictionary<Type, IWindow> _windows = new Dictionary<Type, IWindow>();
    private readonly Stack<IWindow> _openWindows = new Stack<IWindow>();

    private IWindow _defaultWindow; // то самое окно с index=0

    public void Initialize()
    {
        //HideAll();
        Debug.Log("UIManager initialized");
    }

    public void RegisterSceneWindows(List<IWindow> sceneWindows)
    {
        //HideAll();
        _windows.Clear();
        _openWindows.Clear();
        _defaultWindow = null;

        for (int i = 0; i < sceneWindows.Count; i++)
        {
            var window = sceneWindows[i];
            RegisterWindow(window);

            if (i == 0)
            {
                _defaultWindow = window;
            }
        }
        
        if (_defaultWindow != null)
        {
            _defaultWindow.Show();
            _openWindows.Push(_defaultWindow);
            //Debug.Log($"Default window {_defaultWindow.GetType().Name} opened");
        }
    }

    private void RegisterWindow(IWindow window)
    {
        var type = window.GetType();
        if (!_windows.ContainsKey(type))
        {
            _windows.Add(type, window);
            //Debug.Log($"Registering window {type.Name}");
        }
    }

    public void Show<T>() where T : IWindow
    {
        var type = typeof(T);
        if (_windows.TryGetValue(type, out var window))
        {
            if (_openWindows.Count > 0)
            {
                _openWindows.Peek().Hide();
            }

            window.Show();
            _openWindows.Push(window);

            Debug.Log($"Showing window {type.Name}");
        }
        else
        {
            Debug.LogError($"UIManager: {type.Name} не зарегистрировано");
        }
    }

    public void Back()
    {
        if (_openWindows.Count == 0) return;

        var top = _openWindows.Pop();
        top.Hide();

        if (_openWindows.Count > 0)
        {
            _openWindows.Peek().Show();
        }
    }

    public void HideAll()
    {
        foreach (var window in _windows.Values)
            window.Hide();

        _openWindows.Clear();
    }

    public bool HasWindow<T>() where T : IWindow
    {
        return _windows.ContainsKey(typeof(T));
    }

    public void Dispose()
    {
        _openWindows.Clear();
        _windows.Clear();
    }
}
