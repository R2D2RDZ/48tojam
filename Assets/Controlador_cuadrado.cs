using UnityEngine;

public class ControladorOrbital : MonoBehaviour
{
    
    public Transform centro; 
    
    
    public float velocidadDeRotacion = 100f; 
    
    
    public Vector3 ejeDeRotacion = Vector3.up; 

    void Update()
    {
  
        float inputHorizontal = Input.GetAxis("Horizontal"); 

        
        if (inputHorizontal != 0)
        {
            
            Vector3 puntoPivot = centro.position; 
            
            
            float anguloARotar = inputHorizontal * velocidadDeRotacion * Time.deltaTime;

            
            transform.RotateAround(
                puntoPivot,          
                ejeDeRotacion,       
                anguloARotar         
            );
        }
    }
}
