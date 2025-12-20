using UnityEngine;

public class CharacterController3D : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private float animationBlendSpeed = 5f;

    private Vector3 _targetDirection;
    private bool _isMoving = false;
    private bool _shouldKeepWalking = false;
    private float _currentVertValue = 0f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        bool shouldMove = (_isMoving || _shouldKeepWalking) && _targetDirection.magnitude > movementThreshold;
        float targetVertValue = shouldMove ? 1f : 0f;
        
        _currentVertValue = Mathf.MoveTowards(_currentVertValue, targetVertValue, animationBlendSpeed * Time.deltaTime);
        
        if (animator != null)
        {
            animator.SetFloat("Vert", _currentVertValue);
        }
        
        if (shouldMove)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void Move(Vector3 direction, bool keepWalking = false)
    {
        _shouldKeepWalking = keepWalking;
        
        if (direction.magnitude > movementThreshold)
        {
            _targetDirection = direction.normalized;
            _isMoving = true;
        }
        else if (!keepWalking)
        {
            _isMoving = false;
        }
    }
}