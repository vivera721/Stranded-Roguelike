using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private Toggle muteToggle;

    private void Awake()
    {
        if (muteToggle == null)
        {
            return;
        }

        muteToggle.SetIsOnWithoutNotify(GameAudioSettings.IsMuted);
        muteToggle.onValueChanged.AddListener(SetMuted);
    }

    private void OnDestroy()
    {
        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(SetMuted);
        }
    }

    public void StartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainGame");
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void SetMuted(bool isMuted)
    {
        GameAudioSettings.SetMuted(isMuted);
    }
}
