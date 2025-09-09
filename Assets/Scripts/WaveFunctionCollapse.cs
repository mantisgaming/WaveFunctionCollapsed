using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class WaveFunctionCollapse : MonoBehaviour {
    public WFRules rulesFile;

    private const int REGION_SIZE = 3;
    private const int STEP_SIZE = 1;

    [SerializeField]
    [Tooltip("All values must be greater than 0")]
    public Vector3Int size = new Vector3Int(32, 32, 3);

    private Vector3Int regions => new Vector3Int(
        Mathf.Max((size.x - REGION_SIZE) / STEP_SIZE, 0) + 1,
        Mathf.Max((size.y - REGION_SIZE) / STEP_SIZE, 0) + 1,
        Mathf.Max((size.z - REGION_SIZE) / STEP_SIZE, 0) + 1);

    private int regionCount => regions.x * regions.y * regions.z;

    private Tilemap tilemap;

    [SerializeField]
    private TileBase air;

    [SerializeField]
    [Tooltip("Will be placed at (0,0,0) as the seed")]
    private TileBase startingTile;

    private List<TileBase>[,,] m_waveTable;
    private int m_completedRegions = 0;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    private void Start() {
        tilemap.ClearAllTiles();

        ResetWaveTable();
        PlaceInitialTile();
    }

    private void Update() {
        if (m_completedRegions < regionCount) {
            Vector3Int regionPos = new(
                m_completedRegions % regions.x * STEP_SIZE,
                (m_completedRegions / regions.x) % regions.y * STEP_SIZE,
                (m_completedRegions / regions.x / regions.y) % regions.z * STEP_SIZE);

            int i = 0;

            for (i = 0; i < 10; i++) {
                try {
                    CollapseRegion(regionPos);
                    i = 0;
                    break;
                } catch {}
            }

            if (i != 0) {
                Debug.Log("Failed to collapse");
                m_completedRegions = regionCount;
            }

            m_completedRegions++;
        }

        if (m_completedRegions == regionCount) {
            for (int i = 0; i < size.x; i++) {
                for (int j = 0; j < size.y; j++) {
                    for (int k = 0; k < size.z; k++) {
                        if (tilemap.GetTile(new Vector3Int(i, j, k)) == air) {
                            tilemap.SetTile(new Vector3Int(i, j, k), null);
                        }
                    }
                }
            }
            m_completedRegions++;
        }
    }

    private void CollapseRegion(Vector3Int position) {

        Debug.Log($"Collapsing region: {position}");

        // clear region
        for (int i = position.x; i < size.x && i < size.x; i++) {
            for (int j = position.y; j < size.y && j < size.y; j++) {
                for (int k = position.z; k < size.z && k < size.z; k++) {
                    if (tilemap.GetTile(new Vector3Int(i, j, k)) != null) {
                        tilemap.SetTile(new Vector3Int(i, j, k), null);
                        m_waveTable[i, j, k] = new List<TileBase>();
                        for (int w = 0; w < rulesFile.rules.Count; w++) {
                            m_waveTable[i, j, k].Add(rulesFile.rules[w].baseTile);
                        }
                    }
                }
            }
        }

        Vector3Int maxPos = new(
            Mathf.Min(position.x + REGION_SIZE, size.x),
            Mathf.Min(position.y + REGION_SIZE, size.y),
            Mathf.Min(position.z + REGION_SIZE, size.z));


        if (position == Vector3Int.zero)
            PlaceInitialTile();

        for (int i = position.x; i < position.x + REGION_SIZE && i < size.x; i++) {
            for (int j = position.y; j < position.y + REGION_SIZE && j < size.y; j++) {
                for (int k = position.z; k < position.z + REGION_SIZE && k < size.z; k++) {
                    UpdateWavetableFrom(new Vector3Int(i, j, k));
                }
            }
        }

        for (int i = position.x; i < position.x + REGION_SIZE && i < size.x; i++) {
            for (int j = position.y; j < position.y + REGION_SIZE && j < size.y; j++) {
                for (int k = position.z; k < position.z + REGION_SIZE && k < size.z; k++) {
                    CollapseTile(position, maxPos);
                }
            }
        }
    }

    private void ResetWaveTable() {
        m_waveTable = new List<TileBase>[size.x, size.y, size.z];
        m_completedRegions = 0;

        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                for (int k = 0; k < size.z; k++) {
                    m_waveTable[i, j, k] = new List<TileBase>();
                    for (int w = 0; w < rulesFile.rules.Count; w++) {
                        m_waveTable[i, j, k].Add(rulesFile.rules[w].baseTile);
                    }
                }
            }
        }
    }

    private void PlaceInitialTile() {
        tilemap.SetTile(new Vector3Int(0, 0, 0), startingTile);
        m_waveTable[0, 0, 0].Clear();
        m_waveTable[0, 0, 0].Add(startingTile);
        UpdateWavetableFrom(new Vector3Int(0, 0, 0));
    }

    private void UpdateWavetableFrom(Vector3Int position, int depth = 0) {
        if (depth >= 2) return;

        if (position.x > 0 && position.x < size.x - 1 &&
            position.y > 0 && position.y < size.y - 1 &&
            position.z > 0 && position.z < size.z - 1) {

            List<TileBase>[,,] validTiles = new List<TileBase>[3, 3, 3];

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 3; k++) {
                        validTiles[i, j, k] = new List<TileBase>();
                    }
                }
            }
            
            foreach (Rule rule in rulesFile.rules) {
                foreach (KernelRule kernel in rule.kernelRules) {
                    if (DoesRuleFit(kernel, position)) {
                        UnionKernel(kernel, ref validTiles);
                    }
                }
            }

            for (int k = -1; k <= 1; k++) {
                for (int j = -1; j <= 1; j++) {
                    for (int i = -1; i <= 1; i++) {
                        Vector3Int offset = new(i, j, k);
                        Vector3Int target = offset + position;

                        List<TileBase> impossibleTiles = new List<TileBase>();
                        foreach (TileBase tile in m_waveTable[target.x, target.y, target.z]) {
                            if (!validTiles[i+1,j+1,k+1].Contains(tile))
                                impossibleTiles.Add(tile);
                        }

                        foreach (TileBase tile in impossibleTiles) {
                            m_waveTable[target.x, target.y, target.z].Remove(tile);
                        }
                    }
                }
            }
        }

        for (int k = -1; k <= 1; k++) {
            for (int j = -1; j <= 1; j++) {
                for (int i = -1; i <= 1; i++) {
                    Vector3Int offset = new(i, j, k);
                    Vector3Int target = offset + position;

                    if (target.x < 0 || target.y < 0 || target.z < 0 ||
                        target.x >= size.x || target.y >= size.y || target.z >= size.z)
                        continue;

                    if (offset == Vector3Int.zero) continue;

                    UpdateWavetableFrom(target, depth + 1);
                }
            }
        }
    }

    private bool DoesRuleFit(KernelRule kernel, Vector3Int position) {
        for (int k = -1; k <= 1; k++) {
            for (int j = -1; j <= 1; j++) {
                for (int i = -1; i <= 1; i++) {
                    Vector3Int offset = new(i, j, k);
                    Vector3Int target = offset + position;

                    TileBase ruleTile = kernel.getTileAt(offset + Vector3Int.one);
                    if (!m_waveTable[target.x, target.y, target.z].Contains(ruleTile))
                        return false;
                }
            }
        }

        return true;
    }

    private void UnionKernel(KernelRule kernel, ref List<TileBase>[,,] tiles) {
        for (int k = 0; k < 3; k++) {
            for (int j = 0; j < 3; j++) {
                for (int i = 0; i < 3; i++) {
                    TileBase kernelTile = kernel.getTileAt(new Vector3Int(i, j, k));

                    if (!tiles[i, j, k].Contains(kernelTile))
                        tiles[i, j, k].Add(kernelTile);
                }
            }
        }
    }

    private void CollapseTile(Vector3Int min, Vector3Int max) {

        // find a random lowest entropy tile
        int lowestEntropy = -1;
        List<Vector3Int> selectedTiles = new List<Vector3Int>();

        bool full = true;

        for (int i = min.x; i < max.x; i++) {
            for (int j = min.y; j < max.y; j++) {
                for (int k = min.z; k < max.z; k++) {
                    int entropy = m_waveTable[i, j, k].Count;
                    
                    if (tilemap.GetTile(new Vector3Int(i,j,k)) != null)
                        continue;

                    full = false;

                    // Ignore tiles that have no possible collapsed state
                    if (entropy == 0) continue;

                    if (entropy < lowestEntropy || lowestEntropy == -1) {
                        selectedTiles.Clear();

                        lowestEntropy = entropy;
                        selectedTiles.Add(new Vector3Int(i, j, k));
                    } else if (entropy == lowestEntropy) {
                        selectedTiles.Add(new Vector3Int(i, j, k));
                    }
                }
            }
        }

        Debug.Log($"Lowest Entropy: {lowestEntropy}");

        if (lowestEntropy == -1 && !full) {
            // Abort
            throw new System.Exception("Failed to collapse");
        }

        if (lowestEntropy == -1)
            return;

        Vector3Int selectedTilePosition = selectedTiles[Random.Range(0, selectedTiles.Count)];
        List<TileBase> options = m_waveTable[
            selectedTilePosition.x,
            selectedTilePosition.y,
            selectedTilePosition.z
        ];

        TileBase selectedTile = options[Random.Range(0, options.Count)];

        tilemap.SetTile(selectedTilePosition, selectedTile);
        m_waveTable[
            selectedTilePosition.x,
            selectedTilePosition.y,
            selectedTilePosition.z
        ].Clear();
        m_waveTable[
            selectedTilePosition.x,
            selectedTilePosition.y,
            selectedTilePosition.z
        ].Add(selectedTile);

        // update wavetable from the tile
        UpdateWavetableFrom(selectedTilePosition);
    }
}
