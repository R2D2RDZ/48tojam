using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class PlanetaryMovementController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Walking speed on the planet surface.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Force applied when jumping.")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Physics Settings")]
    [Tooltip("Force pulling the player towards the center (0,0).")]
    [SerializeField] private float gravityStrength = 20f;

    [Tooltip("Distance to check for ground.")]
    [SerializeField] private float groundCheckDistance = 1.1f;

    [Tooltip("Layer mask to detect the planet surface.")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Vector2 inputVector;
    private bool isGrounded;
    private bool jumpRequest;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        // Desactivamos gravedad global
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleInput();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        ApplyGravity();
        AlignToPlanet();
        CheckGround();
        ApplyMovement();
    }

    private void HandleInput()
    {
        // 1. Obtener Input de Teclado (WASD/Flechas)
        float x = Input.GetAxisRaw("Horizontal");

        // 2. Obtener Input Táctil (Si existe el Manager)
        if (TouchInputManager.Instance != null)
        {
            // Sumamos el input táctil. 
            // Si presionas 'D' (1) y botón Izquierda (-1), se cancelan (0), lo cual es correcto.
            x += TouchInputManager.Instance.HorizontalInput;
        }

        // Clampeamos entre -1 y 1 para evitar doble velocidad si se usan ambos a la vez
        x = Mathf.Clamp(x, -1f, 1f);

        inputVector = new Vector2(x, 0);

        // 3. Lógica de Salto (Teclado O Botón UI)
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);

        if (TouchInputManager.Instance != null && TouchInputManager.Instance.JumpTriggered)
        {
            jumpInput = true;
        }

        if (jumpInput && isGrounded)
        {
            jumpRequest = true;
        }
    }

    private void ApplyGravity()
    {
        Vector2 directionToCenter = (Vector2.zero - rb.position).normalized;
        rb.AddForce(directionToCenter * gravityStrength);
    }

    private void AlignToPlanet()
    {
        Vector2 directionFromCenter = rb.position - Vector2.zero;
        float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg - 90f;

        float currentAngle = transform.rotation.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, angle, 360f * Time.fixedDeltaTime);

        rb.rotation = newAngle;
    }

    private void CheckGround()
    {
        Vector2 directionToCenter = (Vector2.zero - rb.position).normalized;

        // Debug visual
        // Debug.DrawRay(transform.position, directionToCenter * groundCheckDistance, Color.red);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToCenter, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    private void ApplyMovement()
    {
        Vector2 currentVelocity = rb.linearVelocity;
        Vector2 localVelocity = transform.InverseTransformDirection(currentVelocity);

        localVelocity.x = inputVector.x * moveSpeed;

        if (jumpRequest)
        {
            localVelocity.y = jumpForce;
            jumpRequest = false;
        }

        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }
}