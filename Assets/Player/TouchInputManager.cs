using UnityEngine;

public class TouchInputManager : MonoBehaviour
{
    public static TouchInputManager Instance { get; private set; }

    // Propiedades públicas para leer el estado
    public float HorizontalInput { get; private set; }
    public bool JumpTriggered { get; private set; }

    private void Awake()
    {
        // Configuración Singleton simple
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        // Reseteamos el salto al final del frame para que actúe como "GetKeyDown" (un solo frame)
        // Esto evita que el jugador salte infinitamente si mantiene el botón presionado.
        JumpTriggered = false;
    }

    // --- MÉTODOS PARA VINCULAR A LOS BOTONES UI (Event Triggers) ---

    // Llamar en PointerDown del botón Izquierda
    public void OnLeftBtnDown() => HorizontalInput = -1f;

    // Llamar en PointerDown del botón Derecha
    public void OnRightBtnDown() => HorizontalInput = 1f;

    // Llamar en PointerUp de AMBOS botones de movimiento
    public void OnMovementBtnUp() => HorizontalInput = 0f;

    // Llamar en PointerDown del botón Salto
    public void OnJumpBtnDown() => JumpTriggered = true;
}