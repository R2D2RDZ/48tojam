using UnityEngine;
using UnityEngine.UI; 
using TMPro; // NECESARIO para el tipo TMP_Text

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
    public TMP_Text contadorUIText;              
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
        ActualizarContadorUI(); 
    }

    void Update()
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

    void LateUpdate()
    {
        // Lógica de Órbita
        float inputHorizontal = Input.GetAxis("Horizontal"); 

        if (inputHorizontal != 0 && centro != null)
        {
            Vector3 puntoPivot = centro.position; 
            float anguloARotar = inputHorizontal * velocidadDeRotacion * Time.deltaTime;
            transform.RotateAround(puntoPivot, ejeDeRotacion, anguloARotar);
        }

        // Alineación Radial (Fix de Salto)
        if (centro != null)
        {
            Vector2 direccionDesdeCentro = (transform.position - centro.position).normalized;
            float angulo = Mathf.Atan2(direccionDesdeCentro.y, direccionDesdeCentro.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
        }
    }
    
    // ===================================
    // 3. LÓGICA DE RECOLECCIÓN (MÉTODO REINCORPORADO)
    // ===================================
    void OnTriggerStay2D(Collider2D other)
    {
        // Verifica el Tag y el Input 'Q'
        if (other.CompareTag(tagDelMaterial))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                RecolectarMaterial(other.gameObject);
            }
        }
    }
    
    private void RecolectarMaterial(GameObject material)
    {
        contadorMaterial++;
        ActualizarContadorUI();
        Destroy(material);
    }
    
    private void ActualizarContadorUI()
    {
        if (contadorUIText != null)
        {
            contadorUIText.text = contadorMaterial.ToString();
        }
    }
    
    private void Saltar()
    {
        Vector2 direccionDeSalto = transform.up; 
        // CORRECCIÓN DE API: Usar rb.velocity
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