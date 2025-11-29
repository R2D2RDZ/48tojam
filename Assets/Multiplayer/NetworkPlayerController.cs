using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed.")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("Immediate force applied when jumping.")]
    [SerializeField] private float jumpForce = 14f;

    [Tooltip("Multiplier for gravity when falling (makes jumps feel heavier).")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    // Internal References
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;

    // Input Actions (Local cache)
    private InputAction moveAction;
    private InputAction jumpAction;

    private void Awake()
    {
        // Regla: Obtener referencias en Awake/Start
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // Solo configuramos los inputs si este objeto pertenece al jugador local (nosotros)
        if (IsOwner)
        {
            SetupInput();
            // Opcional: Ajustar la cámara para seguir a este jugador
            // Camera.main.GetComponent<CameraFollow>().SetTarget(transform);
        }
    }

    private void SetupInput()
    {
        // Configuración rápida del Input System (Hardcoded para el ejemplo, idealmente usar Input Action Asset)
        var playerInput = new InputActionMap("Player");

        moveAction = playerInput.AddAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        jumpAction = playerInput.AddAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        moveAction.Enable();
        jumpAction.Enable();

        jumpAction.performed += _ => TryJump();
    }

    private void Update()
    {
        // Regla crítica: Si no somos el dueño, no procesamos input
        if (!IsOwner) return;

        ReadInput();
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        // La física corre en todos los clientes para suavidad, 
        // pero el NetworkTransform corregirá discrepancias.
        if (!IsOwner) return;

        CheckGround();
        ApplyMovement();
        ApplyGravityModifier();
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void HandleSpriteFlip()
    {
        // Flipeamos el sprite cambiando la escala local.
        // Como NetworkTransform sincroniza escala, los otros 49 jugadores verán esto.
        if (moveInput.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveInput.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void ApplyMovement()
    {
        // Movimiento directo de velocidad para respuesta rápida
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void ApplyGravityModifier()
    {
        // Esto hace que el salto se sienta "estilo Mario" (caer más rápido de lo que subes)
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }
        base.OnNetworkDespawn();
    }

    // Visualización en Editor para configurar el GroundCheck
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}