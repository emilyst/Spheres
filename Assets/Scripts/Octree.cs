using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global

// Adapted from Barnes-Hut quadtree description at http://arborjs.org/docs/barnes-hut.
class Octree
{
    public Simulation Simulation;
    public Bounds Bounds;
    public Bounds[] Octants = new Bounds[8];
    public List<Octree> ChildNodes = new(8);
    public Vector3 Barycenter;
    public float TotalMass;
    public PointMass AssignedPointMass;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Octree(Simulation simulation)
    {
        Simulation = simulation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Reset()
    {
        Bounds = default;
        Octants = new Bounds[8];
        ChildNodes.Clear();
        Barycenter = default;
        TotalMass = float.NaN;
        AssignedPointMass = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnGet()
    {
        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnRelease()
    {
        foreach (var node in ChildNodes)
        {
            Simulation.OctreePool.Release(node);
        }

        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnDestroy()
    {
        foreach (var node in ChildNodes)
        {
            node.Reset();
        }

        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EncapsulatePoints(IEnumerable<Vector3> points)
    {
        Bounds = default;

        foreach (var point in points)
        {
            Bounds.Encapsulate(point);
        }

        Bounds.Expand(100f); // fuzz to avoid missing bodies due to floating-point errors

        ExpandToCube();
        DivideIntoOctants();
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
    void DivideIntoOctants()
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

    Bounds LowerBackLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.min.y, Bounds.max.z)) / 2, Bounds.extents);

    Bounds LowerBackRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.min.y, Bounds.max.z)) / 2, Bounds.extents);

    Bounds LowerFrontLeftOctant =>
        new((Bounds.center + Bounds.min) / 2, Bounds.extents);

    Bounds LowerFrontRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.min.y, Bounds.min.z)) / 2, Bounds.extents);

    Bounds UpperBackLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.max.y, Bounds.max.z)) / 2, Bounds.extents);

    Bounds UpperBackRightOctant =>
        new((Bounds.center + Bounds.max) / 2, Bounds.extents);

    Bounds UpperFrontLeftOctant =>
        new((Bounds.center + new Vector3(Bounds.min.x, Bounds.max.y, Bounds.min.z)) / 2, Bounds.extents);

    Bounds UpperFrontRightOctant =>
        new((Bounds.center + new Vector3(Bounds.max.x, Bounds.max.y, Bounds.min.z)) / 2, Bounds.extents);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddPointMasses(PointMass[] pointMasses)
    {
        EncapsulatePoints(Array.ConvertAll(pointMasses, pointMass => pointMass.Point));

        foreach (var pointMass in pointMasses)
        {
            AddPointMass(pointMass);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddPointMass(PointMass pointMass)
    {
        if (float.IsNaN(TotalMass))
        {
            Barycenter = pointMass.Point;
            TotalMass = pointMass.Mass;
            AssignedPointMass = pointMass;

            return;
        }

        if (HasChildNodes())
        {
            var childNode = GetChildNodeContainingPoint(pointMass.Point);

            if (childNode != null)
            {
                childNode.AddPointMass(pointMass);
            }
            else
            {
                AssignPointMassToNewChildNode(pointMass);
            }
        }
        else
        {
            AssignPointMassToNewChildNode(AssignedPointMass);
            AssignPointMassToNewChildNode(pointMass);
            AssignedPointMass = default;
        }

        UpdateBarycenter(pointMass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool HasChildNodes()
    {
        return ChildNodes.Count > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Octree GetChildNodeContainingPoint(Vector3 point)
    {
        foreach (var node in ChildNodes)
        {
            if (node.Contains(point))
            {
                return node;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AssignPointMassToNewChildNode(PointMass pointMass)
    {
        var childNode = Simulation.OctreePool.Get();
        childNode.EncapsulateBounds(GetOctantContainingPoint(pointMass.Point));
        childNode.AddPointMass(pointMass);
        ChildNodes.Add(childNode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Bounds GetOctantContainingPoint(Vector3 point)
    {
        foreach (var octant in Octants)
        {
            if (octant.Contains(point))
            {
                return octant;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool Contains(Vector3 point)
    {
        return Bounds.Contains(point);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateBarycenter(PointMass pointMass)
    {
        TotalMass += pointMass.Mass;
        Barycenter = (Barycenter * TotalMass + pointMass.Point * pointMass.Mass) / (TotalMass + pointMass.Mass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<PointMass> FindAttractors(Vector3 point)
    {
        if (ChildNodes.Count == 0)
        {
            return new List<PointMass> { new(Barycenter, TotalMass, Vector3.zero) };
        }

        var width = (Bounds.size.x + Bounds.size.y + Bounds.size.z) / 3f;
        var distance = Vector3.Distance(Barycenter, point);

        if (width / distance < Simulation.BarnesHutTheta)
        {
            return new List<PointMass> { new(Barycenter, TotalMass, Vector3.zero) };
        }

        var nodePointMasses = new List<PointMass>();

        foreach (var node in ChildNodes)
        {
            if (float.IsNaN(node.TotalMass))
            {
                continue;
            }

            nodePointMasses.AddRange(node.FindAttractors(point));
        }

        return nodePointMasses;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal List<Octree> GetExternalNodes()
    {
        var externalNodes = new List<Octree>();

        if (HasChildNodes())
        {
            foreach (var node in ChildNodes)
            {
                externalNodes.AddRange(node.GetExternalNodes());
            }
        }
        else
        {
            externalNodes.Add(this);
        }

        return externalNodes;
    }
}
