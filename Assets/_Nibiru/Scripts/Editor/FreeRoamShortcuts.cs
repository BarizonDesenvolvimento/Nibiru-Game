using System.Linq;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: o "Modo livre". Liga o Play com a Elena invencível, com armas, no mundo de quem já
    /// saiu da base da Fundação e com TODAS as áreas vivas ao mesmo tempo (base com soldados e helicópteros,
    /// Poço com o Lullu e o Primeiro Servo, wadi), sem barreira nem muro invisível, para ela andar e testar
    /// qualquer coisa em qualquer ponto. Nada é gravado em slot de verdade. Menu: Nibiru > Testes > Modo livre.
    /// </summary>
    [InitializeOnLoad]
    static class FreeRoamShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Modo livre/";

        // Grupos que normalmente só ligam por um evento do passo; no modo livre ficam todos ligados.
        static readonly string[] AlwaysOn = { "Base_Ativa", "Poco_Ativo", "Wadi_Ativo" };

        // Barreiras que fecham caminho em algum passo (cavaletes do cerco, muros invisíveis, limites do acampamento).
        static readonly string[] Blockers = { "Barreira_Norte", "Parede_Invisivel", "Limites_Acampamento" };

        static readonly (string id, string label, Vector3 spot, float yaw)[] Spots =
        {
            ("livre.aldeia", "1. Modo livre: começar na Aldeia", Step8Shortcuts.NicheSpot, 180f),
            ("livre.acampamento", "2. Modo livre: começar no Acampamento", new Vector3(8f, 0f, -108f), 0f),
            ("livre.base", "3. Modo livre: começar na entrada da base", new Vector3(-26f, 0f, -5f), 90f),
            ("livre.poco", "4. Modo livre: começar na boca do Poço", new Vector3(44f, 0f, 62f), 0f),
            ("livre.wadi", "5. Modo livre: começar no wadi", new Vector3(60f, 0f, 170.2f), 0f),
        };

        static FreeRoamShortcuts()
        {
            foreach (var (id, _, spot, yaw) in Spots)
            {
                Vector3 s = spot;
                float y = yaw;
                Register(id, () => Begin(s, y));
            }
        }

        [MenuItem(MenuRoot + "1. Modo livre: começar na Aldeia", priority = 1)]
        static void Village() => Go("livre.aldeia");

        [MenuItem(MenuRoot + "2. Modo livre: começar no Acampamento", priority = 2)]
        static void Camp() => Go("livre.acampamento");

        [MenuItem(MenuRoot + "3. Modo livre: começar na entrada da base", priority = 3)]
        static void Base() => Go("livre.base");

        [MenuItem(MenuRoot + "4. Modo livre: começar na boca do Poço", priority = 4)]
        static void Well() => Go("livre.poco");

        [MenuItem(MenuRoot + "5. Modo livre: começar no wadi", priority = 5)]
        static void Wadi() => Go("livre.wadi");

        static void Begin(Vector3 spot, float yaw)
        {
            Invincible = true; // o modo livre é sempre invencível
            // Os pés no chão de verdade (o Acampamento fica numa elevação).
            // Pela malha de navegação (um raio acertaria telhados e tetos).
            if (UnityEngine.AI.NavMesh.SamplePosition(new Vector3(spot.x, 2f, spot.z), out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                spot = hit.position;
            Step11Shortcuts.Begin(null, Step9Shortcuts.CrossRoadStep, spot, yaw,
                "Modo livre: Elena invencível, com armas, com todas as áreas vivas e sem barreiras. Para voltar a um ponto, use o menu Nibiru > Testes > Modo livre.",
                thenAlso: () => After(1.2f, OpenWorld));
        }

        /// <summary>Liga as áreas e tira as barreiras (também serve para quem já está no Play).</summary>
        static void OpenWorld()
        {
            int on = 0, off = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid()))
            {
                if (AlwaysOn.Contains(t.name) && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); on++; }
                if (Blockers.Contains(t.name) && t.gameObject.activeSelf) { t.gameObject.SetActive(false); off++; }
            }
            Debug.Log($"Modo livre: {on} áreas ligadas, {off} barreiras tiradas.");
        }
    }
}
