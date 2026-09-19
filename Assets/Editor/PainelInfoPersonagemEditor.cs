using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cria a estrutura persistente do modal na Hierarchy da cena de seleção.
/// Assim o layout pode ser ajustado no Scene View antes de executar o jogo.
/// </summary>
[InitializeOnLoad]
public static class PainelInfoPersonagemEditor
{
    private const string NomeModal = "ModalInfoPersonagem";
    private const string SpriteBotaoAzulPath = "Assets/Images/Botões/botao.png";
    private const string SpriteBotaoAmareloPath = "Assets/Images/Botões/botaoAma.png";

    static PainelInfoPersonagemEditor()
    {
        EditorApplication.delayCall += GarantirEstruturaNasCenasAbertas;
    }

    [MenuItem("Ferramentas/Modal de Informações/Criar estrutura na cena aberta")]
    public static void CriarEstruturaNaCenaAberta()
    {
        PainelInfoPersonagem painel = Object.FindFirstObjectByType<PainelInfoPersonagem>(FindObjectsInactive.Include);
        if (painel == null)
        {
            Debug.LogWarning("Nenhum PainelInfoPersonagem foi encontrado na cena aberta.");
            return;
        }

        if (GarantirEstrutura(painel))
        {
            EditorSceneManager.MarkSceneDirty(painel.gameObject.scene);
            EditorSceneManager.SaveScene(painel.gameObject.scene);
        }

        Selection.activeGameObject = painel.transform.Find(NomeModal) != null
            ? painel.transform.Find(NomeModal).gameObject
            : painel.gameObject;
    }

    static void GarantirEstruturaNasCenasAbertas()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool algumaCenaAlterada = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene cena = SceneManager.GetSceneAt(i);
            if (!cena.isLoaded)
                continue;

