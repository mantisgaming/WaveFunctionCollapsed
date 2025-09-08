using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WFRules", menuName = "Scriptable Objects/WFRules")]
public class WFRules : ScriptableObject {
    public List<Rule> rules;

    public WFRules() {
        rules = new List<Rule>();
    }
}

[Serializable]
public class Rule {
    public Rule(TileBase baseTile) {
        this.baseTile = baseTile;
        kernelRules = new List<KernelRule>();
    }

    public TileBase baseTile;
    public List<KernelRule> kernelRules;
}

[Serializable]
public class RuleOffset // Not being used
{
    public RuleOffset(Vector3Int offset)
    {
        this.offset = offset;
        probabilities = new List<TileProbability>();
    }

    public Vector3Int offset;
    public List<TileProbability> probabilities;
}

[Serializable]
public class TileProbability //not being used
{
    public TileProbability(TileBase tile)
    {
        this.tile = tile;
        probability = 0;
    }

    public TileBase tile;
    public int probability;
}


[Serializable]
public class KernelRule
{
    public TileBase[] kernel;
    public int count;

    public KernelRule()
    {
        kernel = new TileBase[27];
    }

    /*pos must have each element between 0 and 2*/
    private int vectorToIndex(Vector3Int pos)
    {

        return (9 * (pos.x)) + (3 * (pos.y)) + pos.z;
    }

    public void setTileAt(Vector3Int pos, TileBase newTile)
    {
        Debug.Log(pos + ", " + newTile);

        kernel[vectorToIndex(pos)] = newTile;
    }

    public TileBase getTileAt(Vector3Int pos)
    {
        return kernel[vectorToIndex(pos)];
    }

    public override bool Equals(object obj) {

        if (obj == null)
            return false;

        if (!(obj is KernelRule))
            return false;

        KernelRule other = (KernelRule)obj;

        for (int k = 0; k < 3; k++) {
            for (int j = 0; j < 3; j++) {
                for (int i = 0; i < 3; i++) {
                    Vector3Int index = new Vector3Int(i, j, k);
                    if (getTileAt(index) != other.getTileAt(index)) //Not equals
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public override int GetHashCode() {
        return kernel.GetHashCode();
    }
}