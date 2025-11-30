using UnityEngine;

public class CuboMaterialPerdido : MonoBehaviour
{
    // Este valor se asigna desde ControladorDios.
    [HideInInspector]
    public int valorMaterial = 0; 
    
    private string tagJugador = "Jugador"; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo reacciona si un Jugador lo toca
        if (other.CompareTag(tagJugador))
        {
            // Asume que el ControladorOrbital tiene la lógica de mochila y recogida.
            ControladorOrbital jugador = other.GetComponent<ControladorOrbital>();

            if (jugador != null)
            {
                // La función RecogerMaterial debe existir en ControladorOrbital
                // y manejar si el material cabe o no.
                bool recogido = jugador.RecogerMaterial(valorMaterial); 

                if (recogido)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}