using UnityEngine;
using Unity.Netcode;
using TMPro;

// Regla: Heredamos de NetworkBehaviour para tener acceso a IsOwner
[RequireComponent(typeof(Rigidbody2D))]
public class OrbitalPlayerController : NetworkBehaviour
{
    [Header("Orbital Settings")]
    [Tooltip("Speed at which the player orbits the center.")]
    [SerializeField] private float rotationSpeed = -30f;

    [Tooltip("Axis used for rotation.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Header("Jump & Gravity Settings")]
    [Tooltip("Force pulling the player towards the center (0,0).")]
    [SerializeField] private float attractionForce = 10f;

    [Tooltip("Force applied when jumping.")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Collection Settings")]
    [Tooltip("Tag required for the collectable objects.")]
    [SerializeField] private string materialTag = "Material 1";

    [Tooltip("UI Text component to display score.")]
    [SerializeField] private TMP_Text counterUIText;

    private int materialCount = 0;
    private Rigidbody2D rb;
    private bool isGrounded = true;

    // Usamos OnNetworkSpawn en lugar de Start para inicialización de red
    public override void OnNetworkSpawn()
    {
        // Regla: Buscar componentes locales
        rb = GetComponent<Rigidbody2D>();

        // Configuración crítica para evitar rotaciones locas por física
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Solo el dueño debe actualizar su propia UI local
        if (IsOwner)
        {
            UpdateCounterUI();
        }
    }

    private void Update()
    {
        // --- REGLA DE ORO MULTIPLAYER ---
        // Si NO soy el dueño de este objeto, no proceso inputs ni fuerzas locales.
        if (!IsOwner) return;

        HandleGravity();
        HandleJumpInput();
    }

    private void LateUpdate()
    {
        // También bloqueamos el movimiento orbital si no somos el dueño
        if (!IsOwner) return;

        HandleOrbitalMovement();
        HandleRadialAlignment();
    }

    private void HandleGravity()
    {
        // Atracción hacia el centro (0,0)
        Vector2 directionToCenter = (Vector2.zero - (Vector2)transform.position).normalized;
        rb.AddForce(directionToCenter * attractionForce);
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            PerformJump();
        }
    }

    private void HandleOrbitalMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            // RotateAround modifica el Transform directamente
            transform.RotateAround(Vector3.zero, rotationAxis, horizontalInput * rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleRadialAlignment()
    {
        // Mantiene los pies apuntando al centro
        Vector2 directionFromCenter = (transform.position - Vector3.zero).normalized;
        float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void PerformJump()
    {
        // Resetear velocidad lineal (Unity 6 API) para salto consistente
        rb.linearVelocity = Vector2.zero;

        Vector2 jumpDirection = transform.up;
        rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
    }

    // --- COLLISION LOGIC ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // La física corre en todos lados, pero solo queremos cambiar estado lógico si somos dueños
        if (!IsOwner) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 directionFromCenter = ((Vector2)transform.position - Vector2.zero).normalized;

            // Verificamos si la normal del contacto apunta hacia afuera (suelo)
            if (Vector2.Dot(contact.normal, directionFromCenter) > 0.9f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    // --- COLLECTION LOGIC ---

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.CompareTag(materialTag))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // En Multiplayer real, destruir un objeto requiere un ServerRpc.
                // Por ahora, lo mantendremos simple, pero ten en cuenta que 
                // destruir el objeto aquí solo lo borrará visualmente si no tiene NetworkObject.
                CollectMaterial(other.gameObject);
            }
        }
    }

    private void CollectMaterial(GameObject material)
    {
        // IMPORTANTE: Si el 'material' tiene NetworkObject, debes despawnearlo via ServerRpc.
        // Si es un objeto local decorativo, Destroy funciona bien.
        Destroy(material);

        materialCount++;
        UpdateCounterUI();
    }

    private void UpdateCounterUI()
    {
        // Verificación extra: Solo actualizar UI si existe y somos el jugador local
        if (counterUIText != null && IsOwner)
        {
            counterUIText.text = materialCount.ToString();
        }
    }
}