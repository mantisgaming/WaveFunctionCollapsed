using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WFRules", menuName = "Scriptable Objects/WFRules")]
public class WFRules : ScriptableObject {
    public List<KernelRule> rules;
    public List<TileBase> tiles;

    private Vector3Int m_maxKernelSize;

    public Vector3Int MaxKernelSize {
        get {
            if (m_maxKernelSize != Vector3Int.zero)
                return m_maxKernelSize;

            foreach (var rule in rules) {
                if (m_maxKernelSize.x < rule.Size.x)
                    m_maxKernelSize.x = rule.Size.x;

                if (m_maxKernelSize.y < rule.Size.y)
                    m_maxKernelSize.y = rule.Size.y;

                if (m_maxKernelSize.z < rule.Size.z)
                    m_maxKernelSize.z = rule.Size.z;
            }

            return m_maxKernelSize;
        }
    }

    public WFRules() {
        rules = new List<KernelRule>();
    }
}

[Serializable]
public class KernelRule
{
    public TileBase[] kernel;
    public int count = 1;
    public Vector3Int Size;

    public KernelRule(Vector3Int size) {
        Size = size;
        kernel = new TileBase[size.x * size.y * size.z];
    }

    private int vectorToIndex(Vector3Int pos) {
        return pos.x + pos.y * Size.x + pos.z * Size.x * Size.y;
    }

    public void setTileAt(Vector3Int pos, TileBase newTile) {
        Debug.Log(pos + ", " + newTile);

        kernel[vectorToIndex(pos)] = newTile;
    }

    public TileBase getTileAt(Vector3Int pos) {
        return kernel[vectorToIndex(pos)];
    }

    public override bool Equals(object obj) {

        if (obj == null)
            return false;

        if (!(obj is KernelRule))
            return false;

        KernelRule other = (KernelRule)obj;

        if (other.Size != Size) return false;

        for (int i = 0; i < Size.x * Size.y * Size.z; i++) {
            if (kernel[i] != other.kernel[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode() {
        return kernel.GetHashCode();
    }
}