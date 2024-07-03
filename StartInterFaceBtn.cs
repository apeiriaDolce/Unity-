using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartInterFaceBtn : MonoBehaviour
{
    private AsyncOperation baseLoad;
    public void StartGame()
    {
        SceneManager.LoadScene("TestScene");
        SceneManager.LoadSceneAsync("GameAwake",LoadSceneMode.Additive);
    }
}