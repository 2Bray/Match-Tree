using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManagerScript : MonoBehaviour
{
    #region Singleton

    private static GameFlowManagerScript _instance = null;

    public static GameFlowManagerScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameFlowManagerScript>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error: GameFlowManager not Found");
                }
            }

            return _instance;
        }
    }

    #endregion

    public bool IsGameOver { get { return isGameOver; } }

    private bool isGameOver = false;


    [Header("UI")]
    public UIGameOver GameOverUI;

    private void Start()
    {
        isGameOver = false;
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0;
        ScoreManagerScript.Instance.SetHighScore();
        GameOverUI.Show();
    }

    public void Exit()
    {
        Application.Quit();
    }
}
