using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GoToMainMenu : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fadeDelay = 0.5f;
    [SerializeField] private string mainMenuSceneName = "StartMenu";
    [SerializeField] private DOTweenAnimation fadeAnimation;
    [SerializeField] private TMP_Text goToMainText;

    private Coroutine returnSequence;
    private bool canReturnToMenu;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        StartReturnSequence();
    }

    private void OnDisable()
    {
        if (returnSequence != null)
        {
            StopCoroutine(returnSequence);
            returnSequence = null;
        }

        canReturnToMenu = false;

        if (fadeAnimation != null)
        {
            fadeAnimation.DOKill();
        }
    }

    private void Update()
    {
        if (!canReturnToMenu)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void SetGameOver()
    {
        StartReturnSequence();
    }

    private void StartReturnSequence()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (returnSequence != null)
        {
            StopCoroutine(returnSequence);
        }

        returnSequence = StartCoroutine(ReturnSequenceRoutine());
    }

    private IEnumerator ReturnSequenceRoutine()
    {
        canReturnToMenu = false;
        ResolveReferences();

        if (fadeAnimation != null)
        {
            fadeAnimation.DOKill();
            fadeAnimation.isIndependentUpdate = true;
            fadeAnimation.autoPlay = false;
        }

        if (goToMainText != null)
        {
            goToMainText.alpha = 0f;
        }

        if (fadeDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(fadeDelay);
        }

        if (fadeAnimation != null)
        {
            fadeAnimation.RewindThenRecreateTweenAndPlay();

            float fadeDuration = Mathf.Max(0f, fadeAnimation.delay + fadeAnimation.duration);
            if (fadeDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(fadeDuration);
            }
        }
        else if (goToMainText != null)
        {
            goToMainText.alpha = 1f;
        }

        canReturnToMenu = true;
        returnSequence = null;
    }

    private void ResolveReferences()
    {
        if (fadeAnimation == null)
        {
            fadeAnimation = GetComponent<DOTweenAnimation>();
        }

        if (goToMainText == null)
        {
            goToMainText = GetComponent<TMP_Text>();
        }
    }
}
