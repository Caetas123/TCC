using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CriadorHUDLuta
{
    [MenuItem("Ferramentas/Criar HUD Luta")]
    public static void CriarHUDLuta()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("Nenhum Canvas encontrado na cena.");
            return;
        }

        Transform hudExistente = canvas.transform.Find("HUDLuta");
        if (hudExistente != null)
        {
            Debug.LogWarning("HUDLuta já existe no Canvas.");
            Selection.activeGameObject = hudExistente.gameObject;
            return;
        }

        GameObject hud = CriarUIObject("HUDLuta", canvas.transform);
        RectTransform hudRect = hud.GetComponent<RectTransform>();
        EsticarTelaInteira(hudRect);

        GameObject nomeP1 = CriarTextoTMP("NomeP1", hud.transform, "P1");
        ConfigurarRect(nomeP1.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -35), new Vector2(300, 40));

        GameObject barraVidaP1 = CriarSlider("BarraVidaP1", hud.transform, new Color(0.8f, 0.1f, 0.1f));
        ConfigurarRect(barraVidaP1.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -40), new Vector2(350, 30));

        GameObject barraEnergiaP1 = CriarSlider("BarraEnergiaP1", hud.transform, new Color(0.1f, 0.4f, 0.9f));
        ConfigurarRect(barraEnergiaP1.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -80), new Vector2(350, 20));

        GameObject nomeP2 = CriarTextoTMP("NomeP2", hud.transform, "P2");
        TMP_Text textoP2 = nomeP2.GetComponent<TMP_Text>();
        textoP2.alignment = TextAlignmentOptions.Right;
        ConfigurarRect(nomeP2.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -35), new Vector2(300, 40));

        GameObject barraVidaP2 = CriarSlider("BarraVidaP2", hud.transform, new Color(0.8f, 0.1f, 0.1f));
        ConfigurarRect(barraVidaP2.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -40), new Vector2(350, 30));

        GameObject barraEnergiaP2 = CriarSlider("BarraEnergiaP2", hud.transform, new Color(0.1f, 0.4f, 0.9f));
        ConfigurarRect(barraEnergiaP2.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -80), new Vector2(350, 20));

        GameObject painelFim = CriarPainel("PainelFim", hud.transform, new Color(0f, 0f, 0f, 0.7f));
        ConfigurarRect(painelFim.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 200));
        painelFim.SetActive(false);

        GameObject textoVencedor = CriarTextoTMP("TextoVencedor", painelFim.transform, "Vencedor");
        TMP_Text textoFim = textoVencedor.GetComponent<TMP_Text>();
        textoFim.alignment = TextAlignmentOptions.Center;
        textoFim.fontSize = 40;

        RectTransform txtFimRect = textoVencedor.GetComponent<RectTransform>();
        txtFimRect.anchorMin = Vector2.zero;
        txtFimRect.anchorMax = Vector2.one;
        txtFimRect.offsetMin = new Vector2(20, 20);
        txtFimRect.offsetMax = new Vector2(-20, -20);

        Selection.activeGameObject = hud;
        Debug.Log("HUDLuta criado com sucesso.");
    }

    static GameObject CriarUIObject(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go;
    }

    static void EsticarTelaInteira(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void ConfigurarRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    static GameObject CriarTextoTMP(string nome, Transform pai, string textoInicial)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(pai, false);

        TMP_Text texto = go.GetComponent<TMP_Text>();
        texto.text = textoInicial;
        texto.fontSize = 28;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Left;

        return go;
    }

    static GameObject CriarPainel(string nome, Transform pai, Color cor)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(pai, false);

        Image img = go.GetComponent<Image>();
        img.color = cor;

        return go;
    }

    static GameObject CriarSlider(string nome, Transform pai, Color corFill)
    {
        GameObject sliderGO = new GameObject(nome, typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(pai, false);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGO.transform, false);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 5);
        fillAreaRect.offsetMax = new Vector2(-5, -5);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = corFill;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        return sliderGO;
    }
}