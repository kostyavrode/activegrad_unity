using System.Collections.Generic;
using UnityEngine;

public class FlappyBirdGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private FlappyBirdController _controller;

    protected override void OnStartGame()
    {
        LoadGamePrefab();
    }

    private void LoadGamePrefab()
    {
        var prefab = Resources.Load<GameObject>("MiniGames/FlappyBirdGame");
        
        if (prefab == null)
        {
            Debug.LogError("[FlappyBirdGameEvent] Префаб FlappyBirdGame не найден в Resources/MiniGames/FlappyBirdGame.prefab");
            return;
        }
        
        _gameRoot = Object.Instantiate(prefab, _parentContainer);
        _gameRoot.name = "FlappyBirdGame";
        
        var ui = _gameRoot.GetComponent<FlappyBirdUI>();
        if (ui == null)
        {
            Debug.LogError("[FlappyBirdGameEvent] Компонент FlappyBirdUI не найден на префабе!");
            return;
        }
        
        _controller = _gameRoot.GetComponent<FlappyBirdController>();
        if (_controller == null)
        {
            _controller = _gameRoot.AddComponent<FlappyBirdController>();
        }
        
        _controller.Initialize(this, ui);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
        {
            Object.Destroy(_gameRoot);
        }
    }

    public void OnGameEnded(int finalScore)
    {
        FinishGame(true, finalScore);
    }
}

public class FlappyBirdController : MonoBehaviour
{
    private FlappyBirdGameEvent _gameEvent;
    private FlappyBirdUI _ui;
    
    private RectTransform _bird;
    private float _birdVelocity;
    private float _gravity = -800f;
    private float _jumpForce = 300f;
    
    private float _pipeSpawnTimer;
    private float _pipeSpawnInterval = 2f;
    private float _pipeSpeed = 200f;
    private float _pipeGap = 300f;
    
    private int _score = 0;
    private bool _isGameOver = false;
    
    private float _gameHeight = 600f;
    private List<RectTransform> _pipes = new List<RectTransform>();

    public void Initialize(FlappyBirdGameEvent gameEvent, FlappyBirdUI ui)
    {
        _gameEvent = gameEvent;
        _ui = ui;
        
        if (_ui == null)
        {
            Debug.LogError("[FlappyBirdController] FlappyBirdUI не назначен!");
            return;
        }
        
        _bird = _ui.Bird;
        if (_bird == null)
        {
            Debug.LogError("[FlappyBirdController] Bird не назначен в FlappyBirdUI!");
            return;
        }
        
        _birdVelocity = 0f;
        _bird.anchoredPosition = new Vector2(-150, 0);
        _bird.localRotation = Quaternion.identity;
        
        if (_ui.ScoreText != null)
            _ui.ScoreText.text = "Score: 0";
        
        if (_ui.FinishButton != null)
            _ui.FinishButton.onClick.AddListener(FinishGame);
        
        _ui.ShowGameOver(false);
    }

    private void Update()
    {
        if (_isGameOver) return;
        
        UpdateBird();
        UpdatePipes();
        CheckCollisions();
    }

