using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class RecursoMineral : NetworkBehaviour
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

    // --- NETCODE VARIABLES ---

    // Sincronizamos si está recargando automáticamente
    private NetworkVariable<bool> isRecharging = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Variable local para evitar que dos jugadores piquen al mismo tiempo la misma roca
    private bool isBeingMinedLocal = false;
    private SpriteRenderer spriteRenderer;
    private Vector3 posicionInicial;

    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        posicionInicial = transform.position;

        // Suscribirse a cambios de estado (si entras tarde a la partida, esto actualiza la visual)
        isRecharging.OnValueChanged += OnRechargingChanged;

        // Inicialización visual según el estado actual del servidor
        UpdateVisibility(isRecharging.Value);
        ActualizarTimerUI(false, "");
    }

    public override void OnNetworkDespawn()
    {
        isRecharging.OnValueChanged -= OnRechargingChanged;
    }

    // Método público que llama el PlayerMiningController
    public bool EstaRecargando()
    {
        return isRecharging.Value; // Leemos el valor de red
    }

    // --- ENTRY POINT ---

    public void IniciarPicado(ControladorOrbital jugador)
    {
        // Validación local rápida
        if (isBeingMinedLocal || isRecharging.Value) return;

        // Pedimos al servidor iniciar el proceso
        // Pasamos el ID del cliente que lo pidió para saber a quién darle el premio
        RequestMiningServerRpc(jugador.OwnerClientId);
    }

    // --- SERVER LOGIC ---

    [ServerRpc(RequireOwnership = false)] // Cualquiera puede pedir picar, no solo el dueño de la roca
    private void RequestMiningServerRpc(ulong requestorClientId)
    {
        // El servidor valida de nuevo (autoridad)
        if (isBeingMinedLocal || isRecharging.Value) return;

        // Inicia el proceso lógico en el servidor
        StartCoroutine(ServerMiningProcess(requestorClientId));
    }

    private IEnumerator ServerMiningProcess(ulong requestorClientId)
    {
        isBeingMinedLocal = true;

        // 1. Avisar a TODOS los clientes que muestren el timer visual
        TriggerVisualTimerClientRpc();

        // Esperar el tiempo de recolección en el servidor
        yield return new WaitForSeconds(tiempoRecoleccion);

        // 2. Calcular resultado
        // Nota: Podríamos usar una NetworkVariable para 'golpesRestantes', 
        // pero como es lógica interna, lo manejamos localmente en el server y reseteamos al recargar.
        // Para simplificar, asumiremos que cada ciclo exitoso resta vida.

        // 3. Dar premio SOLO al jugador que picó
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestorClientId } }
        };
        GiveRewardClientRpc(clientRpcParams);

        // 4. Efecto de golpe visual para todos
        TriggerShakeClientRpc();

        // 5. Lógica de vida (Simplificada: usaremos una variable local del servidor para contar)
        // Si quisiéramos persistencia estricta, usaríamos otra NetworkVariable<int>.
        // Aquí simularemos la reducción de vida:
        // (Nota: Necesitaríamos una variable 'golpesActuales' en la clase, pero para este ejemplo
        //  simplemente comprobaremos si debemos recargar).

        // * Lógica simple: Si no usamos variable de vida, cada golpe cuenta. 
        // Si quieres sincronizar la vida exacta, añade 'private int serverCurrentHits = 3;' arriba.

        // Supongamos que restamos vida aquí (implementación básica):
        serverCurrentHits--;

        if (serverCurrentHits <= 0)
        {
            // Entrar en modo recarga
            isRecharging.Value = true; // Esto dispara OnValueChanged en todos los clientes
            StartCoroutine(ServerRechargeTimer());
        }

        isBeingMinedLocal = false;
    }

    // Variable auxiliar solo del servidor para trackear vida
    private int serverCurrentHits = 3;

    private IEnumerator ServerRechargeTimer()
    {
        // Esperar tiempo de recarga
        // Opcional: Podrías enviar un ClientRpc para mostrar el timer de recarga, 
        // o dejar que el cliente lo deduzca por el estado 'isRecharging'.

        // Para mostrar el timer de 20s en clientes, necesitamos un ClientRpc
        StartRechargeTimerClientRpc();

        yield return new WaitForSeconds(tiempoRecarga);

        // Restaurar
        serverCurrentHits = vidaTotal;
        isRecharging.Value = false; // El mineral reaparece en todos los clientes
    }

    // --- CLIENT LOGIC (Visuales) ---

    [ClientRpc]
    private void TriggerVisualTimerClientRpc()
    {
        // Todos los clientes ven el timer corriendo
        StartCoroutine(VisualCountdownRoutine());
    }

    [ClientRpc]
    private void StartRechargeTimerClientRpc()
    {
        // Todos los clientes ven el timer de recarga
        StartCoroutine(VisualRechargeRoutine());
    }

    [ClientRpc]
    private void TriggerShakeClientRpc()
    {
        StartCoroutine(AnimarAgitacion());
    }

    // Este RPC solo llega al cliente que picó
    [ClientRpc]
    private void GiveRewardClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // Buscamos al jugador local y le damos el premio
        // Como estamos en el cliente dueño, NetworkManager.Singleton.LocalClient.PlayerObject es el nuestro.
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (player != null)
        {
            var miningController = player.GetComponent<ControladorOrbital>();
            if (miningController != null)
            {
                miningController.RecolectarCompletado();
            }
        }
    }

    // Reacción al cambio de la variable de red isRecharging
    private void OnRechargingChanged(bool previous, bool current)
    {
        UpdateVisibility(current);
        if (!current)
        {
            // Si dejó de recargar, limpiamos UI
            ActualizarTimerUI(false, "");
        }
    }

    private void UpdateVisibility(bool isRecharging)
    {
        if (spriteRenderer == null) return;
        Color color = spriteRenderer.color;
        color.a = isRecharging ? opacidadInhabilitado : 1.0f;
        spriteRenderer.color = color;
    }

    // --- CORRUTINAS VISUALES (CLIENT SIDE) ---

    private IEnumerator VisualCountdownRoutine()
    {
        float elapsed = 0f;
        ActualizarTimerUI(true, "");

        while (elapsed < tiempoRecoleccion)
        {
            elapsed += Time.deltaTime;
            float remaining = tiempoRecoleccion - elapsed;
            if (contadorTimerTexto != null)
                contadorTimerTexto.text = remaining.ToString("F1");
            yield return null;
        }
        ActualizarTimerUI(false, "");
    }

    private IEnumerator VisualRechargeRoutine()
    {
        float elapsed = 0f;
        // Solo mostramos texto si quieres ver la cuenta regresiva de 20s
        ActualizarTimerUI(true, "");

        while (elapsed < tiempoRecarga)
        {
            elapsed += Time.deltaTime;
            float remaining = tiempoRecarga - elapsed;
            if (contadorTimerTexto != null)
                contadorTimerTexto.text = remaining.ToString("F0") + "s";
            yield return null;
        }
        ActualizarTimerUI(false, "");
    }

    private IEnumerator AnimarAgitacion()
    {
        float elapsed = 0f;
        while (elapsed < duracionAgitacion)
        {
            float x = Random.Range(-1f, 1f) * intensidadAgitacion;
            float y = Random.Range(-1f, 1f) * intensidadAgitacion;
            transform.position = posicionInicial + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = posicionInicial;
    }

    private void ActualizarTimerUI(bool mostrar, string texto)
    {
        if (contadorTimerTexto != null)
        {
            contadorTimerTexto.gameObject.SetActive(mostrar);
            if (mostrar) contadorTimerTexto.text = texto;
        }
    }
}