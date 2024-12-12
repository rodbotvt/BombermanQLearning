using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Environment;
using Agent;
using System.Linq;

// The hyperparameters the user can modify.
public enum Hyperparameter {
    Epsilon = 0,
    Alpha,
    Gamma,
    MaximumEpisodes,
    MaximumTurns,
    Timescale,
    SizeX,
    SizeY,
    WallPattern,
    ParameterDecay
}

// Implementation of a Q-table.
public class QTable {
    // Fields.
    #region Fields
    public Dictionary<AgentState, float[]> qTable = new();

    #endregion

    // Methods.
    #region Methods
    // Creates an array of Q-values with enough space for each possible action.
    private float[] CreateQValueArray() {
        return new float[Enum.GetNames(typeof(AgentAction)).Length];
    }

    // Fetches an array of Q-values for a given state if it exists, otherwise uses all zeroes.
    public float[] GetQValues(AgentState state) {
        // If the Q-table contains an entry for the given state, use it.
        if(qTable.TryGetValue(state, out float[] qValues)) {
            return qValues;
        }

        // Otherwise initialize the Q-values and return them.
        qValues = CreateQValueArray();
        qTable.Add(state, qValues);
        return qValues;
    }

    // Sets a Q-value for a given state and action.
    public void SetQValue(AgentState state, AgentAction action, float value) {
        // If the Q-table contains an entry for the given state, fetch it.
        if(qTable.TryGetValue(state, out float[] qValues)) {
            qValues[(int)action] = value;
            return;
        }

        // Otherwise initialize the Q-values.
        qValues = CreateQValueArray();
        qValues[(int)action] = value;
        qTable.Add(state, qValues);
    }

    #endregion
}

// Main class that handles learning.
public class QLearning : MonoBehaviour
{
    // Fields.
    #region Fields
    public static QLearning Instance;

    [Header("Hyperparameters")]
    [Range(0, 1)] public float Epsilon; 
    public float DecayedEpsilon;
    [Range(0, 1)] public float Alpha;
    public float DecayedAlpha;
    [Range(0.01f, 1)] public float DiscountFactor;
    [Range(100, 100000)] public int MaximumEpisodeCount;
    public int MaximumTurnsPerEpisode;
    public bool ParameterDecay;
    public int EpisodeCount = 0;
    public int TurnCount = 0;
    public int WinCount = 0;
    public float OverallWinRate = 0;
    public float SnapshotWinRate = 0;
    public static Action EpisodeStepped;

    [Header("Reward")]
    [Tooltip("The reward for staying alive without taking any interesting action.")] public float AliveReward = -1;
    [Tooltip("The reward for placing a bomb next to breakable walls.")] public float EffectiveBombReward = 5;
    [Tooltip("The reward for placing a bomb next to zero breakable walls.")] public float IneffectiveBombReward = -5;
    [Tooltip("The reward for breaking a wall tile.")] public float BreakWallReward = 20;
    [Tooltip("The reward for dying.")] public float DeathReward = -100;

    [Header("Internal")]
    private QTable qTable;
    private AgentBehavior agentBehavior;
    private AgentState agentState;
    private Queue<bool> snapshotWins = new();
    private readonly int snapshotSize = 100;
    private bool learning = false;
    public bool Learning {
        get { return learning; }
        set { 
            if(value)
                Initialize();
            else {
                DecayedEpsilon = 0;
                DecayedAlpha = 0;
            }
            learning = value;
            LearningChanged?.Invoke(learning);
        }
    }
    public static Action<bool> LearningChanged;

    #endregion

    // Methods.
    #region Methods