    private void UpdateBird()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            _birdVelocity = _jumpForce;
        }
        
        _birdVelocity += _gravity * Time.deltaTime;
        _bird.anchoredPosition += new Vector2(0, _birdVelocity * Time.deltaTime);
        
        float rotation = Mathf.Clamp(_birdVelocity * 0.2f, -30f, 30f);
        _bird.localRotation = Quaternion.Euler(0, 0, rotation);
        
        float screenTop = _gameHeight * 0.5f;
        float screenBottom = -_gameHeight * 0.5f;
        
        if (_bird.anchoredPosition.y > screenTop || _bird.anchoredPosition.y < screenBottom)
        {
            GameOver();
        }
    }

    private void UpdatePipes()
    {
        _pipeSpawnTimer -= Time.deltaTime;
        
        if (_pipeSpawnTimer <= 0)
        {
            SpawnPipe();
            _pipeSpawnTimer = _pipeSpawnInterval;
        }
        
        for (int i = _pipes.Count - 1; i >= 0; i--)
        {
            var pipe = _pipes[i];
            if (pipe == null)
            {
                _pipes.RemoveAt(i);
                continue;
            }
            
            pipe.anchoredPosition += new Vector2(-_pipeSpeed * Time.deltaTime, 0);
            
            if (pipe.anchoredPosition.x < -250)
            {
                if (pipe.name.Contains("Top") && !pipe.name.Contains("Scored"))
                {
                    _score++;
                    if (_ui != null && _ui.ScoreText != null)
                        _ui.ScoreText.text = $"Score: {_score}";
                    pipe.name += "_Scored";
                }
                
                if (pipe.anchoredPosition.x < -300)
                {
                    Destroy(pipe.gameObject);
                    _pipes.RemoveAt(i);
                }
            }
        }
    }

    private void SpawnPipe()
    {
        if (_ui == null || _ui.PipesContainer == null)
            return;
            
        float gapY = Random.Range(-150f, 150f);
        
        var topPipe = CreatePipe("TopPipe", gapY + _pipeGap * 0.5f, true);
        var bottomPipe = CreatePipe("BottomPipe", gapY - _pipeGap * 0.5f, false);
        
        _pipes.Add(topPipe);
        _pipes.Add(bottomPipe);
    }

    private RectTransform CreatePipe(string name, float yPos, bool isTop)
    {
        var pipeObj = new GameObject(name);
        pipeObj.transform.SetParent(_ui.PipesContainer, false);
        
        var image = pipeObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.8f, 0.2f);
        
        var rect = pipeObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, _gameHeight);
        rect.anchoredPosition = new Vector2(250, yPos);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, isTop ? 0f : 1f);
        
        return rect;
    }

    private void CheckCollisions()
    {
        Vector2 birdPos = _bird.anchoredPosition;
        Vector2 birdSize = _bird.sizeDelta;
        
        float birdLeft = birdPos.x - birdSize.x * 0.5f;
        float birdRight = birdPos.x + birdSize.x * 0.5f;
        float birdTop = birdPos.y + birdSize.y * 0.5f;
        float birdBottom = birdPos.y - birdSize.y * 0.5f;
        
        foreach (var pipe in _pipes)
        {
            if (pipe == null || pipe.name.Contains("Scored")) continue;
            
            Vector2 pipePos = pipe.anchoredPosition;
            Vector2 pipeSize = pipe.sizeDelta;
            
            float pipeLeft = pipePos.x - pipeSize.x * 0.5f;
            float pipeRight = pipePos.x + pipeSize.x * 0.5f;
            
            if (birdRight < pipeLeft || birdLeft > pipeRight)
                continue;
            
            if (pipe.name.Contains("Top"))
            {
                float pipeTop = pipePos.y;
                float pipeBottom = pipeTop - pipeSize.y;
                
                if (birdTop > pipeBottom && birdBottom < pipeTop)
                {
                    GameOver();
                    return;
                }
            }
            else
            {
                float pipeBottom = pipePos.y;
                float pipeTop = pipeBottom + pipeSize.y;
                
                if (birdTop > pipeBottom && birdBottom < pipeTop)
                {
                    GameOver();
                    return;
                }
            }
        }
    }

    private void GameOver()
    {
        if (_isGameOver) return;
        
        _isGameOver = true;
        if (_ui != null)
            _ui.ShowGameOver(true);
    }

    private void FinishGame()
    {
        if (_gameEvent != null)
        {
            _gameEvent.OnGameEnded(_score);
        }
    }

    private void OnDestroy()
    {
        if (_ui != null && _ui.FinishButton != null)
        {
            _ui.FinishButton.onClick.RemoveAllListeners();
        }
    }
}

