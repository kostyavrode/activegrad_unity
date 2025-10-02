using UnityEngine;

public class CharacterController3D : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 5f;

    private Vector3 _targetDirection;

    public void Move(Vector3 direction)
    {
        if (direction.magnitude > 0.01f)
        {
            _targetDirection = direction.normalized;

            // Поворот
            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // Запускаем анимацию ходьбы
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }
}