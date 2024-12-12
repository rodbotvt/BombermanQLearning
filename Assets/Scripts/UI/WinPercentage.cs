using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Simple piechart-like element for displaying win rates.
public class WinPercentage : MonoBehaviour
{
    // Fields.
    #region Fields 
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool useSnapshot;

    #endregion

    // Methods.
    #region Methods
    private void OnEnable() {
        QLearning.EpisodeStepped += OnEpisodeStepped;
    }

    private void OnDisable() {
        QLearning.EpisodeStepped -= OnEpisodeStepped;
    }

    private void OnEpisodeStepped() {
        float rate = useSnapshot ? QLearning.Instance.SnapshotWinRate : QLearning.Instance.OverallWinRate;
        text.text = string.Format("{0}%", Mathf.RoundToInt(rate * 100));
        fillImage.fillAmount = Mathf.Clamp01(rate);
    }
    #endregion
}
