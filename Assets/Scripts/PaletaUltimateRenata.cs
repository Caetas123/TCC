using System.Collections.Generic;
using UnityEngine;

/// <summary>Paleta que persegue o oponente e deixa cair bolas de tinta por 30 segundos.</summary>
public class PaletaUltimateRenata : MonoBehaviour
{
    private const float TempoParaSegundoFrame = 0.5f;
    private const float DuracaoViagem = 1.1f;
    private const float DuracaoNoAlvo = 30f;
    private const float IntervaloBolas = 2f;

    private static readonly List<PaletaUltimateRenata> ativas = new List<PaletaUltimateRenata>();

    private LutadorController2D origem;
    private LutadorController2D alvo;
    private Sprite[] frames;
    private SpriteRenderer visual;
    private Vector3 posicaoInicial;
    private float tempoViagem;
    private float tempoNoAlvo;
    private float proximaBola = IntervaloBolas;
    private bool travada;
    private bool encerrada;

    public static void Criar(LutadorController2D lutador, LutadorController2D oponente)
    {
        if (lutador == null || oponente == null || lutador.dadosPersonagem == null || lutador.EstaMorto())
            return;

        Sprite[] sprites = lutador.dadosPersonagem.framesPaletaUltimate;
        if (sprites == null || sprites.Length < 3 ||
            sprites[0] == null || sprites[1] == null || sprites[2] == null)
        {
            Debug.LogWarning("Renata precisa dos três sprites da paleta configurados no DadosPersonagem.");
            return;
        }

        // Um novo ultimate substitui a paleta anterior da mesma Renata.
        for (int i = ativas.Count - 1; i >= 0; i--)
            if (ativas[i] != null && ativas[i].origem == lutador)
            {
                ativas[i].encerrada = true;
                Destroy(ativas[i].gameObject);
            }

        GameObject objeto = new GameObject("PaletaUltimate_Renata");
        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[0];
        renderer.sortingLayerID = lutador.spriteRenderer != null ? lutador.spriteRenderer.sortingLayerID : 0;
        renderer.sortingOrder = lutador.spriteRenderer != null ? lutador.spriteRenderer.sortingOrder + 4 : 24;

        float alturaLutador = lutador.spriteRenderer != null ? lutador.spriteRenderer.bounds.size.y : 3f;
        objeto.transform.localScale = Vector3.one * Mathf.Clamp(alturaLutador * 0.35f, 0.8f, 1.8f);
        float topoLutador = lutador.spriteRenderer != null
            ? lutador.spriteRenderer.bounds.max.y : lutador.transform.position.y + alturaLutador;
        float centroLutador = lutador.spriteRenderer != null
            ? lutador.spriteRenderer.bounds.center.x : lutador.transform.position.x;
        objeto.transform.position = new Vector3(
            centroLutador, topoLutador + renderer.bounds.extents.y + 0.15f,
            lutador.transform.position.z - 0.1f);

        PaletaUltimateRenata paleta = objeto.AddComponent<PaletaUltimateRenata>();
        paleta.origem = lutador;
        paleta.alvo = oponente;
        paleta.frames = sprites;
        paleta.visual = renderer;
        paleta.posicaoInicial = objeto.transform.position;
        ativas.Add(paleta);
    }

    private void Update()
    {
        if (encerrada)
            return;

        if (origem == null || alvo == null || origem.EstaMorto() || alvo.EstaMorto())
        {
            Destroy(gameObject);
            return;
        }

        if (!travada)
        {
            tempoViagem += Time.deltaTime;
            if (tempoViagem >= TempoParaSegundoFrame)
                visual.sprite = frames[1];

            transform.position = Vector3.Lerp(posicaoInicial, PosicaoAcimaDoAlvo(),
                Mathf.Clamp01(tempoViagem / DuracaoViagem));
            if (tempoViagem < DuracaoViagem)
                return;

            travada = true;
            visual.sprite = frames[2];
            transform.position = PosicaoAcimaDoAlvo();
        }

        transform.position = PosicaoAcimaDoAlvo();
        tempoNoAlvo += Time.deltaTime;
        while (tempoNoAlvo >= proximaBola && proximaBola <= DuracaoNoAlvo)
        {
            Vector3 origemBola = new Vector3(visual.bounds.center.x,
                visual.bounds.min.y - 0.1f, transform.position.z - 0.05f);
            ProjetilTintaEspecial.CriarQueda(origem, alvo, origemBola);
            proximaBola += IntervaloBolas;
        }

        if (tempoNoAlvo >= DuracaoNoAlvo)
            Destroy(gameObject);
    }

    private Vector3 PosicaoAcimaDoAlvo()
    {
        float x = alvo.spriteRenderer != null
            ? alvo.spriteRenderer.bounds.center.x : alvo.transform.position.x;
        float topoAlvo = alvo.spriteRenderer != null
            ? alvo.spriteRenderer.bounds.max.y : alvo.transform.position.y;
        float y = topoAlvo + visual.bounds.extents.y + 1f;

        Camera cameraCena = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cameraCena != null)
        {
            float distanciaCamera = Mathf.Abs(cameraCena.transform.position.z - transform.position.z);
            Vector3 inferior = cameraCena.ViewportToWorldPoint(new Vector3(0.05f, 0.7f, distanciaCamera));
            Vector3 superior = cameraCena.ViewportToWorldPoint(new Vector3(0.95f, 0.82f, distanciaCamera));
            x = Mathf.Clamp(x, inferior.x + visual.bounds.extents.x,
                superior.x - visual.bounds.extents.x);
            y = Mathf.Clamp(y, inferior.y, superior.y);
        }

        return new Vector3(x, y, posicaoInicial.z);
    }

    public static void LimparTodos()
    {
        for (int i = ativas.Count - 1; i >= 0; i--)
            if (ativas[i] != null)
            {
                ativas[i].encerrada = true;
                Destroy(ativas[i].gameObject);
            }
        ativas.Clear();
    }

    private void OnDestroy()
    {
        ativas.Remove(this);
    }
}
