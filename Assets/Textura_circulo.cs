using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArcTextureRenderer : MonoBehaviour
{
    [Header("Arc Geometry Settings")]
    [Tooltip("Angle in degrees where the arc begins (e.g., 0).")]
    [SerializeField] private float startAngle = 0f;

    [Tooltip("Angle in degrees where the arc ends (e.g., 180).")]
    [SerializeField] private float endAngle = 180f;

    [Tooltip("Number of segments between start and end. Higher values = smoother curve.")]
    [SerializeField] private int segments = 60;

    [Tooltip("Radius of the arc in local units.")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Width of the line renderer.")]
    [SerializeField] private float lineWidth = 1f;

    [Header("Texture Adjustment")]
    [Tooltip("How many times the texture repeats along the LENGTH of the arc.")]
    [SerializeField] private float textureTilingX = 1f;

    [Tooltip("Controls the vertical scale. Increase this if the texture looks stretched vertically on thick lines.")]
    [SerializeField] private float textureTilingY = 1f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        InitializeLineRenderer();
    }

    private void Start()
    {
        DrawArc();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        DrawArc();
    }

    private void InitializeLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;

        // Ensure texture repeats instead of clamping
        lineRenderer.textureMode = LineTextureMode.Tile;
    }

    private void DrawArc()
    {
        if (lineRenderer == null) return;

        // 1. Update Width
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // 2. Update Texture Tiling (X = Length, Y = Height/Thickness)
        // NOTE: Modifying sharedMaterial in Editor affects the asset. 
        // For runtime instances, you might want to use .material instead.
        Material targetMat = Application.isPlaying ? lineRenderer.material : lineRenderer.sharedMaterial;

        if (targetMat != null)
        {
            targetMat.mainTextureScale = new Vector2(textureTilingX, textureTilingY);
        }

        // 3. Calculate Geometry
        int pointCount = segments + 1;
        lineRenderer.positionCount = pointCount;

        float currentAngle = startAngle;
        float angleDifference = endAngle - startAngle;
        float angleStep = angleDifference / segments;

        for (int i = 0; i < pointCount; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));

            currentAngle += angleStep;
        }
    }
}