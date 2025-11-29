using UnityEngine;

public class GravitacionalCentral : MonoBehaviour
{
    // Magnitud de la fuerza que atraerá a los objetos.
    [Header("Configuración de Atracción")]
    public float fuerzaDeAtraccion = 10f; 

    // Radio dentro del cual se atraerán los objetos (usar un valor grande para asegurar la atracción).
    public float radioDeAtraccion = 20f; 

    // Usamos FixedUpdate para aplicar fuerzas de forma consistente con el motor de física.
    void FixedUpdate()
    {
        // 1. Encontrar todos los Rigidbody2D en el radio de atracción.
        // Asumiendo que el cubo está en una capa estándar.
        Collider2D[] objetosCercanos = Physics2D.OverlapCircleAll(transform.position, radioDeAtraccion);

        // 2. Iterar sobre todos los objetos encontrados.
        foreach (Collider2D colisionador in objetosCercanos)
        {
            Rigidbody2D rbObjetivo = colisionador.GetComponent<Rigidbody2D>();

            // Solo atraer si tiene un Rigidbody2D y no es el centro mismo
            if (rbObjetivo != null && rbObjetivo.gameObject != gameObject)
            {
                // Calcular el vector de dirección desde el objeto hacia el centro (el círculo)
                Vector2 direccionHaciaCentro = ((Vector2)transform.position - rbObjetivo.position).normalized;
                
                // Aplicar la fuerza de atracción
                rbObjetivo.AddForce(direccionHaciaCentro * fuerzaDeAtraccion);
            }
        }
    }

    // Opcional: Dibuja el radio de atracción en el editor para visualización.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeAtraccion);
    }
}