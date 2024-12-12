using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Environment;

// Defines cosmetic behavior for a single breakable wall.
public class BreakableWallBehavior : MonoBehaviour
{
    // Fields.
    #region  Fields
    [SerializeField] private GameObject model;

    #endregion

    // Methods.
    #region  Methods
    private void OnEnable() {
        EnvironmentGenerator.EnvironmentGenerated += OnEnvironmentGenerated;
        EnvironmentGenerator.WallBroken += OnWallBroken;
    }

    private void OnDisable() {
        EnvironmentGenerator.EnvironmentGenerated -= OnEnvironmentGenerated;
        EnvironmentGenerator.WallBroken -= OnWallBroken;
    }

    // Turns its model on when the environment is generated.
    private void OnEnvironmentGenerated() {
        model.SetActive(true);
    }

    // Turns its model off when destroyed.
    private void OnWallBroken(Vector2Int position) {
        if(transform.position.x == position.x && transform.position.z == position.y)
            model.SetActive(false);
    }
    #endregion
}
