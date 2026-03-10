using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TelaSelecaoArena : MonoBehaviour
{
    [Header("Preview da arena")]
    public Image imagemPreview;

    [Header("Imagens das arenas")]
    public Sprite previewArena1;
    public Sprite previewArena2;

    private int arenaSelecionada = -1;

    public void EscolherArena1()
    {
        arenaSelecionada = 1;
        imagemPreview.sprite = previewArena1;
    }

    public void EscolherArena2()
    {
        arenaSelecionada = 2;
        imagemPreview.sprite = previewArena2;
    }

    public void ArenaAleatoria()
    {
        arenaSelecionada = Random.Range(1, 3);

        if (arenaSelecionada == 1)
            imagemPreview.sprite = previewArena1;
        else
            imagemPreview.sprite = previewArena2;
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