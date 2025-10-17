using UnityEngine;
using Zenject;

public class CameraController : MonoBehaviour, ITickable, IInitializable
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float zoomSpeed = 0.02f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;

    [Header("Пороговые значения (чувствительность)")]
    [SerializeField] private float horizontalThreshold = 30f; // пиксели
    [SerializeField] private float verticalThreshold = 60f;   // пиксели

    private CharacterService _characterService;
    private Transform _target;
    private float _currentAngle;

    [Inject]
    public void Construct(CharacterService characterService)
    {
        _characterService = characterService;
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

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif

        // Вычисляем позицию камеры
        Vector3 offset = Quaternion.Euler(0, _currentAngle, 0) * new Vector3(0, 0, -distance);
        Vector3 targetPos = _target.position + Vector3.up * height;

        cameraTransform.position = targetPos + offset;
        cameraTransform.LookAt(targetPos);
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButton(0))
        {
            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");

            // Выбираем, какое движение сильнее
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

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                // Если движение больше по X → вращаем, иначе → зумируем
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (Mathf.Abs(delta.x) < horizontalThreshold) return;

                    float deltaX = delta.x * rotationSpeed * Time.deltaTime * 0.1f;
                    _currentAngle += deltaX;
                }
                else
                {
                    if (Mathf.Abs(delta.y) < verticalThreshold) return;

                    float deltaY = delta.y * zoomSpeed;
                    distance -= deltaY;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
        }
    }
}
