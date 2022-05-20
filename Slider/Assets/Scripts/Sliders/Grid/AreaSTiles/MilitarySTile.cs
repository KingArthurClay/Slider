using UnityEngine;

public class MilitarySTile : STile 
{
    // public new int STILE_WIDTH = 13;

    public enum MilitaryTileType {
        Passable, 
        Impassable
    }

    void Awake() {
        STILE_WIDTH = 17;
    }
}