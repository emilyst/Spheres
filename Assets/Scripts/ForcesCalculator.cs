using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

internal struct ForcesCalculatorInput
{
    public int Index1;
    public Vector3 Position1;
    public float Mass1;

    public int Index2;
    public Vector3 Position2;
    public float Mass2;
}

internal struct ForcesCalculatorOutput
{
    public int Index1;
    public Vector3 Force1;

    public int Index2;
    public Vector3 Force2;
}

internal struct ForcesCalculator : IJobParallelFor
{
    public float G;
    public NativeArray<ForcesCalculatorInput> Inputs;
    public NativeArray<ForcesCalculatorOutput> Outputs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(int i)
    {
        var r1 = Inputs[i].Position1;
        var m1 = Inputs[i].Mass1;

        var r2 = Inputs[i].Position2;
        var m2 = Inputs[i].Mass2;

        var distance = Vector3.Distance(r1, r2);
        var index = Inputs[i].Index1;

        if (distance == 0)
        {
            Outputs[i] = new ForcesCalculatorOutput
            {
                Index1 = Inputs[i].Index1,
                Force1 = default,
                Index2 = Inputs[i].Index2,
                Force2 = default,
            };
        }
        else
        {
            var force = G * m1 * m2 / distance * distance / 2.0f;
            var direction = r2 - r1;

            Outputs[i] = new ForcesCalculatorOutput
            {
                Index1 = Inputs[i].Index1,
                Force1 = force * direction,
                Index2 = Inputs[i].Index2,
                Force2 = force * -direction,
            };
        }
    }
}
