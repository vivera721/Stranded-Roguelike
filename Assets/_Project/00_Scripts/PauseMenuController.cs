using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToTitleButton;

    [Header("Audio")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Scenes")]
    [SerializeField] private string titleSceneName = "StartMenu";

    private bool isPaused;
    private bool isChangingScene;
    private bool uiEventsBound;
    private float timeScaleBeforePause = 1f;

    public bool IsPaused => isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeController()
    {
        PauseMenuController existingController =
            UnityEngine.Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);

        if (existingController != null && existingController.isActiveAndEnabled)
        {
            return;
        }

        GameObject scenePauseMenu = FindSceneObject("PauseMenu");
        if (scenePauseMenu == null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("PauseMenuController");
        controllerObject.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        ResolveReferences();
        BindUiEvents();

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        RefreshVolumeSliders();
    }

    private void OnEnable()
    {
        GameAudioSettings.Changed -= RefreshVolumeSliders;
        GameAudioSettings.Changed += RefreshVolumeSliders;
    }

    private void OnDisable()
    {
        GameAudioSettings.Changed -= RefreshVolumeSliders;
    }

    private void OnDestroy()
    {
        UnbindUiEvents();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || isChangingScene)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else if (Time.timeScale > 0f)
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || isChangingScene || pauseMenu == null || Time.timeScale <= 0f)
        {
            return;
        }

        timeScaleBeforePause = Time.timeScale;
        isPaused = true;
        pauseMenu.SetActive(true);
        RefreshVolumeSliders();
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!isPaused || isChangingScene)
        {
            return;
        }

        isPaused = false;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
    }

    public void RestartGame()
    {
        if (isChangingScene)
        {
            return;
        }

        isChangingScene = true;
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void BackToTitle()
    {
        if (isChangingScene)
        {
            return;
        }

        isChangingScene = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void SetBgmVolume(float value)
    {
        if (GameAudioSettings.IsMuted)
        {
            float currentSfxVolume = sfxSlider != null ? sfxSlider.value : 0f;
            GameAudioSettings.SetVolumesAndUnmute(value, currentSfxVolume);
            return;
        }

        GameAudioSettings.SetBgmVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        if (GameAudioSettings.IsMuted)
        {
            float currentBgmVolume = bgmSlider != null ? bgmSlider.value : 0f;
            GameAudioSettings.SetVolumesAndUnmute(currentBgmVolume, value);
            return;
        }

        GameAudioSettings.SetSfxVolume(value);
    }

    private void BindUiEvents()
    {
        if (uiEventsBound)
        {
            return;
        }

        resumeButton?.onClick.AddListener(ResumeGame);
        restartButton?.onClick.AddListener(RestartGame);
        backToTitleButton?.onClick.AddListener(BackToTitle);
        bgmSlider?.onValueChanged.AddListener(SetBgmVolume);
        sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
        uiEventsBound = true;
    }

    private void UnbindUiEvents()
    {
        if (!uiEventsBound)
        {
            return;
        }

        resumeButton?.onClick.RemoveListener(ResumeGame);
        restartButton?.onClick.RemoveListener(RestartGame);
        backToTitleButton?.onClick.RemoveListener(BackToTitle);
        bgmSlider?.onValueChanged.RemoveListener(SetBgmVolume);
        sfxSlider?.onValueChanged.RemoveListener(SetSfxVolume);
        uiEventsBound = false;
    }

    private void RefreshVolumeSliders()
    {
        float displayedBgmVolume = GameAudioSettings.IsMuted ? 0f : GameAudioSettings.BgmVolume;
        float displayedSfxVolume = GameAudioSettings.IsMuted ? 0f : GameAudioSettings.SfxVolume;

        bgmSlider?.SetValueWithoutNotify(displayedBgmVolume);
        sfxSlider?.SetValueWithoutNotify(displayedSfxVolume);
    }

    private void ResolveReferences()
    {
        if (pauseMenu == null)
        {
            pauseMenu = FindSceneObject("PauseMenu");
        }

        if (pauseMenu == null)
        {
            return;
        }

        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (resumeButton == null && button.name == "Resume")
            {
                resumeButton = button;
            }
            else if (restartButton == null && button.name == "Restart")
            {
                restartButton = button;
            }
            else if (backToTitleButton == null && button.name == "BackToTitle")
            {
                backToTitleButton = button;
            }
        }

        Slider[] sliders = pauseMenu.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];

            if (bgmSlider == null && HasNamedParent(slider.transform, pauseMenu.transform, "BGM"))
            {
                bgmSlider = slider;
            }
            else if (sfxSlider == null && HasNamedParent(slider.transform, pauseMenu.transform, "SFX"))
            {
                sfxSlider = slider;
            }
        }
    }

    private static bool HasNamedParent(Transform child, Transform root, string parentName)
    {
        Transform current = child.parent;

        while (current != null && current != root)
        {
            if (current.name == parentName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] sceneTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform.gameObject.scene == activeScene && sceneTransform.name == objectName)
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }
}
