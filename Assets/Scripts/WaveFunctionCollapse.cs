using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;

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

        if (depth >= 3) return;

        TileBase tile = tilemap.GetTile(position);

        Rule rule = rulesFile.rules.Find((rule) => rule.baseTile == tile);

        if (rule == null) return;

        for (int i = 0; i < rule.offsets.Count; i++) {
            RuleOffset offset = rule.offsets[i];

            Vector3Int target = position + offset.offset;

            if (target.x < 0 || target.y < 0 || target.z < 0 ||
                target.x >= size.x || target.y >= size.y || target.z >= size.z)
                continue;

            List<TileBase> impossibleTiles = new List<TileBase>();

            ref List<TileBase> targetWaveTableList = ref m_waveTable[target.x, target.y, target.z];

            for (int j = 0; j < targetWaveTableList.Count; j++) {
                if (!offset.probabilities.Exists((prob) => prob.tile == m_waveTable[target.x, target.y, target.z][j])) {
                    impossibleTiles.Add(targetWaveTableList[j]);
                }
            }

            if (impossibleTiles.Count > 0) {
                for (int j = 0; j < impossibleTiles.Count; j++) {
                    targetWaveTableList.Remove(impossibleTiles[j]);
                }

                UpdateWavetableFrom(target, depth + 1);
            }
        }
    }

    private void CollapseTile() {

        // find a random lowest entropy tile
        int lowestEntropy = -1;
        List<Vector3Int> selectedTiles = new List<Vector3Int>();

        

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int k = 0; k < size.z; k++)
                {
                    int entropy = m_waveTable[i, j, k].Count;

                    if (tilemap.GetTile(new Vector3Int(i, j, k)) != null)
                        continue;

                    // Ignore tiles that have no possible collapsed state
                    if (entropy == 0) continue;

                    if (entropy < lowestEntropy || lowestEntropy == -1)
                    {
                        selectedTiles.Clear();

                        lowestEntropy = entropy;
                        selectedTiles.Add(new Vector3Int(i, j, k));
                    }
                    else if (entropy == lowestEntropy)
                    {
                        selectedTiles.Add(new Vector3Int(i, j, k));
                    }
                }
            }
        }

        if (selectedTiles.Count == 0)
        {
            m_remainingTiles = 0;
            return;
        }

        Vector3Int selectedTilePosition = selectedTiles[Random.Range(0, selectedTiles.Count)];
        List<TileBase> options = m_waveTable[
            selectedTilePosition.x,
            selectedTilePosition.y,
            selectedTilePosition.z
        ];

        List<int> probabilities = new List<int>();

        for (int i = 0; i < options.Count; i++) {
            probabilities.Add(0);
        }

        // fill the tile based on probability weights
        for (int k = -1; k <= 1; k++) {
            for (int j = -1; j <= 1; j++) {
                for (int i = -1; i <= 1; i++) {
                    Vector3Int offset = new Vector3Int(i, j, k);

                    // ignore self
                    if (offset == Vector3Int.zero)
                        continue;

                    TileBase offsetTile = tilemap.GetTile(offset + selectedTilePosition);

                    if (offsetTile == null)
                        continue;

                    Rule rule = rulesFile.rules.Find((rule) => rule.baseTile == offsetTile);

                    RuleOffset ruleOffset = rule.offsets.Find((target) => target.offset == -offset);
                    
                    if (ruleOffset == null)
                        continue;

                    for (int optionIndex = 0; optionIndex < options.Count; optionIndex++) {
                        TileProbability prob = ruleOffset.probabilities.Find((tile) => tile.tile == options[optionIndex]);
                        
                        if (prob == null)
                            continue;

                        probabilities[optionIndex] += prob.probability;
                    }
                }
            }
        }

        int sum = 0;
        for (int i = 0; i < probabilities.Count; i++) {
            sum += probabilities[i];
        }

        TileBase selectedTile = null;

        int finalIndex = Random.Range(0, sum);
        for (int i = 0; i < probabilities.Count; i++) {
            if (finalIndex <= probabilities[i]) {
                selectedTile = options[i];
                break;
            } else
                finalIndex -= probabilities[i];
        }

        if (selectedTile == null) {
            throw new System.Exception("REEEEEE");
        }

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
