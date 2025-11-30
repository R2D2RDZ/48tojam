using Unity.Netcode.Components;
using UnityEngine;

// Permite al cliente sincronizar sus propios parámetros de animación
[DisallowMultipleComponent]
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Esto es la clave: El servidor NO manda, manda el dueño.
    }
}