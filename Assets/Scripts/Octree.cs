using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global

// Adapted from Barnes-Hut quadtree description at http://arborjs.org/docs/barnes-hut.
internal class Octree
{
    public Bounds Bounds;
    public Bounds[] Octants = new Bounds[8];
    public List<Octree> Children = new(8);
    public Vector3 Position;
    public float Mass;
    public Attractor AssignedAttractor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        Bounds = default;
        Octants = new Bounds[8];
        Children.Clear();
        Position = default;
        Mass = float.NaN;
        AssignedAttractor = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EncapsulateBounds(Bounds bounds)
    {
        Bounds = bounds;
        ExpandToCube();
        DivideIntoOctants();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ExpandToCube()
    {
        var maxExtent = Mathf.Max(Bounds.extents.x, Bounds.extents.y, Bounds.extents.z);

        Bounds.SetMinMax(
            Bounds.center - new Vector3(maxExtent, maxExtent, maxExtent),
            Bounds.center + new Vector3(maxExtent, maxExtent, maxExtent)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DivideIntoOctants()
    {
        Octants[0] = LowerBackLeftOctant;
        Octants[1] = LowerBackRightOctant;
        Octants[2] = LowerFrontLeftOctant;
        Octants[3] = LowerFrontRightOctant;
        Octants[4] = UpperBackLeftOctant;
        Octants[5] = UpperBackRightOctant;
        Octants[6] = UpperFrontLeftOctant;
        Octants[7] = UpperFrontRightOctant;
    }

    private Bounds LowerBackLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.min.y, Bounds.max.z)) / 2, Bounds.extents);

    private Bounds LowerBackRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.min.y, Bounds.max.z)) / 2, Bounds.extents);

    private Bounds LowerFrontLeftOctant =>
        new((Bounds.center + Bounds.min) / 2, Bounds.extents);

    private Bounds LowerFrontRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.min.y, Bounds.min.z)) / 2, Bounds.extents);

    private Bounds UpperBackLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.max.y, Bounds.max.z)) / 2, Bounds.extents);

    private Bounds UpperBackRightOctant =>
        new((Bounds.center + Bounds.max) / 2, Bounds.extents);

    private Bounds UpperFrontLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.max.y, Bounds.min.z)) / 2, Bounds.extents);

    private Bounds UpperFrontRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.max.y, Bounds.min.z)) / 2, Bounds.extents);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Populate(IEnumerable<Attractor> attractors, ObjectPool<Octree> octreePool)
    {
        Bounds = default;

        foreach (var attractor in attractors)
        {
            Bounds.Encapsulate(attractor.Position);
        }

        Bounds.Expand(100f); // fuzz to avoid missing bodies due to floating-position errors (need more for faster attractors?)
        ExpandToCube();
        DivideIntoOctants();

        foreach (var attractor in attractors)
        {
            AddAttractor(attractor, octreePool);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddAttractor(Attractor attractor, ObjectPool<Octree> octreePool)
    {
        if (float.IsNaN(Mass))
        {
            Position = attractor.Position;
            Mass = attractor.Mass;
            AssignedAttractor = attractor;

            return;
        }

        if (HasChildren())
        {
            var child = GetChildContainingPosition(attractor.Position);

            if (child != null)
            {
                child.AddAttractor(attractor, octreePool);
            }
            else
            {
                AssignAttractorToNewChild(attractor, octreePool);
            }
        }
        else
        {
            AssignAttractorToNewChild(AssignedAttractor, octreePool);
            AssignAttractorToNewChild(attractor, octreePool);
            AssignedAttractor = default;
        }

        UpdateBarycenter(attractor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasChildren()
    {
        return Children.Count > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Octree GetChildContainingPosition(Vector3 position)
    {
        foreach (var child in Children)
        {
            if (child.Contains(position))
            {
                return child;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssignAttractorToNewChild(Attractor attractor, ObjectPool<Octree> octreePool)
    {
        var child = octreePool.Get();
        child.EncapsulateBounds(GetOctantContainingPosition(attractor.Position));
        child.AddAttractor(attractor, octreePool);
        Children.Add(child);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bounds GetOctantContainingPosition(Vector3 position)
    {
        foreach (var octant in Octants)
        {
            if (octant.Contains(position))
            {
                return octant;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Contains(Vector3 position)
    {
        return Bounds.Contains(position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateBarycenter(Attractor attractor)
    {
        Mass += attractor.Mass;
        Position = (Position * Mass + attractor.Position * attractor.Mass) / (Mass + attractor.Mass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<(Vector3, float)> GenerateCalculatorInputValues(Vector3 position, float theta)
    {
        // Since there are no children in this case, and each childless
        // node should have only one attractor in it, the barycenter and
        // mass of this node are equivalent to the position and mass of
        // its child.
        if (Children.Count == 0)
        {
            return new (Vector3, float)[] { (Position, Mass) };
        }

        var width = (Bounds.size.x + Bounds.size.y + Bounds.size.z) / 3f;
        var distance = Vector3.Distance(Position, position);

        // If the distance is too far, stop recursing and use the entire
        // sub-octree. This is the reason this approximation is faster.
        if (width / distance < theta)
        {
            return new (Vector3, float)[] { (Position, Mass) };
        }

        // If we're not at an external (end) node, and not
        // short-circuiting, we recurse.
        var inputs = new List<(Vector3, float)>();
        foreach (var child in Children)
        {
            if (float.IsNaN(child.Mass))
            {
                continue;
            }

            inputs.AddRange(child.GenerateCalculatorInputValues(position, theta));
        }
        return inputs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal List<Octree> GetExternalChildren()
    {
        var externalChildren = new List<Octree>();

        if (HasChildren())
        {
            foreach (var child in Children)
            {
                externalChildren.AddRange(child.GetExternalChildren());
            }
        }
        else
        {
            externalChildren.Add(this);
        }

        return externalChildren;
    }
}
