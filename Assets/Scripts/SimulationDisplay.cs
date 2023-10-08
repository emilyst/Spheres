using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SimplifyStringInterpolation
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(TMP_Text))]
public class SimulationDisplay : MonoBehaviour
{
    [FormerlySerializedAs("Simulation")] public Simulation simulation;
    [FormerlySerializedAs("DisplayTextMesh")] public TMP_Text displayTextMesh;

    private StringBuilder _displayText;
    private float _secondsElapsed;

    private void Start()
    {
        displayTextMesh = GetComponent<TMP_Text>();
        _displayText = new StringBuilder(1024);
        UpdateDisplayText();
    }

    private void Update()
    {
        if (!HasSecondElapsed())
        {
            return;
        }

        UpdateDisplayText();
        simulation.CalculationsCount = 0;
    }

    internal void UpdateDisplayText()
    {
        _displayText.Clear();

        _displayText.Append("Simulation seed\t\t\t= ").AppendLine(simulation.seed.ToString());
        _displayText.Append("Gravitational constant\t\t≈ ").AppendLine(simulation.G.ToString("E4"));
        _displayText.Append("Count of attractors\t\t= ").AppendLine(simulation.attractorCount.ToString("N0"));
        _displayText.Append("Total system mass\t\t= ").AppendLine(simulation.MassAccumulator.ToString("F4"));
        _displayText.Append("Barycenter coordinates\t= ").AppendLine(simulation.CenterAccumulator.ToString());
        _displayText.Append("Frames per second\t\t≈ ").AppendLine((1f / Time.unscaledDeltaTime).ToString("F4"));
        _displayText.AppendLine(
            simulation.isPaused
                ? $"Calculations per second\t≈ (PAUSED)"
                : $"Calculations per second\t≈ {simulation.CalculationsCount.ToString("N0")}"
        );

        switch (simulation.algorithm)
        {
            case Simulation.AlgorithmType.Exact:
            {
                _displayText.AppendLine("Simulation algorithm\t\t= Exact");
                break;
            }
            case Simulation.AlgorithmType.BarnesHut:
            {
                _displayText.AppendLine("Simulation algorithm\t\t= Barnes–Hut octree");
                _displayText.Append("Barnes–Hut theta\t\t= ").AppendLine(simulation.barnesHutTheta.ToString("F4"));
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        displayTextMesh.SetText(_displayText);
    }

    private bool HasSecondElapsed()
    {
        _secondsElapsed += Time.deltaTime;

        if (!(_secondsElapsed > 1f))
        {
            return false;
        }

        _secondsElapsed %= 1f;

        return true;
    }
}
