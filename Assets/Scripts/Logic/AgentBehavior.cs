using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Environment;
using System;
using Unity.VisualScripting;

namespace Agent {
    // All actions the agent can possibly take.
    public enum AgentAction {
        None = 0,
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        BombUp,
        BombDown,
        BombLeft,
        BombRight
    }

    // Defines the game state for a given agent.
    [Serializable]
    public class AgentState : IEquatable<AgentState> {
        // Fields.
        #region  Fields

        public Vector2Int AgentPosition;
        
        // A rectangular array containing the TileType of each position in the grid.
        public TileType[,] TileGrid;

        // A rectangular array containing the explosion countdown of each position in the grid.
        // This is different from bomb timers.
        public short[,] ExplosionTimers;

        #endregion

        // Methods.
        #region Methods
        public AgentState(Vector2Int size) {
            TileGrid = new TileType[size.x, size.y];
            ExplosionTimers = new short[size.x, size.y];
        }

        // The following functions are necessary for proper use of states as hashmap keys.
        public override int GetHashCode()
        {
            int hash = 17;

            // Hash AgentPosition
            hash = hash * 23 + AgentPosition.GetHashCode();

            // Hash TileGrid
            foreach (var tile in TileGrid)
                hash = hash * 23 + tile.GetHashCode();

            // Hash ExplosionTimers
            foreach (var timer in ExplosionTimers)
                hash = hash * 23 + timer.GetHashCode();

            return hash;
        }

        public bool Equals(AgentState other)
        {
            if(!AgentPosition.Equals(other.AgentPosition))
                return false;

            for(int x = 0; x < EnvironmentGenerator.Instance.Size.x; x++) {
                for(int y = 0; y < EnvironmentGenerator.Instance.Size.y; y++) {
                    if(!TileGrid[x,y].Equals(other.TileGrid[x,y]))
                        return false;
                    if(!ExplosionTimers[x,y].Equals(other.ExplosionTimers[x,y]))
                        return false;
                }
            }

            return true;
        }

        #endregion
    }

    // Defines the behavior of a single agent.
    public class AgentBehavior : MonoBehaviour
    {
        // Fields.
        #region Fields
        [Header("Self")]
        [HideInInspector] public Transform ModelTransform;

        [Header("Bomb")]
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] [Range(1, 5)] private short bombTimer;
        [SerializeField] private short currentBombTimer = 0;
        private GameObject[] bombPool;

        #endregion

        // Methods.
        #region Methods
        private void Awake() {
            bombPool = new GameObject[bombTimer];
        }

        private void OnEnable() {
            EnvironmentGenerator.BombsStepped += OnBombsStepped;
        }

        private void OnDisable() {
            EnvironmentGenerator.BombsStepped -= OnBombsStepped;
        }

        private void OnBombsStepped() {
            currentBombTimer = (short)Mathf.Max(0, currentBombTimer - 1);
        }

        // Generates a pool of bomb GameObjects to avoid constantly instantiating new ones.
        public void GenerateBombPool() {
            for(int i = 0; i < bombPool.Length; i++) {
                GameObject bombObject = Instantiate(bombPrefab, ModelTransform.parent);
                bombObject.SetActive(false);
                bombPool[i] = bombObject;
            }
        }

        // Whether the agent can move in a given delta direction.
        private bool CanMoveToDeltaTile(Vector2Int delta) {
            return EnvironmentGenerator.Instance.GetTileType(new Vector2Int((int)ModelTransform.position.x, (int)ModelTransform.position.z) + delta).Equals(TileType.Walkable);
        }

        // Returns a list of legal actions from the agent's current status.
        public List<AgentAction> GetLegalActions() {
            // Initializes list.
            List<AgentAction> legalActions = new()
            {
                AgentAction.None
            };

            // Checks directions.
            if(CanMoveToDeltaTile(Vector2Int.up)) {
                legalActions.Add(AgentAction.MoveUp);
                if(currentBombTimer == 0)
                    legalActions.Add(AgentAction.BombUp);
            }

            if(CanMoveToDeltaTile(Vector2Int.down)) {
                legalActions.Add(AgentAction.MoveDown);
                if(currentBombTimer == 0)
                    legalActions.Add(AgentAction.BombDown);
            }

            if(CanMoveToDeltaTile(Vector2Int.right)) {
                legalActions.Add(AgentAction.MoveRight);
                if(currentBombTimer == 0)
                    legalActions.Add(AgentAction.BombRight);
            }

            if(CanMoveToDeltaTile(Vector2Int.left)) {
                legalActions.Add(AgentAction.MoveLeft);
                if(currentBombTimer == 0)
                    legalActions.Add(AgentAction.BombLeft);
            }

            return legalActions;
        }

