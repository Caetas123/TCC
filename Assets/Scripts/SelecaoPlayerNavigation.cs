using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Navegação exclusiva da composição nova da seleção de personagens.
/// Cada jogador possui uma rota própria: voltar, informação, configurações
/// de bot e personagens. Nenhuma tecla do P1 aponta para os controles do P2.
/// </summary>
[ExecuteAlways]
public sealed class SelecaoPlayerNavigation : MonoBehaviour
{
    private const int CategoriaVoltar = 0;
    private const int CategoriaInfo = 1;
    private const int CategoriaDificuldade = 2;
    private const int CategoriaEstilo = 3;
    private const int CategoriaPersonagens = 4;

    [Header("Referências da tela nova")]
    [SerializeField] private Button botaoLadoP1;
    [SerializeField] private Button botaoLadoP2;
    [SerializeField] private Button botaoInfoP1;
    [SerializeField] private Button botaoInfoP2;
    [SerializeField] private Button botaoVoltar;
    [SerializeField] private TMP_Dropdown dificuldadeP1;
    [SerializeField] private TMP_Dropdown estiloP1;
    [SerializeField] private TMP_Dropdown dificuldadeP2;
    [SerializeField] private TMP_Dropdown estiloP2;

    [Header("Rota de personagens")]
    [SerializeField] private Button[] slotsPersonagens = Array.Empty<Button>();
    [SerializeField] private int[] indicesPersonagens = Array.Empty<int>();

    private TelaSelecaoPlayer tela;
    private TelaSelecaoPlayerLayout layout;
    private EventSystem eventSystem;
    private bool editorAgendado;
    private bool configuracaoAplicada;
    private bool navegacaoGlobalFoiDesativada;

    private int categoriaP1 = CategoriaPersonagens;
    private int categoriaP2 = CategoriaPersonagens;
    private int indiceSlotP1;
    private int indiceSlotP2 = 1;

    private KeyCode p1Esquerda;
    private KeyCode p1Direita;
    private KeyCode p1Cima;
    private KeyCode p1Baixo;
    private KeyCode p1Confirmar;
    private KeyCode p1Voltar;
    private KeyCode p2Esquerda;
    private KeyCode p2Direita;
    private KeyCode p2Cima;
    private KeyCode p2Baixo;
    private KeyCode p2Confirmar;
    private KeyCode p2Voltar;
    private Vector2 ultimoEixoP1;
    private Vector2 ultimoEixoP2;

    private void Start()
    {
        if (Application.isPlaying)
            StartCoroutine(PrepararDepoisDaTela());
    }

    private void OnDisable()
    {
        RestaurarNavegacaoGlobal();
#if UNITY_EDITOR
        EditorApplication.delayCall -= InicializarNoEditor;
        editorAgendado = false;
#endif
    }

    private void OnDestroy()
    {
        RestaurarNavegacaoGlobal();
    }

    private void RestaurarNavegacaoGlobal()
    {
        // O EventSystem pode sobreviver à troca de cena. Se esta tela deixar
        // sendNavigationEvents desligado, todos os menus seguintes perdem a
        // navegação por teclado/controle.
        EventSystem sistema = eventSystem != null ? eventSystem : EventSystem.current;
        if (sistema != null && navegacaoGlobalFoiDesativada)
            sistema.sendNavigationEvents = true;

        navegacaoGlobalFoiDesativada = false;
    }

    private IEnumerator PrepararDepoisDaTela()
    {
        yield return null;
        Inicializar();
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying || editorAgendado)
            return;

        editorAgendado = true;
        EditorApplication.delayCall += InicializarNoEditor;
    }

    private void InicializarNoEditor()
    {
        editorAgendado = false;
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
            return;

        Inicializar();
    }
