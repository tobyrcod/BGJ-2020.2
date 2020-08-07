using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItem : MonoBehaviour
{
    public Spawner spawnedFrom;

    private void OnDestroy() {
        spawnedFrom.SpawnedItemDestroyed();
    }

    public void SetSpawnedFrom(Spawner spawner) {
        this.spawnedFrom = spawner;
    }
}
