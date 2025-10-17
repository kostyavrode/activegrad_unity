using UnityEngine;
using Zenject;

public class CharacterService : IInitializable
{
    private readonly UserDataService _userData;
    private readonly LocationService _locationService;
    private readonly DiContainer _container;

    private CharacterController3D _character;
    private Vector2 _lastCoords;

    private readonly Vector3 _spawnPoint = Vector3.zero;
    
    //public Transform CharacterTransform => _character.transform;

    public CharacterService(UserDataService userData, LocationService locationService, DiContainer container)
    {
        _userData = userData;
        _locationService = locationService;
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
        
        var clothing = instance.GetComponent<ClothingApplier>();
        clothing.ApplyClothing(_userData.Data);
        
        _lastCoords = _locationService.GetCoordinates();
    }

    public void Update()
    {
        if (_character == null) return;

        var currentCoords = _locationService.GetCoordinates();

        if (currentCoords != Vector2.zero && currentCoords != _lastCoords)
        {
            Vector2 delta = currentCoords - _lastCoords;
            _lastCoords = currentCoords;

            Vector3 direction = new Vector3(delta.y, 0, delta.x); 
            _character.Move(direction);
        }
    }

    public Transform GetCharacterTransform()
    {
        return _character.transform;
    }
}
