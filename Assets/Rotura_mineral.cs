using UnityEngine;
using TMPro; 
using System.Collections;

public class RecursoMineral : MonoBehaviour
{
    [Header("Configuración de Vida y Tiempos")]
    public int vidaTotal = 3; 
    public float tiempoRecoleccion = 4.0f;
    public float tiempoRecarga = 20.0f;   
    
    [Header("Referencias UI")]
    public TMP_Text contadorTimerTexto; 
    
    [Header("Configuración Visual")]
    public float opacidadInhabilitado = 0.3f; 
    public float intensidadAgitacion = 0.1f;
    public float duracionAgitacion = 0.2f;   
    
    private int golpesRestantes;
    private Vector3 posicionInicial;
    private bool estaRecargando = false; 
    private bool estaSiendoPicado = false; 
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("El mineral necesita un SpriteRenderer para la animación de translucidez.");
        }
        
        posicionInicial = transform.position;
        golpesRestantes = vidaTotal;
        ActualizarTimerUI(false, ""); 
        EstablecerVisibilidad(true);
    }
    
    public bool EstaRecargando()
    {
        return estaRecargando;
    }

    public void IniciarPicado(ControladorOrbital jugador) 
    {
        if (estaSiendoPicado || estaRecargando) return; 
        StartCoroutine(ProcesoDePicado(jugador));
    }
    
    // Corutina: Gestiona el contador regresivo de 4 segundos y la lógica de golpes
    private IEnumerator ProcesoDePicado(ControladorOrbital jugador)
    {
        estaSiendoPicado = true;
        float tiempoTranscurrido = 0f;
        
        ActualizarTimerUI(true, ""); 
        
        while (tiempoTranscurrido < tiempoRecoleccion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float tiempoRestante = tiempoRecoleccion - tiempoTranscurrido;

            if (contadorTimerTexto != null)
            {
                contadorTimerTexto.text = tiempoRestante.ToString("F1");
            }
            
            yield return null;
        }

        // --- FIN DEL TIMER DE 4 SEGUNDOS ---
        
        golpesRestantes--; 
        
        // **********************************************
        // 📢 ¡CAMBIO CLAVE AQUÍ!
        // El material se recoge inmediatamente después de cada picado de 4s.
        jugador.RecolectarCompletado();
        // **********************************************

        StartCoroutine(AnimarAgitacion()); 

        // 1. Si la vida llega a cero, entra en el ciclo de recarga
        if (golpesRestantes <= 0)
        {
            // NOTA: La notificación al jugador ya se hizo arriba.
            StartCoroutine(ProcesoDeRecarga());
        }
        else
        {
            // Si le quedan golpes, simplemente oculta el timer
            ActualizarTimerUI(false, "");
        }
        
        estaSiendoPicado = false; 
    }
    
    // Corutina: Gestiona el estado de inhabilitación de 20 segundos
    private IEnumerator ProcesoDeRecarga()
    {
        estaRecargando = true;
        EstablecerVisibilidad(false); 

        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoRecarga)
        {
            tiempoTranscurrido += Time.deltaTime;
            float tiempoRestante = tiempoRecarga - tiempoTranscurrido;

            if (contadorTimerTexto != null)
            {
                contadorTimerTexto.text = tiempoRestante.ToString("F0") + "s";
            }
            
            yield return null;
        }

        // Volver a la normalidad
        golpesRestantes = vidaTotal;
        EstablecerVisibilidad(true); 
        estaRecargando = false;
        ActualizarTimerUI(false, "");
    }
    
    private IEnumerator AnimarAgitacion()
    {
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionAgitacion)
        {
            float offsetX = Random.Range(-1f, 1f) * intensidadAgitacion;
            float offsetY = Random.Range(-1f, 1f) * intensidadAgitacion;
            
            transform.position = posicionInicial + new Vector3(offsetX, offsetY, 0);
            
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.position = posicionInicial;
    }
    
    private void EstablecerVisibilidad(bool esVisible)
    {
        if (spriteRenderer == null) return;
        
        Color colorActual = spriteRenderer.color;
        colorActual.a = esVisible ? 1.0f : opacidadInhabilitado;
        spriteRenderer.color = colorActual;
    }
    
    private void ActualizarTimerUI(bool mostrar, string texto)
    {
        if (contadorTimerTexto != null)
        {
            contadorTimerTexto.gameObject.SetActive(mostrar);
            if (mostrar)
            {
                contadorTimerTexto.text = texto;
            }
        }
    }
}