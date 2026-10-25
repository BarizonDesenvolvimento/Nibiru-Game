using Nibiru.Combat;
using Nibiru.Inventory;
using Nibiru.Player;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar a Elena animada (andar, correr, agachar, esquivar, mirar,
    /// atirar, recarregar, faca, dano e morte). Menu: Nibiru > Testes > Elena.
    /// </summary>
    [InitializeOnLoad]
    static class ElenaShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Elena/";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";

        // Depois do plano de fuga: a patrulha andando rente ao muro, bom para testar o combate.
        static readonly Vector3 PatrolSpot = new Vector3(-47.5f, 0f, 6.5f);

        static ElenaShortcuts()
        {
            Register("elena.laboratorio", () =>
            {
                GiveWeapons();
                StopEnemies();
                Debug.Log("Elena no laboratório, com pistola, munição e faca, e os inimigos parados. Teste andar, correr, agachar, esquivar, mirar, atirar, recarregar e a faca. Dano e morte: atalhos 3 e 4.");
            });
            Register("elena.regiao", () =>
            {
                Step9Shortcuts.Start(revealed: true, extra: new[] { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga" },
                    Step9Shortcuts.CrossRoadStep, PatrolSpot, 90f, GiveWeapons);
                Debug.Log("Elena na beira da Aldeia, com pistola, munição e faca. A patrulha está rente ao muro, a leste.");
            });
        }

        [MenuItem(MenuRoot + "1. Laboratório (Teste_Personagem) com pistola e faca")]
        static void Lab() => Go("elena.laboratorio", LabScenePath);

        [MenuItem(MenuRoot + "2. Região, na beira da Aldeia, com pistola e faca")]
        static void Region() => Go("elena.regiao");

        [MenuItem(MenuRoot + "3. Levar dano (10 de vida)")]
        static void Hurt() => Damage(10f);

        [MenuItem(MenuRoot + "4. Morrer")]
        static void Die() => Damage(10000f);

        static void Damage(float amount)
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (!EditorApplication.isPlaying || motor == null)
            {
                Debug.LogWarning("Ligue o Play com a Elena na cena antes deste atalho.");
                return;
            }
            Vector3 front = motor.transform.position + motor.transform.forward + Vector3.up;
            motor.GetComponent<Health>().TakeDamage(new DamageInfo(amount, front));
        }

        // No laboratório os inimigos vêm atrás da Elena parada e a matam: para testar as animações,
        // eles ficam parados (só nesta partida; nada é salvo na cena).
        static void StopEnemies()
        {
            foreach (var soldier in Object.FindObjectsByType<AI.SoldierAI>(FindObjectsSortMode.None)) soldier.enabled = false;
            foreach (var eyes in Object.FindObjectsByType<AI.SoldierPerception>(FindObjectsSortMode.None)) eyes.enabled = false;
            foreach (var lullu in Object.FindObjectsByType<AI.LulluAI>(FindObjectsSortMode.None)) lullu.enabled = false;
            foreach (var agent in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
                if (agent.isOnNavMesh) agent.isStopped = true;
        }

        static void GiveWeapons()
        {
            var inventory = Object.FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return;
            foreach (var (item, count) in new[] { ("PistolaMakarov", 1), ("Municao9mm", 24), ("FacaDeCampo", 1) })
            {
                var definition = Load<ItemDefinition>("Items/" + item);
                if (!inventory.Has(definition, count)) inventory.Add(definition, count);
            }
        }
    }
}
