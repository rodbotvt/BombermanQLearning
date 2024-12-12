using System;
using System.Collections;
using System.Collections.Generic;
using Agent;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Environment {
    // Defines the kinds of tiles that exist.
    public enum TileType {
        Walkable = 0,
        Unbreakable,
        Breakable,
        Explosion
    }

    // Defines the kinds of breakable wall patterns that can be generated.
    public enum BreakableWallPattern {
        Split = 0,
        Border = 2,
        Grid = 1
    }

    // Provides functionality for generating game boards.
    public class EnvironmentGenerator : MonoBehaviour
    {
        // Fields.
        #region Fields
        public static EnvironmentGenerator Instance;

        [Header("World")]
        [SerializeField] private Transform staticRoot;
        [SerializeField] private Transform dynamicRoot;
        private GameObject agentObject;

        [Header("Prefabs")]
        [SerializeField] private GameObject walkableTilePrefab;
        [SerializeField] private GameObject unbreakableWallPrefab;
        [SerializeField] private GameObject breakableWallPrefab;
        [SerializeField] private GameObject agentPrefab;

        [Header("Generation")]
        public Vector2Int Size = new Vector2Int(6, 6);
        public BreakableWallPattern BreakableWallPattern;
        public TileType[,] TileGrid;
        public short[,] BombTimerGrid;
        public short BreakableWallCount;

        [Header("Events")]
        public static Action BombsStepped;
        public static Action EnvironmentGenerated;
        public static Action<Vector2Int> WallBroken;

        #endregion

        // Methods.
        #region Methods
        private void Awake() {
            if(Instance != null) {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        // Whether the generator can place a breakable wall at the given position
        // according to the environment size and the given breakable wall pattern.
        private bool CanSpawnBreakableWall(Vector2Int position, BreakableWallPattern breakableWallPattern) {
            switch(breakableWallPattern) {
                case BreakableWallPattern.Split: {
                    bool onLeft = position.x < (int)(((float)Size.x) / 2);
                    bool onBottom = position.y < (int)(((float)Size.y) / 2);
                    return onLeft ^ onBottom;
                }
                case BreakableWallPattern.Border: {
                    return position.x == 0 || position.x == Size.x - 1 || position.y == 0 || position.y == Size.y - 1;
                }
                case BreakableWallPattern.Grid: {
                    if(position.y == 0)
                        return false;
                    return position.x % 2 == 0 && position.y % 2 == 1;
                }
                default: {
                    return false;
                }
            }
        }

        // Whether a given position corresponds to a tile within the environment bounds.
        public static bool TileInBounds(Vector2Int position, Vector2Int size) {
            if(position.x < 0 || position.x >= size.x
            || position.y < 0 || position.y >= size.y)
                return false;
            return true;
        }

        // Fetches a tile type from the grid.
        public TileType GetTileType(Vector2Int position) {
            // If position is out of bounds, it must be the border.
            if(!TileInBounds(position, Size))
                return TileType.Unbreakable;

            return TileGrid[position.x, position.y];
        }

        // Fetches a bomb timer from the grid.
        public short GetBombTimer(Vector2Int position) {
            // If position is out of bounds, it must be the border.
            if(!TileInBounds(position, Size))
                return 0;

            return BombTimerGrid[position.x, position.y];
        }

        // Sets the size of the game board.
        public void SetSize(int x, int y) {
            Size = new Vector2Int(x, y);
            GenerateEnvironment();
        }

        // Generates the environment based on a given size and breakable wall pattern.
        // Returns an instantiated agent model.
        private GameObject GenerateEnvironment(BreakableWallPattern BreakableWallPattern) {
            // Creates grids.
            TileGrid = new TileType[Size.x, Size.y];
            BombTimerGrid = new short[Size.x, Size.y];
            
            // Destroys the existing environment if applicable.
            for(int i = staticRoot.childCount - 1; i >= 0; i--)
                Destroy(staticRoot.GetChild(i).gameObject);
            for(int i = dynamicRoot.childCount - 1; i >= 0; i--)
                Destroy(dynamicRoot.GetChild(i).gameObject);
            
            // Instantiates static prefabs from (-1, -1) to (x+1, y+1).
            for(int x = -1; x < Size.x + 1; x++) {
                for(int y = -1; y < Size.y + 1; y++) {
                    // Instantiates.
                    bool isWall = x < 0 || x == Size.x || y < 0 || y == Size.y;
                    GameObject instance = Instantiate(isWall ? unbreakableWallPrefab : walkableTilePrefab, staticRoot);
                    instance.transform.position = new Vector3(x, 0, y);

                    // If a walkable tile, change color if applicable.
                    if(!isWall && (x % 2 == 1 ^ y % 2 == 1) && instance.TryGetComponent(out Renderer renderer))
                        renderer.material.color *= 0.9f;
                }
            }

            // Instantiates breakable prefabs from (0,0) to (x,y) according to pattern.
            BreakableWallCount = 0;
            for(int x = 0; x < Size.x; x++) {
                for(int y = 0; y < Size.y; y++) {
                    // Checks if can instantiate wall at this position, assigning the tile type.
                    if(!CanSpawnBreakableWall(new Vector2Int(x, y), BreakableWallPattern)) {
                        TileGrid[x, y] = TileType.Walkable;
                        continue;
                    }
                    TileGrid[x, y] = TileType.Breakable;
                    BreakableWallCount++;
                    
                    // Instantiates wall.
                    GameObject instance = Instantiate(breakableWallPrefab, dynamicRoot);
                    instance.transform.position = new Vector3(x, 0, y);
                }
            }

            // Positions camera properly.
            Camera.main.transform.position = new Vector3(((float)Size.x) / 2 - 0.5f, Camera.main.transform.position.y, ((float)Size.y) / 2 - 0.5f);

            // Places agent model at the spawn position.
            agentObject = Instantiate(agentPrefab, dynamicRoot);
            agentObject.transform.position = GetSpawnPosition(BreakableWallPattern);
            
            return agentObject;
        }

        public GameObject GenerateEnvironment() {
            return GenerateEnvironment(BreakableWallPattern);
        }

        // Fetches a spawn position for a given breakable wall pattern.
        private Vector3 GetSpawnPosition(BreakableWallPattern BreakableWallPattern) {
            switch(BreakableWallPattern) {
                case BreakableWallPattern.Border: {
                    return new Vector3((int)Size.x / 2, 0, (int)Size.y / 2);
                }
                default: {
                    return Vector3.zero;
                }
            }
        }

        // Clears all past explosion tiles.
        public void ClearExplosions() {
            for(int x = 0; x < Size.x; x++) {
                for(int y = 0; y < Size.y; y++) {
                    if(TileGrid[x,y].Equals(TileType.Explosion))
                        TileGrid[x,y] = TileType.Walkable;
                }
            }
        }

        // Steps all bomb timers, exploding them as necessary.
        // Returns the number of broken walls.
        public short StepBombs() {
            ClearExplosions();

            // Ticks down bomb timers.
            short brokenWalls = 0;
            for(int x = 0; x < Size.x; x++) {
                for(int y = 0 ; y < Size.y; y++) {
                    // Ignore tiles with no bombs.
                    if(BombTimerGrid[x,y] == 0)
                        continue;
                    
                    // Tick down timer.
                    BombTimerGrid[x,y]--;
                    if(BombTimerGrid[x,y] != 0)
                        continue;


                    // If the timer reached 0, cause an explosion and break walls appropriately.
                    Vector2Int position = new Vector2Int(x, y);
                    if(GetTileType(position).Equals(TileType.Breakable)) {
                        brokenWalls++;
                        WallBroken?.Invoke(position);
                    }
                    if(GetTileType(position + Vector2Int.up).Equals(TileType.Breakable)) {
                        brokenWalls++;
                        WallBroken?.Invoke(position + Vector2Int.up);
                    }
                    if(GetTileType(position + Vector2Int.down).Equals(TileType.Breakable)) {
                        brokenWalls++;
                        WallBroken?.Invoke(position + Vector2Int.down);
                    }
                    if(GetTileType(position + Vector2Int.right).Equals(TileType.Breakable)) {
                        brokenWalls++;
                        WallBroken?.Invoke(position + Vector2Int.right);
                    }
                    if(GetTileType(position + Vector2Int.left).Equals(TileType.Breakable)) {
                        brokenWalls++;
                        WallBroken?.Invoke(position + Vector2Int.left);
                    }

                    BreakableWallCount -= brokenWalls;

                    TileGrid[x,y] = TileType.Explosion;
                    if(TileInBounds(position + Vector2Int.up, Size))
                        TileGrid[x,y+1] = TileType.Explosion;
                    if(TileInBounds(position + Vector2Int.down, Size))
                        TileGrid[x,y-1] = TileType.Explosion;
                    if(TileInBounds(position + Vector2Int.right, Size))
                        TileGrid[x+1,y] = TileType.Explosion;
                    if(TileInBounds(position + Vector2Int.left, Size))
                        TileGrid[x-1,y] = TileType.Explosion;
                }
            }

            BombsStepped?.Invoke();
            return brokenWalls;
        }

        #endregion
    }
}


