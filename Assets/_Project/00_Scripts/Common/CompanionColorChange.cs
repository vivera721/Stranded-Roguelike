using UnityEngine;

public enum CompanionColor
{
    Blue,
    Yellow,
    Pink,
    Green
}

public class CompanionColorChange : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Color Animator Overrides")]
    [SerializeField] private AnimatorOverrideController blueController;
    [SerializeField] private AnimatorOverrideController yellowController;
    [SerializeField] private AnimatorOverrideController pinkController;
    [SerializeField] private AnimatorOverrideController greenController;

    public void SetColor(CompanionColor color)
    {
        switch (color)
        {
            case CompanionColor.Blue:
                animator.runtimeAnimatorController = blueController;
                break;
            case CompanionColor.Yellow:
                animator.runtimeAnimatorController = yellowController;
                break;
            case CompanionColor.Pink:
                animator.runtimeAnimatorController = pinkController;
                break;
            case CompanionColor.Green:
                animator.runtimeAnimatorController = greenController;
                break;
        }
    }
}
