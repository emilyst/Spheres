using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(MeshRenderer), typeof(TrailRenderer))]
public class BodyAppearance : MonoBehaviour
{
    [SerializeField] Color Color;
    [SerializeField] [Range(0, 10)] public float TrailSeconds = 2;
    [SerializeField] [Range(0, 10)] float GlowIntensity = 5;
    [SerializeField] [Range(0, 10)] float TrailGlowIntensity = 0.666F;

    int mainColorID = Shader.PropertyToID("_Color");
    int baseColorID = Shader.PropertyToID("_BaseColor");
    int emissionColorID = Shader.PropertyToID("_EmissionColor");

    MeshRenderer meshRenderer;
    TrailRenderer trailRenderer;
    float pauseTime;
    float resumeTime;

    void Start()
    {
        if (Color == default)
        {
            Color = Random.ColorHSV(0.0F, 0.9999F, 0.8F, 1.0F, 1.0F, 1.0F);
        }

        meshRenderer = GetComponent<MeshRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();

        SetGlowEffect(Color);
    }

    public void Pause()
    {
        CancelInvoke(nameof(ResumeTrailTime));

        pauseTime = Time.time;
        trailRenderer.time = Mathf.Infinity;
    }

    public void Resume()
    {
        resumeTime = Time.time;
        trailRenderer.time = (resumeTime - pauseTime) + TrailSeconds;

        Invoke(nameof(ResumeTrailTime), TrailSeconds);
    }

    void ResumeTrailTime()
    {
        trailRenderer.time = TrailSeconds;
    }

    void SetGlowEffect(Color color)
    {
        SetMaterialColor(Color);
        SetTrailMaterialColor(Color);
    }

    void SetMaterialColor(Color color)
    {
        if (meshRenderer == null)
        {
            return;
        }

        var material = meshRenderer.material;

        if (material == null)
        {
            return;
        }

        material.SetColor(mainColorID, color);
        material.SetColor(baseColorID, color * Mathf.Pow(2, GlowIntensity));
        material.SetColor(emissionColorID, color);
    }

    void SetTrailMaterialColor(Color color)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.time = TrailSeconds;

        var material = trailRenderer.material;

        if (material == null)
        {
            return;
        }

        material.SetColor(mainColorID, color);
        material.SetColor(baseColorID, color * Mathf.Pow(2, TrailGlowIntensity));
        material.SetColor(emissionColorID, color);

        var colorGradient = trailRenderer.colorGradient;

        colorGradient.SetKeys(
            new GradientColorKey[]
            {
                new(color * Mathf.Pow(2, TrailGlowIntensity), 0.0F),
                new(color * Mathf.Pow(2, TrailGlowIntensity), 1.0F),
            },
            new GradientAlphaKey[]
            {
                new(1.0F, 0.0F),
                new(float.Epsilon, 1.0F),
            }
        );
    }
}
