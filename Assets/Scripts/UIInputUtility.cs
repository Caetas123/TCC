using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

    /// <summary>
    /// Confirmação usada pelos jogadores nos menus. O menu padrão da Unity só
    /// conhece Enter/Espaço; o jogo também precisa aceitar o ataque configurado
    /// de cada jogador (F/K por padrão, ou a tecla remapeada).
    /// </summary>
    public static bool WasPlayerConfirmPressed()
    {
        return WasKeyPressed(ObterTeclaConfigurada("P1_Ataque", KeyCode.F))
            || WasKeyPressed(ObterTeclaConfigurada("P2_Ataque", KeyCode.K));
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
        float horizontal = LerEixoComSeguranca("Horizontal");
        float vertical = LerEixoComSeguranca("Vertical");

        if (IsKeyPressed(KeyCode.A) || IsKeyPressed(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (IsKeyPressed(KeyCode.D) || IsKeyPressed(KeyCode.RightArrow))
            horizontal = 1f;

        if (IsKeyPressed(KeyCode.W) || IsKeyPressed(KeyCode.UpArrow))
            vertical = 1f;
        else if (IsKeyPressed(KeyCode.S) || IsKeyPressed(KeyCode.DownArrow))
            vertical = -1f;

        return new Vector2(horizontal, vertical);
    }

    /// <summary>
    /// Leitura compatível com o Input System novo e com o Input Manager antigo.
    /// O projeto usa os dois em cenas diferentes, então os menus não podem
    /// depender de apenas uma das APIs.
    /// </summary>
    public static bool WasKeyPressed(KeyCode key)
    {
        if (key == KeyCode.None)
            return false;

        bool legacy = false;
        #if ENABLE_LEGACY_INPUT_MANAGER
        try { legacy = Input.GetKeyDown(key); }
        catch (System.InvalidOperationException) { }
        #endif

        return legacy || ObterControleTeclado(key)?.wasPressedThisFrame == true;
    }

    public static bool IsKeyPressed(KeyCode key)
    {
        if (key == KeyCode.None)
            return false;

        bool legacy = false;
        #if ENABLE_LEGACY_INPUT_MANAGER
        try { legacy = Input.GetKey(key); }
        catch (System.InvalidOperationException) { }
        #endif

        return legacy || ObterControleTeclado(key)?.isPressed == true;
    }

    /// <summary>
    /// Dispara o Submit no elemento atualmente focado. É usado para que as
    /// teclas de ataque também funcionem em Buttons, Dropdowns e outros
    /// Selectables, inclusive com o jogo pausado.
    /// </summary>
    public static bool SubmitSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selecionado = eventSystem != null
            ? eventSystem.currentSelectedGameObject
            : null;

        if (eventSystem == null || selecionado == null || !selecionado.activeInHierarchy)
            return false;

        ExecuteEvents.Execute(
            selecionado,
            new BaseEventData(eventSystem),
            ExecuteEvents.submitHandler);
        return true;
    }

    private static bool WasAnyPressed(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (WasKeyPressed(keys[i]))
                return true;
        }

        return false;
    }

    private static bool WasDirectionalPressed(KeyCode keyboard, KeyCode arrow, string axisName, float threshold)
    {
        if (WasKeyPressed(keyboard) || WasKeyPressed(arrow))
            return true;

        float axis = LerEixoComSeguranca(axisName);
        return threshold > 0f ? axis > threshold : axis < threshold;
    }

    private static float LerEixoComSeguranca(string nome)
    {
        #if ENABLE_LEGACY_INPUT_MANAGER
        try { return Input.GetAxisRaw(nome); }
        catch (System.InvalidOperationException) { }
        #endif
        return 0f;
    }

    private static KeyCode ObterTeclaConfigurada(string chave, KeyCode padrao)
    {
        string valor = PlayerPrefs.GetString(chave, padrao.ToString());
        try { return (KeyCode)System.Enum.Parse(typeof(KeyCode), valor); }
        catch { return padrao; }
    }

    private static KeyControl ObterControleTeclado(KeyCode key)
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return null;

        switch (key)
        {
            case KeyCode.A: return teclado.aKey;
            case KeyCode.B: return teclado.bKey;
            case KeyCode.C: return teclado.cKey;
            case KeyCode.D: return teclado.dKey;
            case KeyCode.E: return teclado.eKey;
            case KeyCode.F: return teclado.fKey;
            case KeyCode.G: return teclado.gKey;
            case KeyCode.H: return teclado.hKey;
            case KeyCode.I: return teclado.iKey;
            case KeyCode.J: return teclado.jKey;
            case KeyCode.K: return teclado.kKey;
            case KeyCode.L: return teclado.lKey;
            case KeyCode.M: return teclado.mKey;
            case KeyCode.N: return teclado.nKey;
            case KeyCode.O: return teclado.oKey;
            case KeyCode.P: return teclado.pKey;
            case KeyCode.Q: return teclado.qKey;
            case KeyCode.R: return teclado.rKey;
            case KeyCode.S: return teclado.sKey;
            case KeyCode.T: return teclado.tKey;
            case KeyCode.U: return teclado.uKey;
            case KeyCode.V: return teclado.vKey;
            case KeyCode.W: return teclado.wKey;
            case KeyCode.X: return teclado.xKey;
            case KeyCode.Y: return teclado.yKey;
            case KeyCode.Z: return teclado.zKey;
            case KeyCode.Alpha0: return teclado.digit0Key;
            case KeyCode.Alpha1: return teclado.digit1Key;
            case KeyCode.Alpha2: return teclado.digit2Key;
            case KeyCode.Alpha3: return teclado.digit3Key;
            case KeyCode.Alpha4: return teclado.digit4Key;
            case KeyCode.Alpha5: return teclado.digit5Key;
            case KeyCode.Alpha6: return teclado.digit6Key;
            case KeyCode.Alpha7: return teclado.digit7Key;
            case KeyCode.Alpha8: return teclado.digit8Key;
            case KeyCode.Alpha9: return teclado.digit9Key;
            case KeyCode.Keypad0: return teclado.numpad0Key;
            case KeyCode.Keypad1: return teclado.numpad1Key;
            case KeyCode.Keypad2: return teclado.numpad2Key;
            case KeyCode.Keypad3: return teclado.numpad3Key;
            case KeyCode.Keypad4: return teclado.numpad4Key;
            case KeyCode.Keypad5: return teclado.numpad5Key;
            case KeyCode.Keypad6: return teclado.numpad6Key;
            case KeyCode.Keypad7: return teclado.numpad7Key;
            case KeyCode.Keypad8: return teclado.numpad8Key;
            case KeyCode.Keypad9: return teclado.numpad9Key;
            case KeyCode.Minus: return teclado.minusKey;
            case KeyCode.Equals: return teclado.equalsKey;
            case KeyCode.LeftBracket: return teclado.leftBracketKey;
            case KeyCode.RightBracket: return teclado.rightBracketKey;
            case KeyCode.Backslash: return teclado.backslashKey;
            case KeyCode.Semicolon: return teclado.semicolonKey;
            case KeyCode.Quote: return teclado.quoteKey;
            case KeyCode.Comma: return teclado.commaKey;
            case KeyCode.Period: return teclado.periodKey;
            case KeyCode.Slash: return teclado.slashKey;
            case KeyCode.BackQuote: return teclado.backquoteKey;
            case KeyCode.UpArrow: return teclado.upArrowKey;
            case KeyCode.DownArrow: return teclado.downArrowKey;
            case KeyCode.LeftArrow: return teclado.leftArrowKey;
            case KeyCode.RightArrow: return teclado.rightArrowKey;
            case KeyCode.Return: return teclado.enterKey;
            case KeyCode.KeypadEnter: return teclado.numpadEnterKey;
            case KeyCode.Space: return teclado.spaceKey;
            case KeyCode.Escape: return teclado.escapeKey;
            case KeyCode.Tab: return teclado.tabKey;
            default: return null;
        }
    }
}
