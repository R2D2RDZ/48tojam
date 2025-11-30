using UnityEngine;
using TMPro;
using System.Collections; 
using UnityEngine.UI;

public class ControladorOrbital : MonoBehaviour
{
    // --- Variables de Órbita ---
    [Header("Configuración de Órbita")]
    public Transform centro;              
    public float velocidadDeRotacion = 100f; 
    public Vector3 ejeDeRotacion = Vector3.forward; 

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

    void Start()
    {
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

    void LateUpdate()
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
            Vector2 direccionDesdeCentro = (transform.position - centro.position).normalized;
            float angulo = Mathf.Atan2(direccionDesdeCentro.y, direccionDesdeCentro.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
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
}