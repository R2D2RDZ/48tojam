using UnityEngine;
using TMPro;
using System.Collections; 
using UnityEngine.UI;

// Regla: Heredamos de NetworkBehaviour para tener acceso a IsOwner
[RequireComponent(typeof(Rigidbody2D))]
public class OrbitalPlayerController : NetworkBehaviour
{
    [Header("Orbital Settings")]
    [Tooltip("Speed at which the player orbits the center.")]
    [SerializeField] private float rotationSpeed = -30f;

    // --- Variables de Salto y Atracción ---
    [Header("Configuración de Salto y Atracción")]
    public float fuerzaDeAtraccion = 10f; 
    public float fuerzaDeSalto = 5f;      
    
    // --- Variables de Recolección ---
    [Header("Configuración de Recolección")]
    public string tagDelMaterial = "Material 1"; 
    public TMP_Text contadorGlobalUIText; 
    public float duracionAnimacionGolpe = 0.2f;   
    public float intensidadAnimacionGolpe = 0.1f; 
    
    private int contadorMaterial = 0;          
    private Rigidbody2D rb;                
    private bool estaEnSuelo = true;       

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
        if (rb == null)
        {
            Debug.LogError("ERROR: El objeto necesita un Rigidbody2D.");
            return;
        }
        
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        ActualizarContadorGlobalUI(); 
        
        if (contadorGlobalUIText == null)
        {
             Debug.LogWarning("Falta asignar el Contador Global UI Text en el Cubo.");
        }
    }

    void Update()
    {
        // El movimiento (Atracción, Salto) solo si NO es kinemático (no está golpeando/animando).
        if (!rb.isKinematic) 
        {
            // Lógica de Atracción Radial
            if (centro != null)
            {
                Vector2 direccionHaciaCentro = ((Vector2)centro.position - (Vector2)transform.position).normalized;
                rb.AddForce(direccionHaciaCentro * fuerzaDeAtraccion);
            }

            // Lógica de Salto (Input)
            if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
            {
                Saltar(); 
            }
        }
    }

    private void Update()
    {
        // La órbita solo si NO es kinemático.
        if (!rb.isKinematic)
        {
            float inputHorizontal = Input.GetAxis("Horizontal"); 
            if (inputHorizontal != 0 && centro != null)
            {
                Vector3 puntoPivot = centro.position; 
                float anguloARotar = inputHorizontal * velocidadDeRotacion * Time.deltaTime;
                transform.RotateAround(puntoPivot, ejeDeRotacion, anguloARotar);
            }
        }
        
        // Alineación Radial (Siempre se ejecuta)
        if (centro != null)
        {
            PerformJump();
        }
    }
    
    // LÓGICA DE INTERACCIÓN CON EL MINERAL
    void OnTriggerStay2D(Collider2D other)
    {
        if (centro == null) return;
        
        RecursoMineral recurso = other.GetComponent<RecursoMineral>();

        if (recurso != null && other.CompareTag(tagDelMaterial))
        {
            // Solo pica si presionamos 'Q' y si el mineral está disponible.
            // La comprobación de !rb.isKinematic es crucial aquí.
            if (Input.GetKeyDown(KeyCode.Q) && !rb.isKinematic && !recurso.EstaRecargando())
            {
                Vector3 posicionRadialInicial = transform.position - centro.position;
                
                // 1. Inicia la animación de OSCILACIÓN (y bloquea el movimiento)
                StartCoroutine(AnimarGolpeRecoleccion(posicionRadialInicial));
                
                // 2. Llama al mineral para que inicie su contador de 4 segundos
                recurso.IniciarPicado(this); 
            }
        }
    }
    
    // Corrutina para la animación de OSCILACIÓN (entra y sale radialmente)
    private IEnumerator AnimarGolpeRecoleccion(Vector3 posicionRadialInicial)
    {
        // 1. Bloquear y detener el movimiento físico
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.isKinematic = true; // ⬅️ CRUCIAL: Bloquea la física para permitir la oscilación forzada
        
        // Asegurar la posición base antes de iniciar el ciclo
        transform.position = centro.position + posicionRadialInicial; 
        
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionAnimacionGolpe)
        {
            float tiempoNormalizado = tiempoTranscurrido / duracionAnimacionGolpe;
            
            // Crea el movimiento suave de ida y vuelta (0 -> 1 -> 0)
            float offset = Mathf.Sin(tiempoNormalizado * Mathf.PI) * intensidadAnimacionGolpe; 
            
            // Multiplica el offset por la dirección radial (transform.up)
            Vector3 offsetVector = transform.up * offset; 
            
            // 2. FORZAR POSICIÓN: Mueve el cubo a la posición base + la oscilación
            transform.position = centro.position + posicionRadialInicial + offsetVector;

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // 3. Regresar a la posición exacta y devolver el control a la física
        transform.position = centro.position + posicionRadialInicial;
        rb.isKinematic = false; // ⬅️ DESBLOQUEO: Devuelve el control para la órbita/salto
    }

    // Este método es llamado por el MINERAL (RecursoMineral) después de cada golpe de 4s.
    public void RecolectarCompletado()
    {
        contadorMaterial++;
        ActualizarContadorGlobalUI();
    }
    
    private void ActualizarContadorGlobalUI()
    {
        if (contadorGlobalUIText != null)
        {
            contadorGlobalUIText.text = "Material: " + contadorMaterial.ToString();
        }
    }
    
    private void Saltar()
    {
        Vector2 direccionDeSalto = transform.up; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(direccionDeSalto * fuerzaDeSalto, ForceMode2D.Impulse);
        estaEnSuelo = false; 
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (centro == null) return;

        foreach (ContactPoint2D contacto in collision.contacts)
        {
            Vector2 direccionDesdeCentro = ((Vector2)transform.position - (Vector2)centro.position).normalized;
            
            if (Vector2.Dot(contacto.normal, direccionDesdeCentro) > 0.9f)
            {
                estaEnSuelo = true;
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