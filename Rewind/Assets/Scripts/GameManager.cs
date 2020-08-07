using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Random = System.Random;
using UnityEngine.Rendering;
using UnityEditor;

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    bool isGamePlaying = true;

    [SerializeField] Spawner[] playerSpawnLocations;
    [SerializeField] Transform playerParent;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int maxPlayerCount;

    [Space]

    [SerializeField] Spawner[] miscSpawnLocations;
    [SerializeField] WeightedSpawnInfo[] miscSpawnPrefabs;
    WeightedList miscSpawnPrefabsList;
    [SerializeField] Transform miscParent;

    [Space]

    [SerializeField] Text scoreUI;
    [SerializeField] Text clonesUI;
    [SerializeField] GameObject gameOverUICanvas;
    [SerializeField] Text gameOverScoreUI;
    [SerializeField] GameObject scoreUICanvas;
    [SerializeField] GameObject camRig;
 
    private int activePlayerIndex;
    [SerializeField] public List<PlayerController> players = new List<PlayerController>();
    [SerializeField] public List<PlayerController> alivePlayers = new List<PlayerController>();
    [SerializeField] public List<PlayerController> killedPlayers = new List<PlayerController>();
    [SerializeField] public List<PlayerController> destroyedPlayers = new List<PlayerController>();
    private int totalScore = 0;
    private int spawnedPlayers = 0;

    private void Start() {
        if (instance == null)
            instance = this;

        miscSpawnPrefabsList = new WeightedList(miscSpawnPrefabs);
        CreateNewPlayer();
        StartCoroutine(SpawnMisc(2f));
    }

    IEnumerator SpawnMisc(float time) {
        yield return new WaitForSeconds(time);

        Random random = new Random();
        while (true) {
            int delay = random.Next(2, 5);
            yield return new WaitForSeconds(delay);
            InstantiateNewMiscPrefab();
        }
    }

    private void Update() {
        if (isGamePlaying) {
            if (Input.GetMouseButtonDown(1)) {
                if (spawnedPlayers < maxPlayerCount) {
                    players[activePlayerIndex].KillPlayer();
                    CreateNewPlayer();
                }
                else {
                    if (killedPlayers.Count > 0) {
                        players[activePlayerIndex].KillPlayer();
                        activePlayerIndex++;
                        activePlayerIndex %= maxPlayerCount;

                        if (players[activePlayerIndex] == null) {
                            activePlayerIndex++;
                            activePlayerIndex %= maxPlayerCount;
                        }

                        players[activePlayerIndex].RevivePlayer();
                    }
                }
            }
        }
    }

    public void ScreenShake() {
        camRig.transform.DOComplete();
        camRig.transform.DOShakePosition(0.2f, 0.5f, 14, 90, false, true);
    }

    public Vector2 GetActivePlayerPosition() {
        if (players[activePlayerIndex] != null)
            return players[activePlayerIndex].transform.position;

        return Vector2.zero;
    }

    private void CreateNewPlayer() {
        activePlayerIndex = players.Count;
        PlayerController newPlayer = InstantiateNewPlayer();
        newPlayer.OnScoreChangedEvent += ScoreChanged;
        players.Add(newPlayer);
        alivePlayers.Add(newPlayer);
        spawnedPlayers++;
    }

    internal void DestroyPlayer(PlayerController playerController) {
        destroyedPlayers.Add(playerController);
        alivePlayers.Remove(playerController);

        Destroy(playerController.gameObject);
        activePlayerIndex = 0;

        clonesUI.text = (3 - destroyedPlayers.Count).ToString();

        CheckForEndOfGame();
    }

    private void CheckForEndOfGame() {
        if (killedPlayers.Count > 0) {
            killedPlayers[0].RevivePlayer();
        }
        else {
            if (spawnedPlayers >= maxPlayerCount) {
                GameOver();
            }
            else {
                CreateNewPlayer();
            }
        }
    }

    private void GameOver() {
        StopAllCoroutines();
        isGamePlaying = false;
        scoreUICanvas.SetActive(false);
        gameOverScoreUI.text = totalScore.ToString();
        gameOverUICanvas.SetActive(true);
    }

    public void ReloadGame() {
        //We declare a variable to store our currentScene's name. We get this through the SceneManager class's GetActiveScene method. 
        string currentScene = SceneManager.GetActiveScene().name;
        //Here we are asking the SceneManager to load the desired scene. In this instance we're providing it our variable 'currentScene'
        SceneManager.LoadScene(currentScene);
    }
    public void LoadScene(string sceneName) {
        //Here we are asking the SceneManager to load the desired scene. In this instance we're providing it our variable 'currentScene'
        SceneManager.LoadScene(sceneName);
    }

    private void ScoreChanged(int score) {
        totalScore += score;
        UpdateScoreUI();
    }

    private void UpdateScoreUI() {
        scoreUI.text = totalScore.ToString();
    }

    private PlayerController InstantiateNewPlayer() {
        Spawner spawner;
        do {
            spawner = GetRandomSpawner(playerSpawnLocations);
        } while (spawner == null);

        return spawner.Spawn(playerPrefab, playerParent).GetComponent<PlayerController>();    
    }

    private GameObject InstantiateNewMiscPrefab() {
        Spawner spawner = GetRandomSpawner(miscSpawnLocations);

        if (spawner != null) {
            GameObject gobject = miscSpawnPrefabsList.GetWeightedRandomPrefab();
            return spawner.Spawn(gobject, miscParent);
        }

        return null;
    }

    private Spawner GetRandomSpawner(Spawner[] spawnLocations) {
        int length = spawnLocations.Length;
        Random random = new Random();

        int index = random.Next(0, length);
        Spawner spawner = spawnLocations[index];

        if (spawner.CanSpawn())
            return spawnLocations[index];

        return null;
    }

    [Serializable]
    public class WeightedSpawnInfo {
        public float weight;
        public GameObject prefab;
    }

    public class WeightedList {
        float totalWeight;
        List<WeightedSpawnInfo> infos = new List<WeightedSpawnInfo>();
        Random random = new Random();

        public WeightedList(WeightedSpawnInfo[] infos) {
            float totalWeight = 0f;
            for (int i = 0; i < infos.Length; i++) {
                this.infos.Add(infos[i]);
                totalWeight += infos[i].weight;
            }

            this.totalWeight = totalWeight;
        }

        public GameObject GetWeightedRandomPrefab() {
            double roll = random.NextDouble() * totalWeight;
            int index = -1;
            for (int i = 0; i < infos.Count; i++) {
                if (roll <= infos[i].weight) { index = i; break; }
                roll -= infos[i].weight;
            }

            if (index == -1)
                index = infos.Count - 1;

            return infos[index].prefab;
        }
    }
}
