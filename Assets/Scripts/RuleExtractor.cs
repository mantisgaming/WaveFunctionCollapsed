using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[RequireComponent (typeof(Tilemap))]
public class RuleExtractor : MonoBehaviour {
    public WFRules rulesFile;

    private Tilemap tilemap;
    [SerializeField]
    private BaseTile air;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    private void Start() {
        rulesFile.rules.Clear();

        setAir();

        // for each tile
        for (int k = 0; k < tilemap.size.z; k++)
        {
            for (int j = 0; j < tilemap.size.y; j++)
            {
                for (int i = 0; i < tilemap.size.x; i++)
                {
                    extractRulesFromTile(new Vector3Int(i, j, k));
                }
            }
        }
    }

    public void setAir()
    {
        //iterate through every tile, if it's null, set it to Air
        for (int z = 0; z < tilemap.size.z; z++)
        {
            for (int y = 0; y < tilemap.size.y; y++)
            {
                for (int x = 0; x < tilemap.size.x; x++)
                {
                    if (tilemap.GetTile(Vector3Int(x, y, z)) == null)
                    {
                        tilemap.SetTile(Vector3Int(x, y, z), air);
                    }
                }
            }
        }
    }

    private void extractRulesFromTile(Vector3Int tileCoord)
    {
        TileBase baseTile = tilemap.GetTile(tileCoord);

        if (baseTile == null)
            return;

        Rule rule = FindRuleForTile(baseTile);

        // for each surrounding tile
        for (int k = -1; k <= 1; k++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3Int offset = new Vector3Int(i, j, k);

                    // ignore self
                    if (offset == Vector3Int.zero)
                        continue;

                    TileBase tile = tilemap.GetTile(tileCoord + offset);

                    if (tile == null)
                        continue;

                    RuleOffset ruleOffset = FindOffsetForRule(rule, offset);
                    TileProbability tileProbability = FindTileProbabilityForTileOffset(ref ruleOffset, tile);

                    tileProbability.probability++;
                }
            }
        }
    }

    private TileProbability FindTileProbabilityForTileOffset(ref RuleOffset ruleOffset, TileBase tile)
    {
        TileProbability tileProbability = ruleOffset.probabilities.Find(
            (prob) => prob.tile == tile
        );

        if (tileProbability == null)
        {
            tileProbability = new TileProbability(tile);
            ruleOffset.probabilities.Add(tileProbability);
        }

        return tileProbability;
    }

    private RuleOffset FindOffsetForRule(Rule rule, Vector3Int offset)
    {
        RuleOffset ruleOffset = rule.offsets.Find(
            (o) => o.offset == offset
        );

        if (ruleOffset == null)
        {
            ruleOffset = new RuleOffset(offset);
            rule.offsets.Add(ruleOffset);
        }

        return ruleOffset;
    }

    private Rule FindRuleForTile(TileBase baseTile)
    {
        Rule rule = rulesFile.rules.Find(
            (rule) => rule.baseTile == baseTile
        );

        if (rule == null)
        {
            rule = new Rule(baseTile);
            rulesFile.rules.Add(rule);
        }

        return rule;
    }
}