            foreach (GameObject raiz in cena.GetRootGameObjects())
            {
                PainelInfoPersonagem[] paineis = raiz.GetComponentsInChildren<PainelInfoPersonagem>(true);
                foreach (PainelInfoPersonagem painel in paineis)
                {
                    if (GarantirEstrutura(painel))
                    {
                        EditorSceneManager.MarkSceneDirty(cena);
                        algumaCenaAlterada = true;
                    }
                }
            }
        }

        if (algumaCenaAlterada)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene cena = SceneManager.GetSceneAt(i);
                if (cena.isLoaded && cena.isDirty)
                    EditorSceneManager.SaveScene(cena);
            }
        }
    }

    static bool GarantirEstrutura(PainelInfoPersonagem painel)
    {
        if (painel == null)
            return false;

        if (painel.transform.Find(NomeModal) != null)
            return AtualizarEstruturaExistente(painel);

        Sprite spriteAzul = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBotaoAzulPath);
        Sprite spriteAmarelo = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBotaoAmareloPath);
        TelaSelecaoPlayer tela = painel.GetComponent<TelaSelecaoPlayer>();
        TMP_FontAsset fonte = tela != null && tela.nomePlayer1 != null ? tela.nomePlayer1.font : null;

        ConfigurarSpritesNoComponente(painel, spriteAzul, spriteAmarelo);

        GameObject overlay = CriarUI(NomeModal, painel.transform);
        overlay.AddComponent<Image>().color = new Color(0.005f, 0.01f, 0.03f, 0.82f);
        DefinirEsticado(overlay.GetComponent<RectTransform>());

        GameObject janela = CriarPainel("JanelaInfoPersonagem", overlay.transform,
            new Vector2(1680f, 930f), new Color(0.035f, 0.06f, 0.13f, 0.98f));
        Outline contorno = janela.AddComponent<Outline>();
        contorno.effectColor = new Color(0.95f, 0.78f, 0.3f, 0.95f);
        contorno.effectDistance = new Vector2(4f, -4f);

        CriarFaixa("FaixaSuperior", janela.transform, new Vector2(0f, 316f), new Vector2(1400f, 3f));
        CriarFaixa("FaixaInferior", janela.transform, new Vector2(0f, -298f), new Vector2(1400f, 2f));

        CriarTexto("TituloInfo", janela.transform, "INFORMAÇÕES DO PERSONAGEM", fonte, 42f,
            Color.white, TextAlignmentOptions.Center, new Vector2(0f, 409f), new Vector2(1400f, 70f), true);
        CriarTexto("SubtituloInfo", janela.transform, "Passe pelos ataques para ver os frames em ação", fonte, 24f,
            new Color(0.8f, 0.86f, 0.95f), TextAlignmentOptions.Center, new Vector2(0f, 362f), new Vector2(1400f, 42f), false);

        GameObject fundoPreview = CriarPainel("FundoPreview", janela.transform, new Vector2(760f, 500f), new Color(0.015f, 0.025f, 0.06f, 0.78f));
        DefinirPosicao(fundoPreview.GetComponent<RectTransform>(), new Vector2(-390f, 15f), new Vector2(760f, 500f));
        fundoPreview.AddComponent<RectMask2D>();

        GameObject preview = CriarUI("PreviewAnimado", fundoPreview.transform);
        Image imagemPreview = preview.AddComponent<Image>();
        imagemPreview.preserveAspect = true;
        imagemPreview.raycastTarget = false;
        imagemPreview.sprite = ObterSpritePreview(tela);
        DefinirPosicao(preview.GetComponent<RectTransform>(), new Vector2(-135f, 25f), new Vector2(360f, 400f));

        GameObject alvo = CriarUI("AlvoAnimado", fundoPreview.transform);
        Image imagemAlvo = alvo.AddComponent<Image>();
        imagemAlvo.preserveAspect = true;
        imagemAlvo.raycastTarget = false;
        imagemAlvo.sprite = ObterSpriteJamanta(tela);
        DefinirPosicao(alvo.GetComponent<RectTransform>(), new Vector2(380f, -55f), new Vector2(280f, 340f));
        alvo.SetActive(false);

        GameObject efeito = CriarUI("EfeitoAnimado", fundoPreview.transform);
        Image imagemEfeito = efeito.AddComponent<Image>();
        imagemEfeito.preserveAspect = true;
        imagemEfeito.raycastTarget = false;
        imagemEfeito.sprite = ObterSpriteRachadura(tela);
        efeito.SetActive(false);
        DefinirPosicao(efeito.GetComponent<RectTransform>(), new Vector2(190f, -205f), new Vector2(430f, 120f));

        CriarTexto("LegendaPreview", janela.transform, "ANIMAÇÃO DE COMBATE", fonte, 21f,
            new Color(0.95f, 0.78f, 0.3f), TextAlignmentOptions.Center, new Vector2(-390f, -335f), new Vector2(760f, 36f), false);

        string[] nomesAcoes = { "ATAQUE", "ESPECIAL", "ULTIMATE", "DEFESA" };
        for (int i = 0; i < nomesAcoes.Length; i++)
        {
            GameObject botao = CriarBotao("BotaoAcao" + i, janela.transform, nomesAcoes[i], fonte,
                new Vector2(190f, 60f), new Color(0.015f, 0.015f, 0.02f, 0.98f), spriteAzul, spriteAmarelo);
            DefinirPosicao(botao.GetComponent<RectTransform>(), new Vector2(-55f + i * 205f, 265f), new Vector2(190f, 60f));
        }

        CriarTexto("DescricaoAcao", janela.transform, "ATAQUE\n\nSelecione uma ação para visualizar os frames da animação e os dados do golpe.", fonte, 52f,
            Color.white, TextAlignmentOptions.TopLeft, new Vector2(390f, 15f), new Vector2(760f, 410f), false);
        CriarTexto("AtributosPersonagem", janela.transform, "ATRIBUTOS\n\nEstilo: Equilibrado\nVelocidade: 6     Força do pulo: 12\nRecarga de energia: 10", fonte, 38f,
            new Color(0.83f, 0.88f, 0.96f), TextAlignmentOptions.TopLeft, new Vector2(390f, -285f), new Vector2(760f, 145f), false);

        GameObject fechar = CriarBotao("BotaoFecharInfo", janela.transform, "FECHAR", fonte,
            new Vector2(220f, 60f), new Color(0.015f, 0.015f, 0.02f, 0.98f), spriteAzul, spriteAmarelo);
        DefinirPosicao(fechar.GetComponent<RectTransform>(), new Vector2(610f, -390f), new Vector2(220f, 60f));

        CriarBotao("BotaoInfoP1", painel.transform, "INFO", fonte,
            new Vector2(140f, 58f), Color.white, spriteAzul, spriteAmarelo);
        CriarBotao("BotaoInfoP2", painel.transform, "INFO", fonte,
            new Vector2(140f, 58f), Color.white, spriteAzul, spriteAmarelo);

        Transform botaoInfoP1 = painel.transform.Find("BotaoInfoP1");
        Transform botaoInfoP2 = painel.transform.Find("BotaoInfoP2");
        DefinirPosicao(botaoInfoP1.GetComponent<RectTransform>(), Vector2.zero, new Vector2(140f, 58f));
        DefinirPosicao(botaoInfoP2.GetComponent<RectTransform>(), Vector2.zero, new Vector2(140f, 58f));
        if (tela != null)
        {
            if (tela.nomePlayer1 != null)
                botaoInfoP1.GetComponent<RectTransform>().position = tela.nomePlayer1.rectTransform.position + new Vector3(0f, -50f, 0f);
            if (tela.nomePlayer2 != null)
                botaoInfoP2.GetComponent<RectTransform>().position = tela.nomePlayer2.rectTransform.position + new Vector3(0f, -50f, 0f);
        }

        EditorUtility.SetDirty(painel);
        EditorUtility.SetDirty(overlay);
        return true;
    }

    static bool AtualizarEstruturaExistente(PainelInfoPersonagem painel)
    {
        Transform modal = painel.transform.Find(NomeModal);
        Transform janela = modal != null ? modal.Find("JanelaInfoPersonagem") : null;
        Transform fundoPreview = janela != null ? janela.Find("FundoPreview") : null;
        if (modal == null || janela == null || fundoPreview == null)
            return false;

        bool alterado = false;
        if (!modal.gameObject.activeSelf)
        {
            modal.gameObject.SetActive(true);
            alterado = true;
        }

        Transform alvo = fundoPreview.Find("AlvoAnimado");
        if (alvo == null)
        {
            alvo = CriarUI("AlvoAnimado", fundoPreview).transform;
            Image imagemAlvo = alvo.gameObject.AddComponent<Image>();
            imagemAlvo.preserveAspect = true;
            imagemAlvo.raycastTarget = false;
            imagemAlvo.sprite = ObterSpriteJamanta(painel.GetComponent<TelaSelecaoPlayer>());
            DefinirPosicao(alvo.GetComponent<RectTransform>(), new Vector2(380f, -55f), new Vector2(280f, 340f));
            alvo.gameObject.SetActive(false);
            alterado = true;
        }

        if (alterado)
        {
            ConfigurarSpritesNoComponente(painel,
                AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBotaoAzulPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBotaoAmareloPath));
            EditorUtility.SetDirty(painel);
            EditorUtility.SetDirty(modal.gameObject);
        }

        return alterado;
    }

    static bool DefinirPosicaoSeNecessario(RectTransform rect, Vector2 posicao, Vector2 tamanho)
    {
        if (rect == null)
            return false;

        bool alterado = rect.anchoredPosition != posicao || rect.sizeDelta != tamanho;
        if (alterado)
            DefinirPosicao(rect, posicao, tamanho);
        return alterado;
    }

    static void ConfigurarSpritesNoComponente(PainelInfoPersonagem painel, Sprite azul, Sprite amarelo)
    {
        SerializedObject serialized = new SerializedObject(painel);
        SerializedProperty spriteAzul = serialized.FindProperty("spriteBotaoAzul");
        SerializedProperty spriteAmarelo = serialized.FindProperty("spriteBotaoAmarelo");
        if (spriteAzul != null && spriteAzul.objectReferenceValue == null)
            spriteAzul.objectReferenceValue = azul;
        if (spriteAmarelo != null && spriteAmarelo.objectReferenceValue == null)
            spriteAmarelo.objectReferenceValue = amarelo;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static Sprite ObterSpritePreview(TelaSelecaoPlayer tela)
    {
        if (tela == null || tela.dadosPersonagens == null)
            return null;

        foreach (DadosPersonagem dados in tela.dadosPersonagens)
        {
            if (dados == null)
                continue;
            if (dados.framesIdle != null && dados.framesIdle.Length > 0 && dados.framesIdle[0] != null)
                return dados.framesIdle[0];
            if (dados.spriteCorpo != null)
                return dados.spriteCorpo;
        }

        return null;
    }

    static Sprite ObterSpriteRachadura(TelaSelecaoPlayer tela)
    {
        if (tela == null || tela.dadosPersonagens == null)
            return null;

        foreach (DadosPersonagem dados in tela.dadosPersonagens)
        {
            if (dados != null && dados.spriteRachadura != null)
                return dados.spriteRachadura;
        }

        return null;
    }

    static Sprite ObterSpriteJamanta(TelaSelecaoPlayer tela)
    {
        if (tela == null || tela.dadosPersonagens == null)
            return null;

        foreach (DadosPersonagem dados in tela.dadosPersonagens)
        {
            if (dados == null || string.IsNullOrEmpty(dados.nomePersonagem))
                continue;

            if (!dados.nomePersonagem.ToUpperInvariant().Contains("JAMANTA"))
                continue;

            if (dados.framesIdle != null && dados.framesIdle.Length > 0)
                return dados.framesIdle[0];
            return dados.spriteCorpo;
        }

        return null;
    }

    static GameObject CriarUI(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);
        Undo.RegisterCreatedObjectUndo(objeto, "Criar modal de informações");
        return objeto;
    }

    static GameObject CriarPainel(string nome, Transform pai, Vector2 tamanho, Color cor)
    {
        GameObject objeto = CriarUI(nome, pai);
        Image imagem = objeto.AddComponent<Image>();
        imagem.color = cor;
        DefinirPosicao(objeto.GetComponent<RectTransform>(), Vector2.zero, tamanho);
        return objeto;
    }

    static GameObject CriarTexto(string nome, Transform pai, string textoInicial, TMP_FontAsset fonte,
        float tamanhoFonte, Color cor, TextAlignmentOptions alinhamento, Vector2 posicao, Vector2 tamanho, bool negrito)
    {
        GameObject objeto = CriarUI(nome, pai);
        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.font = fonte;
        texto.text = textoInicial;
        texto.fontSize = tamanhoFonte;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.fontStyle = negrito ? FontStyles.Bold : FontStyles.Normal;
        texto.raycastTarget = false;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.enableWordWrapping = true;
        if (nome == "DescricaoAcao")
        {
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 28f;
            texto.fontSizeMax = tamanhoFonte;
            texto.lineSpacing = -6f;
        }
        if (nome == "AtributosPersonagem")
        {
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 22f;
            texto.fontSizeMax = Mathf.Min(tamanhoFonte, 34f);
            texto.lineSpacing = -8f;
        }
        DefinirPosicao(objeto.GetComponent<RectTransform>(), posicao, tamanho);
        return objeto;
    }

    static GameObject CriarBotao(string nome, Transform pai, string textoInicial, TMP_FontAsset fonte,
        Vector2 tamanho, Color corNormal, Sprite spriteAzul, Sprite spriteAmarelo)
    {
        GameObject objeto = CriarUI(nome, pai);
        Image imagem = objeto.AddComponent<Image>();
        imagem.sprite = spriteAzul;
        imagem.color = corNormal;

        Button botao = objeto.AddComponent<Button>();
        botao.targetGraphic = imagem;
        botao.transition = Selectable.Transition.SpriteSwap;
        SpriteState estado = botao.spriteState;
        estado.highlightedSprite = spriteAmarelo;
        estado.pressedSprite = spriteAmarelo;
        estado.selectedSprite = spriteAmarelo;
        botao.spriteState = estado;

        CriarTexto("Texto" + nome, objeto.transform, textoInicial, fonte, 30f,
            Color.white, TextAlignmentOptions.Center, Vector2.zero, tamanho, false);
        DefinirPosicao(objeto.GetComponent<RectTransform>(), Vector2.zero, tamanho);
        return objeto;
    }

    static void CriarFaixa(string nome, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        GameObject faixa = CriarUI(nome, pai);
        faixa.AddComponent<Image>().color = new Color(0.95f, 0.78f, 0.3f, 0.9f);
        DefinirPosicao(faixa.GetComponent<RectTransform>(), posicao, tamanho);
    }

    static void DefinirEsticado(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void DefinirPosicao(RectTransform rect, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        rect.localScale = Vector3.one;
    }
}
