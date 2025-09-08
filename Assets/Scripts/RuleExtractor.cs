using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(Tilemap))]
public class RuleExtractor : MonoBehaviour {
    public WFRules rulesFile;

    private Tilemap tilemap;

    [SerializeField]
    private TileBase air;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    public void ExtractRules()
    {
        tilemap = GetComponent<Tilemap>();

        rulesFile.rules.Clear();

        setAir();

        // for each tile
        for (int k = 1; k < tilemap.size.z; k++) //start at z=1 TODO
        {
            for (int j = 0; j < tilemap.size.y; j++)
            {
                for (int i = 0; i < tilemap.size.x; i++)
                {
                    Debug.Log("ReachedTile[" + i + ", " + j + ", " + k + "]");
                    extractRulesFromTile(new Vector3Int(i, j, k) + tilemap.origin);
                }
            }
        }

        clearAir();

        Debug.Log("Finished extractRules");
    }

    public void setAir()
    {
        //iterate through every tile, if it's null, set it to Air
        for (int y = 0; y < tilemap.size.y; y++) {
            for (int x = 0; x < tilemap.size.x; x++) {
                bool hasTile = false;
                for (int z = 0; z < tilemap.size.z; z++) {
                    if (tilemap.GetTile(new Vector3Int(x, y, z) + tilemap.origin) != null) {
                        hasTile = true; break;
                    }
                }
                if (hasTile) {
                    for (int z = 0; z < tilemap.size.z; z++) {

                        if (tilemap.GetTile(new Vector3Int(x, y, z) + tilemap.origin) == null) {
                            tilemap.SetTile(new Vector3Int(x, y, z) + tilemap.origin, air);
                        }
                    }
                }
            }
        }
    }

    public void clearAir() {
        //iterate through every tile, if it's null, set it to Air
        for (int y = 0; y < tilemap.size.y; y++) {
            for (int x = 0; x < tilemap.size.x; x++) {
                for (int z = 0; z < tilemap.size.z; z++) {

                    if (tilemap.GetTile(new Vector3Int(x, y, z) + tilemap.origin) == air) {
                        tilemap.SetTile(new Vector3Int(x, y, z) + tilemap.origin, null);
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

        Rule rule = FindRuleForTile(baseTile); //check for existing rule 
                                               //if I don't have a rule make a rule
        if (rule == null)
        {
            rule = new Rule(baseTile);
            rulesFile.rules.Add(rule);
        }

        KernelRule foundKernelRule = new KernelRule(); //create blank

        //storing a kernel of this position

        bool isValidKernel = true;
        // for each surrounding tile
        for (int k = -1; k <= 1; k++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    

                    // store positions as vects
                    Vector3Int offsetVect = new Vector3Int(i, j, k);
                    Vector3Int kernelVect = new Vector3Int(i + 1, j + 1, k + 1); //to correct to array zero index


                    //find tile at position, store in kernelRule
                    TileBase tileAtLoc = tilemap.GetTile(tileCoord + offsetVect);
                    Debug.Log(tileAtLoc);
                    if (tileAtLoc == null)
                    {
                        return; //contains null, discard the kernel rule its on an edge.
                    }

                    foundKernelRule.setTileAt(kernelVect, tileAtLoc);
                }
            }
        }


        Debug.Log("Kernel made" +foundKernelRule.kernel);

        //test if the kernel made is the same as another already in the list
        bool ruleExists = false;
        foreach (KernelRule currKernel in rule.kernelRules)
        {
            //if it equals we have found a match
            if (currKernel == foundKernelRule)
            {
                ruleExists = true;
                //increase count
                currKernel.count++;
                return; //exist
            }
        }

        //if rule doesn't exist yet add to the list
        if (!ruleExists)
        {
            foundKernelRule.count = 1;
            rule.kernelRules.Add(foundKernelRule);
        }

    }

    /*
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
    }*/

    /*
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
    }*/

    private Rule FindRuleForTile(TileBase baseTile) {
        Rule rule = rulesFile.rules.Find(
            (rule) => rule.baseTile == baseTile
        );

        if (rule == null) {
            rule = new Rule(baseTile);
            rulesFile.rules.Add(rule);
        }

        return rule;
    }
    
}
