using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Pool;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UseNullPropagation

public class Simulation : MonoBehaviour
{
    private List<Attractor> _attractors;
    private Octree _octree;
    private ObjectPool<Octree> _octreePool;
    private SimulationDisplay _simulationDisplay;

    [Header("Simulation")]

    public int seed;
    public int attractorCount = 3;
    public AlgorithmType algorithm = AlgorithmType.Exact;
    [Range(-10, -1)] public float gravitationIntensity = -4;
    [Range(0, 10)] public float barnesHutTheta = 1.5F;
    public bool isPaused = false;

    internal float G;
    internal Vector3 CenterAccumulator = Vector3.zero;
    internal float MassAccumulator;

    internal int CalculationsCount;
    internal ComputeBuffer CalculatorInputsBuffer;
    internal ComputeBuffer CalculatorOutputsBuffer;

    public enum AlgorithmType
    {
        Exact,
        BarnesHut,
    }

    private void OnDrawGizmos()
    {
        if (_octree == null)
        {
            return;
        }

        foreach (var child in _octree.GetExternalChildren())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(child.Bounds.center, child.Bounds.size);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(child.Position, child.Bounds.extents.magnitude * child.Mass / 100);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 120;

        G = Mathf.Pow(10, gravitationIntensity);

        if (seed == 0) seed = Math.Abs((int)DateTime.Now.Ticks);
        Random.InitState(seed);

        _octreePool = new ObjectPool<Octree>(
            createFunc: () => new Octree(),
            actionOnGet: octree =>
            {
                octree.Clear();
            },
            actionOnRelease: octree =>
            {
                octree.Children.ForEach(_octreePool.Release);
                octree.Clear();
            },
            actionOnDestroy: octree =>
            {
                octree.Children.ForEach(node => node.Clear());
                octree.Clear();
            },
            collectionCheck: true,
            defaultCapacity: attractorCount,
            maxSize: (int)Mathf.Pow(attractorCount, 2)
        );

        _octree = _octreePool.Get();

        _attractors = new List<Attractor>(attractorCount);

        for (var i = 0; i < attractorCount; i++)
        {
            _attractors.Add(new Attractor(InstantiateAttractorGameObject()));
        }

        _simulationDisplay = GetComponentInChildren<SimulationDisplay>();
    }

    private void OnEnable()
    {
        CalculatorInputsBuffer = new ComputeBuffer(attractorCount, sizeof(float) * 5);
        CalculatorOutputsBuffer = new ComputeBuffer(attractorCount, sizeof(float) * 5);
    }

    private void OnDisable()
    {
        CalculatorInputsBuffer.Release();
        CalculatorInputsBuffer = null;

        CalculatorOutputsBuffer.Release();
        CalculatorOutputsBuffer = null;
    }

