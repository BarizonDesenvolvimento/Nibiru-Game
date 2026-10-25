using System;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>Informações de um golpe: quanto, de onde e de quem.</summary>
    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public GameObject source;

        public DamageInfo(float amount, Vector3 point, GameObject source = null)
        {
            this.amount = amount;
            this.point = point;
            this.source = source;
        }
    }

    /// <summary>
    /// Vida de qualquer coisa que pode levar dano: a Elena, soldados, o Lullu.
    /// Outros sistemas escutam os eventos para mostrar a barra, tocar som ou morrer.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Tooltip("Vida máxima.")]
        [SerializeField, Min(1f)] float maxHealth = 100f;

        [Tooltip("Opcional: componente que diz quando o dano deve ser ignorado (ex.: PlayerMotor durante a esquiva).")]
        [SerializeField] MonoBehaviour invulnerabilitySource;

        IInvulnerability invulnerability;
        // Componente deste objeto que muda o dano recebido (opcional), achado sozinho.
        IDamageModifier damageModifier;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Fraction => Current / maxHealth;
        public bool IsDead => Current <= 0f;

        /// <summary>Só para testes: ignora todo dano (ligado pelos atalhos de teste do editor).</summary>
        public bool Invincible { get; set; }

        public event Action<DamageInfo> Damaged;
        public event Action<float> Healed;
        public event Action Died;

        void Awake()
        {
            Current = maxHealth;
            invulnerability = invulnerabilitySource as IInvulnerability;
            if (invulnerabilitySource != null && invulnerability == null)
                Debug.LogError("Health: a fonte de invulnerabilidade não implementa IInvulnerability.", this);
            damageModifier = GetComponent<IDamageModifier>();
        }

        /// <summary>Aplica dano. Devolve false se o golpe foi ignorado (morto ou invulnerável).</summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || info.amount <= 0f || Invincible) return false;
            if (invulnerability != null && invulnerability.IsInvulnerable) return false;
            if (damageModifier != null) info.amount *= damageModifier.DamageMultiplier(info);
            if (info.amount <= 0f) return false;

            Current = Mathf.Max(0f, Current - info.amount);
            Damaged?.Invoke(info);
            if (IsDead) Died?.Invoke();
            return true;
        }

        /// <summary>Define a vida direto, sem eventos de dano ou cura. Usado ao voltar de um checkpoint.</summary>
        public void Restore(float value) => Current = Mathf.Clamp(value, 1f, maxHealth);

        /// <summary>Cura até o máximo. Devolve quanto curou de fato.</summary>
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;

            float healed = Mathf.Min(amount, maxHealth - Current);
            if (healed <= 0f) return 0f;
            Current += healed;
            Healed?.Invoke(healed);
            return healed;
        }
    }
}
