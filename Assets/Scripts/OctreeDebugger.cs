using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(Simulation))]
public class OctreeDebugger : MonoBehaviour
{
    [SerializeField] Simulation Simulation;

    void Start()
    {
        Simulation ??= GetComponent<Simulation>();
    }

    void OnDrawGizmos()
    {
        if (Simulation.Octree == null)
        {
            return;
        }

        foreach (var node in Simulation.Octree.GetExternalNodes())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(node.Bounds.center, node.Bounds.size);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(node.Barycenter, node.Bounds.extents.magnitude * node.TotalMass / 100);
        }
    }
}