    private void FixedUpdate()
    {
        if (isPaused)
        {
            return;
        }

        CenterAccumulator = Vector3.zero;
        MassAccumulator = 0f;

        foreach (var attractor in _attractors)
        {
            CenterAccumulator += attractor.Position * attractor.Mass;
            MassAccumulator += attractor.Mass;
        }

        CenterAccumulator /= MassAccumulator;

        switch (algorithm)
        {
            case AlgorithmType.Exact:
            {
                ApplyExactGravitation();
                break;
            }
            case AlgorithmType.BarnesHut:
            {
                ApplyBarnesHutGravitation();
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void OnValidate()
    {
        if (_attractors == null)
        {
            return;
        }

        G = Mathf.Pow(10, gravitationIntensity);

        if (_simulationDisplay != null)
        {
            _simulationDisplay.UpdateDisplayText();
        }
    }

    private GameObject InstantiateAttractorGameObject(GameObject prefab = null)
    {
        var position = Random.insideUnitSphere * 5000; // fixme: scale by attractor count?
        var rotation = Random.rotationUniform;

        prefab ??= Resources.Load<GameObject>("Prefabs/Attractor");
        var go = Instantiate(prefab, position, rotation);
        go.transform.SetParent(transform);

        var rb = go.GetComponent<Rigidbody>();
        rb.mass = Mathf.Clamp(Random.value * 10.0f, 1f, 10f);
        rb.linearVelocity = Random.insideUnitSphere * 1000; // fixme: scale by attractor count and g?

        return go;
    }

    public void TogglePaused()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            foreach (var attractor in _attractors)
            {
                attractor.Pause();
            }
        }
        else
        {
            foreach (var attractor in _attractors)
            {
                attractor.Resume();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyExactGravitation()
    {
        var jobsCount = 0;
        var jobInputs = new NativeArray<ForcesCalculatorInput>(
            (int)Mathf.Pow(attractorCount, 2),
            Allocator.TempJob
        );
        var jobOutputs = new NativeArray<ForcesCalculatorOutput>(
            (int)Mathf.Pow(attractorCount, 2),
            Allocator.TempJob
        );

        for (var i = 0; i < attractorCount; i++)
        {
            for (var j = 0; j < attractorCount; j++)
            {
                if (i >= j) // pairs are order-independent
                {
                    continue;
                }

                jobInputs[jobsCount++] = new ForcesCalculatorInput
                {
                    Index1 = i,
                    Position1 = _attractors[i].Position,
                    Mass1 = _attractors[i].Mass,

                    Index2 = j,
                    Position2 = _attractors[j].Position,
                    Mass2 = _attractors[j].Mass,
                };
            }
        }

        var forcesCalculator = new ForcesCalculator
        {
            G = G,
            Inputs = jobInputs,
            Outputs = jobOutputs,
        };
        var forcesCalculatorJob = forcesCalculator.Schedule(jobsCount, jobsCount / 8);
        forcesCalculatorJob.Complete();

        for (var i = 0; i < attractorCount; i++)
        {
            for (var j = 0; j < attractorCount; j++)
            {
                if (i >= j) // pairs are order-independent
                {
                    continue;
                }

                var output = jobOutputs[--jobsCount];
                _attractors[output.Index1].Velocity += output.Force1;
                _attractors[output.Index2].Velocity += output.Force2;

                CalculationsCount++;
            }
        }

        jobInputs.Dispose();
        jobOutputs.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyBarnesHutGravitation()
    {
        var jobsCount = 0;
        var jobInputs = new NativeArray<ForcesCalculatorInput>(
            (int)Mathf.Pow(attractorCount, 2),
            Allocator.TempJob
        );
        var jobOutputs = new NativeArray<ForcesCalculatorOutput>(
            (int)Mathf.Pow(attractorCount, 2),
            Allocator.TempJob
        );

        _octree.Clear();
        _octree.Populate(_attractors, _octreePool);

        for (var i = 0; i < _attractors.Count; i++)
        {
            foreach (var input in _octree.GenerateCalculatorInputValues(_attractors[i].Position, barnesHutTheta))
            {
                jobInputs[jobsCount++] = new ForcesCalculatorInput
                {
                    Index1 = i,
                    Position1 = _attractors[i].Position,
                    Mass1 = _attractors[i].Mass,

                    // Index2 = -1, // never used
                    Position2 = input.Item1,
                    Mass2 = input.Item2,
                };
            }
        }

        CalculationsCount += jobsCount;

        var forcesCalculator = new ForcesCalculator
        {
            G = G,
            Inputs = jobInputs,
            Outputs = jobOutputs,
        };
        var forcesCalculatorJob = forcesCalculator.Schedule(jobsCount, jobsCount / 8);
        forcesCalculatorJob.Complete();

        for (var i = 0; i < jobsCount; i++)
        {
            _attractors[jobOutputs[i].Index1].Velocity += jobOutputs[i].Force1;
        }

        jobInputs.Dispose();
        jobOutputs.Dispose();
    }
}
