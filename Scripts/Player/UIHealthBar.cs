using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth player;   // Drag your Player here
    public Image healthFill;      // Drag the red Image here

    void OnEnable()
    {
        if (player != null)
            player.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (player != null)
            player.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        // Initialize fill if references are set
        if (player != null) HandleHealthChanged(player.currentHealth, player.maxHealth);
    }

    void HandleHealthChanged(int current, int max)
    {
        if (healthFill == null || max <= 0) return;
        healthFill.fillAmount = (float)current / max; // expects Image Type = Filled (Horizontal, Left)
    }
}
