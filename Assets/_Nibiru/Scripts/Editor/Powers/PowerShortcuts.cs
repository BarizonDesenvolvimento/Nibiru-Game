using System.Linq;
using Nibiru.Player;
using Nibiru.Powers;
using Nibiru.Save;
using Nibiru.Testes;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools.PowerTools
{
    /// <summary>
    /// Só no editor: os dois atalhos do teste do LB num controle de verdade (Nibiru > Testes > Poderes). Os dois põem a
    /// Elena no nicho da Aldeia, logo depois da tabuleta (passo 9), com a pistola, o Eco e dois poderes de teste nas vagas 2 e 3 (LB+RB e
    /// LB+Y), e um quadro no canto da tela que mostra o último poder que saiu e o atraso em milissegundos. A diferença é o
    /// esquema do Eco: 1 = plano A (Eco ao soltar o LB) e 2 = plano B (Eco no Y sozinho). O esquema vale só nesse Play; o
    /// do jogo fica no campo "Eco Scheme" do PlayerInputReader do prefab da Elena.
    /// </summary>
    [InitializeOnLoad]
    static class PowerShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Poderes/";
        // Passo da missão "Siga o Samir até a beira da aldeia": logo depois da tabuleta, a Aldeia está calma.
        const int FollowSamirStep = 15;

        static PowerShortcuts()
        {
            Register("poderes.lb_soltar", () => Begin(PlayerInputReader.EcoScheme.SoltarLB));
            Register("poderes.lb_y", () => Begin(PlayerInputReader.EcoScheme.YSozinho));
        }

        [MenuItem(MenuRoot + "1. Teste do LB: plano A (Eco ao soltar o LB)", priority = 1)]
        static void SchemeA() => Go("poderes.lb_soltar");

        [MenuItem(MenuRoot + "2. Teste do LB: plano B (Eco no Y sozinho)", priority = 2)]
        static void SchemeB() => Go("poderes.lb_y");

        static void Begin(PlayerInputReader.EcoScheme scheme)
        {
            Step9Shortcuts.Start(revealed: true, extra: null, FollowSamirStep, Step8Shortcuts.NicheSpot, 0f, () =>
            {
                PlayerInputReader.SchemeOverride = scheme;
                ElenaShortcuts.GiveWeapons();
                PlayerPowers powers = Object.FindFirstObjectByType<PlayerPowers>();
                if (powers == null)
                {
                    Debug.LogError("Teste do LB: a Elena não tem o PlayerPowers (rode Nibiru > Poderes > Montar poderes na Elena).");
                    return;
                }
                foreach (string code in new[] { "poder.teste_poder_vaga_2", "poder.teste_poder_vaga_3" })
                {
                    PowerDefinition power = SaveRegistry.Instance.Power(code);
                    PowerBook.Grant(power);
                    if (powers.GetComponents<TestPowerBehaviour>().All(b => b.definition != power))
                        powers.gameObject.AddComponent<TestPowerBehaviour>().definition = power;
                }
                powers.RefreshBehaviours();
                if (Object.FindFirstObjectByType<PowerButtonOverlay>() == null)
                    new GameObject("Teste_QuadroDoLB").AddComponent<PowerButtonOverlay>();
                Debug.Log(scheme == PlayerInputReader.EcoScheme.SoltarLB
                    ? "Teste do LB, plano A: toque no LB e solte (Eco); LB+RB (vaga 2); LB+Y (vaga 3); RB sozinho é a faca. Segurar o LB mais de 0,5 s e soltar não faz nada."
                    : "Teste do LB, plano B: Y sozinho (Eco, na hora); LB+RB (vaga 2); LB+Y (vaga 3); LB sozinho não faz nada; RB sozinho é a faca.");
            });
        }
    }
}
