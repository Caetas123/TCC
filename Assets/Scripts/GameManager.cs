using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // O FPS é definido pelo FPSManager a partir da taxa do monitor ou da
        // preferência salva do jogador. Não force 60 ao trocar de cena.
        QualitySettings.vSyncCount = 0;
    }
}
