using UnityEngine;

public class LutadorController2D : MonoBehaviour
{
    [Header("Identificação")]
    public bool jogador1 = true;

    [Header("Movimento")]
    public float velocidade = 45f;
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

    private bool estaNoChao;
    private bool defendendo;
    private float movimento;

    void Start()
    {
        vidaAtual = vidaMax;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        LerInputs();
        VirarParaOponente();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(movimento * velocidade, rb.linearVelocity.y);
        }
    }

    void LerInputs()
    {
        movimento = 0f;
        defendendo = false;

        if (jogador1)
        {
            if (Input.GetKey(KeyCode.A)) movimento = -1f;
            if (Input.GetKey(KeyCode.D)) movimento = 1f;

            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("P1 apertou W | estaNoChao = " + estaNoChao);
                if (estaNoChao)
                    Pular();
            }

            if (Input.GetKey(KeyCode.S))
                defendendo = true;

            if (Input.GetKeyDown(KeyCode.F))
                AtaqueNormal();

            if (Input.GetKeyDown(KeyCode.G))
                AtaqueEspecial();
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) movimento = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) movimento = 1f;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Debug.Log("P2 apertou UP | estaNoChao = " + estaNoChao);
                if (estaNoChao)
                    Pular();
            }

            if (Input.GetKey(KeyCode.DownArrow))
                defendendo = true;

            if (Input.GetKeyDown(KeyCode.K))
                AtaqueNormal();

            if (Input.GetKeyDown(KeyCode.L))
                AtaqueEspecial();
        }
    }

    void Pular()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);

        Debug.Log(gameObject.name + " pulou");
    }

    void AtaqueNormal()
    {
        if (oponente == null) return;

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);

        if (distancia <= alcanceAtaque)
        {
            oponente.ReceberDano(danoAtaque);
            energiaAtual = Mathf.Min(energiaAtual + 10, energiaMax);

            Debug.Log(gameObject.name + " acertou ataque normal");
        }
    }

    void AtaqueEspecial()
    {
        if (oponente == null) return;

        if (energiaAtual < custoEspecial)
        {
            Debug.Log(gameObject.name + " sem energia para especial");
            return;
        }

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);

        if (distancia <= alcanceEspecial)
        {
            energiaAtual -= custoEspecial;
            oponente.ReceberDano(danoEspecial);

            Debug.Log(gameObject.name + " acertou ataque especial");
        }
    }

    public void ReceberDano(int dano)
    {
        int danoFinal = defendendo ? Mathf.RoundToInt(dano * 0.4f) : dano;

        vidaAtual -= danoFinal;

        if (vidaAtual < 0)
            vidaAtual = 0;

        Debug.Log(gameObject.name + " recebeu dano: " + danoFinal + " | Vida atual: " + vidaAtual);
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
        {
            estaNoChao = true;
            Debug.Log(gameObject.name + " tocou no chão");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
        {
            estaNoChao = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
        {
            estaNoChao = false;
            Debug.Log(gameObject.name + " saiu do chão");
        }
    }
}