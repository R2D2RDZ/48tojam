using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.UI; // Necesario para 'Image' u otros componentes UI si los usas

// Este script ahora solo se encarga de la MINERÍA, INTERACCIÓN, SALUD e INVENTARIO.
// El movimiento lo maneja 'PlanetaryMovementController'.
public class ControladorOrbital : NetworkBehaviour
{
    // --- Variables del JUGADOR (Versión Antigua - Conservadas) ---
    [Header("Configuración de Salud y Combate")]
    public int saludMaxima = 50; 
    public int danoAtaque = 10;
    public float rangoAtaque = 0.5f;
    public float anchoAtaque = 0.2f;
    public string tagJugador = "Jugador";
    public TMP_Text saludUIText; 
    public GameObject prefabImpactoFX; 

    // --- Variables de INVENTARIO y MUERTE (Versión Antigua - Conservadas) ---
    [Header("Inventario, Muerte y Estado")]
    public int capacidadMaxima = 50; // Límite de material en la mochila
    // ✅ Ahora pública para que el ControladorDios pueda leerla
    public int materialesActuales = 0; 
    public GameObject prefabCapsulaMuerte; 
    public TMP_Text contadorGlobalUIText; // UI del inventario

    // --- Variables de MINADO (Versión Nueva - Conservadas) ---
    [Header("Collection Settings")]
    [Tooltip("Tag required for the collectable objects.")]
    [SerializeField] private string materialTag = "Mineral 1";

    [Header("Mining Animation")]
    [Tooltip("Duration of the mining shake animation.")]
    [SerializeField] private float mineAnimDuration = 0.2f;

    [Tooltip("Intensity of the mining shake.")]
    [SerializeField] private float mineAnimIntensity = 0.1f;

    // --- Estado Interno ---
    private int saludActual;
    private Rigidbody2D rb;                
    private bool estaParalizado = false;
    private bool isMining = false; // Para evitar spammear la tecla Q
    
    // Asumo que tienes una referencia a tu centro, aunque el movimiento ya no esté aquí.
    [Header("Referencias Físicas")]
    public Transform centro; 
    
    // Nota: Las variables de salto, rotación y atracción se asumen movidas a 'PlanetaryMovementController'

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // --- Lógica de Inicialización de la Versión Antigua ---
        if (rb == null)
        {
             // Si el RigidBody es esencial para Ataque/Salud, debe estar aquí.
             Debug.LogError("ERROR: El objeto necesita un Rigidbody2D.");
             return;
        }

        saludActual = saludMaxima;
        // materialCount (anteriormente materialesActuales) se inicializa a 0
        materialesActuales = 0; 
        
