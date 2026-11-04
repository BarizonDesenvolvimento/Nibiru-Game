using Nibiru.Player;
using Nibiru.Powers;
using UnityEngine;

namespace Nibiru.Testes
{
    /// <summary>
    /// Só para os atalhos de teste do LB: um quadro no canto da tela com o esquema do Eco em uso, o último poder que
    /// saiu, quantos milissegundos depois de apertar o botão ele saiu e por quanto tempo o LB está seguro.
    /// Acrescentado no Play pelos atalhos (Nibiru > Testes > Poderes); some ao sair do Play.
    /// </summary>
    public class PowerButtonOverlay : MonoBehaviour
    {
        PlayerPowers powers;
        string last = "—";
        float lastAt = -10f;
        int count;
        GUIStyle style;

        void Update()
        {
            PlayerPowers current = FindFirstObjectByType<PlayerPowers>();
            if (current == powers) return;
            if (powers != null) powers.Activated -= OnActivated;
            powers = current;
            if (powers != null) powers.Activated += OnActivated;
        }

        void OnDestroy()
        {
            if (powers != null) powers.Activated -= OnActivated;
        }

        void OnActivated(PowerDefinition power)
        {
            count++;
            PlayerInputReader input = powers.Input;
            string latency = input != null && input.UsingGamepad ? $"{input.LastPadPowerLatency * 1000f:0} ms depois de apertar" : "teclado: na hora";
            last = $"{count}. {power.LocalizedName} ({latency})";
            lastAt = Time.unscaledTime;
        }

        void OnGUI()
        {
            if (powers == null || powers.Input == null) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, richText = true };
            PlayerInputReader input = powers.Input;
            string scheme = input.CurrentEcoScheme == PlayerInputReader.EcoScheme.SoltarLB
                ? "Plano A: Eco ao soltar o LB (toque de até 0,5 s)"
                : "Plano B: Eco no Y sozinho (LB só modificador)";
            string held = input.PowerModifierHeldSeconds > 0f ? $"LB seguro há {input.PowerModifierHeldSeconds:0.00} s" : "LB solto";
            string color = Time.unscaledTime - lastAt < 1.5f ? "#E8C46A" : "#FFFFFF";
            GUI.Box(new Rect(16f, 16f, 560f, 120f),
                $"<b>Teste do LB</b>  {scheme}\n{held}\n<color={color}>Último: {last}</color>\nLB+RB = vaga 2 (teste) · LB+Y = vaga 3 (teste)", style);
        }
    }
}
