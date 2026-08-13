using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventoHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action aoEntrar;
    public Action aoSair;

    public void OnPointerEnter(PointerEventData eventData)
    {
        aoEntrar?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        aoSair?.Invoke();
    }
}