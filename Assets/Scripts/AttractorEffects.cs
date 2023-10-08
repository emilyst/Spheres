using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(MeshRenderer), typeof(TrailRenderer))]
public class AttractorEffects : MonoBehaviour
{   [SerializeField] private Color attractorColor;
    [SerializeField] [Range(0, 1000)] public float trailSeconds = 1000;
    [SerializeField] [Range(0, 10)] private float glowIntensity = 3;
    [SerializeField] [Range(0, 10)] private float trailGlowIntensity = 0.033333F;

    private int _mainColorID = Shader.PropertyToID("_Color");
    private int _baseColorID = Shader.PropertyToID("_BaseColor");
    private int _emissionColorID = Shader.PropertyToID("_EmissionColor");

    private MeshRenderer _meshRenderer;
    private TrailRenderer _trailRenderer;

    private void Start()
    {
        if (attractorColor == default)
        {
            attractorColor = Random.ColorHSV(0.0F, 0.9999F, 0.8F, 1.0F, 1.0F, 1.0F);
        }

        _meshRenderer = GetComponent<MeshRenderer>();
        _trailRenderer = GetComponent<TrailRenderer>();

        SetGlowEffect(attractorColor);
    }

    private void SetGlowEffect(Color color)
    {
        SetMaterialColor(this.attractorColor);
        SetTrailMaterialColor(this.attractorColor);
    }

    private void SetMaterialColor(Color color)
    {
        if (_meshRenderer == null)
        {
            return;
        }

        var material = _meshRenderer.material;

        if (material == null)
        {
            return;
        }

        material.SetColor(_mainColorID, color);
        material.SetColor(_baseColorID, color * Mathf.Pow(2, glowIntensity));
        material.SetColor(_emissionColorID, color);
    }

    private void SetTrailMaterialColor(Color color)
    {
        if (_trailRenderer == null)
        {
            return;
        }

        _trailRenderer.time = trailSeconds;

        var material = _trailRenderer.material;

        if (material == null)
        {
            return;
        }

        material.SetColor(_mainColorID, color);
        material.SetColor(_baseColorID, color * Mathf.Pow(2, trailGlowIntensity));
        material.SetColor(_emissionColorID, color);

        var colorGradient = _trailRenderer.colorGradient;

        colorGradient.SetKeys(
            new GradientColorKey[]
            {
                new(color * Mathf.Pow(2, trailGlowIntensity), 0.0F),
                new(color * Mathf.Pow(2, trailGlowIntensity), 1.0F),
            },
            new GradientAlphaKey[]
            {
                new(1.0F, 0.0F),
                new(float.Epsilon, 1.0F),
            }
        );
    }
}
