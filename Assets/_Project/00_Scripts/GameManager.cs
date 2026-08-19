using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 0f; // Pause the game at the start
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
