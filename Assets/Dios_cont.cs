using UnityEngine;
using System.Collections;

public enum Habilidad { Pared, Rayo }

public class ControladorDios : MonoBehaviour
{
    // --- Configuración de Movimiento ---
    [Header("Configuración de Movimiento")]
    public float velocidadDeRotacionDios = 150f; 
    public Vector3 ejeDeRotacion = Vector3.forward; 

    // --- Cooldown y Selección de Habilidad ---
    [Header("Habilidades")]
    public Habilidad habilidadActual = Habilidad.Pared;
    public float tiempoEntreHabilidades = 3.0f;
    private float tiempoSiguienteHabilidad;

    // --- Habilidad 1: Pared ---
    [Header("Habilidad 1: Pared")]
    public GameObject prefabPared;           
    public float distanciaMaxima = 8.0f;     
    private ParedTemporal paredAnterior; 

    // --- Habilidad 2: Rayo ---
    [Header("Habilidad 2: Rayo")]
    public int danoRayo = 30;               
    public float rangoRayo = 1.0f;          
    public GameObject prefabRayoFigura;      
    
    [Header("Materiales Perdidos")]
    public GameObject prefabMaterialPerdido; // Prefab con el script CuboMaterialPerdido
    
    [Header("Referencias")]
    public Transform centro;                 
    public string tagObjetivo = "Jugador"; 

    void Start()
    {
        tiempoSiguienteHabilidad = Time.time;
        
        if (centro == null || prefabPared == null || prefabRayoFigura == null || prefabMaterialPerdido == null)
        {
            Debug.LogError("El ControladorDios necesita referencias asignadas.");
        }
    }

    void Update()
    {
        MoverOrbitalmente();

        if (Input.GetKeyDown(KeyCode.Alpha1)) { habilidadActual = Habilidad.Pared; Debug.Log("Habilidad seleccionada: Pared"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { habilidadActual = Habilidad.Rayo; Debug.Log("Habilidad seleccionada: Rayo"); }

        if (Input.GetMouseButtonDown(0))
        {
            ActivarHabilidadSeleccionada();
        }
    }

    void ActivarHabilidadSeleccionada()
    {
        if (Time.time < tiempoSiguienteHabilidad)
        {
            return;
        }
        
        Vector3 posicionMundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        posicionMundo.z = 0; 

        if (habilidadActual == Habilidad.Pared)
        {
            ColocarPared(posicionMundo);
        }
        else if (habilidadActual == Habilidad.Rayo)
        {
            InvocarRayo(posicionMundo);
        }
        
        tiempoSiguienteHabilidad = Time.time + tiempoEntreHabilidades;
    }

    void MoverOrbitalmente()
    {
        float inputHorizontal = Input.GetAxis("Horizontal"); 
        
        if (inputHorizontal != 0 && centro != null)
        {
            Vector3 puntoPivot = centro.position; 
            float anguloARotar = inputHorizontal * velocidadDeRotacionDios * Time.deltaTime;
            transform.RotateAround(puntoPivot, ejeDeRotacion, anguloARotar);
        }

        if (centro != null)
        {
            Vector2 direccionDesdeCentro = (transform.position - centro.position).normalized;
            float angulo = Mathf.Atan2(direccionDesdeCentro.y, direccionDesdeCentro.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
        }
    }

    // --- HABILIDAD 1: COLOCAR PARED ---
    void ColocarPared(Vector3 posicion)
    {
        if (paredAnterior != null)
        {
            // Asumiendo que ParedTemporal tiene este método
            // paredAnterior.IniciarDesaparicion(); 
        }
        
        Vector2 direccionDesdeCentro = (posicion - centro.position).normalized;
        float angulo = Mathf.Atan2(direccionDesdeCentro.y, direccionDesdeCentro.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotacionPared = Quaternion.Euler(new Vector3(0, 0, angulo));

        GameObject nuevaParedObj = Instantiate(prefabPared, posicion, rotacionPared);
        // paredAnterior = nuevaParedObj.GetComponent<ParedTemporal>();
        
        // if (paredAnterior != null)
        // {
        //      paredAnterior.SetCentro(centro); 
        // }
    }

    // --- HABILIDAD 2: INVOCAR RAYO ---
    void InvocarRayo(Vector3 posicion)
    {
        if (prefabRayoFigura != null)
        {
            Vector2 direccionHaciaCentro = (centro.position - posicion).normalized;
            float angulo = Mathf.Atan2(direccionHaciaCentro.y, direccionHaciaCentro.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotacionRayo = Quaternion.Euler(new Vector3(0, 0, angulo));
            Instantiate(prefabRayoFigura, posicion, rotacionRayo);
        }
        
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(posicion, rangoRayo);

        foreach (Collider2D col in objetosGolpeados)
        {
            if (col.CompareTag(tagObjetivo))
            {
                ControladorOrbital jugador = col.GetComponent<ControladorOrbital>();
                
                if (jugador != null)
                {
                    jugador.RecibirDano(danoRayo);
                    jugador.Paralizar(1.0f); 
                    LanzarMaterialesPerdidos(jugador, posicion);
                }
            }
        }
    }
    
    // Función: Gestión de Materiales Perdidos
    void LanzarMaterialesPerdidos(ControladorOrbital jugador, Vector3 posicionRayo)
    {
        // ✅ ACCESO CORREGIDO: ahora podemos leer materialesActuales
        int materialesPerdidos = jugador.materialesActuales / 2;
        
        if (materialesPerdidos > 0)
        {
            jugador.RestarMateriales(materialesPerdidos);

            if (prefabMaterialPerdido != null)
            {
                int cubosRestantes = materialesPerdidos;
                
                while (cubosRestantes > 0)
                {
                    GameObject cubo = Instantiate(prefabMaterialPerdido, posicionRayo, Quaternion.identity);
                    CuboMaterialPerdido materialScript = cubo.GetComponent<CuboMaterialPerdido>();
                    
                    if (materialScript != null)
                    {
                        int valor = Mathf.Min(cubosRestantes, 5);
                        materialScript.valorMaterial = valor;
                        cubosRestantes -= valor;
                    }

                    Rigidbody2D rbCubo = cubo.GetComponent<Rigidbody2D>();
                    if (rbCubo != null)
                    {
                        Vector2 fuerza = Random.insideUnitCircle.normalized * 50f;
                        rbCubo.AddForce(fuerza, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}