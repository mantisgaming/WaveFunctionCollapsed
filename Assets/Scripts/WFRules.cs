using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WFRules", menuName = "Scriptable Objects/WFRules")]
public class WFRules : ScriptableObject {
    public List<Rule> rules;
}

[Serializable]
public class Rule {
    public Rule(TileBase baseTile) {
        this.baseTile = baseTile;
        offsets = new List<RuleOffset>();
    }

    public TileBase baseTile;
    public List<RuleOffset> offsets;
}

[Serializable]
public class RuleOffset
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
public class TileProbability {
    public TileProbability(TileBase tile) {
        this.tile = tile;
        probability = 0;
    }

    public TileBase tile;
    public int probability;
}