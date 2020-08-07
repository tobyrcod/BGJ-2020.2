using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : SpawnItem
{
    public string collectableName;
    public int score;

    public void Collect(PlayerController player) {
        player.IncreaseScore(score);
        Destroy(this.gameObject);
    }
}
