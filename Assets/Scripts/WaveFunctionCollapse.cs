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

        while (m_remainingTiles > 0) {
            CollapseTile();
            m_remainingTiles--;
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

    private void UpdateWavetableFrom(Vector3Int position) {
        // propagate possible tiles to surrounding tiles
        // recurse once or twice on changed wave table tiles to avoid deadlocks
    }

    private void CollapseTile() {
        // find a random lowest entropy tile
        // fill the tile based on probability weights
        // update wavetable from the tile
    }
}
