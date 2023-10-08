using System.Globalization;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

public struct PointMass
{
    public Vector3 Point;
    public float Mass;
    public Vector3 Velocity;

    public PointMass(Vector3 point, float mass, Vector3 velocity)
    {
        Point = point;
        Mass = mass;
        Velocity = velocity;
    }

    public override string ToString()
    {
        return $"(Point = {Point.ToString()}, Mass = {Mass.ToString(CultureInfo.InvariantCulture)})";
    }
}