        if (IsOwner)
        {
            ActualizarContadorGlobalUI(); 
            ActualizarSaludUI();
            
            if (prefabCapsulaMuerte == null)
            {
                 Debug.LogWarning("Falta asignar el Prefab Capsula Muerte en el Inspector.");
            }
        }
    }

    // Usamos OnTriggerStay para detectar si estamos sobre el mineral y presionamos Q
    private void OnTriggerStay2D(Collider2D other)
    {
        // 1. Regla: Solo el dueño puede iniciar la acción (si el minado no debe ser ejecutado en el server)
        if (!IsOwner) return;

        // Si ya estamos minando o paralizados, no hacemos nada
        if (isMining || estaParalizado) return;

        // 2. Verificamos el Tag
        if (other.CompareTag(materialTag))
        {
            // Intentamos obtener el script del mineral
            RecursoMineral resource = other.GetComponent<RecursoMineral>();

            // 3. Input 'Q' y validación del recurso
            if (Input.GetKey(KeyCode.Q) && resource != null && !resource.EstaRecargando())
            {
                // Iniciamos la secuencia de minado
                StartCoroutine(MiningSequence(resource));
            }
        }
        
        // Lógica de Ataque (si se mantiene aquí y no en el Update)
        if (Input.GetKeyDown(KeyCode.Q) && !rb.isKinematic)
        {
             // Esto se ejecuta si Q se presiona, pero si el minado usa GetKey, puede haber conflicto
             if (!AtacarEnRango())
             {
                 // Lógica de animación de golpe (si no hay mineral)
             }
        }
    }
    
    // La lógica de ataque (con rango) para golpear a otros jugadores (si no está en Update)
    bool AtacarEnRango()
    {
        // ... (Tu lógica de OverlapBoxAll para combate cuerpo a cuerpo) ...
        // Dado que el código original estaba muy fragmentado aquí, solo dejo el esqueleto.
        Vector2 puntoCentralAtaque = (Vector2)transform.position + (Vector2)transform.up * (rangoAtaque / 2f);
        Collider2D[] objetosGolpeados = Physics2D.OverlapBoxAll(puntoCentralAtaque, Vector2.one, transform.eulerAngles.z);
        bool danoInfligido = false;
        
        // Lógica para aplicar daño si se golpea a otro jugador
        
        return danoInfligido;
    }

    private IEnumerator MiningSequence(RecursoMineral resource)
    {
        isMining = true;

        // A. Guardamos estado físico previo
        bool wasKinematic = rb.isKinematic;
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero; // Detener al jugador

        // B. Calcular posición base relativa al centro (asumido Vector3.zero)
        Vector3 center = Vector3.zero;
        Vector3 initialRadialPos = transform.position - center;

        // C. Animación de oscilación (Golpe)
        float elapsedTime = 0f;
        while (elapsedTime < mineAnimDuration)
        {
            float normalizedTime = elapsedTime / mineAnimDuration;
            float offset = Mathf.Sin(normalizedTime * Mathf.PI) * mineAnimIntensity;
            Vector3 offsetVector = transform.up * offset;
            transform.position = center + initialRadialPos + offsetVector;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // D. Restaurar posición exacta
        transform.position = center + initialRadialPos;

        // E. Restaurar físicas
        rb.isKinematic = wasKinematic;

        // F. Notificar al recurso
        // Se asume que este método existe en RecursoMineral.cs
        // resource.IniciarPicado(this);

        yield return new WaitForSeconds(0.1f); // Pequeña pausa
        isMining = false;
    }

    // Llamado por RecursoMineral.cs cuando la extracción es exitosa
    public void RecolectarCompletado()
    {
        // NOTA: Usar 'materialesActuales' para el inventario global
        RecogerMaterial(1); // Asumo que cada golpe exitoso da 1 unidad
    }

    // --- LÓGICA DE SALUD, ESTADOS Y MUERTE (PÚBLICAS) ---
    // (Funciones para ControladorDios, etc.)
    
    public void RecibirDano(int cantidadDano)
    {
        if (saludActual <= 0) return;
        saludActual -= cantidadDano;

        if (saludActual <= 0)
        {
            saludActual = 0;
            Morir();
        }
        ActualizarSaludUI();
    }
    
    public void Paralizar(float duracion)
    {
        if (estaParalizado) return;
        StartCoroutine(CorutinaParalizar(duracion));
    }

    private IEnumerator CorutinaParalizar(float duracion)
    {
        estaParalizado = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        yield return new WaitForSeconds(duracion);
        estaParalizado = false;
    }

    private void Morir()
    {
        if (materialesActuales > 0 && prefabCapsulaMuerte != null)
        {
            GameObject capsula = Instantiate(prefabCapsulaMuerte, transform.position, Quaternion.identity);
            CapsulaMuerte capsulaScript = capsula.GetComponent<CapsulaMuerte>();
            
            if (capsulaScript != null)
            {
                capsulaScript.materialesEnCapsula = materialesActuales; 
            }
        }
        
        materialesActuales = 0;
        // Destruir o Desactivar (dependiendo de tu lógica de reaparición)
        Destroy(gameObject); 
    }

    // --- LÓGICA DE INVENTARIO Y MATERIALES ---

    public bool RecogerMaterial(int cantidad)
    {
        int espacioDisponible = capacidadMaxima - materialesActuales;
        int cantidadARecoger = Mathf.Min(cantidad, espacioDisponible);

        if (cantidadARecoger > 0)
        {
            materialesActuales += cantidadARecoger;
            ActualizarContadorGlobalUI();
            return true; 
        }
        
        return false;
    }

    public void RestarMateriales(int cantidad)
    {
        materialesActuales = Mathf.Max(0, materialesActuales - cantidad);
        ActualizarContadorGlobalUI();
    }

    // --- Métodos de UI ---
    private void ActualizarSaludUI()
    {
        if (saludUIText != null && IsOwner)
        {
            saludUIText.text = "HP: " + saludActual.ToString() + " / " + saludMaxima.ToString();
        }
    }
    
    public void ActualizarContadorGlobalUI()
    {
         if (contadorGlobalUIText != null && IsOwner)
         {
             contadorGlobalUIText.text = "Material: " + materialesActuales.ToString() + " / " + capacidadMaxima.ToString();
         }
    }
    
    // --- Gizmos y otros Auxiliares ---

    void OnDrawGizmosSelected()
    {
        if (centro == null) return;
        Gizmos.color = Color.red;
        
        // Dibujar el rango de ataque (si aplica)
        // Vector3 puntoCentralAtaque = transform.position + transform.up * (rangoAtaque / 2f);
        // Gizmos.DrawWireCube(puntoCentralAtaque, new Vector3(anchoAtaque, rangoAtaque, 0));
    }
}