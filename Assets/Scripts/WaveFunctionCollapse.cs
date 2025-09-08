using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class WaveFunctionCollapse : MonoBehaviour {
    public WFRules rulesFile;

    [SerializeField]
    [Tooltip("All values must be greater than 0")]
    public Vector3Int size = new Vector3Int(32, 32, 3);

    private Tilemap tilemap;

    [SerializeField]
    private TileBase air;

    [SerializeField]
    [Tooltip("Will be placed at (0,0,0) as the seed")]
    private TileBase startingTile;

    private List<TileBase>[,,] m_waveTable;
    private int m_remainingTiles = 0;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    private void Start() {
        tilemap.ClearAllTiles();

        ResetWaveTable();
        PlaceInitialTile();
    }

    private void FixedUpdate() {
        for (int i = 0; i < 16; i++) {
            if (m_remainingTiles > 0) {
                CollapseTile();
                m_remainingTiles--;
            }
        }

        if (m_remainingTiles == 0) {
            for (int i = 0; i < size.x; i++) {
                for (int j = 0; j < size.y; j++) {
                    for (int k = 0; k < size.z; k++) {
                        if (tilemap.GetTile(new Vector3Int(i, j, k)) == air) {
                            tilemap.SetTile(new Vector3Int(i, j, k), null);
                        }
                    }
                }
            }
        }
    }

    private void ResetWaveTable() {
        m_waveTable = new List<TileBase>[size.x, size.y, size.z];
        m_remainingTiles = size.x * size.y * size.z;

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
        m_remainingTiles--;
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

    private void CollapseTile() {

        // find a random lowest entropy tile
        int lowestEntropy = -1;
        List<Vector3Int> selectedTiles = new List<Vector3Int>();

        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                for (int k = 0; k < size.z; k++) {
                    int entropy = m_waveTable[i, j, k].Count;
                    
                    if (tilemap.GetTile(new Vector3Int(i,j,k)) != null)
                        continue;

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
