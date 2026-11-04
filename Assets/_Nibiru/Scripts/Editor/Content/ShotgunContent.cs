using System.Linq;
using Nibiru.Combat;
using Nibiru.EditorTools.Audio;
using Nibiru.Inventory;
using Nibiru.Player;
using Nibiru.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// B6, a espingarda: os dados da arma (números aprovados em Docs/Plano-B6-Espingarda.md), a casa trancada da
    /// Aldeia com a espingarda e 4 cartuchos (os outros 2 vêm no cano: 6 na região inteira), a chave que a Umm Karim
    /// dá quando o Yusuf recebe o remédio e o modelo da espingarda na mão da Elena.
    /// Menu: Nibiru > Conteúdo > Povoar a espingarda (B6) na cena aberta.
    ///
    /// Rode depois dos menus da Aldeia e do Remédio para Yusuf (precisa do evento EntregouAntibioticos). A casa
    /// trancada fica no lugar do bloco Casa_8 (ele só perde o desenho e o colisor; rodar de novo refaz tudo).
    /// </summary>
    static class ShotgunContent
    {
        const string RootName = "Conteudo_CasaTrancada";
        const string ElenaPrefab = "Assets/_Nibiru/Prefabs/Player/Elena.prefab";
        const string ShotgunPrefab = "Assets/_Nibiru/Prefabs/Objetos/Espingarda.prefab";
        const string AmmoBoxPrefab = "Assets/_Nibiru/Prefabs/Objetos/CaixaMunicao.prefab";
        const string HouseBlock = "Casa_8";

        // Centro da casa (o bloco Casa_8 tem 6 x 6 m) e a porta, no lado leste, aberta para a rua.
        static readonly Vector3 HouseCenter = new Vector3(-46f, 0f, -12f);

        [MenuItem("Nibiru/Conteúdo/Povoar a espingarda (B6) na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar a espingarda.");
                return;
            }

            IdPrefix = "espingarda";
            ConfigureData();
            SetupElenaPrefab();

            var shotgun = Load<ItemDefinition>("Items/Espingarda");
            var cartridge = Load<ItemDefinition>("Items/Cartucho");
            var delivered = Load<StoryEvent>("Events/EntregouAntibioticos");
            if (shotgun == null || cartridge == null || delivered == null)
            {
                Debug.LogError("Espingarda: faltam a espingarda, o cartucho ou o evento EntregouAntibioticos. Rode antes os menus da Aldeia e do Remédio para Yusuf.");
                return;
            }

            var key = Asset<ItemDefinition>("Items/ChaveDaCasa", i =>
            {
                i.id = "chave_da_casa";
                i.category = ItemCategory.Chave;
            });
            StoryEvent opened = Event("AbriuACasaTrancada", "A Elena abriu a casa trancada da Aldeia com a chave da Umm Karim (a espingarda e os cartuchos estão lá dentro).");

            Transform root = NewRoot(RootName, "Povoar a espingarda");
            HideHouseBlock();
            BuildHouse(root, key, opened, shotgun, cartridge);

            // A Umm Karim dá a chave quando o Yusuf recebe o remédio (não repete ao voltar de um checkpoint).
            var giver = root.gameObject.AddComponent<ItemGiver>();
            SetField(giver, "item", key);
            Listener(root.gameObject, delivered, e => Call(e, giver.Give));
            SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log("Espingarda (B6) povoada: casa trancada em Casa_8, 1 espingarda e 4 cartuchos (mais 2 no cano), chave dada pela Umm Karim. Salve a cena.");
        }

        // ---------- Dados ----------

        /// <summary>Os números aprovados da espingarda (Docs/Plano-B6-Espingarda.md, decisões do dono de 4/nov/2026).</summary>
        public static void ConfigureData()
        {
            ProvisionalSounds.Generate(overwrite: false);
            var gun = Load<FirearmDefinition>("Combat/Espingarda");
            if (gun == null)
            {
                Debug.LogError("Falta Data/Combat/Espingarda.");
                return;
            }
            gun.magazineSize = 2;
            gun.pellets = 8;
            gun.damagePerPellet = 16f;
            gun.spread = 5f;
            gun.range = 14f;
            gun.fireInterval = 0.4f;
            gun.reloadTime = 2.2f; // só vale sem recarga cartucho a cartucho
            gun.recoil = 7f;
            gun.noiseRadius = 45f;
            gun.slot = 2;
            gun.holdStyle = HoldStyle.Longa;
            gun.swapTime = 0.4f;
            gun.falloffStart = 5f;
            gun.minDamageFactor = 0.35f;
            gun.reloadPerRound = true;
            gun.reloadOpenTime = 0.5f;
            gun.reloadRoundTime = 0.8f;
            gun.staggerRange = 6f;
            gun.staggerTime = 0.8f;
            gun.knockdownRange = 3f;
            gun.knockdownPellets = 6;
            gun.knockdownTime = 1.5f;
            gun.fireSound = AssetDatabase.LoadAssetAtPath<AudioClip>(ProvisionalSounds.ShotgunShot);
            EditorUtility.SetDirty(gun);

            var pistol = Load<FirearmDefinition>("Combat/Pistola");
            if (pistol != null)
            {
                pistol.slot = 1;
                pistol.swapTime = 0.4f;
                EditorUtility.SetDirty(pistol);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- Elena ----------

        /// <summary>Um modelo por arma na mão da Elena (a pistola que já havia e a espingarda), ligados ao PlayerWeapons.</summary>
        public static void SetupElenaPrefab()
        {
            var shotgunArt = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunPrefab);
            var pistol = Load<FirearmDefinition>("Combat/Pistola");
            var shotgun = Load<FirearmDefinition>("Combat/Espingarda");
            if (shotgunArt == null || pistol == null || shotgun == null)
            {
                Debug.LogError("Espingarda: faltam o prefab da arte ou os dados das armas.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(ElenaPrefab);
            try
            {
                Transform hand = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ArmaNaMao");
                var weapons = root.GetComponentInChildren<PlayerWeapons>(true);
                if (hand == null || weapons == null)
                {
                    Debug.LogError("Espingarda: o prefab da Elena não tem ArmaNaMao ou PlayerWeapons.");
                    return;
                }

                // Pistola: o que já estava na mão (corpo do blockout, arte e cano) vai para um grupo só.
                Transform pistolGroup = hand.Find("Modelo_Pistola");
                if (pistolGroup == null)
                {
                    pistolGroup = new GameObject("Modelo_Pistola").transform;
                    pistolGroup.SetParent(hand, false);
                    foreach (Transform child in hand.Cast<Transform>().Where(c => c != pistolGroup && c.name != "Modelo_Espingarda").ToArray())
                        child.SetParent(pistolGroup, false);
                }
                Transform pistolMuzzle = pistolGroup.Find("Cano");
                // Pontos de encaixe da pistola: o cabo cai onde a palma já ficava (medido no clipe de mira) e a mão esquerda apoia do lado
                // esquerdo (a pose de mira já é de duas mãos). Só são criados se faltarem: o dono ajusta a olho com a câmera livre.
                Transform pistolRight = EnsureGrip(pistolGroup, "Grip_Direita", new Vector3(-0.008f, 0.013f, -0.03f));
                Transform pistolLeft = EnsureGrip(pistolGroup, "Grip_Esquerda", new Vector3(-0.045f, 0.0f, -0.02f));

                // Espingarda: o modelo do Tripo (a ponta do cano é a origem do prefab) e um cano para o rastro do tiro.
                // Espingarda: o modelo do Tripo (a ponta do cano é a origem do prefab) e um cano para o rastro do tiro. Se o grupo já existe
                // (rodar o menu de novo), fica como está, com os pontos de encaixe que o dono ajustou.
                Transform shotgunGroup = hand.Find("Modelo_Espingarda");
                Transform shotgunMuzzle;
                if (shotgunGroup == null)
                {
                    shotgunGroup = new GameObject("Modelo_Espingarda").transform;
                    shotgunGroup.SetParent(hand, false);
                    var art = (GameObject)PrefabUtility.InstantiatePrefab(shotgunArt, shotgunGroup);
                    art.name = "Arte";
                    art.transform.localPosition = new Vector3(0f, 0.07f, 0.72f);
                    art.transform.localRotation = Quaternion.identity;
                    shotgunMuzzle = new GameObject("Cano").transform;
                    shotgunMuzzle.SetParent(shotgunGroup, false);
                    shotgunMuzzle.localPosition = new Vector3(0f, 0.07f, 0.73f);
                    shotgunGroup.gameObject.SetActive(false);
                }
                else shotgunMuzzle = shotgunGroup.Find("Cano");
                // Cabo (a empunhadura atrás do gatilho) e guarda-mão da espingarda, a partir das medidas do modelo (1,15 m, ponta na origem).
                Transform shotgunRight = EnsureGrip(shotgunGroup, "Grip_Direita", new Vector3(0f, 0.0f, -0.06f));
                Transform shotgunLeft = EnsureGrip(shotgunGroup, "Grip_Esquerda", new Vector3(0f, 0.015f, 0.32f));
                shotgunGroup.gameObject.SetActive(false);

                var serialized = new SerializedObject(weapons);
                SerializedProperty list = serialized.FindProperty("models");
                list.arraySize = 2;
                Fill(list.GetArrayElementAtIndex(0), pistol, pistolGroup.gameObject, pistolMuzzle, pistolRight, pistolLeft);
                Fill(list.GetArrayElementAtIndex(1), shotgun, shotgunGroup.gameObject, shotgunMuzzle, shotgunRight, shotgunLeft);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // A arma vai ao cabo, que cai na palma da mão direita (medida no clipe de mira de pistola).
                var animatorScript = root.GetComponentInChildren<PlayerAnimator>(true);
                if (animatorScript != null)
                {
                    var animatorSerialized = new SerializedObject(animatorScript);
                    animatorSerialized.FindProperty("heldGunOffset").vector3Value = new Vector3(-0.008f, 0.033f, 0.05f);
                    animatorSerialized.ApplyModifiedPropertiesWithoutUndo();
                    // O componente HandGripIK (IK do Animator) foi trocado pelo cálculo no PlayerAnimator; sai do prefab.
                    Animator modelAnimator = animatorScript.GetComponentInChildren<Animator>(true);
                    if (modelAnimator != null) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(modelAnimator.gameObject);
                }

                PrefabUtility.SaveAsPrefabAsset(root, ElenaPrefab);
                Debug.Log("Espingarda: modelos da pistola e da espingarda ligados no prefab da Elena.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Cria o ponto de encaixe se ainda não existir (os valores que o dono ajustar ficam).
        static Transform EnsureGrip(Transform group, string name, Vector3 localPosition)
        {
            Transform grip = group.Find(name);
            if (grip != null) return grip;
            grip = new GameObject(name).transform;
            grip.SetParent(group, false);
            grip.localPosition = localPosition;
            return grip;
        }

        static void Fill(SerializedProperty element, FirearmDefinition firearm, GameObject model, Transform muzzle, Transform rightGrip, Transform leftGrip)
        {
            element.FindPropertyRelative("rightGrip").objectReferenceValue = rightGrip;
            element.FindPropertyRelative("leftGrip").objectReferenceValue = leftGrip;
            element.FindPropertyRelative("firearm").objectReferenceValue = firearm;
            element.FindPropertyRelative("model").objectReferenceValue = model;
            element.FindPropertyRelative("muzzle").objectReferenceValue = muzzle;
        }

        // ---------- Cena ----------

        // O bloco Casa_8 e a placa da porta do blockout (Porta_8, na mesma parede leste) saem; a casa nova toma o lugar.
        static void HideHouseBlock()
        {
            foreach (string name in new[] { HouseBlock, "Porta_8" })
            {
                GameObject block = GameObject.Find(name);
                if (block == null)
                {
                    Debug.LogWarning($"Espingarda: não achei {name}. Rode o menu da Aldeia.");
                    continue;
                }
                var renderer = block.GetComponent<Renderer>();
                var collider = block.GetComponent<Collider>();
                if (renderer != null) renderer.enabled = false;
                if (collider != null) collider.enabled = false;
            }
        }

        static void BuildHouse(Transform root, ItemDefinition key, StoryEvent opened, ItemDefinition shotgun, ItemDefinition cartridge)
        {
            Transform house = Group(root, "Casa_Trancada");
            Vector3 c = HouseCenter;
            const float h = 3.8f;
            const string wall = "BO_TijoloBarro";

            Prim(PrimitiveType.Cube, house, "Piso", c + new Vector3(0f, 0.05f, 0f), new Vector3(5.4f, 0.1f, 5.4f), "BO_Terra", collider: ColliderMode.None);
            Prim(PrimitiveType.Cube, house, "Parede_Oeste", c + new Vector3(-2.85f, h / 2f, 0f), new Vector3(0.3f, h, 6f), wall);
            Prim(PrimitiveType.Cube, house, "Parede_Norte", c + new Vector3(0f, h / 2f, 2.85f), new Vector3(6f, h, 0.3f), wall);
            Prim(PrimitiveType.Cube, house, "Parede_Sul", c + new Vector3(0f, h / 2f, -2.85f), new Vector3(6f, h, 0.3f), wall);
            Prim(PrimitiveType.Cube, house, "Parede_Leste_Norte", c + new Vector3(2.85f, h / 2f, 2.0f), new Vector3(0.3f, h, 2.0f), wall);
            Prim(PrimitiveType.Cube, house, "Parede_Leste_Sul", c + new Vector3(2.85f, h / 2f, -2.0f), new Vector3(0.3f, h, 2.0f), wall);
            // Vão de 2 m de largura por 2,3 m de altura: a cápsula da Elena tem 1 m de largura e 2 m de altura (o pé fica 5 cm acima do chão) e precisa de folga.
            Prim(PrimitiveType.Cube, house, "Verga", c + new Vector3(2.85f, 3.05f, 0f), new Vector3(0.3f, 1.5f, 2.0f), wall);
            Prim(PrimitiveType.Cube, house, "Teto", c + new Vector3(0f, 3.65f, 0f), new Vector3(6f, 0.3f, 6f), "BO_TijoloBarroEscuro");

            // A porta trancada: um colisor sólido que some quando a chave é usada.
            GameObject door = Prim(PrimitiveType.Cube, house, "Porta_Trancada", c + new Vector3(2.85f, 1.15f, 0f), new Vector3(0.15f, 2.3f, 2.0f), "BO_Madeira");
            var locked = door.AddComponent<LockedDoor>();
            SetField(locked, "key", key);
            SetField(locked, "raiseOnOpen", opened);
            Listener(door, opened, e => Call(e, locked.Open));
            StableId(door);

            // Luz quente por dentro (a casa não tem janelas).
            var lightObject = new GameObject("Luz_Interna");
            lightObject.transform.SetParent(house, false);
            lightObject.transform.position = c + new Vector3(0f, 2.8f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.82f, 0.55f);

            // Mesa junto da parede oeste, com a espingarda e a caixa de cartuchos.
            Prim(PrimitiveType.Cube, house, "Mesa", c + new Vector3(-2.2f, 0.45f, 0f), new Vector3(0.8f, 0.9f, 2f), "BO_Madeira");

            var gun = new GameObject("Espingarda_Casa");
            gun.transform.SetParent(house, false);
            gun.transform.position = c + new Vector3(-2.2f, 0.99f, -0.4f);
            var box = gun.AddComponent<BoxCollider>();
            box.size = new Vector3(0.3f, 0.2f, 1.2f);
            var shotgunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunPrefab);
            if (shotgunPrefab != null)
            {
                var art = (GameObject)PrefabUtility.InstantiatePrefab(shotgunPrefab, gun.scene);
                art.name = "Arte";
                art.transform.SetParent(gun.transform, false);
                art.transform.localPosition = new Vector3(0f, 0f, 0.57f);
                art.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Pickup(gun, shotgun, 1, null);

            GameObject shells = Prim(PrimitiveType.Cube, house, "Municao_Cartuchos_Casa", c + new Vector3(-2.2f, 0.97f, 0.65f),
                new Vector3(0.16f, 0.1f, 0.1f), "BO_TurquesaFundacao", 20f);
            Pickup(shells, cartridge, 4, null);
            UseModel(shells, AmmoBoxPrefab, 0.9f);
            Debug.Log("Casa trancada: porta no lado leste de Casa_8; a espingarda (2 no cano) e 4 cartuchos na mesa; 6 cartuchos na região inteira.");
        }
    }
}
