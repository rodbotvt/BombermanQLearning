using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Environment;

// Defines cosmetic behavior for a single bomb.
public class BombBehavior : MonoBehaviour
{
    // Fields.
    #region Fields
    [Header("Internal")]
    // A timer used for removing the cosmetic bomb object.
    private short timer;
    private short initialTimer;
    [SerializeField] private Renderer meshRenderer;

    #endregion

    // Properties.
    #region Properties
    public short Timer {
        get { return timer; }
        set { timer = value;
            initialTimer = value;}
    }
    #endregion

    // Methods.
    #region Methods
    private void OnEnable() {
        EnvironmentGenerator.BombsStepped += OnBombsStepped;
    }

    private void OnDisable() {
        EnvironmentGenerator.BombsStepped -= OnBombsStepped;
    }

    private void OnBombsStepped() {
        timer--;
        if(timer == 0) {
            gameObject.SetActive(false);
        } else {
            if(timer != initialTimer && timer % 2 != initialTimer % 2)
                meshRenderer.material.EnableKeyword("_EMISSION");
            else
                meshRenderer.material.DisableKeyword("_EMISSION");
        }
    }
    #endregion
}
