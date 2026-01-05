using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlappyBirdGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private FlappyBirdController _controller;
    private float _gameWidth = 400f;
    private float _gameHeight = 600f;

    protected override void OnStartGame()
    {
        CreateGameUI();
    }

    private void CreateGameUI()
    {
        _gameRoot = new GameObject("FlappyBirdGame");
        _gameRoot.transform.SetParent(_parentContainer, false);
        
        var rootRect = _gameRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        
        var canvas = _gameRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        
        var scaler = _gameRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(400, 600);
        
        _gameRoot.AddComponent<GraphicRaycaster>();
        
        CreateBackground();
        _controller = _gameRoot.AddComponent<FlappyBirdController>();
        _controller.Initialize(this);
    }

    private void CreateBackground()
    {
        var bg = new GameObject("Background");
        bg.transform.SetParent(_gameRoot.transform, false);
        
        var image = bg.AddComponent<Image>();
        image.color = new Color(0.4f, 0.8f, 1f);
        
        var rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
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
    
    private TMP_Text _scoreText;
    private TMP_Text _gameOverText;
    private Button _finishButton;
    
    private float _gameHeight = 600f;
    private List<RectTransform> _pipes = new List<RectTransform>();

    public void Initialize(FlappyBirdGameEvent gameEvent)
    {
        _gameEvent = gameEvent;
        CreateBird();
        CreateUI();
    }

    private void CreateBird()
    {
        var birdObj = new GameObject("Bird");
        birdObj.transform.SetParent(transform, false);
        
        var image = birdObj.AddComponent<Image>();
        image.color = Color.yellow;
        
        _bird = birdObj.GetComponent<RectTransform>();
        _bird.sizeDelta = new Vector2(40, 40);
        _bird.anchoredPosition = new Vector2(-150, 0);
        _bird.anchorMin = new Vector2(0.5f, 0.5f);
        _bird.anchorMax = new Vector2(0.5f, 0.5f);
        _bird.pivot = new Vector2(0.5f, 0.5f);
        
        _birdVelocity = 0f;
    }

    private void CreateUI()
    {
        var scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(transform, false);
        
        _scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        _scoreText.text = "Score: 0";
        _scoreText.fontSize = 24;
        _scoreText.color = Color.white;
        _scoreText.alignment = TextAlignmentOptions.TopFlush;
        
        var scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 1f);
        scoreRect.anchorMax = new Vector2(0.5f, 1f);
        scoreRect.anchoredPosition = new Vector2(0, -30);
        scoreRect.sizeDelta = new Vector2(200, 50);
        
        var gameOverObj = new GameObject("GameOverText");
        gameOverObj.transform.SetParent(transform, false);
        gameOverObj.SetActive(false);
        
        _gameOverText = gameOverObj.AddComponent<TextMeshProUGUI>();
        _gameOverText.text = "Game Over!";
        _gameOverText.fontSize = 32;
        _gameOverText.color = Color.red;
        _gameOverText.alignment = TextAlignmentOptions.Center;
        
        var gameOverRect = gameOverObj.GetComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.6f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.6f);
        gameOverRect.anchoredPosition = Vector2.zero;
        gameOverRect.sizeDelta = new Vector2(300, 100);
        
        var buttonObj = new GameObject("FinishButton");
        buttonObj.transform.SetParent(transform, false);
        buttonObj.SetActive(false);
        
        _finishButton = buttonObj.AddComponent<Button>();
        _finishButton.onClick.AddListener(FinishGame);
        
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.3f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.3f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(150, 50);
        
        var buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.6f, 0.2f);
        _finishButton.targetGraphic = buttonBg;
        
        var buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Finish";
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
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
                    _scoreText.text = $"Score: {_score}";
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
        float gapY = Random.Range(-150f, 150f);
        
        var topPipe = CreatePipe("TopPipe", gapY + _pipeGap * 0.5f, true);
        var bottomPipe = CreatePipe("BottomPipe", gapY - _pipeGap * 0.5f, false);
        
        _pipes.Add(topPipe);
        _pipes.Add(bottomPipe);
    }

    private RectTransform CreatePipe(string name, float yPos, bool isTop)
    {
        var pipeObj = new GameObject(name);
        pipeObj.transform.SetParent(transform, false);
        
        var image = pipeObj.AddComponent<Image>();
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
        
        foreach (var pipe in _pipes)
        {
            if (pipe == null || pipe.name.Contains("Scored")) continue;
            
            Vector2 pipePos = pipe.anchoredPosition;
            Vector2 pipeSize = pipe.sizeDelta;
            
            if (pipe.name.Contains("Top"))
            {
                float pipeTop = pipePos.y;
                float pipeBottom = pipeTop - pipeSize.y;
                
                if (birdPos.x + birdSize.x * 0.5f > pipePos.x - pipeSize.x * 0.5f &&
                    birdPos.x - birdSize.x * 0.5f < pipePos.x + pipeSize.x * 0.5f &&
                    birdPos.y + birdSize.y * 0.5f > pipeBottom)
                {
                    GameOver();
                    return;
                }
            }
            else
            {
                float pipeBottom = pipePos.y;
                float pipeTop = pipeBottom + pipeSize.y;
                
                if (birdPos.x + birdSize.x * 0.5f > pipePos.x - pipeSize.x * 0.5f &&
                    birdPos.x - birdSize.x * 0.5f < pipePos.x + pipeSize.x * 0.5f &&
                    birdPos.y - birdSize.y * 0.5f < pipeTop)
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
        _gameOverText.gameObject.SetActive(true);
        _finishButton.gameObject.SetActive(true);
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
        if (_finishButton != null)
        {
            _finishButton.onClick.RemoveAllListeners();
        }
    }
}

