using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;

// Este script ahora solo se encarga de la MINERÍA e INTERACCIÓN.
// El movimiento lo maneja 'PlanetaryMovementController'.
public class ControladorOrbital : NetworkBehaviour
{
    [Header("Collection Settings")]
    [Tooltip("Tag required for the collectable objects.")]
    [SerializeField] private string materialTag = "Mineral 1";

    [Tooltip("UI Text component to display score.")]
    [SerializeField] private TMP_Text counterUIText;

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
            UpdateCounterUI();
        }
    }

    // Usamos OnTriggerStay para detectar si estamos sobre el mineral y presionamos Q
    private void OnTriggerStay2D(Collider2D other)
    {
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
            {
                // Iniciamos la secuencia de minado
                StartCoroutine(MiningSequence(resource));
            }
        }
    }

    private IEnumerator MiningSequence(RecursoMineral resource)
    {
        isMining = true;

        // A. Guardamos estado físico previo
        // Al ponerlo Kinematic, el script de Movimiento (PlanetaryMovement) dejará de afectarlo
        // porque el Rigidbody ignorará las fuerzas.
        bool wasKinematic = rb.isKinematic;
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero; // Detener al jugador

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
    }

    private void UpdateCounterUI()
    {
        if (counterUIText != null && IsOwner)
        {
            counterUIText.text = "Material: " + materialCount.ToString();
        }
    }
}