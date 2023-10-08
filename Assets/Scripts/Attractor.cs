using System.Globalization;
using System.Text;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global

public class Attractor
{
    private Rigidbody _rigidbody;
    private TrailEffects _trailEffects;
    private StringBuilder _displayStringBuilder;

    public Attractor(GameObject gameObject = null)
    {
        var o = gameObject ?? new GameObject();
        _rigidbody = o.GetComponent<Rigidbody>();
        _trailEffects = o.GetComponent<TrailEffects>();
        _displayStringBuilder = new StringBuilder();
    }

    public float Mass
    {
        get => _rigidbody.mass;
        set => _rigidbody.mass = value;
    }

    public Vector3 Position
    {
        get => _rigidbody.position;
        set => _rigidbody.position = value;
    }

    public Vector3 Velocity
    {
        get => _rigidbody.linearVelocity;
        set => _rigidbody.linearVelocity = value;
    }

    public override string ToString()
    {
        return _displayStringBuilder
            .Clear()
            .Append("(Position = ")
            .Append(Position.ToString())
            .Append(", Mass = ")
            .Append(Mass.ToString(CultureInfo.InvariantCulture))
            .Append(")")
            .ToString();
    }

    private bool _isPaused;
    private Vector3 _previousLinearVelocity;
    private Vector3 _previousAngularVelocity;

    public void Pause()
    {
        _previousLinearVelocity = _rigidbody.linearVelocity;
        _previousAngularVelocity = _rigidbody.angularVelocity;
        _trailEffects.Pause();

        _isPaused = true;
        _rigidbody.isKinematic = _isPaused;
    }

    public void Resume()
    {
        _isPaused = false;
        _rigidbody.isKinematic = _isPaused;

        _rigidbody.linearVelocity = _previousLinearVelocity;
        _rigidbody.angularVelocity =_previousAngularVelocity;
        _trailEffects.Resume();
    }
}
