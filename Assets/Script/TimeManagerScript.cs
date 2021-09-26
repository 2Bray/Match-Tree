using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManagerScript : MonoBehaviour
{
    #region Singleton

    private static TimeManagerScript _instance = null;

    public static TimeManagerScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimeManagerScript>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error: TimeManager not Found");
                }
            }

            return _instance;
        }
    }

    #endregion

    public int duration;

    private float time;

    private void Start()
    {
        time = 0;
    }

    private void Update()
    {
        if (GameFlowManagerScript.Instance.IsGameOver)
        {
            return;
        }

        if (time > duration)
        {
            GameFlowManagerScript.Instance.GameOver();
            return;
        }

        time += Time.deltaTime;
    }

    public float GetRemainingTime()
    {
        return duration - time;
    }
}
