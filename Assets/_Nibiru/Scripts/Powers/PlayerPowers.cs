using System;
using System.Collections.Generic;
using Nibiru.Combat;
using Nibiru.Player;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>O que o HUD (ME5) mostra de cada poder.</summary>
    public enum PowerState
    {
        NaoPossui,
        Pronto,
        Ativo,
        Recarregando,
    }

    /// <summary>
    /// Fica na Elena. Lê as 3 vagas de poder no <see cref="PlayerInputReader"/> (com a regra do LB do controle já resolvida
    /// lá), acha o poder que ela tem naquela vaga, confere os requisitos e a recarga, chama o efeito
    /// (<see cref="PowerBehaviour"/>) e começa a recarga no <see cref="PowerBook"/>. A recarga anda em tempo de jogo
    /// (Time.deltaTime): não anda com o jogo pausado, em menus, no inventário, na leitura nem nos diálogos.
    /// </summary>
    public class PlayerPowers : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Health health;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerMeleeAttack melee;
        [SerializeField] PlayerWeapons weapons;

        readonly List<PowerBehaviour> behaviours = new List<PowerBehaviour>();
        readonly Dictionary<PowerDefinition, float> activeUntil = new Dictionary<PowerDefinition, float>();

        /// <summary>Avisa a cada poder usado (os atalhos de teste do LB mostram na tela).</summary>
        public event Action<PowerDefinition> Activated;

        public PlayerInputReader Input => input;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (melee == null) melee = GetComponent<PlayerMeleeAttack>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();
            RefreshBehaviours();
        }

        /// <summary>Procura de novo os efeitos no objeto (os testes acrescentam efeitos de teste no Play).</summary>
        public void RefreshBehaviours()
        {
            behaviours.Clear();
            GetComponents(behaviours);
        }

        void Update()
        {
            PowerBook.Tick(Time.deltaTime);
            if (input == null) return;
            for (int slot = 1; slot <= 3; slot++)
                if (input.PowerSlotPressed((PowerSlot)slot)) TryActivateSlot((PowerSlot)slot);
        }

        /// <summary>O efeito do poder que a Elena tem nesta vaga (null se ela não tem nenhum com efeito).</summary>
        public PowerBehaviour BehaviourFor(PowerSlot slot)
        {
            foreach (PowerBehaviour b in behaviours)
                if (b != null && b.isActiveAndEnabled && b.Definition != null && b.Definition.slot == slot && PowerBook.Has(b.Definition))
                    return b;
            return null;
        }

        public bool TryActivateSlot(PowerSlot slot)
        {
            PowerBehaviour behaviour = BehaviourFor(slot);
            return behaviour != null && TryActivate(behaviour);
        }

        public bool TryActivate(PowerBehaviour behaviour)
        {
            PowerDefinition power = behaviour.Definition;
            if (!CanUse(power) || !behaviour.CanActivateNow) return false;
            behaviour.Activate();
            PowerBook.StartCooldown(power);
            if (power.duration > 0f) activeUntil[power] = Time.time + power.duration;
            Activated?.Invoke(power);
            return true;
        }

        /// <summary>Os requisitos comuns e os do arquivo do poder. Não há custo por uso: o limite é a recarga.</summary>
        public bool CanUse(PowerDefinition power)
        {
            if (power == null || !PowerBook.Has(power) || PowerBook.CooldownLeft(power) > 0f) return false;
            if (input != null && (!input.GameplayEnabled || !input.CombatEnabled)) return false;
            if (health != null && health.IsDead) return false;
            if (motor != null && (motor.IsDodging || (motor.Aiming && !power.usableWhileAiming) || (motor.IsCrouching && !power.usableWhileCrouched))) return false;
            if (melee != null && melee.IsAttacking) return false;
            if (weapons != null && weapons.IsReloading && !power.usableWhileReloading) return false;
            return true;
        }

        public bool IsActive(PowerDefinition power) => power != null && activeUntil.TryGetValue(power, out float until) && Time.time < until;

        public PowerState StateOf(PowerDefinition power)
        {
            if (!PowerBook.Has(power)) return PowerState.NaoPossui;
            if (IsActive(power)) return PowerState.Ativo;
            return PowerBook.CooldownLeft(power) > 0f ? PowerState.Recarregando : PowerState.Pronto;
        }

        /// <summary>O nome do botão da vaga no aparelho em uso ("Q", "LB", "LB + RB"...).</summary>
        public string ButtonLabel(PowerSlot slot) => input != null ? input.PowerSlotLabel(slot) : "";
    }
}
