using System;
using System.Collections.Generic;
using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// A única fonte de verdade sobre os poderes da Elena: quais ela tem e quanto falta de recarga em cada um.
    /// Os dados moram no <see cref="WorldState"/> (por código permanente), então o checkpoint congela, a morte restaura
    /// e o salvamento grava tudo junto com o resto do mundo.
    /// Quem pode mudar: só quem dá o poder (a tabuleta, o puzzle do selo do ME2), o carregamento e os atalhos de teste.
    /// Quem pode perguntar: qualquer sistema (Has, Owned, CooldownLeft, evento Changed).
    /// Na demo a Elena nunca perde um poder; <see cref="Revoke"/> existe para os testes e para o jogo inteiro.
    /// </summary>
    public static class PowerBook
    {
        /// <summary>Avisa quando um poder entra ou sai (não a cada quadro da recarga).</summary>
        public static event Action Changed;

        static int lastTickFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Changed = null;
            lastTickFrame = -1;
        }

        public static bool Has(PowerDefinition power) => power != null && WorldState.HasPower(power.Id);

        public static bool HasCode(string code) => WorldState.HasPower(code);

        /// <summary>Os poderes que a Elena tem, em ordem de código (os que o registro não conhece ficam de fora).</summary>
        public static List<PowerDefinition> Owned
        {
            get
            {
                var list = new List<PowerDefinition>();
                SaveRegistry registry = SaveRegistry.Instance;
                if (registry == null) return list;
                foreach (string code in WorldState.PowerCodes)
                {
                    PowerDefinition power = registry.Power(code);
                    if (power != null) list.Add(power);
                }
                return list;
            }
        }

        /// <summary>Dá o poder. Sem efeito se ela já o tem. Devolve true se ele é novo.</summary>
        public static bool Grant(PowerDefinition power)
        {
            if (power == null || Has(power)) return false;
            WorldState.AddPower(power.Id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Tira o poder e a recarga dele. Sem uso na demo (a Elena nunca perde um ME); existe para os testes.</summary>
        public static bool Revoke(PowerDefinition power)
        {
            if (power == null || !Has(power)) return false;
            WorldState.RemovePower(power.Id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Segundos de jogo que faltam para o poder ficar pronto (0 = pronto).</summary>
        public static float CooldownLeft(PowerDefinition power) => power == null ? 0f : WorldState.GetPowerCooldown(power.Id);

        /// <summary>Começa a recarga cheia do poder (chamado por quem dispara, ao usar).</summary>
        public static void StartCooldown(PowerDefinition power)
        {
            if (power != null && power.cooldown > 0f) WorldState.SetPowerCooldown(power.Id, power.cooldown);
        }

        /// <summary>
        /// Anda a recarga de todos os poderes, uma vez por quadro (mesmo que dois componentes chamem). Recebe o tempo do
        /// jogo (Time.deltaTime), que para quando a pausa, o inventário, a leitura e os diálogos param o tempo.
        /// </summary>
        public static void Tick(float deltaTime)
        {
            if (Time.frameCount == lastTickFrame) return;
            lastTickFrame = Time.frameCount;
            if (deltaTime > 0f) WorldState.TickPowerCooldowns(deltaTime);
        }

        /// <summary>
        /// Converte a posse antiga (evento "Poder:" + nome do arquivo, usado antes do ME1) em posse de verdade e tira o
        /// evento. Usado ao ler um save antigo e pelos atalhos de teste. Devolve quantos poderes converteu.
        /// </summary>
        public static int ConvertLegacyEvents()
        {
            SaveRegistry registry = SaveRegistry.Instance;
            if (registry == null) return 0;
            int count = 0;
            foreach (PowerDefinition power in registry.powers)
            {
                if (power == null || !WorldState.HasStoryEvent(power.LegacyEventId)) continue;
                WorldState.RemoveStoryEvent(power.LegacyEventId);
                if (!WorldState.HasPower(power.Id)) WorldState.AddPower(power.Id);
                count++;
            }
            if (count > 0) Changed?.Invoke();
            return count;
        }
    }
}
