using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    public async Task GoToScene(string sceneName)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        //SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("You hit Quit");
    }
}
