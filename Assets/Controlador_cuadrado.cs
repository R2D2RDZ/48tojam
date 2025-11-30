using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;

// Este script ahora solo se encarga de la MINERÍA e INTERACCIÓN.
// El movimiento lo maneja 'PlanetaryMovementController'.
public class ControladorOrbital : NetworkBehaviour
{
<<<<<<< HEAD
    // --- Variables de Órbita ---
    [Header("Configuración de Órbita")]
    public Transform centro;              
    public float velocidadDeRotacion = 100f; 
    public Vector3 ejeDeRotacion = Vector3.forward; 

    // --- Variables de Salto y Atracción ---
    [Header("Configuración de Salto y Atracción")]
    public float fuerzaDeAtraccion = 10f; 
    public float fuerzaDeSalto = 5f;      
    
    // --- Configuración de Recolección y Animación ---
    [Header("Configuración de Recolección y Animación")]
    public string tagDelMaterial = "Material 1"; 
    public TMP_Text contadorGlobalUIText; 
    public float duracionAnimacionGolpe = 0.2f;   
    public float intensidadAnimacionGolpe = 0.1f; 
    
    // --- Sistema de Combate y Salud ---
    [Header("Sistema de Combate y Salud")]
    public int saludMaxima = 50; 
    public int danoAtaque = 10;
    public float rangoAtaque = 0.5f;
    public float anchoAtaque = 0.2f;
    public string tagJugador = "Jugador";
    public TMP_Text saludUIText; 

    [Header("Efectos Visuales")]
    public GameObject prefabImpactoFX; 
    
    // 📢 CAMPOS DE INVENTARIO Y MUERTE
    [Header("Inventario, Muerte y Estado")]
    public int capacidadMaxima = 50; // Límite de material en la mochila
    
    // ✅ CORRECCIÓN CS0122: Debe ser público para que Dios pueda leerlo.
    public int materialesActuales = 0; 
    
    public GameObject prefabCapsulaMuerte; 
    
    // --- Variables Privadas ---
    private int saludActual;
    private Rigidbody2D rb;                
    private bool estaEnSuelo = true;       
    private bool estaParalizado = false;
=======
    [Header("Collection Settings")]
    [Tooltip("Tag required for the collectable objects.")]
    [SerializeField] private string materialTag = "Mineral 1";

    [Tooltip("UI Text component to display score.")]
    [SerializeField] private TMP_Text counterUIText;
>>>>>>> b5f36093d7ce31adb5cd7c0abb9abd3150ea9119

    [Header("Mining Animation")]
    [Tooltip("Duration of the mining shake animation.")]
    [SerializeField] private float mineAnimDuration = 0.2f;

    [Tooltip("Intensity of the mining shake.")]
    [SerializeField] private float mineAnimIntensity = 0.1f;

    // Estado interno
    private int materialCount = 0;
    private Rigidbody2D rb;
    private bool isMining = false; // Para evitar spammear la tecla Q

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        // Solo el dueño actualiza su UI inicial
        if (IsOwner)
        {
<<<<<<< HEAD
            Debug.LogError("ERROR: El objeto necesita un Rigidbody2D.");
            return;
        }
        
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        saludActual = saludMaxima;
        materialesActuales = 0;
        
        ActualizarContadorGlobalUI(); 
        ActualizarSaludUI(); 
        
