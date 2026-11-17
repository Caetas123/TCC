using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Objetos de cenário que podem ser usados durante a luta.
///
/// O objeto permanece visível com uma silhueta amarela. Quando um jogador
/// humano se aproxima, aparece a tecla dele dentro de um círculo. Segurar a
/// tecla completa o círculo e reproduz os frames de interação do personagem.
/// </summary>
public sealed class ObjetoInterativoLuta : MonoBehaviour
{
    private static readonly List<ObjetoInterativoLuta> objetosAtivos = new List<ObjetoInterativoLuta>();
    private static readonly Dictionary<string, Sprite> spritesCriados = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite[]> animacoesCriadas = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, Sprite> glifosTecla = new Dictionary<string, Sprite>();
    private static Sprite fundoTecla;
    private static string ultimaCenaInicializada;

    private const float DuracaoPadraoSegurar = 0.8f;
    private const float DistanciaHorizontalPadrao = 2.4f;
    private const float DistanciaVerticalPadrao = 3.1f;
    private const float RaioPrompt = 0.44f;
    private const int SegmentosCirculo = 32;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer contornoSprite;
    private Material materialContorno;
    private Material materialPrompt;
    private Bounds limitesLocaisVisiveis;
    private GameManagerLuta partida;
    private LutadorController2D[] lutadoresDaCena;
    private bool arremessoIniciado;
    private Collider2D colisor;
    private GameObject prompt;
    private GameObject objetoArremessado;
    private TextMeshPro textoTecla;
    private SpriteRenderer glifoTecla;
    private LineRenderer circuloCompleto;
    private LineRenderer circuloProgresso;
    private LutadorController2D alvoAtual;
    private float progresso;
    private bool emUso;
    private bool arremessoConcluido;
    private bool consumidoNaRodada;

