using UnityEngine;

public class TeamSpawnPoint : MonoBehaviour
{
    [Tooltip("0 for Team A, 1 for Team B")]
    public int teamID;

    private void OnDrawGizmos()
    {
        // Dibujo visual para ver los puntos en el editor
        if (teamID == 0)
        {
            Gizmos.color = Color.green;
        }
        else if (teamID == 1) 
        {
            Gizmos.color = Color.red;
        }
        else
        {
             Gizmos.color= Color.blue;
        }
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}