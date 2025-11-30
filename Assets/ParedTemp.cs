using UnityEngine;
using System.Collections;

public class ParedTemporal : MonoBehaviour
{
    [Header("Configuración de Animación")]
    public float duracionDesaparicion = 0.5f;
    public float distanciaCaida = 10f; // Distancia que cae hacia el centro

    [Header("Configuración de Daño")]
    public int danoCaida = 10;
    public float fuerzaDisparoLateral = 500f; // Fuerza para lanzar al jugador

    private Transform centroDelPlaneta; 
    private Vector3 posicionInicial;
    private bool estaCayendo = false;
    private Rigidbody2D rb; 

    void Start()
    {
        posicionInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null) rb.isKinematic = true; 
    }

    public void SetCentro(Transform centro)
    {
        centroDelPlaneta = centro;
    }

    public void IniciarDesaparicion()
    {
        if (centroDelPlaneta == null) {
            Destroy(gameObject);
            return;
        }

        estaCayendo = true; 
        if (rb != null) rb.isKinematic = false; 

        StopAllCoroutines(); 
        StartCoroutine(Desaparecer());
    }

    private IEnumerator Desaparecer()
    {
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionDesaparicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracionDesaparicion;
            
            // Animación de Caída: Mueve la pared RADIALMENTE HACIA ADENTRO.
            Vector3 destino = posicionInicial + transform.up * -distanciaCaida; 
            
            transform.position = Vector3.Lerp(posicionInicial, destino, progreso);

            yield return null;
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!estaCayendo) return; 

        if (collision.gameObject.CompareTag("Jugador"))
        {
            ControladorOrbital jugador = collision.gameObject.GetComponent<ControladorOrbital>();
            
            if (jugador != null)
            {
                jugador.RecibirDano(danoCaida);
                
                Rigidbody2D rbJugador = jugador.GetComponent<Rigidbody2D>();

                if (rbJugador != null)
                {
                    Vector2 direccionDisparo = transform.right;
                    if (Random.Range(0, 2) == 0) 
                    {
                        direccionDisparo *= -1;
                    }

                    rbJugador.AddForce(direccionDisparo * fuerzaDisparoLateral, ForceMode2D.Impulse);
                }
                
                estaCayendo = false; 
            }
        }
    }
}