    // Initializes 
    private void Awake() {
        if(Instance != null) {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // Initializes a learning attempt.
    private void Initialize() {
        // Initializes Q-table.
        qTable = new();

        // Generate the environment, fetching the returned agent.
        agentBehavior = GetComponent<AgentBehavior>();
        agentBehavior.ModelTransform = EnvironmentGenerator.Instance.GenerateEnvironment().transform;
        agentBehavior.GenerateBombPool();

        EpisodeCount = 0;
        TurnCount = 0;
        WinCount = 0;
        OverallWinRate = 0;
        SnapshotWinRate = 0;
        snapshotWins.Clear();
        
        // Fetches initial state
        agentState = agentBehavior.GetCurrentState();
    }

    // Starts a new episode if applicable, otherwise stops learning.
    private void Restart() {
        // Generate the environment, fetching the returned agent.
        agentBehavior.ModelTransform = EnvironmentGenerator.Instance.GenerateEnvironment().transform;
        agentBehavior.GenerateBombPool();
        TurnCount = 0;
        
        // Fetches initial state
        agentState = agentBehavior.GetCurrentState();

        if(EpisodeCount > MaximumEpisodeCount) {
            Learning = false;
        }
    }

    // Main Q-learning loop.
    private void FixedUpdate() {
        if(qTable == null || agentBehavior.ModelTransform == null)
            return;
            
        // Decays hyperparameters.
        if(learning) {
            if(ParameterDecay) {
                DecayedEpsilon = Mathf.Lerp(Epsilon, 0.001f, Mathf.Clamp01((float)EpisodeCount / MaximumEpisodeCount));
                DecayedAlpha = Mathf.Lerp(Alpha, 0.001f, Mathf.Clamp01((float)EpisodeCount / MaximumEpisodeCount));
                
            } else {
                DecayedEpsilon = Epsilon;
                DecayedAlpha = Alpha;
            }
        } else {
            DecayedEpsilon = 0;
            DecayedAlpha = 0;
        }
        float epsilon = DecayedEpsilon;
        float alpha = DecayedAlpha;
            
        // Fetches legal actions.
        List<AgentAction> legalActions = agentBehavior.GetLegalActions(); 
        
        // Chooses an action according to exploration or exploitation.
        // If exploring, choose a random action.
        // If exploiting, choose the action with the highest Q-value from this state.
        int legalActionIndex;
        float[] qValues = qTable.GetQValues(agentState);
        if(UnityEngine.Random.value < epsilon) {
            legalActionIndex = UnityEngine.Random.Range(0, legalActions.Count);
        }
        else {
            float largestQValue = float.NegativeInfinity;
            int tempIndex = 0;
            for(int i = 0; i < qValues.Length; i++) {
                if(!legalActions.Contains((AgentAction)i))
                    continue;

                float value = qValues[i];
                if(value > largestQValue) {
                    largestQValue = value;
                    tempIndex = i;
                }
            }

            AgentAction tempAction = (AgentAction)tempIndex;
            legalActionIndex = legalActions.IndexOf(tempAction);
        }
        AgentAction action = legalActions[legalActionIndex];

        // Perform the action, fetching the next state and instant reward.
        Tuple<AgentState, float> tuple = agentBehavior.TakeAction(action);
        AgentState nextState = tuple.Item1;
        float instantReward = tuple.Item2;

        // Perform the Q-value update.
        float oldQValue = qValues[(int)action];
        float nextMaxQValue = qTable.GetQValues(nextState).Max();
        float newQValue = oldQValue + alpha * (instantReward + DiscountFactor * nextMaxQValue - oldQValue);
        qTable.SetQValue(agentState, action, newQValue);

        // Updates the current state.
        agentState = nextState;
        TurnCount++;

        // If the agent died or destroyed all walls, terminate the episode.
        // Regenerate the environment and start again.
        if(instantReward == DeathReward || EnvironmentGenerator.Instance.BreakableWallCount == 0 || TurnCount > MaximumTurnsPerEpisode) {
            bool win = EnvironmentGenerator.Instance.BreakableWallCount == 0; 
            if(win)
                WinCount++;

            if(snapshotWins.Count == snapshotSize)
                snapshotWins.Dequeue();
            snapshotWins.Enqueue(win);

            EpisodeCount++;
            OverallWinRate = ((float)WinCount) / EpisodeCount;
            SnapshotWinRate = ((float)snapshotWins.Count(x => x)) / Mathf.Min(EpisodeCount, snapshotSize);
            Restart();
        }
        EpisodeStepped?.Invoke();
    }
    #endregion
}
