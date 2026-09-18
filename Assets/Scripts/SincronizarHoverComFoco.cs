using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ── Como usar ────────────────────────────────────────────────────────────
// Adiciona esse componente UMA VEZ em cada cena que usa botões "normais" da
// Unity (sem sistema de destaque próprio em script) — por exemplo no próprio
// GameObject do EventSystem, ou em qualquer objeto raiz do Canvas.
// NÃO usar em botões que já têm ButtonVisualPersonalizado — esse componente
// já cuida de tudo sozinho (hover, foco e sprites); rodar os dois juntos no
// mesmo botão é exatamente o que causa dois botões parecendo destacados ao
// mesmo tempo.
//
// ── O que resolve ────────────────────────────────────────────────────────
// A Unity, por padrão, tem dois estados visuais independentes num botão:
//   - "Highlighted": o mouse está em cima (sozinho, não move o foco real)
//   - "Selected": o foco de verdade, o que o teclado/controle usa
// Se você navega com teclado até o botão A (fica "Selected") e depois só
// passa o MOUSE por cima do botão B sem clicar, a Unity mostra os dois
// destacados ao mesmo tempo — um "Highlighted", outro "Selected" — porque
// mover o mouse por cima não muda o foco real sozinho.
//
// Esse script corrige isso: sempre que o mouse entra em qualquer botão da
// cena, ele TAMBÉM vira o foco de verdade (chama SetSelectedGameObject nele).
// Resultado: só existe UM destaque por vez, seja qual for o meio usado
// (mouse, teclado ou controle).
public class SincronizarHoverComFoco : MonoBehaviour
{
    [Tooltip("Verifica periodicamente se apareceram novos botões na cena (ex: um painel que abre depois). Desligar só se tiver certeza que a cena não muda.")]
    public bool observarNovosObjetos = true;

    [Tooltip("A cada quantos segundos procurar por botões novos (só usado se observarNovosObjetos estiver ligado).")]
    public float intervaloVerificacao = 1f;

    private readonly HashSet<Selectable> jaConfigurados = new HashSet<Selectable>();

    void Start()
    {
        ConfigurarTodosNaCena();

        if (observarNovosObjetos)
            InvokeRepeating(nameof(ConfigurarTodosNaCena), intervaloVerificacao, intervaloVerificacao);
    }

    void ConfigurarTodosNaCena()
    {
        Selectable[] todos = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        foreach (Selectable s in todos)
        {
            if (s == null || jaConfigurados.Contains(s)) continue;
            jaConfigurados.Add(s);

            // Botão já tem controle próprio de hover/foco/sprite — não mexe,
            // senão os dois sistemas brigam e mostram dois destaques juntos.
            // (checagem por nome, sem referenciar o tipo direto, pra este
            // arquivo compilar mesmo que ButtonVisualPersonalizado esteja em
            // outro Assembly Definition ou ainda não tenha sido adicionado)
            bool temControleProprio = false;
            foreach (Component c in s.GetComponents<Component>())
            {
                if (c != null && c.GetType().Name == "ButtonVisualPersonalizado")
                {
                    temControleProprio = true;
                    break;
                }
            }
            if (temControleProprio)
                continue;

            if (s.GetComponent<HoverAssumeFoco>() == null)
                s.gameObject.AddComponent<HoverAssumeFoco>();
        }
    }
}

// Componente auxiliar — adicionado automaticamente pelo SincronizarHoverComFoco
// acima em cada Selectable da cena. Não precisa (e não deve) ser adicionado
// manualmente em nenhum botão.
public class HoverAssumeFoco : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Selectable s = GetComponent<Selectable>();
        if (s == null || !s.interactable || !s.gameObject.activeInHierarchy) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}