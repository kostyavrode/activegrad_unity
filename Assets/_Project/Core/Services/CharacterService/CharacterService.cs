using UnityEngine;
using Zenject;

public class CharacterService : IInitializable, ITickable
{
    private readonly UserDataService _userData;
    private readonly LocationService _locationService;
    private readonly SmoothMapMovementService _mapMovementService;
    private readonly DiContainer _container;

    private CharacterController3D _character;
    private Vector2 _lastCoords;
    private bool _lastCoordsInitialized = false;
    private const float COORDINATE_THRESHOLD = 0.0001f;

    private readonly Vector3 _spawnPoint = Vector3.zero;

    public CharacterService(UserDataService userData, LocationService locationService, SmoothMapMovementService mapMovementService, DiContainer container)
    {
        _userData = userData;
        _locationService = locationService;
        _mapMovementService = mapMovementService;
        _container = container;
    }

    public void Initialize()
    {
        SpawnCharacter();
    }

    private void SpawnCharacter()
    {
        var prefab = Resources.Load<GameObject>("Character/CharacterBase");
        var instance = _container.InstantiatePrefab(prefab, _spawnPoint, Quaternion.identity, null);

        _character = instance.GetComponent<CharacterController3D>();
        if (_character == null)
        {
            _character = instance.GetComponentInChildren<CharacterController3D>();
        }
        if (_character == null)
        {
            _character = instance.AddComponent<CharacterController3D>();
        }
        
        var clothing = instance.GetComponent<ClothingApplier>();
        clothing.ApplyClothing(_userData.Data);

        MapShadowHelper.EnableCastShadows(instance.transform);
        
        _lastCoords = _locationService.GetCoordinates();
    }

    public void Tick()
    {
        if (_character == null)
        {
            return;
        }

        var currentCoords = _locationService.GetCoordinates();

        if (currentCoords == Vector2.zero)
        {
            return;
        }

        if (!_lastCoordsInitialized)
        {
            _lastCoords = currentCoords;
            _lastCoordsInitialized = true;
            return;
        }

        bool isMapLerping = _mapMovementService.IsLerping();
        Vector3 mapDirection = _mapMovementService.GetCurrentMapDirection();
        
        if (isMapLerping && mapDirection.magnitude > 0.001f)
        {
            _character.Move(mapDirection, keepWalking: true);
        }
        else if (!isMapLerping)
        {
            float distance = Vector2.Distance(currentCoords, _lastCoords);
            
            if (distance > COORDINATE_THRESHOLD)
            {
                Vector2 delta = currentCoords - _lastCoords;
                
                float latDelta = delta.y;
                float lonDelta = delta.x;
                
                Vector3 direction = new Vector3(lonDelta, 0, latDelta);
                
                if (direction.magnitude > 0.001f)
                {
                    _character.Move(direction.normalized, keepWalking: false);
                }
                else
                {
                    _character.Move(Vector3.zero, keepWalking: false);
                }
                
                _lastCoords = currentCoords;
            }
            else
            {
                _character.Move(Vector3.zero, keepWalking: false);
            }
        }
    }

    public Transform GetCharacterTransform()
    {
        return _character?.transform;
    }
}
