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
    private bool m_do_generation = true;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    private void Start() {
        tilemap.ClearAllTiles();

        ResetWaveTable();
        PlaceInitialTile();
    }

    private void Update() {
        if (m_do_generation) {
            CollapseTile();
        }
    }

    private void ResetWaveTable() {
        m_waveTable = new List<TileBase>[size.x, size.y, size.z];

        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                for (int k = 0; k < size.z; k++) {
                    m_waveTable[i, j, k] = new List<TileBase>(rulesFile.tiles);
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

    private void ClearAir() {
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

    private void UpdateWavetableAround(Vector3Int pos, int depth = 0) {
        if (depth >= 2) return;

        var kSize = rulesFile.MaxKernelSize;

        for (int i = 0; i < kSize.x; i++) {
            for (int j = 0; j < kSize.y; j++) {
                for (int k = 0; k < kSize.z; k++) {
                    Vector3Int offset = new(i, j, k);
                    UpdateWavetableFrom(pos - offset);
                }
            }
        }

        for (int i = -kSize.x + 1; i < kSize.x; i++) {
            for (int j = -kSize.y + 1; j < kSize.y; j++) {
                for (int k = -kSize.z + 1; k < kSize.z; k++) {
                    Vector3Int offset = new(i, j, k);
                    UpdateWavetableAround(pos + offset, depth + 1);
                }
            }
        }
    }

    private void UpdateWavetableFrom(Vector3Int position) {
        if (position.x < size.x &&
            position.y < size.y &&
            position.z < size.z) {

            var kSize = rulesFile.MaxKernelSize;

            List<TileBase>[,,] validTiles = new List<TileBase>[kSize.x, kSize.y, kSize.z];

            for (int i = 0; i < kSize.x; i++) {
                for (int j = 0; j < kSize.y; j++) {
                    for (int k = 0; k < kSize.z; k++) {
                        validTiles[i, j, k] = new List<TileBase>();
                    }
                }
            }

            foreach (KernelRule rule in rulesFile.rules) {
                if (DoesRuleFit(rule, position)) {
                    UnionKernel(rule, ref validTiles);
                }
            }

            for (int k = 0; k < kSize.z; k++) {
                for (int j = 0; j < kSize.y; j++) {
                    for (int i = 0; i < kSize.x; i++) {
                        Vector3Int offset = new(i, j, k);
                        Vector3Int target = offset + position;

                        List<TileBase> impossibleTiles = new List<TileBase>();
                        foreach (TileBase tile in m_waveTable[target.x, target.y, target.z]) {
                            if (!validTiles[i,j,k].Contains(tile))
                                impossibleTiles.Add(tile);
                        }

                        foreach (TileBase tile in impossibleTiles) {
                            m_waveTable[target.x, target.y, target.z].Remove(tile);
                        }
                    }
                }
            }
        }
    }

    private bool DoesRuleFit(KernelRule kernel, Vector3Int position) {
        for (int k = 0; k < kernel.Size.z; k++) {
            for (int j = 0; j < kernel.Size.y; j++) {
                for (int i = 0; i < kernel.Size.x; i++) {
                    Vector3Int offset = new(i, j, k);
                    Vector3Int target = offset + position;

                    if (target.x < 0 && target.x > size.x &&
                        target.y < 0 && target.y > size.y &&
                        target.z < 0 && target.z > size.z)
                        continue;

                    TileBase ruleTile = kernel.getTileAt(offset);
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

        bool full = true;

        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                for (int k = 0; k < size.z; k++) {
                    int entropy = m_waveTable[i, j, k].Count;

                    if (tilemap.GetTile(new Vector3Int(i, j, k)) != null)
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
        }

        if (lowestEntropy == -1) {
            m_do_generation = false;
            ClearAir();

            if (!full) {
                Debug.LogError("Failed to collapse");
            }
            return;
        }

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
        UpdateWavetableAround(selectedTilePosition);
    }
}
