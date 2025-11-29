using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircularTextureRenderer : MonoBehaviour
{
    [Header("Circle Settings")]
    [Tooltip("Number of segments to make the circle smooth. Higher is smoother.")]
    [SerializeField] private int segments = 60;

    [Tooltip("Radius of the circle in world units.")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Width of the line renderer.")]
    [SerializeField] private float lineWidth = 1f;

    [Header("Texture Settings")]
    [Tooltip("How many times the texture repeats around the circle.")]
    [SerializeField] private float textureTilingMultiplier = 1f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        InitializeLineRenderer();
    }

    private void Start()
    {
        DrawCircle();
    }

    // Permite ver los cambios en el editor en tiempo real
    private void OnValidate()
    {
        if (lineRenderer == null) 
            lineRenderer = GetComponent<LineRenderer>();
            
        DrawCircle();
    }

    private void InitializeLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // Regla 3 & 4: Configuración inicial vía código para evitar errores manuales
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        // CRITICAL: This allows the texture to repeat instead of stretch
        lineRenderer.textureMode = LineTextureMode.Tile; 
    }

    private void DrawCircle()
    {
        if (lineRenderer == null) return;

        // Apply width settings
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Apply material tiling logic
        // We update the material's texture scale to control repetition
        if (lineRenderer.sharedMaterial != null)
        {
            lineRenderer.sharedMaterial.mainTextureScale = new Vector2(textureTilingMultiplier, 1f);
        }

        // Generate geometry
        lineRenderer.positionCount = segments;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius; // Using Z for horizontal circle (XZ plane)

            // If you need a vertical circle (XY plane), swap z with y:
            // lineRenderer.SetPosition(i, new Vector3(x, z, 0f));
            
            lineRenderer.SetPosition(i, new Vector3(x, z, 0));
        }
    }
}