using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mantém a interface em um espaço de design 16:9 sem colocar imagens sobre
/// a câmera do jogo. As barras externas são responsabilidade da câmera de
/// fundo, nunca de um Image dentro do Canvas.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class UIResponsiveLayout : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private const float ProporcaoMinimaParaLargura = 4f / 3f;
    private const float ProporcaoReferencia = 16f / 9f;

    private CanvasScaler scaler;
    private RectTransform areaSegura;
    private int larguraAnterior;
    private int alturaAnterior;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstalarNaCenaAtual()
    {
        AplicarEmTodosOsCanvas();
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        AplicarEmTodosOsCanvas();
    }

    private static void AplicarEmTodosOsCanvas()
    {
        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CanvasScaler canvasScaler in scalers)
        {
            if (canvasScaler == null)
                continue;

            Canvas canvas = canvasScaler.GetComponent<Canvas>();
            if (!EhCanvasPrincipal(canvas))
                continue;

            UIResponsiveLayout adaptador = canvasScaler.GetComponent<UIResponsiveLayout>();
            if (adaptador == null)
                adaptador = canvasScaler.gameObject.AddComponent<UIResponsiveLayout>();

            adaptador.AplicarAgora();
        }
    }

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        GarantirAreaSegura();
        AplicarAgora();
    }

    private void OnEnable()
    {
        AplicarAgora();
    }

    private void Update()
    {
        if (larguraAnterior != Screen.width || alturaAnterior != Screen.height)
            AplicarAgora();
    }

    public void AplicarAgora()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (!EhCanvasPrincipal(canvas))
            return;

        if (scaler == null)
            scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        GarantirAreaSegura();

        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        float proporcaoTela = Mathf.Max(1f, Screen.width) / Mathf.Max(1f, Screen.height);
        float transicao = Mathf.InverseLerp(
            ProporcaoMinimaParaLargura,
            ProporcaoReferencia,
            proporcaoTela);
        scaler.matchWidthOrHeight = Mathf.SmoothStep(0f, 1f, transicao);

        if (areaSegura != null)
        {
            areaSegura.anchorMin = new Vector2(0.5f, 0.5f);
            areaSegura.anchorMax = new Vector2(0.5f, 0.5f);
            areaSegura.pivot = new Vector2(0.5f, 0.5f);
            areaSegura.anchoredPosition = Vector2.zero;
            areaSegura.sizeDelta = referenceResolution;
            areaSegura.localScale = Vector3.one;
            areaSegura.SetAsLastSibling();
        }

        larguraAnterior = Screen.width;
        alturaAnterior = Screen.height;
    }

    private void GarantirAreaSegura()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (!EhCanvasPrincipal(canvas))
            return;

        // Remove o objeto defeituoso criado pela versão anterior. Mesmo como
        // primeiro filho, um Image de Canvas Overlay é desenhado sobre o mundo.
        Transform fundoAntigo = transform.Find("UI_16x9_LetterboxBackground");
        if (fundoAntigo != null)
        {
            fundoAntigo.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(fundoAntigo.gameObject);
            else
                DestroyImmediate(fundoAntigo.gameObject);
        }

        if (areaSegura == null)
            areaSegura = transform.Find("UI_16x9_SafeArea") as RectTransform;

        if (areaSegura == null)
        {
            GameObject objetoArea = new GameObject("UI_16x9_SafeArea", typeof(RectTransform));
            objetoArea.layer = gameObject.layer;
            areaSegura = objetoArea.GetComponent<RectTransform>();
            areaSegura.SetParent(transform, false);
        }

        List<Transform> filhosParaMover = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform filho = transform.GetChild(i);
            if (filho != areaSegura && filho != fundoAntigo)
                filhosParaMover.Add(filho);
        }

        foreach (Transform filho in filhosParaMover)
            filho.SetParent(areaSegura, false);
    }

    private static bool EhCanvasPrincipal(Canvas canvas)
    {
        if (canvas == null || canvas.rootCanvas != canvas)
            return false;

        Transform pai = canvas.transform.parent;
        return pai == null || pai.GetComponent<Camera>() != null;
    }
}