    private Sprite spriteObjeto;
    private Sprite spriteArremesso;
    private Vector2 tamanhoVisualArremesso = Vector2.one;
    private string nomeObjeto;
    private Color corBorda = new Color(1f, 0.83f, 0.08f, 1f);
    private float duracaoSegurar = DuracaoPadraoSegurar;
    private float distanciaHorizontal = DistanciaHorizontalPadrao;
    private float distanciaVertical = DistanciaVerticalPadrao;
    private float escalaVisual = 1f;
    private Vector2 tamanhoColisor = new Vector2(1f, 1f);
    private float alturaPrompt = 1.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegistrarInicializacaoDeCena()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        CriarObjetosDaCena(SceneManager.GetActiveScene());
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        CriarObjetosDaCena(cena);
    }

    private static void CriarObjetosDaCena(Scene cena)
    {
        if (cena.name != "cena1" && cena.name != "cena2")
            return;

        if (ultimaCenaInicializada == cena.name &&
            GameObject.Find("ObjetosInterativos") != null)
            return;

        GameObject antigo = GameObject.Find("ObjetosInterativos");
        if (antigo != null)
            Destroy(antigo);

        ultimaCenaInicializada = cena.name;
        GameObject raiz = new GameObject("ObjetosInterativos");

        float y = 542.25f;
        LutadorController2D[] lutadores = FindObjectsByType<LutadorController2D>(FindObjectsSortMode.None);
        if (lutadores != null && lutadores.Length > 0)
        {
            float menorY = float.MaxValue;
            for (int i = 0; i < lutadores.Length; i++)
            {
                if (lutadores[i] != null)
                    menorY = Mathf.Min(menorY, lutadores[i].transform.position.y);
            }

            if (menorY < float.MaxValue)
                y = menorY - 2.55f;
        }

        Camera cameraCena = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        float centroX = cameraCena != null ? cameraCena.transform.position.x : 935.5845f;

        if (cena.name == "cena1")
        {
            CriarObjeto(
                raiz.transform,
                "BolaInterativa",
                "Interativos/bola_quad",
                new Vector3(centroX - 1.75f, y + 0.32f, 0f),
                0.15f,
                new Vector2(0.85f, 0.85f));

            CriarObjeto(
                raiz.transform,
                "ArmarioInterativo",
                "Interativos/armario_ginasio",
                // Escala e linha de apoio da porta azul no fundo da quadra.
                new Vector3(centroX - 4.3f, y + 3.3f, 0f),
                0.24f,
                new Vector2(0.95f, 1.65f));
        }
        else
        {
            // O TV fixado na parede continua pertencendo ao fundo. O computador
            // da mesa e a mochila da cadeira são sprites separados para poderem
            // desaparecer no arremesso e voltar apenas no próximo round/reinício.
            CriarObjeto(
                raiz.transform,
                "ComputadorInterativo",
                "Interativos/computador_sala",
                new Vector3(centroX - 7.65f, y + 3.35f, 0f),
                0.25f,
                new Vector2(1.25f, 0.95f));

            CriarObjeto(
                raiz.transform,
                "MochilaInterativa",
                "Interativos/mochila_sala",
                new Vector3(centroX + 6.25f, y + 2.25f, 0f),
                0.20f,
                new Vector2(1.0f, 1.1f));
        }
    }

    private static void CriarObjeto(
        Transform pai,
        string nome,
        string caminhoSprite,
        Vector3 posicao,
        float escala,
        Vector2 tamanhoColisor)
    {
        Sprite sprite = CarregarSprite(caminhoSprite, 256f);
        if (sprite == null)
        {
            Debug.LogWarning("[ObjetoInterativoLuta] Sprite não encontrado: " + caminhoSprite);
            return;
        }

        GameObject objeto = new GameObject(nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.position = posicao;
        objeto.transform.localScale = Vector3.one * escala;

        SpriteRenderer sr = objeto.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = 0;
        sr.sortingOrder = nome == "BolaInterativa" ? 12 : -1;

        ObjetoInterativoLuta controlador = objeto.AddComponent<ObjetoInterativoLuta>();
        controlador.Configurar(nome, sprite, sr, escala, tamanhoColisor);
    }

    private static Sprite CarregarSprite(string caminho, float pixelsPorUnidade)
    {
        string chave = caminho + "_" + pixelsPorUnidade;
        if (spritesCriados.TryGetValue(chave, out Sprite existente) && existente != null)
            return existente;

        Sprite spriteImportado = Resources.Load<Sprite>(caminho);
        if (spriteImportado != null)
        {
            ConfigurarFiltroPixelado(spriteImportado.texture);
            spritesCriados[chave] = spriteImportado;
            return spriteImportado;
        }

        Texture2D textura = Resources.Load<Texture2D>(caminho);
        if (textura == null)
            return null;

        ConfigurarFiltroPixelado(textura);

        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, textura.width, textura.height),
            new Vector2(0.5f, 0f),
            pixelsPorUnidade,
            0,
            SpriteMeshType.FullRect);
        sprite.name = textura.name + "_RuntimeSprite";
        spritesCriados[chave] = sprite;
        return sprite;
    }

    private static Sprite[] CarregarAnimacao(string personagem)
    {
        string pasta = "AnimacoesObjeto/" + personagem;
        if (animacoesCriadas.TryGetValue(pasta, out Sprite[] existente) && existente != null)
            return existente;

        Sprite[] importadas = Resources.LoadAll<Sprite>(pasta);
        if (importadas != null && importadas.Length > 0)
        {
            System.Array.Sort(importadas, CompararSpritesPorNome);
            Sprite[] normalizadas = new Sprite[importadas.Length];
            for (int i = 0; i < importadas.Length; i++)
            {
                Sprite origem = importadas[i];
                if (origem == null || origem.texture == null)
                {
                    normalizadas[i] = origem;
                    continue;
                }

                ConfigurarFiltroPixelado(origem.texture);

                // Os frames foram exportados com PPU 100 e pivô variável. O corpo
                // da luta usa PPU 64 e pivô centro-baixo; normalizar aqui mantém
                // altura, chão e alinhamento iguais aos demais frames.
                normalizadas[i] = Sprite.Create(
                    origem.texture,
                    origem.rect,
                    new Vector2(0.5f, 0f),
                    64f,
                    0,
                    SpriteMeshType.FullRect);
                normalizadas[i].name = origem.name + "_Normalizado64_Runtime";
            }

            animacoesCriadas[pasta] = normalizadas;
            return normalizadas;
        }

        Texture2D[] texturas = Resources.LoadAll<Texture2D>(pasta);
        if (texturas == null || texturas.Length == 0)
            return null;

        System.Array.Sort(texturas, CompararTexturasPorNome);
        Sprite[] frames = new Sprite[texturas.Length];
        for (int i = 0; i < texturas.Length; i++)
        {
            Texture2D textura = texturas[i];
            ConfigurarFiltroPixelado(textura);
            frames[i] = Sprite.Create(
                textura,
                new Rect(0f, 0f, textura.width, textura.height),
                new Vector2(0.5f, 0f),
                64f,
                0,
                SpriteMeshType.FullRect);
            frames[i].name = textura.name + "_RuntimeSprite";
        }

        animacoesCriadas[pasta] = frames;
        return frames;
    }

    private static void ConfigurarFiltroPixelado(Texture2D textura)
    {
        if (textura == null)
            return;

        textura.filterMode = FilterMode.Point;
        textura.anisoLevel = 0;
        textura.mipMapBias = 0f;
    }

    private static int CompararSpritesPorNome(Sprite a, Sprite b)
    {
        return CompararNomes(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
    }

    private static int CompararTexturasPorNome(Texture2D a, Texture2D b)
    {
        return CompararNomes(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
    }

    private static int CompararNomes(string a, string b)
    {
        int numeroA = ExtrairNumeroFrame(a);
        int numeroB = ExtrairNumeroFrame(b);
        int comparacaoNumero = numeroA.CompareTo(numeroB);
        return comparacaoNumero != 0 ? comparacaoNumero : string.CompareOrdinal(a, b);
    }

    private static int ExtrairNumeroFrame(string nome)
    {
        int separador = nome.LastIndexOf('_');
        if (separador >= 0 && int.TryParse(nome.Substring(separador + 1), out int numero))
            return numero;

        return int.MaxValue;
    }

    private void Configurar(
        string nome,
        Sprite sprite,
        SpriteRenderer sr,
        float escala,
        Vector2 tamanho)
    {
        nomeObjeto = nome;
        spriteObjeto = sprite;

        spriteRenderer = sr;
        escalaVisual = escala;
        tamanhoColisor = tamanho;
        // Estes objetos ficam no plano dos móveis/parede; a aproximação do
        // lutador acontece no chão. Usa a distância horizontal para alcançá-los.
        if (nome == "ArmarioInterativo" || nome == "ComputadorInterativo" ||
            nome == "MochilaInterativa")
        {
            distanciaHorizontal = 1.6f;
            distanciaVertical = float.PositiveInfinity;
        }
        alturaPrompt = Mathf.Max(1.0f, sprite.bounds.size.y * escalaVisual);
        Rect recorte = ObterRecorteVisivel(nome, sprite);
        Vector2 minimo = (recorte.position - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit;
        Vector2 tamanhoVisivel = recorte.size / sprite.pixelsPerUnit;
        limitesLocaisVisiveis = new Bounds(minimo + tamanhoVisivel * 0.5f, tamanhoVisivel);
        // O pivô do projétil fica no centro dos pixels visíveis, não no pé do PNG.
        string chave = nome + "_ArremessoCentralizado";
        if (!spritesCriados.TryGetValue(chave, out spriteArremesso) || spriteArremesso == null)
        {
            spriteArremesso = Sprite.Create(sprite.texture, recorte, Vector2.one * 0.5f,
                sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            spriteArremesso.name = chave;
            spritesCriados[chave] = spriteArremesso;
        }
        tamanhoVisualArremesso = tamanhoVisivel * escalaVisual;
        partida = FindFirstObjectByType<GameManagerLuta>();
        lutadoresDaCena = FindObjectsByType<LutadorController2D>(FindObjectsSortMode.None);

        CriarBordaSprite();

        colisor = gameObject.AddComponent<BoxCollider2D>();
        colisor.isTrigger = true;
        ((BoxCollider2D)colisor).size = tamanhoColisor / Mathf.Max(0.01f, escalaVisual);

        CriarPrompt();
        EsconderPrompt();
    }

    private static Rect ObterRecorteVisivel(string nome, Sprite sprite)
    {
        // Limites de alfa dos PNGs, em pixels da textura. Independem do pivô,
        // PPU e do recorte definido pelo importador do Unity.
        Rect area = nome switch
        {
            "ArmarioInterativo" => new Rect(525, 79, 487, 865),
            "BolaInterativa" => new Rect(476, 212, 585, 600),
            "ComputadorInterativo" => new Rect(339, 190, 731, 712),
            "MochilaInterativa" => new Rect(285, 168, 708, 829),
            _ => sprite.rect
        };
        return Rect.MinMaxRect(Mathf.Max(area.xMin, sprite.rect.xMin),
            Mathf.Max(area.yMin, sprite.rect.yMin), Mathf.Min(area.xMax, sprite.rect.xMax),
            Mathf.Min(area.yMax, sprite.rect.yMax));
    }

    private void CriarBordaSprite()
    {
        Shader shader = Resources.Load<Shader>("ObjetoContorno");
        if (shader == null)
        {
            Debug.LogError("[ObjetoInterativoLuta] Shader de contorno ausente.");
            return;
        }
        GameObject borda = new GameObject("ContornoSilhueta", typeof(SpriteRenderer));
        borda.transform.SetParent(transform, false);
        contornoSprite = borda.GetComponent<SpriteRenderer>();
        // FullRect preserva a margem transparente necessária para o halo.
        string chave = nomeObjeto + "_Contorno";
        if (!spritesCriados.TryGetValue(chave, out Sprite sprite) || sprite == null)
        {
            sprite = Sprite.Create(spriteObjeto.texture, spriteObjeto.rect,
                spriteObjeto.pivot / spriteObjeto.rect.size, spriteObjeto.pixelsPerUnit,
                0, SpriteMeshType.FullRect);
            spritesCriados[chave] = sprite;
        }
        contornoSprite.sprite = sprite;
        contornoSprite.sortingLayerID = spriteRenderer.sortingLayerID;
        contornoSprite.sortingOrder = spriteRenderer.sortingOrder + 1;
        materialContorno = new Material(shader);
        materialContorno.SetColor("_Color", corBorda);
        materialContorno.SetFloat("_Largura", 0.045f * spriteObjeto.pixelsPerUnit / escalaVisual);
        contornoSprite.sharedMaterial = materialContorno;
    }

    private Bounds ObterLimitesVisiveis()
    {
        Vector3 tamanho = Vector3.Scale(limitesLocaisVisiveis.size, transform.lossyScale);
        return new Bounds(transform.TransformPoint(limitesLocaisVisiveis.center),
            new Vector3(Mathf.Abs(tamanho.x), Mathf.Abs(tamanho.y), Mathf.Abs(tamanho.z)));
    }

    private float DistanciaHorizontalPara(LutadorController2D lutador)
    {
        float dx = Mathf.Abs(lutador.transform.position.x - transform.position.x);
        if (float.IsPositiveInfinity(distanciaVertical) && spriteRenderer != null)
        {
            Bounds visiveis = ObterLimitesVisiveis();
            dx = Mathf.Max(0f,
                Mathf.Abs(lutador.transform.position.x - visiveis.center.x) - visiveis.extents.x);
        }
        return dx;
    }

    private void OnEnable()
    {
        if (!objetosAtivos.Contains(this))
            objetosAtivos.Add(this);
    }

    private void OnDisable()
    {
        objetosAtivos.Remove(this);
        if (alvoAtual != null)
            alvoAtual = null;
    }

    private void OnDestroy()
    {
        if (materialContorno != null) Destroy(materialContorno);
        if (materialPrompt != null) Destroy(materialPrompt);
        if (prompt != null) Destroy(prompt);
        if (objetoArremessado != null) Destroy(objetoArremessado);
    }

    private void Update()
    {
        if (materialContorno != null && !consumidoNaRodada)
            materialContorno.SetFloat("_Pulso", 0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f));

        if (consumidoNaRodada)
        {
            EsconderPrompt();
            return;
        }

        if (Time.timeScale == 0f || emUso || partida == null || !partida.PodePausar())
        {
            EsconderPrompt();
            return;
        }

        LutadorController2D novoAlvo = EncontrarAlvoMaisProximo();
        if (novoAlvo != alvoAtual)
        {
            alvoAtual = novoAlvo;
            progresso = 0f;
        }

        if (alvoAtual == null)
        {
            progresso = 0f;
            EsconderPrompt();
            return;
        }

        MostrarPrompt(alvoAtual);
        KeyCode tecla = alvoAtual.ObterTeclaInteracao();
        if (tecla == KeyCode.None || !Input.GetKey(tecla))
        {
            progresso = Mathf.MoveTowards(progresso, 0f, Time.deltaTime * 3.5f);
            AtualizarCirculo(progresso);
            return;
        }

        progresso = Mathf.MoveTowards(progresso, 1f, Time.deltaTime / Mathf.Max(0.05f, duracaoSegurar));
        AtualizarCirculo(progresso);

        if (progresso >= 0.999f)
            StartCoroutine(UsarObjeto(alvoAtual));
    }

    private LutadorController2D EncontrarAlvoMaisProximo()
    {
        LutadorController2D[] lutadores = lutadoresDaCena;
        if (lutadores == null) return null;
        LutadorController2D melhor = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < lutadores.Length; i++)
        {
            LutadorController2D lutador = lutadores[i];
            if (lutador == null || !lutador.isActiveAndEnabled || lutador.controladoPorIA)
                continue;
            if (!lutador.PodeInteragirComObjeto())
                continue;

            float dx = DistanciaHorizontalPara(lutador);
            float dy = Mathf.Abs(lutador.transform.position.y - transform.position.y);
            if (dx > distanciaHorizontal || dy > distanciaVertical)
                continue;

            float distancia = dx + dy * 0.35f;
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhor = lutador;
            }
        }

        return melhor;
    }

    public static bool TentarUsoPelaIA(LutadorController2D lutador)
    {
        if (Time.timeScale == 0f || lutador == null || !lutador.isActiveAndEnabled ||
            !lutador.controladoPorIA || !lutador.PodeInteragirComObjeto())
            return false;
        if (lutador.oponente == null || lutador.oponente.EstaMorto() ||
            Mathf.Abs(lutador.oponente.transform.position.x - lutador.transform.position.x) >
            ObterAlcanceMaximoArremesso())
            return false;

        GameManagerLuta partida = FindFirstObjectByType<GameManagerLuta>();
        if (partida == null || !partida.PodePausar())
            return false;

        ObjetoInterativoLuta escolhido = null;
        float menorDistancia = float.MaxValue;
        for (int i = 0; i < objetosAtivos.Count; i++)
        {
            ObjetoInterativoLuta objeto = objetosAtivos[i];
            if (objeto == null || !objeto.isActiveAndEnabled || objeto.emUso || objeto.consumidoNaRodada || objeto.progresso > 0.05f)
                continue;

            float dx = objeto.DistanciaHorizontalPara(lutador);
            float dy = Mathf.Abs(lutador.transform.position.y - objeto.transform.position.y);
            if (dx > objeto.distanciaHorizontal || dy > objeto.distanciaVertical)
                continue;

            float distancia = dx + dy * 0.35f;
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                escolhido = objeto;
            }
        }

        if (escolhido == null || escolhido.ObterFramesDoPersonagem(lutador) == null)
            return false;

        escolhido.StartCoroutine(escolhido.UsarObjeto(lutador));
        return true;
    }

    private void CriarPrompt()
    {
        prompt = new GameObject("PromptInteracao");
        prompt.transform.SetParent(transform.parent, false);
        prompt.transform.localScale = Vector3.one;
        prompt.transform.position = ObterPosicaoPrompt();

        GameObject fundo = new GameObject("FundoEscuroTecla", typeof(SpriteRenderer));
        fundo.transform.SetParent(prompt.transform, false);
        SpriteRenderer disco = fundo.GetComponent<SpriteRenderer>();
        disco.sprite = ObterFundoTecla();
        disco.sortingOrder = 98;

        GameObject letra = new GameObject("LetraPixelada", typeof(SpriteRenderer));
        letra.transform.SetParent(prompt.transform, false);
        glifoTecla = letra.GetComponent<SpriteRenderer>();
        glifoTecla.sortingOrder = 101;

        GameObject textoObjeto = new GameObject("Tecla", typeof(TextMeshPro));
        textoObjeto.transform.SetParent(prompt.transform, false);
        textoTecla = textoObjeto.GetComponent<TextMeshPro>();
        textoTecla.alignment = TextAlignmentOptions.Center;
        textoTecla.fontSize = 4.5f;
        textoTecla.fontStyle = FontStyles.Bold;
        textoTecla.color = Color.white;
        textoTecla.outlineColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        textoTecla.outlineWidth = 0.34f;
        textoTecla.enableWordWrapping = false;
        textoTecla.sortingLayerID = 0;
        textoTecla.sortingOrder = 100;
        textoTecla.transform.localPosition = Vector3.zero;

        circuloCompleto = CriarLinhaCirculo("Borda", corBorda, 0.065f, true);
        circuloProgresso = CriarLinhaCirculo("Progresso", new Color(1f, 0.96f, 0.55f, 1f), 0.10f, false);
    }

    private static Sprite ObterFundoTecla()
    {
        if (fundoTecla != null)
            return fundoTecla;

        Texture2D textura = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[64 * 64];
        Color32 escuro = new Color32(10, 18, 32, 245);
        Color32 aroInterno = new Color32(185, 111, 18, 255);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float raio = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                pixels[y * 64 + x] = raio <= 30f ? (raio >= 27f ? aroInterno : escuro) : new Color32(0, 0, 0, 0);
            }
        }
        textura.SetPixels32(pixels);
        textura.Apply();
        fundoTecla = Sprite.Create(textura, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 76f);
        fundoTecla.name = "FundoTeclaInteracao_Runtime";
        return fundoTecla;
    }

    private static Sprite ObterGlifoTecla(string tecla)
    {
        if (tecla != "Q" && tecla != "O")
            return null;
        if (glifosTecla.TryGetValue(tecla, out Sprite existente) && existente != null)
            return existente;

        string[] desenho = tecla == "Q"
            ? new[] { ".#####.", "##...##", "##...##", "##...##", "##...##", "##.#.##", "##..##.", ".####.#", ".....##" }
            : new[] { ".#####.", "##...##", "##...##", "##...##", "##...##", "##...##", "##...##", ".#####.", "......." };
        Texture2D textura = new Texture2D(7, 9, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[7 * 9];
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 7; x++)
                pixels[(8 - y) * 7 + x] = desenho[y][x] == '#'
                    ? new Color32(255, 251, 226, 255)
                    : new Color32(0, 0, 0, 0);
        textura.SetPixels32(pixels);
        textura.Apply();
        Sprite glifo = Sprite.Create(textura, new Rect(0f, 0f, 7f, 9f), new Vector2(0.5f, 0.5f), 21f);
        glifo.name = "Tecla" + tecla + "_Runtime";
        glifosTecla[tecla] = glifo;
        return glifo;
    }

    private LineRenderer CriarLinhaCirculo(string nome, Color cor, float largura, bool completo)
    {
        GameObject objeto = new GameObject(nome, typeof(LineRenderer));
        objeto.transform.SetParent(prompt.transform, false);
        LineRenderer linha = objeto.GetComponent<LineRenderer>();
        linha.useWorldSpace = false;
        linha.loop = completo;
        linha.widthMultiplier = largura;
        linha.startColor = cor;
        linha.endColor = cor;
        linha.numCapVertices = 3;
        linha.numCornerVertices = 3;
        linha.sortingLayerID = 0;
        linha.sortingOrder = 99;
        if (materialPrompt == null)
            materialPrompt = new Material(Shader.Find("Sprites/Default"));
        linha.sharedMaterial = materialPrompt;

        if (completo)
        {
            linha.positionCount = SegmentosCirculo;
            for (int i = 0; i < SegmentosCirculo; i++)
                linha.SetPosition(i, PontoCirculo(i / (float)SegmentosCirculo));
        }

        return linha;
    }

    private Vector3 PontoCirculo(float progressoCircular)
    {
        float angulo = (-90f + progressoCircular * 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angulo) * RaioPrompt, Mathf.Sin(angulo) * RaioPrompt, 0f);
    }

    private void MostrarPrompt(LutadorController2D lutador)
    {
        if (prompt == null || textoTecla == null)
            return;

        prompt.SetActive(true);
        string tecla = ObterNomeTecla(lutador.ObterTeclaInteracao());
        textoTecla.text = tecla;
        Sprite glifo = ObterGlifoTecla(tecla);
        glifoTecla.sprite = glifo;
        glifoTecla.enabled = glifo != null;
        textoTecla.enabled = glifo == null;
        // A tecla fica no centro visual do objeto, e não acima dele. Isso
        // mantém Q/O legíveis mesmo quando o objeto está na parede ou na mesa.
        prompt.transform.position = ObterPosicaoPrompt();
        AtualizarCirculo(progresso);
    }

    private Vector3 ObterPosicaoPrompt()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return ObterLimitesVisiveis().center;

        return transform.position + Vector3.up * (ObterAlturaPrompt() * 0.5f);
    }

    private float ObterAlturaPrompt()
    {
        return alturaPrompt;
    }

    private void AtualizarCirculo(float valor)
    {
        if (circuloProgresso == null)
            return;

        valor = Mathf.Clamp01(valor);
        if (valor <= 0.001f)
        {
            circuloProgresso.positionCount = 0;
            return;
        }

        int pontos = Mathf.Max(2, Mathf.CeilToInt(SegmentosCirculo * valor) + 1);
        circuloProgresso.positionCount = pontos;
        for (int i = 0; i < pontos; i++)
        {
            float percentual = pontos <= 1 ? 0f : (i / (float)(pontos - 1)) * valor;
            circuloProgresso.SetPosition(i, PontoCirculo(percentual));
        }
    }

    private void EsconderPrompt()
    {
        if (prompt != null)
            prompt.SetActive(false);
    }

    private IEnumerator UsarObjeto(LutadorController2D lutador)
    {
        emUso = true;
        arremessoIniciado = false;
        arremessoConcluido = false;
        progresso = 0f;
        EsconderPrompt();

        Sprite[] frames = ObterFramesDoPersonagem(lutador);
        if (frames == null || frames.Length == 0 ||
            !lutador.IniciarAnimacaoObjeto(frames, 10f, () => IniciarArremesso(lutador)))
        {
            emUso = false;
            alvoAtual = null;
            yield break;
        }

        objetoArremessado = CriarObjetoArremessado(lutador);
        DefinirVisibilidade(false);

        while ((lutador != null && lutador.EstaInteragindoComObjeto()) ||
               (arremessoIniciado && !arremessoConcluido))
        {
            // Depois da soltura apenas a trajetória controla o projétil.
            if (!arremessoIniciado && objetoArremessado != null)
                PosicionarObjetoNasMaos(lutador);
            yield return null;
        }

        if (objetoArremessado != null)
        {
            Destroy(objetoArremessado);
            objetoArremessado = null;
        }

        // O objeto foi consumido neste round. Ele não volta durante a mesma
        // luta; o GameManager chama ResetarObjetosDaRodada no próximo round.
        consumidoNaRodada = arremessoIniciado;
        DefinirVisibilidade(!consumidoNaRodada);
        alvoAtual = null;
        emUso = false;
    }

    private GameObject CriarObjetoArremessado(LutadorController2D lutador)
    {
        if (spriteArremesso == null)
            return null;

        GameObject objeto = new GameObject(nomeObjeto + "_Arremessado");
        SpriteRenderer rendererObjeto = objeto.AddComponent<SpriteRenderer>();
        rendererObjeto.sprite = spriteArremesso;
        rendererObjeto.sortingLayerID = 0;
        rendererObjeto.sortingOrder = 20;

        Vector2 tamanhoNatural = spriteArremesso.bounds.size;
        objeto.transform.localScale = new Vector3(
            tamanhoVisualArremesso.x / Mathf.Max(0.01f, tamanhoNatural.x),
            tamanhoVisualArremesso.y / Mathf.Max(0.01f, tamanhoNatural.y),
            1f);
        PosicionarObjetoNasMaos(lutador, objeto);
        return objeto;
    }

    private void PosicionarObjetoNasMaos(LutadorController2D lutador)
    {
        PosicionarObjetoNasMaos(lutador, objetoArremessado);
    }

    private static void PosicionarObjetoNasMaos(LutadorController2D lutador, GameObject objeto)
    {
        if (lutador == null || objeto == null)
            return;

        float direcao = Mathf.Sign(lutador.transform.localScale.x);
        if (Mathf.Abs(direcao) < 0.01f)
            direcao = 1f;

        objeto.transform.position = lutador.ObterPosicaoCintura() +
            new Vector3(direcao * 0.65f, 0f, -0.35f);
    }

    private void IniciarArremesso(LutadorController2D lutador)
    {
        if (arremessoIniciado || lutador == null || lutador.EstaMorto() || lutador.oponente == null)
        {
            arremessoConcluido = true;
            return;
        }

        arremessoIniciado = true;
        PosicionarObjetoNasMaos(lutador);
        StartCoroutine(AnimarArremesso(lutador, lutador.oponente));
    }

    private IEnumerator AnimarArremesso(LutadorController2D lutador, LutadorController2D alvo)
    {
        Vector3 inicio = objetoArremessado != null
            ? objetoArremessado.transform.position
            : lutador.transform.position;
        float alcanceMaximo = ObterAlcanceMaximoArremesso();
        Vector3 posicaoAlvoNoLancamento = alvo.ObterPosicaoCintura();
        float deslocamentoX = posicaoAlvoNoLancamento.x - inicio.x;
        float deslocamentoLimitado = Mathf.Clamp(deslocamentoX, -alcanceMaximo, alcanceMaximo);
        Vector3 fim = new Vector3(
            inicio.x + deslocamentoLimitado,
            posicaoAlvoNoLancamento.y,
            inicio.z);
        float duracao = Mathf.Clamp(Mathf.Abs(deslocamentoLimitado) / 18f, 0.18f, 0.55f);
        float tempo = 0f;
        bool acertou = false;
        float direcao = deslocamentoX < 0f ? -1f : 1f;
        Vector3 anterior = inicio;

        while (tempo < duracao)
        {
            if (alvo == null || alvo.EstaMorto())
                break;

            float percentual = Mathf.Clamp01(tempo / duracao);
            Vector3 posicao = Vector3.Lerp(inicio, fim, percentual);
            posicao.y += Mathf.Sin(percentual * Mathf.PI) * 0.8f;

            if (objetoArremessado != null)
            {
                objetoArremessado.transform.position = posicao;
                objetoArremessado.transform.rotation = Quaternion.Euler(0f, 0f, -direcao * 720f * tempo);
            }

            Vector3 posicaoAtualAlvo = alvo.ObterPosicaoCintura();
            if (DistanciaAoSegmento(posicaoAtualAlvo, anterior, posicao) <= 0.8f)
            {
                AplicarDanoObjeto(lutador, alvo);
                acertou = true;
                break;
            }

            anterior = posicao;
            tempo += Time.deltaTime;
            yield return null;
        }

        // Confere o último ponto da trajetória; não há acerto automático.
        if (!acertou && alvo != null && !alvo.EstaMorto())
        {
            if (objetoArremessado != null)
                objetoArremessado.transform.position = fim;
            if (DistanciaAoSegmento(alvo.ObterPosicaoCintura(), anterior, fim) <= 0.8f)
                AplicarDanoObjeto(lutador, alvo);
        }

        arremessoConcluido = true;
    }

    private static float DistanciaAoSegmento(Vector2 ponto, Vector2 inicio, Vector2 fim)
    {
        Vector2 trecho = fim - inicio;
        float t = trecho.sqrMagnitude > 0.0001f
            ? Mathf.Clamp01(Vector2.Dot(ponto - inicio, trecho) / trecho.sqrMagnitude) : 0f;
        return Vector2.Distance(ponto, inicio + trecho * t);
    }

    private static float ObterAlcanceMaximoArremesso()
    {
        Camera cameraCena = Camera.main;
        // Metade da largura visível da arena (câmera ortográfica).
        return cameraCena != null && cameraCena.orthographic
            ? cameraCena.orthographicSize * cameraCena.aspect
            : 10f;
    }

    private void AplicarDanoObjeto(LutadorController2D origem, LutadorController2D alvo)
    {
        if (alvo == null || alvo.EstaMorto())
            return;

        int danoBase = origem != null && origem.dadosPersonagem != null && origem.dadosPersonagem.danoAtaque > 0
            ? origem.dadosPersonagem.danoAtaque
            : 10;
        int dano = Mathf.Max(1, Mathf.RoundToInt(danoBase * 1.5f));
        alvo.ReceberDano(dano);
    }

    private Sprite[] ObterFramesDoPersonagem(LutadorController2D lutador)
    {
        if (lutador == null || lutador.dadosPersonagem == null)
            return null;

        string nome = lutador.dadosPersonagem.nomePersonagem;
        if (string.Equals(nome, "Diego", System.StringComparison.OrdinalIgnoreCase))
            return CarregarAnimacao("Diego");
        if (string.Equals(nome, "Jamanta", System.StringComparison.OrdinalIgnoreCase))
            return CarregarAnimacao("Jamanta");
        if (string.Equals(nome, "Renata", System.StringComparison.OrdinalIgnoreCase))
            return CarregarAnimacao("Renata");

        return null;
    }

    private void DefinirVisibilidade(bool visivel)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visivel;
        if (contornoSprite != null)
            contornoSprite.enabled = visivel;
    }

    public static void ResetarObjetosDaRodada()
    {
        for (int i = objetosAtivos.Count - 1; i >= 0; i--)
        {
            ObjetoInterativoLuta objeto = objetosAtivos[i];
            if (objeto != null)
                objeto.ResetarParaNovoRound();
        }
    }

    private void ResetarParaNovoRound()
    {
        if (objetoArremessado != null)
        {
            Destroy(objetoArremessado);
            objetoArremessado = null;
        }

        StopAllCoroutines();
        consumidoNaRodada = false;
        arremessoIniciado = false;
        emUso = false;
        progresso = 0f;
        alvoAtual = null;
        arremessoConcluido = true;
        DefinirVisibilidade(true);
        EsconderPrompt();
    }

    private static string ObterNomeTecla(KeyCode tecla)
    {
        switch (tecla)
        {
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.LeftControl: return "CTRL";
            case KeyCode.LeftShift: return "SHIFT";
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.Space: return "ESP";
            case KeyCode.Return: return "ENTER";
            case KeyCode.Keypad0: return "0";
            case KeyCode.Keypad1: return "1";
            case KeyCode.Keypad2: return "2";
            case KeyCode.Keypad3: return "3";
            case KeyCode.Keypad4: return "4";
            case KeyCode.Keypad5: return "5";
            case KeyCode.Keypad6: return "6";
            case KeyCode.Keypad7: return "7";
            case KeyCode.Keypad8: return "8";
            case KeyCode.Keypad9: return "9";
            default: return tecla.ToString().ToUpperInvariant();
        }
    }
}
