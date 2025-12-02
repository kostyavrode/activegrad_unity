using UnityEngine;
using Zenject;

public class CameraController : MonoBehaviour, ITickable, IInitializable
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float zoomSpeed = 0.01f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    
    [SerializeField] private float horizontalThreshold = 30f;
    [SerializeField] private float verticalThreshold = 120f;

    [SerializeField] private Transform _menu;

    private CharacterService _characterService;
    private SightsUpdater _sightsUpdater;
    private Transform _target;
    private float _currentAngle;

    [Inject]
    public void Construct(CharacterService characterService, SightsUpdater sightsUpdater)
    {
        _characterService = characterService;
        _sightsUpdater = sightsUpdater;
    }

    public void Initialize()
    {
        _currentAngle = 0f;
    }

    public void Tick()
    {
        if (_target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _target = player.transform;
            else
                return;
        }

//#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
//#else
//        HandleTouchInput();
//#endif

        Vector3 offset = Quaternion.Euler(0, _currentAngle, 0) * new Vector3(0, 0, -distance);
        Vector3 targetPos = _target.position + Vector3.up * height;

        cameraTransform.position = targetPos + offset;
        cameraTransform.LookAt(targetPos);
    }

    private void HandleMouseInput()
    {
        if (_menu.gameObject.activeSelf)
        {
            if (Input.GetMouseButton(0))
            {
                /*Ray ray = cameraTransform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                CheckHit(ray);*/
                
                
                float deltaX = Input.GetAxis("Mouse X");
                float deltaY = Input.GetAxis("Mouse Y");

                if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
                {
                    if (Mathf.Abs(deltaX) < horizontalThreshold * 0.01f) return;
                    _currentAngle += deltaX * rotationSpeed * Time.deltaTime;
                }
                else
                {
                    if (Mathf.Abs(deltaY) < verticalThreshold * 0.01f) return;
                    distance -= deltaY * zoomSpeed * 50f;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
        }
    }
    
    private void CheckHit(Ray ray)
    {
        if (Physics.Raycast(ray, out var hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            OnObjectClicked(clickedObject);
        }
    }
    
    private void OnObjectClicked(GameObject obj)
    {
        if (obj.TryGetComponent(out SightObject sightObject))
        {   
            _sightsUpdater.CreateSightDetailsPopup(sightObject.GetSightInfo());
        }
    }
}
