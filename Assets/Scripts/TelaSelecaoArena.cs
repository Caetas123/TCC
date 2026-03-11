using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TelaSelecaoArena : MonoBehaviour
{
    [Header("Imagens das arenas")]
    public Image imagemArena1;
    public Image imagemArena2;

    private int arenaSelecionada = -1;
    private bool sorteando = false;

    public void EscolherArena1()
    {
        arenaSelecionada = 1;
        DestacarArena(1);
        IniciarLuta();
    }

    public void EscolherArena2()
    {
        arenaSelecionada = 2;
        DestacarArena(2);
        IniciarLuta();
    }

    public void ArenaAleatoria()
    {
        if (!sorteando)
            StartCoroutine(SorteioAnimado());
    }
    IEnumerator SorteioAnimado()
    {
        sorteando = true;
        float tempo = 0.1f;
        for (int i = 0; i < 10; i++)
        {
            int arenaTemp = Random.Range(1, 3);
            DestacarArena(arenaTemp);
            yield return new WaitForSeconds(tempo);
            tempo += 0.05f;
        }

        arenaSelecionada = Random.Range(1, 3);
        DestacarArena(arenaSelecionada);
        sorteando = false;
        IniciarLuta();
    }

    void DestacarArena(int arena)
    {
        Color normal = Color.white;
        Color selecionado = Color.yellow;

        imagemArena1.color = normal;
        imagemArena2.color = normal;

        if (arena == 1)
            imagemArena1.color = selecionado;

        if (arena == 2)
            imagemArena2.color = selecionado;
    }

    public void IniciarLuta()
    {
        if (arenaSelecionada == -1)
        {
            Debug.Log("Selecione uma arena primeiro");
            return;
        }

        PlayerPrefs.SetInt("ArenaSelecionada", arenaSelecionada);
        PlayerPrefs.Save();

        if (arenaSelecionada == 1)
            SceneManager.LoadScene("cena1");

        if (arenaSelecionada == 2)
            SceneManager.LoadScene("cena2");
    }

    public void Voltar()
    {
        SceneManager.LoadScene("SelecaoPlayer");
    }
}