        // Constructs a state object based on the agent's current status.
        public AgentState GetCurrentState() {
            Vector2Int size = EnvironmentGenerator.Instance.Size;
            AgentState state = new(size);

            Vector2Int position = new Vector2Int((int)ModelTransform.position.x, (int)ModelTransform.position.z);
            state.AgentPosition = position;
            state.TileGrid = new TileType[size.x, size.y];
            for(int x = 0; x < size.x; x++) {
                for(int y = 0 ; y < size.y; y++) {
                    state.TileGrid[x,y] = EnvironmentGenerator.Instance.TileGrid[x,y];
                    short timer = EnvironmentGenerator.Instance.BombTimerGrid[x,y];
                    if(timer == 0)
                        continue;
                
                    Vector2Int currPos = new Vector2Int(x, y);
                    if(!EnvironmentGenerator.Instance.GetTileType(currPos).Equals(TileType.Unbreakable)) {
                        state.ExplosionTimers[x,y] = (short)Mathf.Min(timer, state.ExplosionTimers[x,y] == 0 ? 100 : state.ExplosionTimers[x,y]);
                    }
                    if(!EnvironmentGenerator.Instance.GetTileType(currPos + Vector2Int.up).Equals(TileType.Unbreakable)) {
                        state.ExplosionTimers[x,y+1] = (short)Mathf.Min(timer, state.ExplosionTimers[x,y+1] == 0 ? 100 : state.ExplosionTimers[x,y+1]);
                    }
                    if(!EnvironmentGenerator.Instance.GetTileType(currPos + Vector2Int.down).Equals(TileType.Unbreakable)) {
                        state.ExplosionTimers[x,y-1] = (short)Mathf.Min(timer, state.ExplosionTimers[x,y-1] == 0 ? 100 : state.ExplosionTimers[x,y-1]);
                    }
                    if(!EnvironmentGenerator.Instance.GetTileType(currPos + Vector2Int.right).Equals(TileType.Unbreakable)) {
                        state.ExplosionTimers[x+1,y] = (short)Mathf.Min(timer, state.ExplosionTimers[x+1,y] == 0 ? 100 : state.ExplosionTimers[x+1,y]);
                    }
                    if(!EnvironmentGenerator.Instance.GetTileType(currPos + Vector2Int.left).Equals(TileType.Unbreakable)) {
                        state.ExplosionTimers[x-1,y] = (short)Mathf.Min(timer, state.ExplosionTimers[x-1,y] == 0 ? 100 : state.ExplosionTimers[x-1,y]);
                    }
                }
            }

            return state;
        }

        // Computes the reward for a given state-action-state pair.
        private float ComputeReward(AgentState state, AgentAction action, AgentState nextState, Vector3 delta, short brokenWalls) {
            // Death by explosion. Reward accordingly.
            if(EnvironmentGenerator.Instance.GetTileType(nextState.AgentPosition).Equals(TileType.Explosion))
                return QLearning.Instance.DeathReward;

            // Base reward is equal to the alive reward plus the reward for breaking walls.
            float reward = QLearning.Instance.AliveReward;
            reward += brokenWalls * QLearning.Instance.BreakWallReward; 

            // If placing a bomb, check if there are breakable walls next to it. Reward accordingly.
            if(action.ToString().Contains("Bomb")) {
                Vector2Int bombPosition = new Vector2Int((int)(nextState.AgentPosition.x + delta.x), (int)(nextState.AgentPosition.y + delta.z));
                bool nextToWall = EnvironmentGenerator.Instance.GetTileType(bombPosition).Equals(TileType.Breakable);
                nextToWall |= EnvironmentGenerator.Instance.GetTileType(bombPosition + Vector2Int.up).Equals(TileType.Breakable);
                nextToWall |= EnvironmentGenerator.Instance.GetTileType(bombPosition + Vector2Int.down).Equals(TileType.Breakable);
                nextToWall |= EnvironmentGenerator.Instance.GetTileType(bombPosition + Vector2Int.right).Equals(TileType.Breakable);
                nextToWall |= EnvironmentGenerator.Instance.GetTileType(bombPosition + Vector2Int.left).Equals(TileType.Breakable);

                reward += nextToWall ? QLearning.Instance.EffectiveBombReward : QLearning.Instance.IneffectiveBombReward;
            } else if(!action.Equals(AgentAction.None)) {
                // If moving, check if moving closer to a breakable wall and reward accordingly.
                short oldShortestSteps = short.MaxValue;
                float newShortestSteps = short.MaxValue;
                for(int x = 0; x < EnvironmentGenerator.Instance.Size.x; x++) {
                    for(int y = 0; y < EnvironmentGenerator.Instance.Size.y; y++) {
                        if(state.TileGrid[x, y].Equals(TileType.Breakable))
                            oldShortestSteps = (short)Mathf.Min(oldShortestSteps, Mathf.Abs(state.AgentPosition.x - x) + Mathf.Abs(state.AgentPosition.y - y));

                        if(nextState.TileGrid[x, y].Equals(TileType.Breakable))
                            newShortestSteps = (short)Mathf.Min(newShortestSteps, Mathf.Abs(nextState.AgentPosition.x - x) + Mathf.Abs(nextState.AgentPosition.y - y));
                    }
                }

                if(oldShortestSteps > newShortestSteps)
                    reward += 5;
                else if(oldShortestSteps < newShortestSteps)
                    reward -= 5;

                // Additionally, reward the agent if they avoided an explosion.
                if(state.ExplosionTimers[state.AgentPosition.x, state.AgentPosition.y] == 1 && state.AgentPosition.Equals(nextState.AgentPosition))
                    reward += 10;
            } else {
                reward = QLearning.Instance.AliveReward;
            }

            return reward;
        }

