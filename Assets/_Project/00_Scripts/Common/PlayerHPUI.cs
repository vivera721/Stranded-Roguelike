using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [Header("Fill Image")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image damageBar;

    [Header("Damage Effect")]
    [SerializeField, Min(0f)] private float damageDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float damageLerpDuration = 0.4f;

    private Coroutine damageCoroutine;

    public void SetHp(float amount)
    {
        amount = Mathf.Clamp01(amount);

        float previousHpAmount = hpBar.fillAmount;

        hpBar.fillAmount = amount;

        if (damageBar == null)
            return;

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        if (amount < previousHpAmount)
            damageCoroutine = StartCoroutine(AnimateDamageBar(amount));
        else
            damageBar.fillAmount = amount;

    }

    IEnumerator AnimateDamageBar(float targetAmount)
    {
        // Show Red Bar Delay
        yield return new WaitForSeconds(damageDelay);

        float startAmount = damageBar.fillAmount;
        float timer = 0f;

        while(timer < damageLerpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / damageLerpDuration;

            damageBar.fillAmount = Mathf.Lerp(startAmount, targetAmount, t);

            yield return null;
        }

        damageBar.fillAmount = targetAmount;
    }
}
