using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class RuleExtractor : MonoBehaviour {
    public WFRules rulesFile;

    private Tilemap tilemap;

    [SerializeField]
    private TileBase air;
    [SerializeField]
    private Vector3Int kernelSize = new(3,3,2);

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    public void ExtractRules() {
        tilemap = GetComponent<Tilemap>();

        rulesFile.rules.Clear();
        rulesFile.tiles.Clear();

        setAir();

        // for each tile
        for (int k = 0; k < tilemap.size.z; k++) {
            for (int j = 0; j < tilemap.size.y; j++) {
                for (int i = 0; i < tilemap.size.x; i++) {
                    var kernelPos = new Vector3Int(i, j, k);
                    var tile = tilemap.GetTile(kernelPos);

                    if (tile != null && !rulesFile.tiles.Contains(tile)) {
                        rulesFile.tiles.Add(tile);
                    }

                    if (i + kernelSize.x <= tilemap.size.x &&
                        j + kernelSize.y <= tilemap.size.y &&
                        k + kernelSize.z <= tilemap.size.z) {
                        extractRulesFromTile(kernelPos + tilemap.origin);
                    }
                }
            }
        }

        clearAir();

        Debug.Log("Finished extractRules");
    }

    public void setAir() {
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

    private void extractRulesFromTile(Vector3Int tileCoord) {
        KernelRule newRule = new(kernelSize);

        for (int k = 0; k < kernelSize.z; k++) {
            for (int j = 0; j < kernelSize.z; j++) {
                for (int i = 0; i < kernelSize.z; i++) {
                    Vector3Int kPos = new(i, j, k);
                    newRule.setTileAt(kPos, tilemap.GetTile(kPos + tileCoord));
                }
            }
        }

        KernelRule existingRule = rulesFile.rules.Find((rule) => rule.Equals(newRule));

        if (existingRule == null) {
            rulesFile.rules.Add(newRule);
        } else {
            existingRule.count++;
        }
    }
}
