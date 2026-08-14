using UnityEngine;
using UnityEngine.UI;

// ── Como usar ────────────────────────────────────────────────────────────
// Adiciona esse componente no MESMO GameObject onde está o TelaSelecaoPlayer
// (ele pega a referência sozinho via GetComponent, não precisa arrastar nada
// de novo no Inspector — os campos que ele usa já são públicos lá).
//
// ── Por que um script SÓ pra essa tela, e não um genérico pra cena toda ──
// Essa tela tem uma regra específica que um "desligar tudo que for
// Selectable" não respeita: existem 3 zonas de navegação separadas —
//   • Zona do Player 1: só as teclas/controle do P1 mexem nela
//   • Zona do Player 2: só as teclas/controle do P2 mexem nela
//   • Zona geral (Iniciar/Voltar): qualquer um dos dois mexe
// O painel de configuração de IA de cada player segue a mesma regra (o
// painel do P1 só aceita teclas do P1, o do P2 só teclas do P2). Um script
// genérico que varre TODO Selectable da cena não tem como saber dessa
// divisão — esse aqui lê direto do TelaSelecaoPlayer.cs pra desligar
// exatamente (e só) os objetos que pertencem a cada zona.
//
// ── O que resolve ────────────────────────────────────────────────────────
// Mesmo motivo do DesligarNavegacaoNativa.cs (script genérico feito pras
// outras telas): todo Selectable tem uma Navigation NATIVA da Unity que
// reage sozinha às setas/analógico (mesmos eixos que a navegação
// customizada desta tela também lê). Basta um clique de mouse selecionar de
// verdade um desses botões pra, a partir daí, toda seta ser processada
// duas vezes — pela navegação customizada (grupoAtual/focoP1/focoP2) E pela
// navegação nativa ao mesmo tempo — o que trava a volta pros personagens
// via teclado/controle depois de usar o mouse. Esse script desliga a
// Navigation nativa (Mode.None) de cada botão da tela, uma vez só, no
// Start — só o sistema customizado manda, sempre.
[RequireComponent(typeof(TelaSelecaoPlayer))]
public class DesligarNavegacaoNativaTelaSelecaoPlayer : MonoBehaviour
{
    void Start()
    {
        TelaSelecaoPlayer tela = GetComponent<TelaSelecaoPlayer>();
        if (tela == null) return;

        // ── Zona do Player 1 ──────────────────────────────────────────────
        DesligarArray(tela.botoesPlayer1);
        Desligar(tela.botaoDeselecionarP1);
        Desligar(tela.botaoRandomP1);
        Desligar(tela.dropdownEstiloIAP1);
        Desligar(tela.dropdownDificuldadeIAP1);
        Desligar(tela.botaoSalvarConfigIAP1);
        Desligar(tela.botaoFecharConfigIAP1);

        // ── Zona do Player 2 ──────────────────────────────────────────────
        DesligarArray(tela.botoesPlayer2);
        Desligar(tela.botaoDeselecionarP2);
        Desligar(tela.botaoRandomP2);
        Desligar(tela.dropdownEstiloIAP2);
        Desligar(tela.dropdownDificuldadeIAP2);
        Desligar(tela.botaoSalvarConfigIAP2);
        Desligar(tela.botaoFecharConfigIAP2);

        // ── Zona geral (compartilhada pelos dois players) ─────────────────
        Desligar(tela.botaoIniciar);
        Desligar(tela.botaoVoltar);
    }

    void DesligarArray(Button[] botoes)
    {
        if (botoes == null) return;
        foreach (Button b in botoes)
            Desligar(b);
    }

    void Desligar(Selectable s)
    {
        if (s == null) return;

        Navigation nav = s.navigation;
        if (nav.mode == Navigation.Mode.None) return; // já estava desligado, não mexe

        nav.mode = Navigation.Mode.None;
        s.navigation = nav;
    }
}
