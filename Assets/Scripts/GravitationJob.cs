using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

struct GravitationInput
{
    public int Index1;
    public PointMass PointMass1;

    public int Index2;
    public PointMass PointMass2;
}

struct GravitationOutput
{
    public int Index1;
    public Vector3 Force1;

    public int Index2;
    public Vector3 Force2;
}

struct GravitationJob : IJobParallelFor
{
    public float G;
    public NativeArray<GravitationInput> JobInputs;
    public NativeArray<GravitationOutput> JobOutputs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(int i)
    {
        var r1 = JobInputs[i].PointMass1.Point;
        var m1 = JobInputs[i].PointMass1.Mass;

        var r2 = JobInputs[i].PointMass2.Point;
        var m2 = JobInputs[i].PointMass2.Mass;

        var distance = Vector3.Distance(r1, r2);
        var index = JobInputs[i].Index1;

        if (distance == 0)
        {
            JobOutputs[i] = new GravitationOutput
            {
                Index1 = JobInputs[i].Index1,
                Force1 = default,
                Index2 = JobInputs[i].Index2,
                Force2 = default,
            };
        }
        else
        {
            var force = G * m1 * m2 / distance * distance;
            var direction = r2 - r1;

            JobOutputs[i] = new GravitationOutput
            {
                Index1 = JobInputs[i].Index1,
                Force1 = force * direction / 2.0f,
                Index2 = JobInputs[i].Index2,
                Force2 = force * -direction / 2.0f,
            };
        }
    }
}
