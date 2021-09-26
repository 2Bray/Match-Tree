using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManagerScript : MonoBehaviour
{
    private static int highScore;

    #region Singleton
    private static ScoreManagerScript _instance = null;

    public static ScoreManagerScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScoreManagerScript>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error: ScoreManager not Found");
                }
            }

            return _instance;
        }
    }
    #endregion

    public int tileRatio;
    public int comboRatio;

    public int HighScore { get { return highScore; } }
    public int CurrentScore { get { return currentScore; } }

    private int currentScore;

    private void Start()
    {
        ResetCurrentScore();
    }

    public void ResetCurrentScore()
    {
        currentScore = 0;
    }

    public void IncrementCurrentScore(int tileCount, int comboCount)
    {
        currentScore += (tileCount * tileRatio) * (comboCount * comboRatio);
        SoundManagerScript.Instance.PlayScore(comboCount > 1);
    }

    public void SetHighScore()
    {
        highScore = currentScore > highScore ? currentScore : highScore;
    }
}