        // Takes an action, returning a tuple containing:
        // 1. The resulting state.
        // 2. The instantaneous reward.
        public Tuple<AgentState, float> TakeAction(AgentAction action) {
            EnvironmentGenerator.Instance.ClearExplosions();
            AgentState state = GetCurrentState();

            // Determines which action to take and in what direction.
            Vector3 delta = Vector3.zero;
            bool spawnBomb = false;
            switch(action) {
                case AgentAction.MoveUp: {
                    delta = Vector3.forward;
                    break;
                }
                case AgentAction.MoveDown: {
                    delta = Vector3.back;
                    break;
                }
                case AgentAction.MoveRight: {
                    delta = Vector3.right;
                    break;
                }
                case AgentAction.MoveLeft: {
                    delta = Vector3.left;
                    break;
                }
                case AgentAction.BombUp: {
                    delta = Vector3.forward;
                    spawnBomb = true;
                    break;
                }
                case AgentAction.BombDown: {
                    delta = Vector3.back;
                    spawnBomb = true;
                    break;
                }
                case AgentAction.BombRight: {
                    delta = Vector3.right;
                    spawnBomb = true;
                    break;
                }
                case AgentAction.BombLeft: {
                    delta = Vector3.left;
                    spawnBomb = true;
                    break;
                }
            }

            short brokenWalls;
            if(spawnBomb) {
                // Steps current bombs before placing down a new one.
                brokenWalls = EnvironmentGenerator.Instance.StepBombs();
                currentBombTimer = bombTimer;

                // Places cosmetic bomb from pool.
                foreach(GameObject bombObject in bombPool) {
                    if(bombObject.activeInHierarchy)
                        continue;
                    bombObject.transform.position = ModelTransform.position + delta;
                    bombObject.SetActive(true);
                    if(bombObject.TryGetComponent(out BombBehavior bomb))
                        bomb.Timer = bombTimer;
                    break;
                }

                // Updates grids.
                Vector2Int bombPosition = new Vector2Int((int)(ModelTransform.position + delta).x, (int)(ModelTransform.position + delta).z);
                EnvironmentGenerator.Instance.TileGrid[bombPosition.x, bombPosition.y] = TileType.Unbreakable;
                EnvironmentGenerator.Instance.BombTimerGrid[bombPosition.x, bombPosition.y] = bombTimer;
            } else {
                // Moves agent.
                ModelTransform.position += delta;

                // Steps current bombs after moving.
                brokenWalls = EnvironmentGenerator.Instance.StepBombs();
            }

            AgentState nextState = GetCurrentState();

            // If the agent is caught in an explosion, return the death reward.
            // Otherwise, compute the reward from the amount of destroyed walls.
            float reward = ComputeReward(state, action, nextState, delta, brokenWalls);
            
            // Returns the current state.
            return new Tuple<AgentState, float>(nextState, reward);
        }

        #endregion
    }
}

