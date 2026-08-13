using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UIFocusUtility
{
    public static void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public static void Select(GameObject target)
    {
        if (EventSystem.current == null || target == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    public static Coroutine SelectNextFrame(MonoBehaviour owner, GameObject target)
    {
        if (owner == null || target == null)
            return null;

        return owner.StartCoroutine(SelectNextFrameRoutine(target));
    }

    private static IEnumerator SelectNextFrameRoutine(GameObject target)
    {
        yield return null;
        Select(target);
    }
}
