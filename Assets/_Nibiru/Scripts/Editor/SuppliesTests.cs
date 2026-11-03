using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.Inventory;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Teste de contagem da munição, dos curativos e dos kits médicos da região (tarefas B2a e B2b). Não liga o Play e
    /// não mexe em salvamento: só lê a cena aberta (Regiao1_SitioDeUr). Resumo no Console.
    /// Menu: Nibiru > Testes > Suprimentos > Teste de contagem (resumo no Console).
    /// </summary>
    static class SuppliesTests
    {
        // Metas da B2 (decididas em 3/nov/2026).
        const int TotalAmmo = 30;
        const int GuaranteedBeforeHall = 6;
        const int Bandages = 4;
        const int Kits = 3;

        struct Found
        {
            public ItemPickup pickup;
            public string item;
            public int amount;
            public Vector3 pos;
            public bool inArena;
        }

        [MenuItem("Nibiru/Testes/Suprimentos/Teste de contagem (resumo no Console)")]
        static void Run()
        {
            var report = new StringBuilder();
            int failed = 0;
            void Result(bool ok, string name, string detail)
            {
                if (!ok) failed++;
                report.AppendLine($"{(ok ? "OK" : "FALHOU")}  {name}: {detail}");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "Regiao1_SitioDeUr")
            {
                Debug.LogWarning("Suprimentos, teste de contagem: abra a cena Regiao1_SitioDeUr antes.");
                return;
            }

            var all = new List<Found>();
            foreach (ItemPickup p in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(p);
                var item = so.FindProperty("item").objectReferenceValue;
                all.Add(new Found
                {
                    pickup = p,
                    item = item != null ? item.name : "(vazio)",
                    amount = so.FindProperty("amount").intValue,
                    pos = p.transform.position,
                    inArena = p.transform.GetComponentsInParent<Transform>(true).Any(t => t.name == "Primeiro_Servo"),
                });
            }

            var ammo = all.Where(f => f.item == "Municao9mm").ToList();
            var bandages = all.Where(f => f.item == "Curativo").ToList();
            var kits = all.Where(f => f.item == "KitMedico").ToList();

            int ammoTotal = ammo.Sum(f => f.amount);
            Result(ammoTotal == TotalAmmo, "Munição no mapa", $"{ammoTotal} balas em {ammo.Count} lugares (meta {TotalAmmo})");

            var arenaAmmo = ammo.Where(f => f.inArena).ToList();
            Result(arenaAmmo.Count == 0, "Nenhuma munição fixa na arena do Primeiro Servo", arenaAmmo.Count == 0 ? "a luta não depende de bala" : "há munição em " + string.Join(", ", arenaAmmo.Select(f => f.pickup.name)));

            // Antes da antecâmara: no Poço, descida e fundo, antes da boca da antecâmara (z < 80), fora da luta.
            int beforeHall = ammo.Where(f => !f.inArena && f.pos.x > 35f && f.pos.z >= 55f && f.pos.z < 80f && f.pos.y < -5f).Sum(f => f.amount);
            Result(beforeHall >= GuaranteedBeforeHall, "Munição garantida antes da antecâmara", $"{beforeHall} balas (mínimo {GuaranteedBeforeHall})");

            int bandageTotal = bandages.Sum(f => f.amount);
            int bandagesArena = bandages.Count(f => f.inArena);
            Result(bandageTotal == Bandages && bandagesArena == 2 && bandages.Count - bandagesArena == 2,
                "Curativos não mudaram", $"{bandageTotal} curativos ({bandagesArena} na arena, {bandages.Count - bandagesArena} nas Trincheiras)");

            Result(kits.Sum(f => f.amount) == Kits && kits.Count == Kits, "Kits médicos", $"{kits.Sum(f => f.amount)} em {kits.Count} lugares (meta {Kits})");

            // Um kit em cada momento: antes da luta (antecâmara), opcional (tenda médica) e antes do wadi (passagem).
            bool hall = kits.Any(f => !f.inArena && f.pos.x > 36f && f.pos.x < 54f && f.pos.z >= 80f && f.pos.z < 89.5f && f.pos.y < -5f);
            bool optional = kits.Any(f => f.pos.x > -14f && f.pos.x < -6f && f.pos.z > 14f && f.pos.z < 20f);
            bool beforeWadi = kits.Any(f => f.pos.x < 36f && f.pos.z > 110f && f.pos.z < 141f);
            Result(hall && optional && beforeWadi, "Kits: antecâmara, opcional e antes do wadi", $"antecâmara={hall}, tenda médica (opcional)={optional}, passagem antes do wadi={beforeWadi}");
            Result(!kits.Any(f => f.inArena), "Nenhum kit dentro do grupo da luta", "a luta pode ser feita sem eles");

            // Chão e malha de navegação, e códigos estáveis.
            var added = all.Where(f => f.pickup.transform.GetComponentsInParent<Transform>(true).Any(t => t.name == "Conteudo_Suprimentos")).ToList();
            var offNav = added.Where(f => !NavMesh.SamplePosition(f.pos, out _, 1f, NavMesh.AllAreas)).Select(f => f.pickup.name).ToList();
            Result(added.Count == 7 && offNav.Count == 0, "Os 7 itens novos existem e estão a menos de 1 m da malha", offNav.Count == 0 ? $"{added.Count} itens" : "fora da malha: " + string.Join(", ", offNav));

            // Um item em cima de um ponto de retorno ficaria dentro da Elena quando ela volta dele.
            var spawns = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c =>
            {
                var point = new SerializedObject(c).FindProperty("spawnPoint").objectReferenceValue as Transform;
                return (id: c.GetComponent<PersistentId>().Id, pos: (point != null ? point : c.transform).position);
            }).ToList();
            var onSpawn = all.SelectMany(f => spawns.Where(s => Vector3.Distance(s.pos, f.pos) < 1f).Select(s => $"{f.pickup.name} ({s.id})")).ToList();
            Result(onSpawn.Count == 0, "Nenhum item a menos de 1 m de um ponto de retorno", onSpawn.Count == 0 ? $"{spawns.Count} pontos conferidos" : string.Join(", ", onSpawn));

            var ids = all.Select(f => f.pickup.GetComponent<PersistentId>()?.Id).ToList();
            var dupes = ids.Where(i => !string.IsNullOrEmpty(i)).GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Result(ids.All(i => !string.IsNullOrEmpty(i)) && dupes.Count == 0, "Códigos estáveis de todos os itens do mapa", dupes.Count == 0 ? $"{ids.Count} códigos, sem repetição" : "repetidos: " + string.Join(", ", dupes));

            Debug.Log($"Suprimentos, teste de contagem ({(failed == 0 ? "tudo OK" : failed + " FALHOU")}):\n" + report);
        }
    }
}
