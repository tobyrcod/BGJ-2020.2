using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    SpawnItem spawnedItem;

    public GameObject Spawn(GameObject spawnPrefab, Transform parent) {
        if (spawnedItem != null) {
            Destroy(spawnedItem.gameObject);
            spawnedItem = null;
        }

        spawnedItem = Instantiate(spawnPrefab, this.transform.position, Quaternion.identity, parent).GetComponent<SpawnItem>();
        spawnedItem.SetSpawnedFrom(this);
        return spawnedItem.gameObject;
    }

    internal void SpawnedItemDestroyed() {
        spawnedItem = null;
    }

    public bool CanSpawn() {
        return spawnedItem == null;
    }
}
