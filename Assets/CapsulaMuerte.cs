using UnityEngine;

// ✅ CORRECCIÓN: Agregado : MonoBehaviour
public class CapsulaMuerte : MonoBehaviour 
{
    // Esta variable es pública para que el jugador pueda asignarle el material al morir.
    [Header("Materiales en el Contenedor")]
    public int materialesEnCapsula = 0;
    
    [Header("Configuración de Tiempo")]
    public float tiempoVidaCapsula = 60f; 

    private string tagJugador = "Jugador";

    void Start()
    {
        // Se autodestruye después de un tiempo si nadie la recoge
        if (tiempoVidaCapsula > 0)
        {
            Destroy(gameObject, tiempoVidaCapsula);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // El jugador pasa por encima de la cápsula
        if (materialesEnCapsula > 0 && other.CompareTag(tagJugador))
        {
            // Intentamos obtener el script del jugador
            ControladorOrbital jugador = other.GetComponent<ControladorOrbital>();

            if (jugador != null)
            {
                int espacioDisponible = jugador.capacidadMaxima - jugador.materialesActuales;
                int cantidadATransferir = Mathf.Min(materialesEnCapsula, espacioDisponible);

                if (cantidadATransferir > 0)
                {
                    // Transferir materiales al jugador
                    jugador.materialesActuales += cantidadATransferir;
                    jugador.ActualizarContadorGlobalUI();
                    
                    // Restar materiales de la cápsula
                    materialesEnCapsula -= cantidadATransferir;
                    
                    // Si la cápsula queda vacía, se destruye inmediatamente
                    if (materialesEnCapsula <= 0)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}