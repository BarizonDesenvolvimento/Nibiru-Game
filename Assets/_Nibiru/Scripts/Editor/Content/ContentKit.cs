using System.Linq;
using System;
using System.Reflection;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Content
{
    /// <summary>
    /// Ferramentas comuns dos scripts que povoam as áreas (Acampamento, Trincheiras...):
    /// peças da cena, gatilhos, itens, soldados, helicóptero, eventos do Inspector e arquivos
    /// de dados. Cada área usa com "using static ContentKit" dentro do namespace.
    /// </summary>
    static class ContentKit
    {
        public const string DataFolder = "Assets/_Nibiru/Data";
        const string MaterialFolder = "Assets/_Nibiru/Art/Materials/Blockout";
        const string GlassMaterial = "BO_Vidro";

        /// <summary>Começo dos códigos fixos (PersistentId) da área que está sendo montada.</summary>
        public static string IdPrefix = "conteudo";

        // ---------- Grupo da área ----------

        /// <summary>Apaga o grupo antigo da área (se houver) e cria um novo, vazio.</summary>
        public static Transform NewRoot(string name, string undoLabel)
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(name).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, undoLabel);
            return root;
        }

        // ---------- Peças da cena ----------

        public enum ColliderMode { Solid, Trigger, None }

        public static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        public static Transform Point(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = position;
            return t;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, string material, float yaw = 0f, ColliderMode collider = ColliderMode.Solid)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            ApplyCollider(go, collider);
            return go;
        }

        public static GameObject Local(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 scale, string material, ColliderMode collider = ColliderMode.None)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            ApplyCollider(go, collider);
            return go;
        }

        static void ApplyCollider(GameObject go, ColliderMode mode)
        {
            var collider = go.GetComponent<Collider>();
            if (mode == ColliderMode.None) Object.DestroyImmediate(collider);
            else collider.isTrigger = mode == ColliderMode.Trigger;
        }

        /// <summary>Corpo deitado (cápsula tombada), sem colisão.</summary>
        public static GameObject Body(Transform parent, string name, Vector3 groundPosition, float yaw, string material)
        {
            GameObject body = Prim(PrimitiveType.Capsule, parent, name, groundPosition + Vector3.up * 0.25f, new Vector3(0.5f, 0.9f, 0.5f), material, collider: ColliderMode.None);
            body.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            return body;
        }

        // Caixa invisível marcada como gatilho.
        public static GameObject Zone(Transform parent, string name, Vector3 center, Vector3 size, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        /// <summary>Área que dispara um evento da história quando a Elena entra.</summary>
        public static GameObject EventZone(Transform parent, string name, Vector3 center, Vector3 size, StoryEvent storyEvent)
        {
            GameObject zone = Zone(parent, name, center, size);
            SetField(zone.AddComponent<StoryEventTrigger>(), "storyEvent", storyEvent);
            return zone;
        }

        public static void Pickup(GameObject go, ItemDefinition item, int amount, StoryEvent raise)
        {
            var pickup = go.AddComponent<ItemPickup>();
            SetField(pickup, "item", item);
            SetField(pickup, "amount", amount);
            SetField(pickup, "raiseOnPickup", raise);
            StableId(go);
        }

        public static void Checkpoint(Transform parent, string name, Vector3 center, Vector3 size, Vector3 spawn, float spawnYaw)
        {
            GameObject zone = Zone(parent, name, center, size);
            Transform spawnPoint = Point(zone.transform, "Retorno", spawn);
            spawnPoint.rotation = Quaternion.Euler(0f, spawnYaw, 0f);
            var checkpoint = zone.AddComponent<Save.Checkpoint>();
            SetField(checkpoint, "spawnPoint", spawnPoint);
            StableId(zone);
        }

        // Código fixo a partir do nome, para continuar o mesmo quando o menu reconstrói a cena.
        public static void StableId(GameObject go)
        {
            var id = go.GetComponent<PersistentId>();
            if (id == null) id = go.AddComponent<PersistentId>();
            SetField(id, "id", IdPrefix + "." + go.name.ToLowerInvariant());
        }

        public static DialogueStarter DialogueStarterOn(GameObject go, DialogueDefinition dialogue, string prompt, bool interactable)
        {
            var starter = go.AddComponent<DialogueStarter>();
            SetField(starter, "dialogue", dialogue);
            SetField(starter, "prompt", prompt);
            SetField(starter, "interactable", interactable);
            SetField(starter, "onlyOnce", true);
            return starter;
        }

        // Conversa sem objeto para interagir, começada por uma sequência ou um evento.
        public static DialogueStarter DialogueTrigger(Transform parent, string name, DialogueDefinition dialogue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return DialogueStarterOn(go, dialogue, "", interactable: false);
        }

        public static void Audio(GameObject go, string clipPath, bool loop, bool playOnAwake, float volume, float minDistance, float maxDistance)
        {
            var source = go.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0.3f;
        }

        public static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        // ---------- Helicóptero e soldados ----------

        /// <summary>Helicóptero da Fundação em formas simples: branco com faixa turquesa, hélices girando e som.</summary>
        public static GameObject Helicopter(Transform parent, string name, Vector3 position, float yaw)
        {
            var heli = new GameObject(name);
            heli.transform.SetParent(parent, false);
            heli.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Transform h = heli.transform;

            // Cabine: a parte de trás fechada, a da frente com piso e vidro (o piloto aparece lá dentro).
            Local(PrimitiveType.Cube, h, "Cabine", new Vector3(0f, 1.6f, -0.6f), new Vector3(2.4f, 2.2f, 3f), "BO_BrancoFundacao", ColliderMode.Solid);
            Local(PrimitiveType.Cube, h, "Piso_Cabine", new Vector3(0f, 0.85f, 1.9f), new Vector3(2.4f, 0.7f, 2f), "BO_BrancoFundacao", ColliderMode.Solid);
            GameObject glass = Local(PrimitiveType.Cube, h, "Vidro", new Vector3(0f, 1.95f, 1.9f), new Vector3(2.3f, 1.5f, 2f), "BO_BrancoFundacao", ColliderMode.Solid);
            glass.GetComponent<Renderer>().sharedMaterial = Glass();
            glass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Local(PrimitiveType.Cube, h, "Faixa", new Vector3(0f, 1f, 0.4f), new Vector3(2.45f, 0.3f, 5.05f), "BO_TurquesaFundacao");
            Local(PrimitiveType.Cube, h, "Assento", new Vector3(0.5f, 1.35f, 1.7f), new Vector3(0.55f, 0.3f, 0.5f), "BO_VerdeOliva");
            Local(PrimitiveType.Cube, h, "Encosto", new Vector3(0.5f, 1.8f, 1.42f), new Vector3(0.55f, 0.8f, 0.12f), "BO_VerdeOliva");

            // O piloto, sentado no banco da direita.
            var pilot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EditorTools.SoldiersSetup.PilotPrefab), h);
            pilot.name = "Piloto";
            pilot.transform.SetLocalPositionAndRotation(new Vector3(0.5f, 1.5f, 1.68f), Quaternion.identity);
            Local(PrimitiveType.Cube, h, "Cauda", new Vector3(0f, 2.1f, -4.3f), new Vector3(0.5f, 0.6f, 5f), "BO_BrancoFundacao");
            Local(PrimitiveType.Cube, h, "Leme", new Vector3(0f, 2.8f, -6.6f), new Vector3(0.15f, 1.4f, 0.8f), "BO_TurquesaFundacao");
            Local(PrimitiveType.Cube, h, "Esqui_E", new Vector3(-1.1f, 0.08f, 0.4f), new Vector3(0.15f, 0.15f, 4.6f), "BO_Metal");
            Local(PrimitiveType.Cube, h, "Esqui_D", new Vector3(1.1f, 0.08f, 0.4f), new Vector3(0.15f, 0.15f, 4.6f), "BO_Metal");

            var rotor = new GameObject("Rotor");
            rotor.transform.SetParent(h, false);
            rotor.transform.localPosition = new Vector3(0f, 2.85f, 0.4f);
            Local(PrimitiveType.Cube, rotor.transform, "Pa_1", Vector3.zero, new Vector3(11f, 0.05f, 0.35f), "BO_Metal");
            Local(PrimitiveType.Cube, rotor.transform, "Pa_2", Vector3.zero, new Vector3(0.35f, 0.05f, 11f), "BO_Metal");
            SetField(rotor.AddComponent<Spinner>(), "speed", 900f);

            var tailRotor = new GameObject("Rotor_Cauda");
            tailRotor.transform.SetParent(h, false);
            tailRotor.transform.localPosition = new Vector3(0.3f, 2.4f, -6.6f);
            Local(PrimitiveType.Cube, tailRotor.transform, "Pa", Vector3.zero, new Vector3(0.05f, 1.6f, 0.2f), "BO_Metal");
            var tailSpin = tailRotor.AddComponent<Spinner>();
            SetField(tailSpin, "axis", Vector3.right);
            SetField(tailSpin, "speed", 1400f);

            Audio(heli, EditorTools.Audio.ProvisionalSounds.Helicopter, loop: true, playOnAwake: true, volume: 1f, minDistance: 20f, maxDistance: 500f);
            return heli;
        }

        // Vidro azulado e transparente da cabine (criado se ainda não existir).
        static Material Glass()
        {
            Material glass = Mat(GlassMaterial);
            if (glass != null) return glass;
            glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glass.SetColor("_BaseColor", new Color(0.55f, 0.7f, 0.75f, 0.3f));
            glass.SetFloat("_Surface", 1f);
            glass.SetFloat("_Blend", 0f);
            glass.SetFloat("_Smoothness", 0.9f);
            glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0f);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(glass, $"{MaterialFolder}/{GlassMaterial}.mat");
            return glass;
        }

        /// <summary>Qual soldado da Fundação: o Soldado é o das patrulhas; o Líder comanda; o Atirador fica no posto dele.</summary>
        public enum SoldierModel { Soldado, Lider, Atirador }

        /// <summary>
        /// Soldado do prefab, de guarda ou com patrulha. Nasce como figurante (sem IA, sem
        /// percepção e sem malha de navegação): um evento liga tudo com <see cref="WakeSoldiers"/>.
        /// Sem modelo dado, o líder usa o do Líder de esquadra e os outros, o do Soldado.
        /// </summary>
        public static GameObject Soldier(Transform parent, string name, Vector3 position, Vector3 lookAt, Transform[] patrol, bool leader, SoldierModel? model = null)
        {
            string path = (model ?? (leader ? SoldierModel.Lider : SoldierModel.Soldado)) switch
            {
                SoldierModel.Lider => EditorTools.SoldiersSetup.LeaderPrefab,
                SoldierModel.Atirador => EditorTools.SoldiersSetup.SniperPrefab,
                _ => EditorTools.SoldiersSetup.SoldierPrefab,
            };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            Vector3 flat = Vector3.ProjectOnPlane(lookAt - position, Vector3.up);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(flat));

            var settings = AssetDatabase.LoadAssetAtPath<SoldierSettings>($"{DataFolder}/AI/{(leader ? "LiderEsquadrao" : "SoldadoContencao")}.asset");
            var ai = go.GetComponent<SoldierAI>();
            var aiSo = new SerializedObject(ai);
            aiSo.FindProperty("settings").objectReferenceValue = settings;
            aiSo.FindProperty("isLeader").boolValue = leader;
            SerializedProperty waypoints = aiSo.FindProperty("waypoints");
            waypoints.arraySize = patrol?.Length ?? 0;
            for (int i = 0; i < waypoints.arraySize; i++) waypoints.GetArrayElementAtIndex(i).objectReferenceValue = patrol[i];
            aiSo.ApplyModifiedProperties();

            var perception = go.GetComponent<SoldierPerception>();
            var perceptionSo = new SerializedObject(perception);
            perceptionSo.FindProperty("settings").objectReferenceValue = settings;
            perceptionSo.ApplyModifiedProperties();

            ai.enabled = false;
            perception.enabled = false;
            // Sem o agente ligado, ninguém levanta o soldado até a altura certa: põe à mão.
            var agent = go.GetComponent<NavMeshAgent>();
            go.transform.position = position + Vector3.up * agent.baseOffset;
            agent.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
            PrefabUtility.RecordPrefabInstancePropertyModifications(perception);
            PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
            return go;
        }

        /// <summary>Liga a IA de todos os soldados do grupo (malha de navegação, percepção e IA).</summary>
        public static void WakeSoldiers(UnityEvent e, Transform soldiers)
        {
            foreach (Transform soldier in soldiers)
            {
                SetEnabled(e, soldier.GetComponent<NavMeshAgent>(), true);
                SetEnabled(e, soldier.GetComponent<SoldierPerception>(), true);
                SetEnabled(e, soldier.GetComponent<SoldierAI>(), true);
            }
        }

        /// <summary>O animador do modelo do soldado (para mirar, atirar e cair numa cena).</summary>
        public static SoldierAnimator AnimatorOf(GameObject soldier) => soldier.GetComponentInChildren<SoldierAnimator>(true);

        public static Transform MuzzleOf(GameObject soldier)
        {
            var so = new SerializedObject(soldier.GetComponent<SoldierAI>());
            var muzzle = so.FindProperty("muzzle").objectReferenceValue as Transform;
            return muzzle != null ? muzzle : soldier.transform;
        }

        // ---------- Eventos do Inspector ----------

        public static void Listener(GameObject go, StoryEvent storyEvent, Action<UnityEvent> fill)
        {
            var listener = go.AddComponent<StoryEventListener>();
            SetField(listener, "storyEvent", storyEvent);
            fill(GetField<UnityEvent>(listener, "response"));
        }

        public static TimedSequence.Step S(string note, float wait, Action<UnityEvent> fill, StoryEvent orUntil = null)
        {
            var step = new TimedSequence.Step { note = note, wait = wait, orUntil = orUntil };
            fill?.Invoke(step.action);
            return step;
        }

        public static void SetSteps(TimedSequence sequence, params TimedSequence.Step[] steps) => SetField(sequence, "steps", steps);

        public static void Call(UnityEvent e, UnityAction action) => UnityEventTools.AddPersistentListener(e, action);

        // Chamada com um objeto como argumento (por exemplo, WalkTo de um ponto).
        public static void Call<T>(UnityEvent e, UnityAction<T> action, T argument) where T : Object =>
            UnityEventTools.AddObjectPersistentListener(e, action, argument);

        // Chamada com um sim/não como argumento (por exemplo, AimAt de um soldado).
        public static void Call(UnityEvent e, UnityAction<bool> action, bool argument) =>
            UnityEventTools.AddBoolPersistentListener(e, action, argument);

        public static void SetActive(UnityEvent e, GameObject go, bool active) =>
            UnityEventTools.AddBoolPersistentListener(e, go.SetActive, active);

        public static void SetEnabled(UnityEvent e, Behaviour behaviour, bool enabled)
        {
            var setter = (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), behaviour, "set_enabled");
            UnityEventTools.AddBoolPersistentListener(e, setter, enabled);
        }

        public static void Notify(UnityEvent e, NotificationPoster poster, string message) =>
            UnityEventTools.AddStringPersistentListener(e, poster.Post, message);

        // ---------- Campos privados ----------

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void SetField(Object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, Flags) ?? throw new MissingFieldException(target.GetType().Name, field);
            info.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        public static T GetField<T>(Object target, string field) where T : class
        {
            FieldInfo info = target.GetType().GetField(field, Flags) ?? throw new MissingFieldException(target.GetType().Name, field);
            return (T)info.GetValue(target);
        }

        // ---------- Arquivos de dados ----------

        // Carrega o arquivo se existir; se não, cria e preenche. Nunca sobrescreve um que já existe.
        public static T Asset<T>(string relativePath, Action<T> fill) where T : ScriptableObject
        {
            string path = $"{DataFolder}/{relativePath}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
            LinkKeys(asset);
            return asset;
        }

        // Arquivo novo com texto traduzível: liga os campos às chaves padrão. O texto não vem do script: precisa
        // estar em Resources/Localization/pt-BR.json (os scripts só dizem os códigos das falas, respostas e passos).
        static void LinkKeys(ScriptableObject asset)
        {
            foreach (string missing in Localization.LocKeys.AssignDefaults(asset))
                Debug.LogWarning($"Falta o texto no pt-BR.json: {missing}");
            EditorUtility.SetDirty(asset);
        }

        public static T Load<T>(string relativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{relativePath}.asset");

        public static StoryEvent Event(string name, string note) =>
            Asset<StoryEvent>("Events/" + name, e => e.note = note);

        public static DialogueDefinition Dialogue(string name, StoryEvent raiseOnEnd, params DialogueNode[] nodes) =>
            Asset<DialogueDefinition>("Dialogues/" + name, d =>
            {
                d.nodes = nodes;
                d.raiseOnEnd = raiseOnEnd;
            });

        // Os textos não ficam nos scripts: a fala "id" da conversa "Nome" tem o texto na chave dlg.Nome.id do
        // Resources/Localization/pt-BR.json. Escreva o texto lá antes de criar a conversa.
        public static DialogueNode N(string id, string speaker, string next = null, params DialogueChoice[] choices) =>
            new DialogueNode { id = id, speaker = speaker, next = next ?? "", choices = choices };

        // Resposta com código permanente (c1, c2...); o texto está em dlg.Nome.fala.c1 no pt-BR.json.
        public static DialogueChoice C(string id, string next, StoryEvent raise) =>
            new DialogueChoice { id = id, next = next, raise = raise };

        // Passo de missão com código permanente (s1, s2...); o texto está em quest.Nome.s1 no pt-BR.json.
        public static QuestStep Step(string id, StoryEvent completeOn = null, ItemDefinition item = null, int count = 1) =>
            new QuestStep { id = id, completeOn = completeOn, requiredItem = item, requiredCount = count };

        /// <summary>
        /// Acrescenta passos no fim de uma missão que já existe, cada um só se ainda não houver
        /// um passo com o mesmo evento e o mesmo item. Os passos novos recebem as chaves de texto padrão.
        /// </summary>
        public static void AppendSteps(QuestDefinition quest, params QuestStep[] newSteps)
        {
            var steps = new System.Collections.Generic.List<QuestStep>(quest.steps);
            int before = steps.Count;
            foreach (QuestStep step in newSteps)
                if (!steps.Exists(s => s.completeOn == step.completeOn && s.requiredItem == step.requiredItem))
                    steps.Add(step);
            if (steps.Count == before) return;

            Undo.RecordObject(quest, "Acrescentar passos à missão");
            quest.steps = steps.ToArray();
            foreach (var duplicate in quest.steps.Where(s => !string.IsNullOrEmpty(s.id)).GroupBy(s => s.id).Where(g => g.Count() > 1))
                Debug.LogError($"Missão {quest.name}: o código de passo \"{duplicate.Key}\" está repetido.");
            LinkKeys(quest);
        }
    }
}
