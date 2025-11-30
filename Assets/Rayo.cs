using UnityEngine;

public class DestructorAutomatico : MonoBehaviour
{
    public float tiempoDeVida = 0.5f; // ⬅️ DEBE TENER UN VALOR POSITIVO
    
    void Start()
    {
        // Llama a Destroy después del tiempoDeVida
        Destroy(gameObject, tiempoDeVida);
    }
}