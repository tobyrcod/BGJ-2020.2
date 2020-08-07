using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MenuManager : MonoBehaviour
{
    public void LoadScene(string sceneName) {
        //Here we are asking the SceneManager to load the desired scene. In this instance we're providing it our variable 'currentScene'
        SceneManager.LoadScene(sceneName);
    }
}
