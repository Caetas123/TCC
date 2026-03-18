using UnityEngine;

public class LutadorController2D : MonoBehaviour
{
    [Header("Identificação")]
    public bool jogador1 = true;

    [Header("Movimento")]
    public float velocidade = 82f;
    public float forcaPulo = 52f;

    [Header("Combate")]
    public int vidaMax = 100;
    public int vidaAtual = 100;
    public int energiaMax = 100;
    public int energiaAtual = 0;
    public int danoAtaque = 10;
    public int danoEspecial = 25;
    public int custoEspecial = 30;
    public float alcanceAtaque = 2f;
    public float alcanceEspecial = 2.5f;

    [Header("Referências")]
    public Rigidbody2D rb;
    public LutadorController2D oponente;

    [Header("Teclas atuais")]
    public KeyCode teclaEsquerda;
    public KeyCode teclaDireita;
    public KeyCode teclaPular;
    public KeyCode teclaAtaque;
    public KeyCode teclaEspecial;
    public KeyCode teclaDefender;

    private bool estaNoChao;
    private bool defendendo;
    private float movimento;

    void Start()
    {
        vidaAtual = vidaMax;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        CarregarTeclas();
    }

    void Update()
    {
        LerInputs();
        VirarParaOponente();
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(movimento * velocidade, rb.linearVelocity.y);
    }

    void CarregarTeclas()
    {
        if (jogador1)
        {
            teclaEsquerda = StringParaKeyCodeSeguro("P1_Esquerda", KeyCode.A);
            teclaDireita = StringParaKeyCodeSeguro("P1_Direita", KeyCode.D);
            teclaPular = StringParaKeyCodeSeguro("P1_Pular", KeyCode.W);
            teclaAtaque = StringParaKeyCodeSeguro("P1_Ataque", KeyCode.F);
            teclaEspecial = StringParaKeyCodeSeguro("P1_Especial", KeyCode.G);
            teclaDefender = KeyCode.S;
        }
        else
        {
            teclaEsquerda = StringParaKeyCodeSeguro("P2_Esquerda", KeyCode.LeftArrow);
            teclaDireita = StringParaKeyCodeSeguro("P2_Direita", KeyCode.RightArrow);
            teclaPular = StringParaKeyCodeSeguro("P2_Pular", KeyCode.UpArrow);
            teclaAtaque = StringParaKeyCodeSeguro("P2_Ataque", KeyCode.K);
            teclaEspecial = StringParaKeyCodeSeguro("P2_Especial", KeyCode.L);
            teclaDefender = KeyCode.DownArrow;
        }

        Debug.Log($"{gameObject.name} | Esq:{teclaEsquerda} Dir:{teclaDireita} Pulo:{teclaPular} Atk:{teclaAtaque} Esp:{teclaEspecial}");
    }

    KeyCode StringParaKeyCodeSeguro(string chave, KeyCode padrao)
    {
        string valor = PlayerPrefs.GetString(chave, padrao.ToString());

        if (string.IsNullOrWhiteSpace(valor))
            return padrao;

        try
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), valor);
        }
        catch
        {
            Debug.LogWarning($"Tecla inválida em {chave}: {valor}. Usando padrão {padrao}");
            return padrao;
        }
    }

    void LerInputs()
    {
        movimento = 0f;
        defendendo = false;

        if (Input.GetKey(teclaEsquerda))
            movimento = -1f;

        if (Input.GetKey(teclaDireita))
            movimento = 1f;

        if (Input.GetKeyDown(teclaPular) && estaNoChao)
            Pular();

        if (Input.GetKey(teclaDefender))
            defendendo = true;

        if (Input.GetKeyDown(teclaAtaque))
            AtaqueNormal();

        if (Input.GetKeyDown(teclaEspecial))
            AtaqueEspecial();
    }

    void Pular()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);
    }

    void AtaqueNormal()
    {
        if (oponente == null) return;

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);

        if (distancia <= alcanceAtaque)
        {
            oponente.ReceberDano(danoAtaque);
            energiaAtual = Mathf.Min(energiaAtual + 10, energiaMax);
        }
    }

    void AtaqueEspecial()
    {
        if (oponente == null) return;
        if (energiaAtual < custoEspecial) return;

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);

        if (distancia <= alcanceEspecial)
        {
            energiaAtual -= custoEspecial;
            oponente.ReceberDano(danoEspecial);
        }
    }

    public void ReceberDano(int dano)
    {
        int danoFinal = defendendo ? Mathf.RoundToInt(dano * 0.4f) : dano;

        vidaAtual -= danoFinal;
        if (vidaAtual < 0)
            vidaAtual = 0;
    }

    void VirarParaOponente()
    {
        if (oponente == null) return;

        Vector3 escala = transform.localScale;

        if (transform.position.x < oponente.transform.position.x)
            escala.x = Mathf.Abs(escala.x);
        else
            escala.x = -Mathf.Abs(escala.x);

        transform.localScale = escala;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
            estaNoChao = true;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
            estaNoChao = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
            estaNoChao = false;
    }
}