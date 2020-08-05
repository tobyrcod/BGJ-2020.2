using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Transform spawn;
    [SerializeField] Transform playerParent;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int maxPlayerCount;
    private int activePlayerIndex;
    private List<PlayerController> players = new List<PlayerController>();

    private void Start() {
        CreateNewPlayer();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.P)) {
            if (players.Count < maxPlayerCount) {
                players[activePlayerIndex].KillPlayer();
                CreateNewPlayer();
            }
            else {
                players[activePlayerIndex].KillPlayer();
                activePlayerIndex++;
                activePlayerIndex %= maxPlayerCount;
                players[activePlayerIndex].RevivePlayer();
            }
        }
    }

    private void CreateNewPlayer() {
        activePlayerIndex = players.Count;
        players.Add(InstantiateNewPlayer());
    }

    private PlayerController InstantiateNewPlayer() {
        return Instantiate(playerPrefab, spawn.position, Quaternion.identity, playerParent).GetComponent<PlayerController>();
    }
}
