using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Transform spawn;
    [SerializeField] Transform playerParent;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int maxPlayerCount;
    [SerializeField] Transform activeCamera;
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

        //Seb Camera Follow
        Vector2 activePlayerPos = players[activePlayerIndex].transform.position;
        activeCamera.position = new Vector3(activePlayerPos.x, activePlayerPos.y, activeCamera.position.z);
    }

    private void CreateNewPlayer() {
        activePlayerIndex = players.Count;
        players.Add(InstantiateNewPlayer());
    }

    private PlayerController InstantiateNewPlayer() {
        return Instantiate(playerPrefab, spawn.position, Quaternion.identity, playerParent).GetComponent<PlayerController>();
    }
}
