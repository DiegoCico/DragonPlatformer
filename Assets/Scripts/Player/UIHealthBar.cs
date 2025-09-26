using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHealthBar : MonoBehaviour
{
    public PlayerHealth player;
    public Image fill;                 // Image type = Filled, Fill Method = Horizontal
    public TextMeshProUGUI label;      // optional "HP 5/6"

    void OnEnable()
    {
        if (player) player.onHealthChanged.AddListener(UpdateBar);
        if (player) UpdateBar(player.HP, player.maxHP);
    }
    void OnDisable()
    {
        if (player) player.onHealthChanged.RemoveListener(UpdateBar);
    }

    void UpdateBar(int hp, int max)
    {
        if (fill) fill.fillAmount = max > 0 ? (float)hp / max : 0f;
        if (label) label.text = $"HP {hp}/{max}";
    }
}
