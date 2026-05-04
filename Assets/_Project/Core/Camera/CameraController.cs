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
    [SerializeField] private float minTilt = -20f;
    [SerializeField] private float maxTilt = 60f;
    [SerializeField] private float tiltSpeed = 80f;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSmoothSpeed = 5f;
    
    [SerializeField] private float horizontalThreshold = 30f;
    [SerializeField] private float verticalThreshold = 120f;

    [SerializeField] private Transform _menu;

    private CharacterService _characterService;
    private SightsUpdater _sightsUpdater;
    private Transform _target;
    private Transform _canvasTransform;
    private float _currentAngle;
    private float _currentTilt;

    [Inject]
    public void Construct(CharacterService characterService, SightsUpdater sightsUpdater)
    {
        _characterService = characterService;
        _sightsUpdater = sightsUpdater;
    }

    public void Initialize()
    {
        _currentAngle = 0f;
        _currentTilt = Mathf.Clamp(_currentTilt, minTilt, maxTilt);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _target = player.transform;

        var canvas = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas != null) _canvasTransform = canvas.transform;
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

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif

        Vector3 offset = Quaternion.Euler(_currentTilt, _currentAngle, 0) * new Vector3(0, 0, -distance);
        Vector3 targetPos = _target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPos + offset;

        if (followPlayer)
        {
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, followSmoothSpeed * Time.deltaTime);
        }
        else
        {
            cameraTransform.position = desiredPosition;
        }
        cameraTransform.LookAt(targetPos);
    }

    private void HandleMouseInput()
    {
        if (_canvasTransform != null && _canvasTransform.childCount > 1) return;

        if (_menu.gameObject.activeSelf)
        {
            if (Input.GetMouseButton(0))
            {
                /*Ray ray = cameraTransform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                CheckHit(ray);*/
                
                
                float deltaX = Input.GetAxis("Mouse X");
                float deltaY = Input.GetAxis("Mouse Y");

                if (Mathf.Abs(deltaX) >= horizontalThreshold * 0.01f)
                {
                    _currentAngle += deltaX * rotationSpeed * Time.deltaTime;
                }

                if (Mathf.Abs(deltaY) >= verticalThreshold * 0.01f)
                {
                    _currentTilt -= deltaY * tiltSpeed * Time.deltaTime;
                    _currentTilt = Mathf.Clamp(_currentTilt, minTilt, maxTilt);
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
            {
                distance -= scroll * zoomSpeed * 100f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
    }
    
    private void HandleTouchInput()
    {
        if (_canvasTransform != null && _canvasTransform.childCount > 1) return;

        if (!_menu.gameObject.activeSelf) return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x * 0.1f;
                float deltaY = touch.deltaPosition.y * 0.1f;

                if (Mathf.Abs(deltaX) >= horizontalThreshold * 0.01f)
                    _currentAngle += deltaX * rotationSpeed * Time.deltaTime;

                if (Mathf.Abs(deltaY) >= verticalThreshold * 0.01f)
                {
                    _currentTilt -= deltaY * tiltSpeed * Time.deltaTime;
                    _currentTilt = Mathf.Clamp(_currentTilt, minTilt, maxTilt);
                }
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Позиции пальцев на предыдущем кадре
            Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
            Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

            float prevDistance = Vector2.Distance(touch0Prev, touch1Prev);
            float currDistance = Vector2.Distance(touch0.position, touch1.position);

            float delta = prevDistance - currDistance;

            if (Mathf.Abs(delta) > 0.1f)
            {
                distance += delta * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
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
