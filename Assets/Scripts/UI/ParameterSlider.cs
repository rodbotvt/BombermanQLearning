using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Environment;

// Simple slider for altering hyperparameters.
public class ParameterSlider : MonoBehaviour
{
    // Fields.
    #region Fields
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private Slider slider;

    [Header("Data")]
    [SerializeField] private Hyperparameter parameter;
    [SerializeField] private Vector2 parameterRange;

    #endregion

    // Methods.
    #region Methods
    private void OnEnable() {
        QLearning.LearningChanged += OnLearningChanged;
    }

    private void OnDisable() {
        QLearning.LearningChanged -= OnLearningChanged;
    }

    // Toggle its usability when learning change states.
    private void OnLearningChanged(bool learning) {
        if(parameter.Equals(Hyperparameter.Timescale))
            return;
        slider.interactable = !learning;
        if(!learning)
            UpdateQLearning(false);
    }

    // Formats title according to the kind of hyperparameter this slider uses.
    private void FormatTitle(float value) {
        string readableString = string.Concat(parameter.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
        switch(parameter) {
            case Hyperparameter.WallPattern: {
                title.text = string.Format("{0} ({1})", readableString, (BreakableWallPattern)Mathf.RoundToInt(value));
                break;
            }
            case Hyperparameter.MaximumEpisodes:
            case Hyperparameter.MaximumTurns:
            case Hyperparameter.SizeX:
            case Hyperparameter.SizeY:
            {
                title.text = string.Format("{0} ({1})", readableString, Mathf.RoundToInt(value));
                break;
            }
            case Hyperparameter.ParameterDecay: {
                title.text = string.Format("{0} ({1})", readableString, Mathf.RoundToInt(value) != 0 ? "True" : "False");
                break;
            }
            case Hyperparameter.Timescale: {
                float scale = Mathf.Lerp(parameterRange.x, parameterRange.y, Mathf.Pow((value + parameterRange.x) / parameterRange.y, 2f));
                title.text = string.Format("{0} ({1})", readableString, scale > 1 ? (int)scale : scale);
                break;
            }
            default: {
                title.text = string.Format("{0} ({1})", readableString, value);
                break;
            }
        }
    }

    // Sets up slider.
    private void Start() {
        float startValue = 0;
        switch(parameter) {
            case Hyperparameter.Epsilon: {
                startValue = QLearning.Instance.Epsilon;
                break;
            }
            case Hyperparameter.Alpha: {
                startValue = QLearning.Instance.Alpha;
                break;
            }
            case Hyperparameter.Gamma: {
                startValue = QLearning.Instance.DiscountFactor;
                break;
            }
            case Hyperparameter.MaximumEpisodes: {
                startValue = QLearning.Instance.MaximumEpisodeCount;
                break;
            }
            case Hyperparameter.MaximumTurns: {
                startValue = QLearning.Instance.MaximumTurnsPerEpisode;
                break;
            }
            case Hyperparameter.Timescale: {
                startValue = parameterRange.y * Mathf.Pow(1 / parameterRange.y, 0.5f) - parameterRange.x;
                Time.timeScale = Mathf.Lerp(parameterRange.x, parameterRange.y, Mathf.Pow((startValue + parameterRange.x) / parameterRange.y, 2f));
                break;
            }
            case Hyperparameter.SizeX: {
                startValue = EnvironmentGenerator.Instance.Size.x;
                break;
            }
            case Hyperparameter.SizeY: {
                startValue = EnvironmentGenerator.Instance.Size.y;
                break;
            }
            case Hyperparameter.WallPattern: {
                startValue = (int)EnvironmentGenerator.Instance.BreakableWallPattern;
                break;
            }
            case Hyperparameter.ParameterDecay: {
                startValue = QLearning.Instance.ParameterDecay ? 1 : 0;
                break;
            }
        }
        slider.minValue = parameterRange.x;
        slider.maxValue = parameterRange.y;
        slider.value = startValue;

        // Sets up title.
        FormatTitle(startValue);
    }

    // Updates parameters of the QLearning instance.
    private void UpdateQLearning(bool canGenerate) {
        float value = slider.value;
        switch(parameter) {
            case Hyperparameter.Epsilon: {
                QLearning.Instance.Epsilon = value;
                break;
            }
            case Hyperparameter.Alpha: {
                QLearning.Instance.Alpha = value;
                break;
            }
            case Hyperparameter.Gamma: {
                QLearning.Instance.DiscountFactor = value;
                break;
            }
            case Hyperparameter.MaximumEpisodes: {
                slider.value = Mathf.RoundToInt(value);
                QLearning.Instance.MaximumEpisodeCount = (int)value;
                break;
            }
            case Hyperparameter.MaximumTurns: {
                slider.value = Mathf.RoundToInt(value);
                QLearning.Instance.MaximumTurnsPerEpisode = (int)value;
                break;
            }
            case Hyperparameter.Timescale: {
                Time.timeScale = Mathf.Lerp(parameterRange.x, parameterRange.y, Mathf.Pow((value + parameterRange.x) / parameterRange.y, 2f));
                break;
            }
            case Hyperparameter.SizeX: {
                value = value % 2 == 1 ? value + 1 : value;
                if(canGenerate)
                    EnvironmentGenerator.Instance.SetSize(Mathf.RoundToInt(value), EnvironmentGenerator.Instance.Size.y);
                break;
            }
            case Hyperparameter.SizeY: {
                value = value % 2 == 1 ? value + 1 : value;
                if(canGenerate)
                    EnvironmentGenerator.Instance.SetSize(EnvironmentGenerator.Instance.Size.x, Mathf.RoundToInt(value));
                break;
            }
            case Hyperparameter.WallPattern: {
                EnvironmentGenerator.Instance.BreakableWallPattern = (BreakableWallPattern)Mathf.RoundToInt(value);
                if(canGenerate)
                    EnvironmentGenerator.Instance.GenerateEnvironment();
                break;
            }
            case Hyperparameter.ParameterDecay: {
                QLearning.Instance.ParameterDecay = value == 1 ? true : false;
                break;
            }
        }
    }

    // Called when the slider value is updated in the UI.
    public void SliderValueChanged() {
        UpdateQLearning(true);
        FormatTitle(slider.value);
    }
    
    #endregion
}
