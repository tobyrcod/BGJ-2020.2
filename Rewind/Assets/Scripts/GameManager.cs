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

    [SerializeField] PlayerController activePlayer;
    [SerializeField] public List<PlayerController> frozenPlayers = new List<PlayerController>();

    private int totalScore = 0;
    private int spawnedPlayers = 0;
    private int destroyedPlayersCount = 0;

    private void Start() {
        if (instance == null)
            instance = this;

        miscSpawnPrefabsList = new WeightedList(miscSpawnPrefabs);
        activePlayer = CreateNewPlayer();
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
            if (Input.GetKeyDown(KeyCode.O)) {
                if (spawnedPlayers < maxPlayerCount) {
                    activePlayer.FreezePlayer();
                    activePlayer = CreateNewPlayer();
                }
                else {
                    if (frozenPlayers.Count > 0) {
                        activePlayer.FreezePlayer();
                        activePlayer = frozenPlayers[0];
                        activePlayer.RevivePlayer();
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
        if (activePlayer != null)
            return activePlayer.transform.position;

        return Vector2.zero;
    }

    private PlayerController CreateNewPlayer() {
        PlayerController newPlayer = InstantiateNewPlayer();
        newPlayer.OnScoreChangedEvent += ScoreChanged;
        newPlayer.gameObject.name = $"Player{spawnedPlayers}";
        spawnedPlayers++;

        return newPlayer;
    }

    internal void DestroyActivePlayer() {
        destroyedPlayersCount++;
        Destroy(activePlayer.gameObject);
        clonesUI.text = (3 - destroyedPlayersCount).ToString();

        CheckForEndOfGame();
    }

    private void CheckForEndOfGame() {
        if (destroyedPlayersCount >= maxPlayerCount) {
            GameOver();
        }
        else {
            if (frozenPlayers.Count > 0) {
                activePlayer = frozenPlayers[0];
                activePlayer.RevivePlayer();
            }
            else {
                GameOver();
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