#endif

    public void Inicializar()
    {
        ResolverReferencias();
        CarregarTeclas();
        GarantirCamadaDeInteracao();
        FecharModalNoEstadoInicial();
        ConfigurarNavegacaoExplicita();

        if (Application.isPlaying)
        {
            GarantirEventSystem();
            SelecionarPrimeiroFoco();
        }

        configuracaoAplicada = true;
    }

    private void Update()
    {
        if (!Application.isPlaying || !configuracaoAplicada)
            return;

        if (ModalEstaAberto())
            return;

        GarantirEventSystem();
        // A seleção pode transformar um lado em BOT durante a partida.
        // Recalcular a rota inclui imediatamente os dropdowns habilitados e
        // mantém os controles bloqueados fora da navegação.
        ConfigurarNavegacaoExplicita();
        ProcessarVoltarGlobal();
        ProcessarEnterGlobal();
        ProcessarLado(1);
        ProcessarLado(2);
    }

    private void ResolverReferencias()
    {
        tela = GetComponentInParent<TelaSelecaoPlayer>();
        layout = GetComponentInParent<TelaSelecaoPlayerLayout>();

        botaoLadoP1 = Encontrar<Button>("PainelP1/SelecionarLadoP1");
        botaoLadoP2 = Encontrar<Button>("PainelP2/SelecionarLadoP2");
        botaoInfoP1 = Encontrar<Button>("PainelP1/InfoIP1");
        botaoInfoP2 = Encontrar<Button>("PainelP2/InfoIP2");
        botaoVoltar = Encontrar<Button>("BotaoVoltarSelecao");
        dificuldadeP1 = Encontrar<TMP_Dropdown>("PainelP1/DificuldadeBotP1");
        estiloP1 = Encontrar<TMP_Dropdown>("PainelP1/EstiloBotP1");
        dificuldadeP2 = Encontrar<TMP_Dropdown>("PainelP2/DificuldadeBotP2");
        estiloP2 = Encontrar<TMP_Dropdown>("PainelP2/EstiloBotP2");

        List<Button> slots = new List<Button>();
        List<int> indices = new List<int>();
        Transform roster = transform.Find("RosterPersonagens");
        if (roster != null)
        {
            foreach (Button candidato in roster.GetComponentsInChildren<Button>(true))
            {
                if (candidato == null || candidato.gameObject.name.StartsWith("SlotPersonagemBloqueado"))
                    continue;

                string sufixo = candidato.gameObject.name.Substring("SlotPersonagem".Length);
                int indice;
                if (int.TryParse(sufixo, out indice))
                {
                    slots.Add(candidato);
                    indices.Add(indice);
                }
            }
        }

        slotsPersonagens = slots.ToArray();
        indicesPersonagens = indices.ToArray();
        if (slotsPersonagens.Length > 0)
        {
            indiceSlotP1 = Mathf.Clamp(indiceSlotP1, 0, slotsPersonagens.Length - 1);
            indiceSlotP2 = Mathf.Clamp(indiceSlotP2, 0, slotsPersonagens.Length - 1);
        }
    }

    private T Encontrar<T>(string caminho) where T : Component
    {
        Transform alvo = transform.Find(caminho);
        return alvo != null ? alvo.GetComponent<T>() : null;
    }

    private void CarregarTeclas()
    {
        p1Esquerda = LerTecla("P1_Esquerda", KeyCode.A);
        p1Direita = LerTecla("P1_Direita", KeyCode.D);
        p1Cima = LerTecla("P1_Pular", KeyCode.W);
        p1Baixo = LerTecla("P1_Defender", KeyCode.S);
        p1Confirmar = LerTecla("P1_Ataque", KeyCode.F);
        p1Voltar = LerTecla("P1_Especial", KeyCode.G);

        p2Esquerda = LerTecla("P2_Esquerda", KeyCode.LeftArrow);
        p2Direita = LerTecla("P2_Direita", KeyCode.RightArrow);
        p2Cima = LerTecla("P2_Pular", KeyCode.UpArrow);
        p2Baixo = LerTecla("P2_Defender", KeyCode.DownArrow);
        p2Confirmar = LerTecla("P2_Ataque", KeyCode.K);
        p2Voltar = LerTecla("P2_Especial", KeyCode.L);
    }

    private KeyCode LerTecla(string chave, KeyCode padrao)
    {
        string valor = PlayerPrefs.GetString(chave, padrao.ToString());
        try { return (KeyCode)Enum.Parse(typeof(KeyCode), valor); }
        catch { return padrao; }
    }

    private void GarantirCamadaDeInteracao()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }

        PrepararSelectable(botaoLadoP1);
        PrepararSelectable(botaoLadoP2);
        PrepararSelectable(botaoInfoP1);
        PrepararSelectable(botaoInfoP2);
        PrepararSelectable(botaoVoltar);
        for (int i = 0; i < slotsPersonagens.Length; i++)
            PrepararSelectable(slotsPersonagens[i]);

        PrepararDropdown(dificuldadeP1);
        PrepararDropdown(estiloP1);
        PrepararDropdown(dificuldadeP2);
        PrepararDropdown(estiloP2);
    }

    private void PrepararSelectable(Selectable selectable)
    {
        if (selectable == null)
            return;

        selectable.interactable = true;
        Graphic graphic = selectable.targetGraphic != null
            ? selectable.targetGraphic
            : selectable.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;
    }

    private void PrepararDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        // O próprio TMP_Dropdown controla abertura, lista, rolagem e clique.
        Image imagem = dropdown.GetComponent<Image>();
        if (imagem != null)
            imagem.raycastTarget = true;
    }

    private bool ModalEstaAberto()
    {
        // O modal editado na Hierarchy é filho direto do Canvas nesta cena.
        // A busca interna fica como compatibilidade para outra organização.
        Transform modal = transform.parent != null
            ? transform.parent.Find("ModalInfoPersonagem")
            : null;
        if (modal == null)
            modal = transform.Find("ModalInfoPersonagem");
        if (modal != null && modal.gameObject.activeInHierarchy)
            return true;

        // O painel de configuração da IA é um modal funcional. Enquanto ele
        // estiver aberto, a navegação da tela principal não pode roubar o foco.
        return tela != null && tela.ConfiguracaoIAEstaAbertaPublicamente();
    }

    private void FecharModalNoEstadoInicial()
    {
        Transform modal = transform.parent != null
            ? transform.parent.Find("ModalInfoPersonagem")
            : null;
        if (modal == null)
            modal = transform.Find("ModalInfoPersonagem");
        if (modal != null)
            modal.gameObject.SetActive(false);
    }

    private void ConfigurarNavegacaoExplicita()
    {
        ConfigurarRota(1);
        ConfigurarRota(2);
    }

    private void ConfigurarRota(int lado)
    {
        Button info = lado == 1 ? botaoInfoP1 : botaoInfoP2;
        TMP_Dropdown dificuldade = lado == 1 ? dificuldadeP1 : dificuldadeP2;
        TMP_Dropdown estilo = lado == 1 ? estiloP1 : estiloP2;

        List<Selectable> controles = new List<Selectable>();
        if (botaoVoltar != null) controles.Add(botaoVoltar);
        if (info != null) controles.Add(info);
        if (dificuldade != null && dificuldade.interactable) controles.Add(dificuldade);
        if (estilo != null && estilo.interactable) controles.Add(estilo);

        for (int i = 0; i < slotsPersonagens.Length; i++)
        {
            if (slotsPersonagens[i] != null && slotsPersonagens[i].interactable)
                controles.Add(slotsPersonagens[i]);
        }

        int primeiroSlot = -1;
        for (int i = 0; i < controles.Count; i++)
        {
            if (Array.IndexOf(slotsPersonagens, controles[i]) >= 0)
            {
                primeiroSlot = i;
                break;
            }
        }

        for (int i = 0; i < controles.Count; i++)
        {
            Selectable cima = i > 0 ? controles[i - 1] : null;
            Selectable baixo = i + 1 < controles.Count ? controles[i + 1] : null;
            if (primeiroSlot >= 0 && i >= primeiroSlot)
            {
                cima = i > primeiroSlot ? controles[primeiroSlot - 1] : controles[primeiroSlot - 1 >= 0 ? primeiroSlot - 1 : 0];
                baixo = null;
            }
            Configurar(controles[i], null, null, cima, baixo);
        }

        for (int i = 0; i < slotsPersonagens.Length; i++)
        {
            Button slot = slotsPersonagens[i];
            if (slot == null || !slot.interactable)
                continue;

            Configurar(slot,
                i > 0 ? slotsPersonagens[i - 1] : null,
                i + 1 < slotsPersonagens.Length ? slotsPersonagens[i + 1] : null,
                PrimeiroDisponivel(estilo, dificuldade, info, botaoVoltar),
                null);
        }
    }

    private Selectable PrimeiroDisponivel(params Selectable[] candidatos)
    {
        for (int i = 0; i < candidatos.Length; i++)
        {
            if (candidatos[i] != null && candidatos[i].interactable)
                return candidatos[i];
        }
        return null;
    }

    private void Configurar(Selectable atual, Selectable esquerda, Selectable direita,
        Selectable cima, Selectable baixo)
    {
        if (atual == null)
            return;

        Navigation navigation = atual.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft = esquerda;
        navigation.selectOnRight = direita;
        navigation.selectOnUp = cima;
        navigation.selectOnDown = baixo;
        atual.navigation = navigation;
    }

    private void GarantirEventSystem()
    {
        eventSystem = EventSystem.current;
        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>(true);
        if (eventSystem == null)
            return;

        if (EventSystem.current == null)
        {
            System.Reflection.MethodInfo onEnable = typeof(EventSystem).GetMethod(
                "OnEnable", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (onEnable != null)
                onEnable.Invoke(eventSystem, null);
        }

        eventSystem = EventSystem.current != null ? EventSystem.current : eventSystem;
        // O input próprio desta tela separa P1/P2. O mouse continua normal,
        // mas a navegação automática global não pode roubar o foco do outro lado.
        eventSystem.sendNavigationEvents = false;
        navegacaoGlobalFoiDesativada = true;
    }

    private void SelecionarPrimeiroFoco()
    {
        DefinirFocoVisual(1, CategoriaPersonagens, indiceSlotP1, false);
        DefinirFocoVisual(2, CategoriaPersonagens, indiceSlotP2, false);

        // Um EventSystem só possui um objeto selecionado. A tela tem dois focos
        // simultâneos, portanto não podemos deixar o foco visual de um jogador
        // depender desse objeto global.
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }

    private void ProcessarVoltarGlobal()
    {
        bool cancelarPressionado = Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(p1Voltar) || Input.GetKeyDown(p2Voltar) ||
            TeclaCancelarControle(1) || TeclaCancelarControle(2);

        if (!cancelarPressionado)
            return;

        // Antes a lista era fechada incondicionalmente em todo frame. Por isso
        // o clique chegava a abrir o TMP_Dropdown, mas ele sumia imediatamente.
        if (FecharDropdownAberto())
            return;

        if (botaoVoltar != null && botaoVoltar.interactable)
            botaoVoltar.onClick.Invoke();
    }

    private void ProcessarEnterGlobal()
    {
        if (ExisteDropdownAberto())
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (tela != null)
                tela.ConfirmarInicioPublicamente();
        }
    }

    private void ProcessarLado(int lado)
    {
        bool cima;
        bool baixo;
        bool esquerda;
        bool direita;
        bool confirmar;
        LerEntrada(lado, out cima, out baixo, out esquerda, out direita, out confirmar);

        // Enquanto a lista está aberta, as setas pertencem aos itens do
        // dropdown. A tela principal fica congelada para não pular categoria,
        // botão ou slot por trás da lista.
        TMP_Dropdown dropdownAberto = ObterDropdownAberto(lado);
        if (dropdownAberto != null)
        {
            if (tela != null)
            {
                if (cima || esquerda)
                    tela.MoverDropdownAbertoPublicamente(dropdownAberto, -1);
                else if (baixo || direita)
                    tela.MoverDropdownAbertoPublicamente(dropdownAberto, 1);
            }

            if (confirmar)
            {
                dropdownAberto.Hide();
                DefinirFocoVisual(lado,
                    dropdownAberto == ObterDificuldade(lado)
                        ? CategoriaDificuldade : CategoriaEstilo,
                    IndiceSlotAtual(lado), false);
            }
            return;
        }

        if (cima)
            MoverCategoria(lado, -1);
        else if (baixo)
            MoverCategoria(lado, 1);

        if (esquerda || direita)
            MoverSlot(lado, esquerda ? -1 : 1);

        if (confirmar)
            ConfirmarFoco(lado);
    }

    private void LerEntrada(int lado, out bool cima, out bool baixo,
        out bool esquerda, out bool direita, out bool confirmar)
    {
        KeyCode teclaCima = lado == 1 ? p1Cima : p2Cima;
        KeyCode teclaBaixo = lado == 1 ? p1Baixo : p2Baixo;
        KeyCode teclaEsquerda = lado == 1 ? p1Esquerda : p2Esquerda;
        KeyCode teclaDireita = lado == 1 ? p1Direita : p2Direita;
        KeyCode teclaConfirmar = lado == 1 ? p1Confirmar : p2Confirmar;

        cima = Input.GetKeyDown(teclaCima);
        baixo = Input.GetKeyDown(teclaBaixo);
        esquerda = Input.GetKeyDown(teclaEsquerda);
        direita = Input.GetKeyDown(teclaDireita);
        confirmar = Input.GetKeyDown(teclaConfirmar);

        Gamepad controle = ObterControle(lado);
        if (controle == null)
            return;

        Vector2 eixo = controle.dpad.ReadValue();
        if (eixo.sqrMagnitude < 0.01f)
            eixo = controle.leftStick.ReadValue();

        Vector2 anterior = lado == 1 ? ultimoEixoP1 : ultimoEixoP2;
        cima |= eixo.y > 0.55f && anterior.y <= 0.55f;
        baixo |= eixo.y < -0.55f && anterior.y >= -0.55f;
        esquerda |= eixo.x < -0.55f && anterior.x >= -0.55f;
        direita |= eixo.x > 0.55f && anterior.x <= 0.55f;
        confirmar |= controle.buttonSouth.wasPressedThisFrame;

        if (lado == 1) ultimoEixoP1 = eixo;
        else ultimoEixoP2 = eixo;
    }

    private Gamepad ObterControle(int lado)
    {
        if (Gamepad.all.Count < lado)
            return null;
        return Gamepad.all[lado - 1];
    }

    private bool TeclaCancelarControle(int lado)
    {
        Gamepad controle = ObterControle(lado);
        return controle != null && controle.buttonEast.wasPressedThisFrame;
    }

    private TMP_Dropdown ObterDropdownAberto(int lado)
    {
        TMP_Dropdown dificuldade = ObterDificuldade(lado);
        if (dificuldade != null && dificuldade.IsExpanded)
            return dificuldade;

        TMP_Dropdown estilo = ObterEstilo(lado);
        return estilo != null && estilo.IsExpanded ? estilo : null;
    }

    private bool ExisteDropdownAberto()
    {
        return ObterDropdownAberto(1) != null || ObterDropdownAberto(2) != null;
    }

    private bool FecharDropdownAberto()
    {
        TMP_Dropdown aberto = ObterDropdownAberto(1);
        if (aberto == null)
            aberto = ObterDropdownAberto(2);
        if (aberto == null)
            return false;

        aberto.Hide();
        return true;
    }

    private void MoverCategoria(int lado, int delta)
    {
        int categoria = lado == 1 ? categoriaP1 : categoriaP2;
        for (int tentativas = 0; tentativas < 5; tentativas++)
        {
            categoria += delta;
            if (categoria < CategoriaVoltar) categoria = CategoriaPersonagens;
            if (categoria > CategoriaPersonagens) categoria = CategoriaVoltar;

            if (CategoriaDisponivel(lado, categoria))
            {
                DefinirFocoVisual(lado, categoria, IndiceSlotAtual(lado), true);
                return;
            }
        }
    }

    private bool CategoriaDisponivel(int lado, int categoria)
    {
        if (categoria == CategoriaVoltar)
            return botaoVoltar != null;
        if (categoria == CategoriaInfo)
            return ObterInfo(lado) != null;
        if (categoria == CategoriaDificuldade)
            return ObterDificuldade(lado) != null && ObterDificuldade(lado).interactable;
        if (categoria == CategoriaEstilo)
            return ObterEstilo(lado) != null && ObterEstilo(lado).interactable;
        return slotsPersonagens.Length > 0;
    }

    private void MoverSlot(int lado, int delta)
    {
        if ((lado == 1 ? categoriaP1 : categoriaP2) != CategoriaPersonagens ||
            slotsPersonagens.Length == 0)
            return;

        int indice = IndiceSlotAtual(lado) + delta;
        if (indice < 0) indice = slotsPersonagens.Length - 1;
        if (indice >= slotsPersonagens.Length) indice = 0;
        DefinirFocoVisual(lado, CategoriaPersonagens, indice, true);
    }

    private int IndiceSlotAtual(int lado)
    {
        return lado == 1 ? indiceSlotP1 : indiceSlotP2;
    }

    private void ConfirmarFoco(int lado)
    {
        int categoria = lado == 1 ? categoriaP1 : categoriaP2;
        if (categoria == CategoriaVoltar)
        {
            if (botaoVoltar != null) botaoVoltar.onClick.Invoke();
        }
        else if (categoria == CategoriaInfo)
        {
            Button info = ObterInfo(lado);
            if (info != null) info.onClick.Invoke();
        }
        else if (categoria == CategoriaDificuldade)
        {
            AbrirDropdown(ObterDificuldade(lado));
        }
        else if (categoria == CategoriaEstilo)
        {
            AbrirDropdown(ObterEstilo(lado));
        }
        else if (categoria == CategoriaPersonagens && layout != null && slotsPersonagens.Length > 0)
        {
            int indice = IndiceSlotAtual(lado);
            layout.SelecionarPersonagemPorNavegacao(lado, indicesPersonagens[indice]);
            DefinirFocoVisual(lado, CategoriaPersonagens, indice, true);
        }
    }

    private void AbrirDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null || !dropdown.interactable)
            return;

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(dropdown.gameObject);
        dropdown.Show();
    }

    private Button ObterInfo(int lado) => lado == 1 ? botaoInfoP1 : botaoInfoP2;
    private TMP_Dropdown ObterDificuldade(int lado) => lado == 1 ? dificuldadeP1 : dificuldadeP2;
    private TMP_Dropdown ObterEstilo(int lado) => lado == 1 ? estiloP1 : estiloP2;

    private void DefinirFocoVisual(int lado, int categoria, int indiceSlot, bool selecionarObjeto)
    {
        indiceSlot = slotsPersonagens.Length == 0
            ? 0
            : Mathf.Clamp(indiceSlot, 0, slotsPersonagens.Length - 1);

        if (lado == 1)
        {
            categoriaP1 = categoria;
            indiceSlotP1 = indiceSlot;
        }
        else
        {
            categoriaP2 = categoria;
            indiceSlotP2 = indiceSlot;
        }

        if (layout != null)
            layout.DefinirFocoNavegacao(lado, categoria,
                slotsPersonagens.Length > 0 ? indicesPersonagens[indiceSlot] : -1);

        // Não sincronizar com EventSystem aqui: ele é global e faria o slot
        // movimentado por um jogador parecer ser o foco do outro. A confirmação
        // desta tela é direta e o visual é mantido por lado no Layout.
    }

    private Selectable ObterSelectable(int lado, int categoria, int indiceSlot)
    {
        if (categoria == CategoriaVoltar) return botaoVoltar;
        if (categoria == CategoriaInfo) return ObterInfo(lado);
        if (categoria == CategoriaDificuldade) return ObterDificuldade(lado);
        if (categoria == CategoriaEstilo) return ObterEstilo(lado);
        if (categoria == CategoriaPersonagens && slotsPersonagens.Length > 0)
            return slotsPersonagens[Mathf.Clamp(indiceSlot, 0, slotsPersonagens.Length - 1)];
        return null;
    }

    private void AtualizarFocoPeloObjetoSelecionado()
    {
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        GameObject objeto = eventSystem.currentSelectedGameObject;
        if (objeto == (botaoInfoP1 != null ? botaoInfoP1.gameObject : null))
            DefinirFocoVisual(1, CategoriaInfo, indiceSlotP1, false);
        else if (objeto == (botaoInfoP2 != null ? botaoInfoP2.gameObject : null))
            DefinirFocoVisual(2, CategoriaInfo, indiceSlotP2, false);
        else if (objeto == (dificuldadeP1 != null ? dificuldadeP1.gameObject : null))
            DefinirFocoVisual(1, CategoriaDificuldade, indiceSlotP1, false);
        else if (objeto == (dificuldadeP2 != null ? dificuldadeP2.gameObject : null))
            DefinirFocoVisual(2, CategoriaDificuldade, indiceSlotP2, false);
        else if (objeto == (estiloP1 != null ? estiloP1.gameObject : null))
            DefinirFocoVisual(1, CategoriaEstilo, indiceSlotP1, false);
        else if (objeto == (estiloP2 != null ? estiloP2.gameObject : null))
            DefinirFocoVisual(2, CategoriaEstilo, indiceSlotP2, false);
        else if (objeto == (botaoVoltar != null ? botaoVoltar.gameObject : null))
        {
            DefinirFocoVisual(1, CategoriaVoltar, indiceSlotP1, false);
            DefinirFocoVisual(2, CategoriaVoltar, indiceSlotP2, false);
        }
        else
        {
            for (int i = 0; i < slotsPersonagens.Length; i++)
            {
                if (objeto != slotsPersonagens[i].gameObject)
                    continue;

                // O mouse trabalha com o lado que está ativo. A seleção
                // continua independente para os dois jogadores, então as
                // duas bordas aparecem quando ambos escolherem o mesmo slot.
                int ladoMouse = layout != null ? layout.ObterLadoAtivo() : 1;
                DefinirFocoVisual(ladoMouse, CategoriaPersonagens, i, false);
                break;
            }
        }
    }
}
