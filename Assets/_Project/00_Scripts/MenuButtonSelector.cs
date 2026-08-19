using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject selectedArrow;

    private void Awake()
    {
        if (selectedArrow != null)
            selectedArrow.SetActive(false);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowSelector();

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        HideSelector();
    }
    public void OnSelect(BaseEventData eventData)
    {
        // Optional: Add visual feedback for selection
        ShowSelector();
    }
    public void OnDeselect(BaseEventData eventData)
    {
        // Optional: Add visual feedback for deselection
        HideSelector();
    }
    private void ShowSelector()
    {
        if (selectedArrow != null)
            selectedArrow.SetActive(true);
    }
    public void HideSelector()
    {
        if (selectedArrow != null)
            selectedArrow.SetActive(false);
    }
}
