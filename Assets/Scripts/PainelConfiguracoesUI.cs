using UnityEngine;
using UnityEngine.EventSystems;

public class PainelConfiguracoesUI : MonoBehaviour
{
    public GameObject primeiroBotao;

    private void OnEnable()
    {
        SelecionarPrimeiroBotao();
    }

    public void SelecionarPrimeiroBotao()
    {
        if (primeiroBotao == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(primeiroBotao);
    }
}