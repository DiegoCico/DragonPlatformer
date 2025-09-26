using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [System.Serializable] public class HealthEvent : UnityEvent<int,int> {} // (hp,max)

    [Header("Health")]
    public int maxHP = 6;
    public int startHP = 6;

    [Header("Events")]
    public HealthEvent onHealthChanged;
    public UnityEvent onDeath;

    public int HP { get; private set; }

    void Awake() => HP = Mathf.Clamp(startHP, 0, maxHP);

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        HP = Mathf.Max(0, HP - amount);
        onHealthChanged?.Invoke(HP, maxHP);
        if (HP == 0) onDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        HP = Mathf.Min(maxHP, HP + amount);
        onHealthChanged?.Invoke(HP, maxHP);
    }
}
