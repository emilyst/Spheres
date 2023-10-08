using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using System;
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
    GameObject[] gameObjects;
    Rigidbody[] rigidbodies;
    PointMass[] pointMasses;
    BodyAppearance[] appearances;

    internal Octree Octree;
    internal ObjectPool<Octree> OctreePool;

    internal float G;
    internal Vector3 Barycenter = Vector3.zero;

    public GameObject Body;
    public SimulationDisplay SimulationDisplay;

    [Header("Simulation")]

    public int Seed;
    public int PointMassCount = 3;
    public AlgorithmType Algorithm = AlgorithmType.Exact;
    [Range(-10, -1)] public float GravitationIntensity = -4;
    [Range(0, 10)] public float BarnesHutTheta = 1.5F;
    public bool Paused;

    internal int CalculationsCount;
    internal float TotalMass;

    public enum AlgorithmType
    {
        Exact,
        BarnesHut,
    }

    void Start()
    {
        Application.targetFrameRate = 120;

        G = Mathf.Pow(10, GravitationIntensity);

        if (Seed == 0) Seed = Math.Abs((int)DateTime.Now.Ticks);
        Random.InitState(Seed);

        Body ??= Resources.Load<GameObject>("Prefabs/Body");

        gameObjects = new GameObject[PointMassCount];
        rigidbodies = new Rigidbody[PointMassCount];
        pointMasses = new PointMass[PointMassCount];
        appearances = new BodyAppearance[PointMassCount];

        OctreePool = new ObjectPool<Octree>(
            createFunc: () => new Octree(this),
            actionOnGet: t => t.OnGet(),
            actionOnRelease: t => t.OnRelease(),
            actionOnDestroy: t => t.OnDestroy(),
            collectionCheck: true,
            defaultCapacity: PointMassCount,
            maxSize: (int)Mathf.Pow(PointMassCount, 2)
        );
        Octree = OctreePool.Get();

        for (var i = 0; i < PointMassCount / 2; i++)
        {
            var go = InstantiatePrefab(Body);
            var r = go.GetComponent<Rigidbody>();
            var pma = go.GetComponent<BodyAppearance>();

            gameObjects[i] = go;
            rigidbodies[i] = r;
            appearances[i] = pma;
        }

        for (var i = PointMassCount / 2; i < PointMassCount; i++)
        {
            var go = InstantiateOtherPrefab(Body);
            var r = go.GetComponent<Rigidbody>();
            var pma = go.GetComponent<BodyAppearance>();

            gameObjects[i] = go;
            rigidbodies[i] = r;
            appearances[i] = pma;
        }

        SimulationDisplay = GetComponentInChildren<SimulationDisplay>();
    }

    void FixedUpdate()
    {
        if (Paused)
        {
            return;
        }

        UpdatePointMassesFromRigidbodies();

        switch (Algorithm)
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

    void OnValidate()
    {
        if (gameObjects == null)
        {
            return;
        }

        G = Mathf.Pow(10, GravitationIntensity);

        if (SimulationDisplay != null)
        {
            SimulationDisplay.UpdateDisplayText();
        }
    }

    void UpdatePointMassesFromRigidbodies()
    {
        Barycenter = Vector3.zero;
        TotalMass = 0f;

        for (var i = 0; i < PointMassCount; i++)
        {
            var mass = rigidbodies[i].mass;
            var point = rigidbodies[i].position;
            var velocity = rigidbodies[i].velocity;

            pointMasses[i] = new PointMass(point, mass, velocity);

            Barycenter += point * mass;
            TotalMass += mass;
        }

        Barycenter /= TotalMass;
    }

    GameObject InstantiatePrefab(GameObject prefab)
    {
        var position = Random.insideUnitSphere * 500 + Vector3.left * 2000; // fixme: scale by pointmass count?
        var rotation = Random.rotationUniform;

        var go = Instantiate(prefab, position, rotation);
        go.transform.SetParent(transform);

        var rb = go.GetComponent<Rigidbody>();
        rb.mass = Mathf.Clamp(Random.value * 10.0f, 1f, 10f);
        rb.velocity = Random.insideUnitSphere * 100 + Vector3.down * 500; // fixme: scale by pointmass count and g?

        return go;
    }

    GameObject InstantiateOtherPrefab(GameObject prefab)
    {
        var position = Random.insideUnitSphere * 500 + Vector3.right * 2000; // fixme: scale by pointmass count?
        var rotation = Random.rotationUniform;

        var go = Instantiate(prefab, position, rotation);
        go.transform.SetParent(transform);

        var rb = go.GetComponent<Rigidbody>();
        rb.mass = Mathf.Clamp(Random.value * 10.0f, 1f, 10f);
        rb.velocity = Random.insideUnitSphere * 100 + Vector3.up * 500; // fixme: scale by pointmass count and g?

        return go;
    }

    public void TogglePaused()
    {
        // ReSharper disable once AssignmentInConditionalExpression
        if (Paused = !Paused)
        {
            for (var i = 0; i < PointMassCount; i++)
            {
                rigidbodies[i].velocity = pointMasses[i].Velocity;
                rigidbodies[i].isKinematic = true;
                appearances[i].Pause();
            }
        }
        else
        {
            for (var i = 0; i < PointMassCount; i++)
            {
                rigidbodies[i].isKinematic = false;
                rigidbodies[i].velocity = pointMasses[i].Velocity;
                appearances[i].Resume();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ApplyExactGravitation()
    {
        var jobsCount = 0;
        var jobInputs = new NativeArray<GravitationInput>(
            (int)Mathf.Pow(PointMassCount, 2),
            Allocator.TempJob
        );
        var jobOutputs = new NativeArray<GravitationOutput>(
            (int)Mathf.Pow(PointMassCount, 2),
            Allocator.TempJob
        );

        for (var i = 0; i < PointMassCount; i++)
        {
            for (var j = 0; j < PointMassCount; j++)
            {
                if (i >= j) // pairs are order-independent
                {
                    continue;
                }

                jobInputs[jobsCount++] = new GravitationInput
                {
                    Index1 = i,
                    PointMass1 = pointMasses[i],

                    Index2 = j,
                    PointMass2 = pointMasses[j],
                };
            }
        }

        new GravitationJob
                {
                    G = G,
                    JobInputs = jobInputs,
                    JobOutputs = jobOutputs,
                }
            .Schedule(jobsCount, jobsCount / 8)
            .Complete();

        for (var i = 0; i < PointMassCount; i++)
        {
            for (var j = 0; j < PointMassCount; j++)
            {
                if (i >= j) // pairs are order-independent
                {
                    continue;
                }

                var output = jobOutputs[--jobsCount];

                rigidbodies[output.Index1].velocity += output.Force1;
                rigidbodies[output.Index2].velocity += output.Force2;

                CalculationsCount++;
            }
        }

        jobInputs.Dispose();
        jobOutputs.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ApplyBarnesHutGravitation()
    {
        var jobsCount = 0;
        var jobInputs = new NativeArray<GravitationInput>(
            (int)Mathf.Pow(PointMassCount, 2),
            Allocator.TempJob
        );
        var jobOutputs = new NativeArray<GravitationOutput>(
            (int)Mathf.Pow(PointMassCount, 2),
            Allocator.TempJob
        );

        Octree.Reset();
        Octree.AddPointMasses(pointMasses);

        for (var i = 0; i < PointMassCount; i++)
        {
            var attractors = Octree.FindAttractors(pointMasses[i].Point);

            foreach (var attractor in attractors)
            {
                jobInputs[jobsCount++] = new GravitationInput
                {
                    Index1 = i,
                    PointMass1 = pointMasses[i],
                    PointMass2 = attractor,
                };
            }
        }

        CalculationsCount += jobsCount;

        new GravitationJob
            {
                G = G,
                JobInputs = jobInputs,
                JobOutputs = jobOutputs,
            }
            .Schedule(jobsCount, jobsCount / 8)
            .Complete();

        for (var i = 0; i < jobsCount; i++)
        {
            rigidbodies[jobOutputs[i].Index1].velocity += jobOutputs[i].Force1;
        }

        jobInputs.Dispose();
        jobOutputs.Dispose();
    }
}
