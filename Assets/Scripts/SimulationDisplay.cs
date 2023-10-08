using System;
using System.Text;
using TMPro;
using UnityEngine;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SimplifyStringInterpolation
// ReSharper disable UseNullPropagation

[RequireComponent(typeof(TMP_Text))]
public class SimulationDisplay : MonoBehaviour
{
    public Simulation Simulation;
    public TMP_Text DisplayTextMesh;

    StringBuilder displayText;
    float secondsElapsed;

    void Start()
    {
        DisplayTextMesh = GetComponent<TMP_Text>();
        displayText = new StringBuilder(1024);
        UpdateDisplayText();
    }

    void Update()
    {
        if (!HasSecondElapsed())
        {
            return;
        }

        UpdateDisplayText();
        Simulation.CalculationsCount = 0;
    }

    internal void UpdateDisplayText()
    {
        displayText.Clear();

        displayText.AppendLine($"Simulation seed\t\t\t= {Simulation.Seed.ToString()}");
        displayText.AppendLine($"Gravitational constant\t≈ {Simulation.G.ToString("E4")}");
        displayText.AppendLine($"Count of point masses\t= {Simulation.PointMassCount.ToString("N0")}");
        displayText.AppendLine($"Total system mass\t\t= {Simulation.TotalMass.ToString("F4")}");
        displayText.AppendLine($"Barycenter coordinates\t= {Simulation.Barycenter.ToString()}");
        displayText.AppendLine($"Frames per second\t\t≈ {(1f / Time.unscaledDeltaTime).ToString("F4")}");
        displayText.AppendLine(
            Simulation.Paused
                ? $"Calculations per second\t≈ (PAUSED)"
                : $"Calculations per second\t≈ {Simulation.CalculationsCount.ToString("N0")}"
        );

        switch (Simulation.Algorithm)
        {
            case Simulation.AlgorithmType.Exact:
            {
                displayText.AppendLine($"Simulation algorithm\t= Exact");
                break;
            }
            case Simulation.AlgorithmType.BarnesHut:
            {
                displayText.AppendLine($"Simulation algorithm\t= Barnes–Hut octree");
                displayText.AppendLine($"Barnes–Hut theta\t\t= {Simulation.BarnesHutTheta.ToString("F4")}");
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        DisplayTextMesh.SetText(displayText);
    }

    bool HasSecondElapsed()
    {
        secondsElapsed += Time.deltaTime;

        if (!(secondsElapsed > 1f))
        {
            return false;
        }

        secondsElapsed %= 1f;

        return true;
    }
}
