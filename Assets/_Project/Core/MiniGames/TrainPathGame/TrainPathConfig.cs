using UnityEngine;

[CreateAssetMenu(fileName = "TrainPathConfig", menuName = "ActiveGrad/MiniGames/TrainPath Config")]
public class TrainPathConfig : ScriptableObject
{
    [Header("Карта — Размер")]
    [SerializeField] private float _mapWidth = 800f;
    [SerializeField] private float _mapHeight = 600f;

    [Header("Карта — Станции")]
    [SerializeField] private int _minStations = 10;
    [SerializeField] private int _maxStations = 16;

    [Header("Карта — Время в пути")]
    [SerializeField] private float _minTravelTime = 0.5f;
    [SerializeField] private float _maxTravelTime = 5f;
    [SerializeField] private float _travelTimeVarianceMin = 0.6f;
    [SerializeField] private float _travelTimeVarianceMax = 1.5f;

    [Header("Карта — Ожидание на станции")]
    [SerializeField] private float _minWaitTime = 0.3f;
    [SerializeField] private float _maxWaitTime = 2.5f;

    [Header("Карта — Генерация путей")]
    [SerializeField] private float _connectionRadiusFactor = 0.42f;
    [SerializeField] private float _extraConnectionProbability = 0.65f;

    [Header("Груз")]
    [SerializeField] private int _minCargoStations = 2;
    [SerializeField] private int _maxCargoStations = 4;
    [SerializeField] private int _stationsPerCargo = 3;

    [Header("Таймер")]
    [SerializeField] private float _baseCountdownTime = 90f;
    [SerializeField] private float _timePerCargo = 20f;

    [Header("Спрайты")]
    [SerializeField] private Sprite _stationSprite;
    [SerializeField] private Sprite _startStationSprite;
    [SerializeField] private Sprite _endStationSprite;
    [SerializeField] private Sprite _cargoStationSprite;
    [SerializeField] private Sprite _trainSprite;

    [Header("Размеры")]
    [SerializeField] private Vector2 _stationSize = new Vector2(34f, 34f);
    [SerializeField] private Vector2 _trainSize = new Vector2(40f, 40f);
    [SerializeField] private float _clickRadius = 30f;

    [Header("Цвета — Станции")]
    [SerializeField] private Color _colorDefault = new Color(0.30f, 0.35f, 0.80f, 1f);
    [SerializeField] private Color _colorHighlight = new Color(1.00f, 0.90f, 0.10f, 1f);
    [SerializeField] private Color _colorCargo = new Color(0.90f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _colorCollected = new Color(0.42f, 0.45f, 0.50f, 1f);
    [SerializeField] private Color _colorStart = new Color(0.20f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color _colorEnd = new Color(0.75f, 0.20f, 0.20f, 1f);

    [Header("Цвета — Пути")]
    [SerializeField] private Color _pathColorFast = new Color(0.20f, 0.85f, 0.35f, 0.85f);
    [SerializeField] private Color _pathColorSlow = new Color(0.90f, 0.25f, 0.20f, 0.85f);
    [SerializeField] private float _pathWidthFast = 6f;
    [SerializeField] private float _pathWidthSlow = 3f;

    [Header("Размеры текста")]
    [SerializeField] private int _waitTimeFontSize = 10;
    [SerializeField] private int _pathTimeFontSize = 10;

    public float MapWidth => _mapWidth;
    public float MapHeight => _mapHeight;
    public int MinStations => _minStations;
    public int MaxStations => _maxStations;
    public float MinTravelTime => _minTravelTime;
    public float MaxTravelTime => _maxTravelTime;
    public float TravelTimeVarianceMin => _travelTimeVarianceMin;
    public float TravelTimeVarianceMax => _travelTimeVarianceMax;
    public float MinWaitTime => _minWaitTime;
    public float MaxWaitTime => _maxWaitTime;
    public float ConnectionRadiusFactor => _connectionRadiusFactor;
    public float ExtraConnectionProbability => _extraConnectionProbability;
    public int MinCargoStations => _minCargoStations;
    public int MaxCargoStations => _maxCargoStations;
    public int StationsPerCargo => _stationsPerCargo;
    public float BaseCountdownTime => _baseCountdownTime;
    public float TimePerCargo => _timePerCargo;
    public Sprite StationSprite => _stationSprite;
    public Sprite StartStationSprite => _startStationSprite;
    public Sprite EndStationSprite => _endStationSprite;
    public Sprite CargoStationSprite => _cargoStationSprite;
    public Sprite TrainSprite => _trainSprite;
    public Vector2 StationSize => _stationSize;
    public Vector2 TrainSize => _trainSize;
    public float ClickRadius => _clickRadius;
    public Color ColorDefault => _colorDefault;
    public Color ColorHighlight => _colorHighlight;
    public Color ColorCargo => _colorCargo;
    public Color ColorCollected => _colorCollected;
    public Color ColorStart => _colorStart;
    public Color ColorEnd => _colorEnd;
    public Color PathColorFast => _pathColorFast;
    public Color PathColorSlow => _pathColorSlow;
    public float PathWidthFast => _pathWidthFast;
    public float PathWidthSlow => _pathWidthSlow;
    public int WaitTimeFontSize => _waitTimeFontSize;
    public int PathTimeFontSize => _pathTimeFontSize;
}