        if (prefabCapsulaMuerte == null)
        {
             Debug.LogWarning("Falta asignar el Prefab Capsula Muerte en el Inspector.");
=======
            UpdateCounterUI();
>>>>>>> b5f36093d7ce31adb5cd7c0abb9abd3150ea9119
        }
    }

    // Usamos OnTriggerStay para detectar si estamos sobre el mineral y presionamos Q
    private void OnTriggerStay2D(Collider2D other)
    {
<<<<<<< HEAD
        if (estaParalizado)
        {
            return; 
        }

        if (!rb.isKinematic) 
        {
            if (centro != null)
            {
                Vector2 direccionHaciaCentro = ((Vector2)centro.position - (Vector2)transform.position).normalized;
                rb.AddForce(direccionHaciaCentro * fuerzaDeAtraccion);
            }

            if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
=======
        // 1. Regla: Solo el dueño puede iniciar la acción
        if (!IsOwner) return;

        // 2. Si ya estamos minando, no hacemos nada
        if (isMining) return;

        // 3. Verificamos el Tag
        if (other.CompareTag(materialTag))
        {
            // Intentamos obtener el script del mineral (asumiendo que existe según tu código anterior)
            RecursoMineral resource = other.GetComponent<RecursoMineral>();

            // 4. Input 'Q' y validación del recurso
            if (Input.GetKey(KeyCode.Q) && resource != null && !resource.EstaRecargando())
>>>>>>> b5f36093d7ce31adb5cd7c0abb9abd3150ea9119
            {
                // Iniciamos la secuencia de minado
                StartCoroutine(MiningSequence(resource));
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && !rb.isKinematic)
        {
            if (!AtacarEnRango())
            {
                Vector3 posicionRadialInicial = transform.position - centro.position;
                StartCoroutine(AnimarGolpeRecoleccion(posicionRadialInicial));
            }
        }
    }

    private IEnumerator MiningSequence(RecursoMineral resource)
    {
<<<<<<< HEAD
        if (!rb.isKinematic && !estaParalizado)
        {
            float inputHorizontal = Input.GetAxis("Horizontal"); 
            if (inputHorizontal != 0 && centro != null)
            {
                Vector3 puntoPivot = centro.position; 
                float anguloARotar = inputHorizontal * velocidadDeRotacion * Time.deltaTime;
                transform.RotateAround(puntoPivot, ejeDeRotacion, anguloARotar);
            }
        }
        
        if (centro != null)
        {
            Vector2 direccionDesdeCentro = (transform.position - centro.position).normalized;
            float angulo = Mathf.Atan2(direccionDesdeCentro.y, direccionDesdeCentro.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
        }
    }
    
    // --- Detección de Recurso Mineral ---
    void OnTriggerStay2D(Collider2D other)
    {
        if (centro == null || rb.isKinematic || estaParalizado) return;
        
        // La lógica de RecursoMineral aquí asume que existe ese script
    }
    
    // --- LÓGICA DE ATAQUE (CON RANGO Y DIRECCIÓN) ---
    bool AtacarEnRango()
    {
        Vector2 puntoCentralAtaque = (Vector2)transform.position + (Vector2)transform.up * (rangoAtaque / 2f);
        Vector2 tamanoAtaque = new Vector2(anchoAtaque, rangoAtaque); 
        float anguloRotacion = transform.eulerAngles.z; 

        Collider2D[] objetosGolpeados = Physics2D.OverlapBoxAll(
            puntoCentralAtaque, 
            tamanoAtaque, 
            anguloRotacion 
        );

        bool danoInfligido = false;

        foreach (Collider2D col in objetosGolpeados)
        {
            if (col.gameObject == gameObject) continue; 
            
            if (col.CompareTag(tagJugador))
            {
                ControladorOrbital enemigo = col.GetComponent<ControladorOrbital>();
                
                if (enemigo != null)
                {
                    Vector3 posicionRadialInicial = transform.position - centro.position;
                    StartCoroutine(AnimarGolpeRecoleccion(posicionRadialInicial));
                    
                    enemigo.RecibirDano(danoAtaque);
                    
                    danoInfligido = true;
                    return danoInfligido;
                }
            }
        }
        return danoInfligido;
    }

    private IEnumerator AnimarGolpeRecoleccion(Vector3 posicionRadialInicial)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.isKinematic = true; 
        
        transform.position = centro.position + posicionRadialInicial; 
        
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionAnimacionGolpe)
        {
            float tiempoNormalizado = tiempoTranscurrido / duracionAnimacionGolpe;
            float offset = Mathf.Sin(tiempoNormalizado * Mathf.PI) * intensidadAnimacionGolpe; 
            
            Vector3 offsetVector = transform.up * offset; 
            transform.position = centro.position + posicionRadialInicial + offsetVector;
=======
        isMining = true;

        // A. Guardamos estado físico previo
        // Al ponerlo Kinematic, el script de Movimiento (PlanetaryMovement) dejará de afectarlo
        // porque el Rigidbody ignorará las fuerzas.
        bool wasKinematic = rb.isKinematic;
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero; // Detener al jugador
>>>>>>> b5f36093d7ce31adb5cd7c0abb9abd3150ea9119

        // B. Calcular posición base relativa al centro (0,0) para la animación
        // Asumimos que el centro del mundo es Vector3.zero
        Vector3 center = Vector3.zero;
        Vector3 initialRadialPos = transform.position - center;

        // C. Animación de oscilación (Golpe)
        float elapsedTime = 0f;
        while (elapsedTime < mineAnimDuration)
        {
            float normalizedTime = elapsedTime / mineAnimDuration;

            // Movimiento de entrada y salida (Sinusoide)
            float offset = Mathf.Sin(normalizedTime * Mathf.PI) * mineAnimIntensity;

            // Calculamos dirección hacia arriba (radial hacia afuera)
            Vector3 offsetVector = transform.up * offset;

            // Aplicamos posición forzada
            transform.position = center + initialRadialPos + offsetVector;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

<<<<<<< HEAD
        transform.position = centro.position + posicionRadialInicial;
        rb.isKinematic = false; 
    }

    // --- LÓGICA DE SALUD, ESTADOS Y MUERTE (PÚBLICAS) ---
    public void RecibirDano(int cantidadDano)
    {
        if (saludActual <= 0) return;

        saludActual -= cantidadDano;

        if (prefabImpactoFX != null)
        {
            Instantiate(prefabImpactoFX, transform.position, Quaternion.identity);
        }
        
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
                // Este acceso ya es válido
                capsulaScript.materialesEnCapsula = materialesActuales; 
            }
        }
        
        materialesActuales = 0;
        Destroy(gameObject); 
    }

    // --- LÓGICA DE INVENTARIO Y MATERIALES (PÚBLICAS) ---

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

    public int DepositarMateriales()
    {
        int depositado = materialesActuales;
        materialesActuales = 0;
        ActualizarContadorGlobalUI();
        return depositado;
    }

    // --- Métodos Auxiliares y Gizmos ---

    private void Saltar()
    {
        Vector2 direccionDeSalto = transform.up; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(direccionDeSalto * fuerzaDeSalto, ForceMode2D.Impulse);
        estaEnSuelo = false; 
=======
        // D. Restaurar posición exacta
        transform.position = center + initialRadialPos;

        // E. Restaurar físicas
        rb.isKinematic = wasKinematic;

        // F. Notificar al recurso (Lógica externa)
        // Nota: resource.IniciarPicado(this) debería manejar la lógica de "reducir vida" del mineral
        resource.IniciarPicado(this);

        // G. Si el recurso se rompió (opcional, depende de tu lógica en RecursoMineral),
        // sumamos puntos. Aquí asumo que RecursoMineral te llamará de vuelta a 'RecolectarCompletado'
        // o si es instantáneo, lo sumamos aquí.

        // Pequeña pausa para no spammear
        yield return new WaitForSeconds(0.1f);

        isMining = false;
    }

    // Este método es llamado por el script externo 'RecursoMineral' cuando se completa la extracción
    public void RecolectarCompletado()
    {
        materialCount++;
        UpdateCounterUI();
>>>>>>> b5f36093d7ce31adb5cd7c0abb9abd3150ea9119
    }

    private void UpdateCounterUI()
    {
        if (counterUIText != null && IsOwner)
        {
            counterUIText.text = "Material: " + materialCount.ToString();
        }
    }
    
    private void ActualizarSaludUI()
    {
        if (saludUIText != null)
        {
            saludUIText.text = "HP: " + saludActual.ToString() + " / " + saludMaxima.ToString();
        }
    }
    
    public void ActualizarContadorGlobalUI()
    {
         if (contadorGlobalUIText != null)
         {
             contadorGlobalUIText.text = "Material: " + materialesActuales.ToString() + " / " + capacidadMaxima.ToString();
         }
    }
    
    void OnDrawGizmosSelected()
    {
        if (centro == null || transform.hasChanged == false) return;
        
        Gizmos.color = Color.red;
        
        Vector3 puntoCentralAtaque = transform.position + transform.up * (rangoAtaque / 2f);
        Vector3 tamanoAtaque = new Vector3(anchoAtaque, rangoAtaque, 0); 
        
        Matrix4x4 currentRotation = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(puntoCentralAtaque, transform.rotation, Vector3.one);
        
        Gizmos.DrawWireCube(Vector3.zero, tamanoAtaque);
        
        Gizmos.matrix = currentRotation; 
    }
}