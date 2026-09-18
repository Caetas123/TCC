using UnityEngine;

public static class UIInputUtility
{
    private static readonly KeyCode[] SubmitKeys =
    {
        KeyCode.Return,
        KeyCode.KeypadEnter,
        KeyCode.JoystickButton0,
        KeyCode.JoystickButton9
    };

    private static readonly KeyCode[] CancelKeys =
    {
        KeyCode.Escape,
        KeyCode.JoystickButton1,
        KeyCode.JoystickButton6,
        KeyCode.JoystickButton8
    };

    public static bool WasSubmitPressed()
    {
        return WasAnyPressed(SubmitKeys);
    }

    public static bool WasCancelPressed()
    {
        return WasAnyPressed(CancelKeys);
    }

    public static bool WasNavigateUpPressed()
    {
        return WasDirectionalPressed(KeyCode.W, KeyCode.UpArrow, "Vertical", 0.5f);
    }

    public static bool WasNavigateDownPressed()
    {
        return WasDirectionalPressed(KeyCode.S, KeyCode.DownArrow, "Vertical", -0.5f);
    }

    public static bool WasNavigateLeftPressed()
    {
        return WasDirectionalPressed(KeyCode.A, KeyCode.LeftArrow, "Horizontal", -0.5f);
    }

    public static bool WasNavigateRightPressed()
    {
        return WasDirectionalPressed(KeyCode.D, KeyCode.RightArrow, "Horizontal", 0.5f);
    }

    public static Vector2 ReadNavigationAxis()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;

        return new Vector2(horizontal, vertical);
    }

    private static bool WasAnyPressed(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
                return true;
        }

        return false;
    }

    private static bool WasDirectionalPressed(KeyCode keyboard, KeyCode arrow, string axisName, float threshold)
    {
        if (Input.GetKeyDown(keyboard) || Input.GetKeyDown(arrow))
            return true;

        float axis = Input.GetAxisRaw(axisName);
        return threshold > 0f ? axis > threshold : axis < threshold;
    }
}
