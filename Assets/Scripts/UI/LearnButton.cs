using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Simple button for toggling learning.
public class LearnButton : MonoBehaviour
{
    // Fields.
    #region Fields
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    [Space]
    [SerializeField] private Color startColor;
    [SerializeField] private Color stopColor;
    
    #endregion

    // Methods.
    #region Methods
    private void OnEnable() {
        QLearning.LearningChanged += OnLearningChanged;
    }
    
    private void OnDisable() {
        QLearning.LearningChanged -= OnLearningChanged;
    }

    private void OnLearningChanged(bool learning) {
        buttonImage.color = !learning ? startColor : stopColor;
        buttonText.text = !learning ? "Start" : "Stop";
    }

    public void OnClick() {
        QLearning.Instance.Learning = !QLearning.Instance.Learning; 
    }

    #endregion
}
