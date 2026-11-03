using System.Linq;
using Nibiru.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Munição e kits médicos da região (tarefas B2a e B2b). Põe no mapa 18 balas e os 3 kits médicos e tira a munição
    /// fixa da arena do Primeiro Servo, para a região ter cerca de 30 balas no total (12 já dadas pelo chefe de
    /// segurança e pelo líder das Trincheiras). Os 4 curativos (2 nas Trincheiras, 2 na arena) não mudam.
    ///
    /// Regras: pelo menos 6 balas garantidas antes da antecâmara; nenhuma bala fixa na arena (a luta tem de poder ser
    /// vencida com faca, tiros e o poder, sem depender de munição); o resto em áreas de exploração, algumas em cantos
    /// opcionais. Kits: um na antecâmara antes do flash, um opcional (tenda médica da base) e um na passagem dos
    /// Guardiões, antes do wadi.
    ///
    /// Menu: Nibiru > Conteúdo > Posicionar munição e kits na cena aberta. Rode depois dos menus da Base, do Poço e da
    /// Aldeia (eles não mexem neste grupo, mas o do Poço refaz a arena sem a munição antiga).
    /// </summary>
    static class SuppliesContent
    {
        public const string RootName = "Conteudo_Suprimentos";

        // Nome, lugar (a altura é só uma dica: o chão é achado por raio), quantidade, e se é kit.
        public readonly struct Spot
        {
            public readonly string name;
            public readonly Vector3 position;
            public readonly int amount;
            public readonly bool kit;
            public readonly float yaw;
            public readonly string note;

            public Spot(string name, Vector3 position, int amount, bool kit, float yaw, string note)
            {
                this.name = name;
                this.position = position;
                this.amount = amount;
                this.kit = kit;
                this.yaw = yaw;
                this.note = note;
            }
        }

        public static readonly Spot[] Spots =
        {
            // Munição: 6 garantidas no patamar do meio da descida (antes da antecâmara), 4 na passagem dos Guardiões,
            // 4 atrás da mesquita da Aldeia e 4 atrás do contêiner norte da base (cantos opcionais).
            // Nenhum item a menos de 1 m de um ponto de retorno: o colisor dele ficaria dentro da Elena ao voltar.
            new Spot("Municao_Poco_Descida", new Vector3(49.5f, -7.2f, 79.6f), 6, false, 20f, "Patamar do meio da descida do Poço (Patamar_2), longe dos pontos de retorno e do alcance do Lullu"),
            new Spot("Municao_Passagem", new Vector3(31.6f, -6.5f, 124f), 4, false, -15f, "Rampa da passagem dos Guardiões, antes do wadi"),
            new Spot("Municao_Aldeia_Mesquita", new Vector3(-64f, 0f, -8.6f), 4, false, 35f, "Canto opcional, atrás da mesquita"),
            new Spot("Municao_Base_Conteiner", new Vector3(-1.5f, 0f, 19.4f), 4, false, -25f, "Canto opcional, atrás do contêiner norte da base"),

            // Kits médicos.
            new Spot("Kit_Antecamara", new Vector3(38.7f, -14f, 82.9f), 1, true, 10f, "Antecâmara, no nicho do canto sudoeste (entre os blocos de canto e a frente da plataforma oeste), fora do caminho mas à vista de quem entra, antes do flash e da luta (fora do grupo da luta)"),
            new Spot("Kit_Tenda_Medica", new Vector3(-12.6f, 0f, 18.8f), 1, true, 40f, "Opcional: canto dos fundos da tenda médica da base"),
            new Spot("Kit_Passagem", new Vector3(32.5f, -3f, 132f), 1, true, -10f, "Perto do fim da passagem dos Guardiões, antes da seção final (wadi)"),
        };

        [MenuItem("Nibiru/Conteúdo/Posicionar munição e kits na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de posicionar munição e kits.");
                return;
            }

            IdPrefix = "suprimentos";
            var ammo = Load<ItemDefinition>("Items/Municao9mm");
            var kit = Load<ItemDefinition>("Items/KitMedico");
            if (ammo == null || kit == null)
            {
                Debug.LogWarning("Faltam os itens Municao9mm ou KitMedico em Data/Items.");
                return;
            }

            RemoveArenaAmmo();

            Transform root = NewRoot(RootName, "Posicionar munição e kits");
            foreach (Spot s in Spots)
            {
                Vector3 ground = GroundAt(s.position, out bool hit);
                if (!hit) Debug.LogWarning($"{s.name}: não achei chão perto de {s.position}; ficou na altura da dica.");
                else if (Mathf.Abs(ground.y - s.position.y) > 1.5f) Debug.LogWarning($"{s.name}: o chão achado (y = {ground.y:F1}) está longe da dica (y = {s.position.y:F1}); confira.");

                Vector3 size = s.kit ? new Vector3(0.34f, 0.16f, 0.22f) : new Vector3(0.16f, 0.1f, 0.1f);
                GameObject go = Prim(PrimitiveType.Cube, root, s.name, ground + Vector3.up * (size.y * 0.5f + 0.02f), size,
                    s.kit ? "BO_BrancoFundacao" : "BO_TurquesaFundacao", s.yaw);
                Pickup(go, s.kit ? kit : ammo, s.amount, null);

                var onNav = NavMesh.SamplePosition(ground, out NavMeshHit nav, 1f, NavMesh.AllAreas);
                Debug.Log($"{s.name}: {(s.kit ? "kit médico" : s.amount + " balas")} em ({ground.x:F1}, {ground.y:F1}, {ground.z:F1}) - {s.note}"
                          + (onNav ? "" : " [AVISO: fora da malha de navegação]"));
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log("Munição e kits posicionados. Salve a cena.");
        }

        // A munição que a arena do Primeiro Servo tinha antes da B2a (o script do Servo já não a cria).
        static void RemoveArenaAmmo()
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<ItemPickup>().ToArray())
            {
                if (t == null || !t.gameObject.scene.IsValid() || t.gameObject.name != "Municao_Plataforma_Oeste") continue;
                Undo.DestroyObjectImmediate(t.gameObject);
                Debug.Log("Munição fixa da arena do Primeiro Servo removida.");
            }
        }

        // Primeiro chão sólido embaixo da dica de altura, sem contar gatilhos.
        static Vector3 GroundAt(Vector3 hint, out bool hit)
        {
            Vector3 start = new Vector3(hint.x, hint.y + 2.5f, hint.z);
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 9f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                hit = false;
                return hint;
            }
            hit = true;
            return hits.OrderBy(h => h.distance).First().point;
        }
    }
}
