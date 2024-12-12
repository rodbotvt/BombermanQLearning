using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// The kind of statistics that kind be displayed.
public enum StatisticType {
    Epsilon = 0,
    Alpha,
    EpisodeCount,
    TurnCount,
    WinCount
}

// Simple output display for learning statistics.
[RequireComponent(typeof(TextMeshProUGUI))]
public class Statistic : MonoBehaviour
{
    // Fields.
    #region Fields
    private TextMeshProUGUI text;
    [SerializeField] private StatisticType type;

    #endregion

    // Methods.
    #region Methods
    private void Awake() {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable() {
        QLearning.EpisodeStepped += OnEpisodeStepped;   
    }

    private void OnDisable() {
        QLearning.EpisodeStepped -= OnEpisodeStepped;
    }

    private void OnEpisodeStepped() {
        switch(type) {
            case StatisticType.Epsilon: {
                text.text = string.Format("Current Epsilon: {0:F5}", QLearning.Instance.DecayedEpsilon);
                break;
            }
            case StatisticType.Alpha: {
                text.text = string.Format("Current Alpha: {0:F5}", QLearning.Instance.DecayedAlpha);
                break;
            }
            case StatisticType.EpisodeCount: {
                text.text = string.Format("Episode Count: {0}", QLearning.Instance.EpisodeCount);
                break;
            }
            case StatisticType.TurnCount: {
                text.text = string.Format("Turn Count In Episode: {0}", QLearning.Instance.TurnCount);
                break;
            }
            case StatisticType.WinCount: {
                text.text = string.Format("Total Wins: {0}", QLearning.Instance.WinCount);
                break;
            }
        }
    }

    #endregion
}
