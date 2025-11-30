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

        // IMPORTANTE: Desactivamos la gravedad estándar de Unity para usar la nuestra radial.
        rb.gravityScale = 0f;

        // Congelamos la rotación física para controlarla nosotros manualmente.
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        // 1. Regla de Oro: Solo el dueño controla su input
        if (!IsOwner) return;

        HandleInput();
    }

    private void FixedUpdate()
    {
        // 2. Las físicas corren en el dueño y se replican vía NetworkTransform
        // O si es ServerAuth, esto debería correr en el Servidor. 
        // Asumimos ClientAuth (NetworkTransform estándar) para respuesta inmediata.
        if (!IsOwner) return;

        ApplyGravity();
        AlignToPlanet();
        CheckGround();
        ApplyMovement();
    }

    private void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        inputVector = new Vector2(x, 0);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequest = true;
        }
    }

    private void ApplyGravity()
    {
        // Calculamos la dirección hacia el centro (0,0)
        Vector2 directionToCenter = (Vector2.zero - rb.position).normalized;

        // Aplicamos fuerza constante hacia el centro
        rb.AddForce(directionToCenter * gravityStrength);
    }

    private void AlignToPlanet()
    {
        // Calculamos el vector que va del centro al jugador (Arriba relativo)
        Vector2 directionFromCenter = rb.position - Vector2.zero;

        // Calculamos el ángulo en grados para rotar el sprite
        // -90 es un ajuste común porque Atan2 asume 0 grados a la derecha (eje X)
        float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg - 90f;

        // Aplicamos la rotación suavemente (aunque FixedUpdate es rápido, Lerp ayuda visualmente)
        // Usamos MoveTowardsAngle para evitar giros locos de 360 grados
        float currentAngle = transform.rotation.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, angle, 360f * Time.fixedDeltaTime); // Rotación instantánea pero limpia

        rb.rotation = newAngle;
    }

    private void CheckGround()
    {
        // Lanzamos un rayo desde el jugador hacia el centro del planeta
        Vector2 directionToCenter = (Vector2.zero - rb.position).normalized;

        // Debug visual del rayo
        Debug.DrawRay(transform.position, directionToCenter * groundCheckDistance, Color.red);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToCenter, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    private void ApplyMovement()
    {
        // 1. Obtener la velocidad actual del objeto
        Vector2 currentVelocity = rb.linearVelocity; // Unity 6 API (antes rb.velocity)

        // 2. Convertir esa velocidad global a local relativa al jugador
        // transform.InverseTransformDirection convierte un vector del mundo a coordenadas locales del objeto
        Vector2 localVelocity = transform.InverseTransformDirection(currentVelocity);

        // 3. Sobrescribir SOLO la velocidad en X (movimiento lateral)
        // Mantenemos la velocidad en Y (caída/gravedad) intacta
        localVelocity.x = inputVector.x * moveSpeed;

        // 4. Lógica de Salto
        if (jumpRequest)
        {
            // Añadimos velocidad instantánea hacia arriba (eje Y local)
            localVelocity.y = jumpForce;
            jumpRequest = false;
        }

        // 5. Convertir de nuevo a velocidad global y aplicar
        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }
}