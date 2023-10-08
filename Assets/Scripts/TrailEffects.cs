using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(MeshRenderer), typeof(TrailRenderer))]
public class TrailEffects : MonoBehaviour
{
    private TrailRenderer _trailRenderer;

    private float _trailSecondsAtPauseTime;
    private float _pauseTime;
    private float _resumeTime;

    private void Start()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
    }

    public void Pause()
    {
        CancelInvoke(nameof(ResumeTrailTime));

        _pauseTime = Time.time;
        _trailSecondsAtPauseTime = _trailRenderer.time;
        _trailRenderer.time = Mathf.Infinity;
    }

    public void Resume()
    {
        _resumeTime = Time.time;
        _trailRenderer.time = (_resumeTime - _pauseTime) + _trailSecondsAtPauseTime;

        Invoke(nameof(ResumeTrailTime), _trailSecondsAtPauseTime);
    }

    private void ResumeTrailTime()
    {
        _trailRenderer.time = _trailSecondsAtPauseTime;
    }
}
