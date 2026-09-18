using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Script de EDITOR — precisa ficar dentro de uma pasta chamada "Editor"
// (ex: Assets/Editor/CorrigirHoverFocoEmMassa.cs), senão a Unity tenta
// incluir ele no build do jogo e dá erro.
//
// O que faz: varre todo Button e Dropdown (legado e TMP) da cena ABERTA no
// momento, e em cada um que ainda não tem o ButtonVisualPersonalizado:
//   1. Lê o sprite normal atual (Image.sprite) e os sprites Highlighted/
//      Selected já configurados no Sprite Swap nativo.
//   2. Anexa o ButtonVisualPersonalizado e copia esses 3 sprites pra ele.
//   3. Desliga o Transition nativo (vira None).
//   4. Remove o HoverAssumeFoco antigo, se sobrou algum.
//
// Depois de rodar, é só CONFERIR se os sprites vieram certos (o script só
// copia o que já estava preenchido no Inspector) e SALVAR a cena (Ctrl+S).
public static class CorrigirHoverFocoEmMassa
{
    [MenuItem("Tools/Foco e Hover/Aplicar em todos os Botões e Dropdowns da cena aberta")]
    public static void AplicarNaCenaAberta()
    {
        Selectable[] todos = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int botoesAtualizados = 0;
        int dropdownsAtualizados = 0;
        int puladosSemImagem = 0;

        foreach (Selectable s in todos)
        {
            if (s == null) continue;

            bool ehButton = s is Button;
            bool ehDropdown = s is TMP_Dropdown || s is Dropdown;
            if (!ehButton && !ehDropdown) continue; // por enquanto só Button e Dropdown

            if (s.GetComponent<ButtonVisualPersonalizado>() != null) continue; // já corrigido

            Image imagem = s.GetComponent<Image>();
            if (imagem == null) { puladosSemImagem++; continue; } // sem Image não dá pra trocar sprite

            SpriteState estado = s.spriteState;

            Undo.RegisterCompleteObjectUndo(s.gameObject, "Aplicar Foco/Hover");

            ButtonVisualPersonalizado bvp = Undo.AddComponent<ButtonVisualPersonalizado>(s.gameObject);
            bvp.normalSprite = imagem.sprite;
            bvp.highlightedSprite = estado.highlightedSprite;
            bvp.selectedSprite = estado.selectedSprite;

            s.transition = Selectable.Transition.None;

            HoverAssumeFoco velho = s.GetComponent<HoverAssumeFoco>();
            if (velho != null)
                Undo.DestroyObjectImmediate(velho);

            EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(bvp);

            if (ehButton) botoesAtualizados++;
            else dropdownsAtualizados++;
        }

        if (botoesAtualizados > 0 || dropdownsAtualizados > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[Foco/Hover em massa] Botões corrigidos: {botoesAtualizados} | Dropdowns corrigidos: {dropdownsAtualizados} | Pulados (sem Image): {puladosSemImagem}. Confere os sprites e SALVA a cena (Ctrl+S).");
    }
}
