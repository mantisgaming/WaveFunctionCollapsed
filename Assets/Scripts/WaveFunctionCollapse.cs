using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.GraphicsBuffer;

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
        UpdateWavetableAround(new Vector3Int(0, 0, 0));
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

        UpdateWavetableAt(pos);

        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                for (int k = -1; k <= 1; k++) {
                    Vector3Int offset = new(i, j, k);
                    if (offset == Vector3Int.zero)
                        continue;
                    UpdateWavetableAt(pos + offset);
                }
            }
        }

        for (int i = -kSize.x + 1; i < kSize.x; i++) {
            for (int j = -kSize.y + 1; j < kSize.y; j++) {
                for (int k = -kSize.z + 1; k < kSize.z; k++) {
                    Vector3Int offset = new(i, j, k);
                    if (offset == Vector3Int.zero)
                        continue;
                    UpdateWavetableAround(pos + offset, depth + 1);
                }
            }
        }
    }

    private void UpdateWavetableAt(Vector3Int position) {
        if (position.x < size.x && position.x >= 0 &&
            position.y < size.y && position.y >= 0 &&
            position.z < size.z && position.z >= 0) {

            var kSize = rulesFile.MaxKernelSize;

            List<TileBase> validTiles = new List<TileBase>(rulesFile.tiles);

            for (int i = 0; i < kSize.x; i++) {
                for (int j = 0; j < kSize.y; j++) {
                    for (int k = 0; k < kSize.z; k++) {
                        Vector3Int offset = new(i, j, k);
                        List<TileBase> ruleTiles = new List<TileBase>();
                        foreach (KernelRule rule in rulesFile.rules) {
                            if (DoesRuleFit(rule, position - offset)) {
                                TileBase tile = rule.getTileAt(offset);
                                if (!ruleTiles.Contains(tile))
                                    ruleTiles.Add(tile);
                            }
                        }

                        List<TileBase> impossibleTiles = new List<TileBase>();
                        foreach (TileBase tile in validTiles) {
                            if (!ruleTiles.Contains(tile))
                                impossibleTiles.Add(tile);
                        }

                        foreach (TileBase tile in impossibleTiles) {
                            ruleTiles.Remove(tile);
                        }
                    }
                }
            }

            m_waveTable[position.x, position.y, position.z] = validTiles;
        }
    }

    private bool DoesRuleFit(KernelRule kernel, Vector3Int position) {
        for (int k = 0; k < kernel.Size.z; k++) {
            for (int j = 0; j < kernel.Size.y; j++) {
                for (int i = 0; i < kernel.Size.x; i++) {
                    Vector3Int offset = new(i, j, k);
                    Vector3Int target = offset + position;

                    if (target.x < 0 || target.x >= size.x ||
                        target.y < 0 || target.y >= size.y ||
                        target.z < 0 || target.z >= size.z)
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
        for (int k = 0; k < kernel.Size.x; k++) {
            for (int j = 0; j < kernel.Size.y; j++) {
                for (int i = 0; i < kernel.Size.z; i++) {
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
