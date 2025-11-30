using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimationController : MonoBehaviour
{
    // Nombres de parámetros cacheados para optimizar rendimiento (String to Hash)
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private Animator animator;
    private Rigidbody2D rb;

    private void Start()
    {
        // Regla 4: Inicialización automática de componentes
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        // Obtenemos la magnitud de la velocidad (qué tan rápido se mueve sin importar dirección)
        // Si usas movimiento solo horizontal, podrías usar Mathf.Abs(rb.velocity.x)
        float currentSpeed = rb.linearVelocity.magnitude;

        // Enviamos el valor al Animator
        animator.SetFloat(SpeedParam, currentSpeed);
    }
}