using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneLoader
{
    private readonly AudioManager _audioManager;
    private readonly SceneMusicConfig _sceneMusicConfig;

    public SceneLoader(AudioManager audioManager, [InjectOptional] SceneMusicConfig sceneMusicConfig = null)
    {
        _audioManager = audioManager;
        _sceneMusicConfig = sceneMusicConfig;
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        PlaySceneMusic(sceneName);
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
            yield return null;

        PlaySceneMusic(sceneName);
    }

    private void PlaySceneMusic(string sceneName)
    {
        if (_sceneMusicConfig == null)
            return;

        if (_sceneMusicConfig.TryGetTrack(sceneName, out var clip))
            _audioManager.PlayMusic(clip);
    }
}