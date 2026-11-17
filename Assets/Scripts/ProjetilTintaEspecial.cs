using System.Collections.Generic;
using UnityEngine;

/// <summary>Bola física do especial da Renata. A cor é escolhida a cada disparo.</summary>
public class ProjetilTintaEspecial : MonoBehaviour
{
    private static readonly Color[] CoresPaleta =
    {
        new Color32(235, 54, 70, 255),   // vermelho
        new Color32(255, 213, 53, 255),  // amarelo
        new Color32(70, 195, 92, 255),   // verde
        new Color32(58, 148, 238, 255),  // azul
        new Color32(161, 83, 207, 255)  // roxo
    };

    private static readonly List<ProjetilTintaEspecial> ativos = new List<ProjetilTintaEspecial>();
    private static Sprite spritePadrao;

    private LutadorController2D origem;
    private LutadorController2D alvo;
    private Rigidbody2D corpo;
    private int dano;
    private bool impactou;
    private float alturaDosPes;
    private float metadeAlturaVisual;

    public static void Criar(LutadorController2D lutador, LutadorController2D oponente)
    {
        if (lutador == null || oponente == null || lutador.dadosPersonagem == null || lutador.EstaMorto())
            return;

        DadosPersonagem dados = lutador.dadosPersonagem;
        float direcao = lutador.transform.localScale.x < 0f ? -1f : 1f;
        Vector3 posicao = lutador.ObterPosicaoCintura() + new Vector3(direcao * 0.65f, 0.2f, -0.05f);
        CriarInterno(lutador, oponente, posicao,
            new Vector2(direcao * Mathf.Max(0f, dados.velocidadeProjetilTinta),
                dados.impulsoVerticalProjetilTinta), 3f);
    }

    public static void CriarQueda(LutadorController2D lutador, LutadorController2D oponente, Vector3 posicao)
    {
        if (lutador == null || oponente == null || lutador.dadosPersonagem == null || lutador.EstaMorto())
            return;

        // O ultimate solta a mesma bola, mas sem nenhum impulso lateral ou vertical.
        CriarInterno(lutador, oponente, posicao, Vector2.zero, 5f);
    }

    private static void CriarInterno(LutadorController2D lutador, LutadorController2D oponente,
        Vector3 posicao, Vector2 velocidadeInicial, float tempoVida)
    {
        DadosPersonagem dados = lutador.dadosPersonagem;
        GameObject objeto = new GameObject("BolaDeTinta_Renata");
        objeto.transform.position = posicao;

        SpriteRenderer visual = objeto.AddComponent<SpriteRenderer>();
        visual.sprite = dados.spriteProjetilTinta != null ? dados.spriteProjetilTinta : ObterSpritePadrao();
        visual.color = CoresPaleta[Random.Range(0, CoresPaleta.Length)];
        visual.sortingLayerID = lutador.spriteRenderer != null ? lutador.spriteRenderer.sortingLayerID : 0;
        visual.sortingOrder = lutador.spriteRenderer != null ? lutador.spriteRenderer.sortingOrder + 2 : 20;

        CircleCollider2D colisor = objeto.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.24f;

        Rigidbody2D corpo = objeto.AddComponent<Rigidbody2D>();
        corpo.gravityScale = Mathf.Max(0f, dados.gravidadeProjetilTinta);
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.linearVelocity = velocidadeInicial;

        ProjetilTintaEspecial projetil = objeto.AddComponent<ProjetilTintaEspecial>();
        projetil.origem = lutador;
        projetil.alvo = oponente;
        projetil.corpo = corpo;
        projetil.dano = dados.danoEspecial;
        projetil.alturaDosPes = lutador.spriteRenderer != null
            ? lutador.spriteRenderer.bounds.min.y : lutador.transform.position.y;
        projetil.metadeAlturaVisual = visual.bounds.extents.y;
        ativos.Add(projetil);
        Destroy(objeto, tempoVida);
    }

    private void FixedUpdate()
    {
        if (impactou || corpo == null)
            return;

        // Antecipar o próximo passo da física evita um frame visível sob o chão.
        float passo = Time.fixedDeltaTime;
        float proximaAltura = corpo.position.y + corpo.linearVelocity.y * passo +
            0.5f * Physics2D.gravity.y * corpo.gravityScale * passo * passo;
        if (proximaAltura - metadeAlturaVisual <= alturaDosPes)
        {
            impactou = true;
            Destroy(gameObject);
        }
    }

    public static void LimparTodos()
    {
        for (int i = ativos.Count - 1; i >= 0; i--)
            if (ativos[i] != null)
                Destroy(ativos[i].gameObject);
        ativos.Clear();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (impactou || outro == null)
            return;

        LutadorController2D lutadorAtingido = outro.GetComponentInParent<LutadorController2D>();
        if (lutadorAtingido == origem)
            return;

        if (lutadorAtingido == alvo)
        {
            impactou = true;
            if (!alvo.EstaMorto())
            {
                alvo.ReceberDano(dano);
            }
            Destroy(gameObject);
        }
        else if (lutadorAtingido == null && !outro.isTrigger)
        {
            // O chão ou um obstáculo encerra a trajetória sem dano.
            impactou = true;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        ativos.Remove(this);
    }

    private static Sprite ObterSpritePadrao()
    {
        if (spritePadrao != null)
            return spritePadrao;

        const int tamanho = 12;
        Texture2D textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[tamanho * tamanho];

        for (int y = 0; y < tamanho; y++)
        for (int x = 0; x < tamanho; x++)
        {
            float dx = x - 5.5f;
            float dy = y - 5.5f;
            float raio = dx * dx + dy * dy;
            if (raio > 30f)
                pixels[y * tamanho + x] = new Color32(0, 0, 0, 0);
            else if (x <= 4 && y >= 7 && raio < 15f)
                pixels[y * tamanho + x] = new Color32(255, 255, 255, 255);
            else
                pixels[y * tamanho + x] = new Color32(220, 220, 220, 255);
        }

        textura.SetPixels32(pixels);
        textura.Apply();
        spritePadrao = Sprite.Create(textura, new Rect(0f, 0f, tamanho, tamanho),
            new Vector2(0.5f, 0.5f), 24f, 0, SpriteMeshType.FullRect);
        spritePadrao.name = "BolaDeTinta_Runtime";
        return spritePadrao;
    }
}
