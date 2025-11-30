using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement; // Necesario para detectar cambio de escena
using Unity.Cinemachine; // Unity 6 (Usa 'Cinemachine' si es versión anterior)

public class NetworkCameraSetup : NetworkBehaviour
{
    [Header("Camera Configuration")]
    [Tooltip("The tag used to identify the Virtual Camera in any scene.")]
    [SerializeField] private string cameraTag = "PlayerCamera";

    // 1. Se ejecuta al nacer el objeto
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Configurar cámara inicial (si ya estamos en la escena correcta)
        FindAndSetupCamera();

        // 2. SUSCRIPCIÓN: Escuchar cambios de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 3. Limpieza: Muy importante desuscribirse al destruir el objeto
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // 4. Este método se dispara automáticamente al cargar nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Pequeño log para debug
        Debug.Log($"[CameraSetup] Scene Loaded: {scene.name}. Searching for camera...");
        FindAndSetupCamera();
    }

    private void FindAndSetupCamera()
    {
        // Buscar la cámara por Tag en la NUEVA escena
        GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);

        if (cameraObj != null)
        {
            // Unity 6 / Cinemachine 3.0
            var cmCamera = cameraObj.GetComponent<CinemachineCamera>();

            // Si usas Cinemachine 2.x (clásico), descomenta esto y comenta el de arriba:
            // var cmCamera = cameraObj.GetComponent<CinemachineVirtualCamera>();

            if (cmCamera != null)
            {
                cmCamera.Follow = transform;

                // Opcional: Si es un juego planetario y quieres que rote:
                // cmCamera.LookAt = transform; 

                Debug.Log($"[CameraSetup] Success! Linked Player {OwnerClientId} to Camera.");
            }
            else
            {
                Debug.LogError($"[CameraSetup] Object tagged '{cameraTag}' found, but missing Cinemachine component.");
            }
        }
        else
        {
            // Es normal que no encuentre cámara en el Lobby, así que usamos LogWarning
            Debug.LogWarning("[CameraSetup] No 'PlayerCamera' found in this scene.");
        }
    }
}