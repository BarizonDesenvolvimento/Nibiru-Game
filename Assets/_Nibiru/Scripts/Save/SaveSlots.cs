using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Nibiru.Combat;
using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.Memories;
using Nibiru.Quests;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nibiru.Save
{
    /// <summary>
    /// Salvamento em disco, com 3 slots. Cada slot é um arquivo JSON (save_1.json a save_3.json) em
    /// Application.persistentDataPath, com o último checkpoint (posição, vida, inventário, munição) e o estado do mundo
    /// daquele momento, mais um resumo para a tela de slots (área, data e hora, tempo jogado, demo concluída).
    /// ultimo_slot.json guarda qual slot foi usado por último ("Continuar").
    ///
    /// A partida grava sempre no slot escolhido ao começar: no "Novo jogo" e a cada checkpoint, altar ou salvamento
    /// automático. Nunca dentro de uma memória jogável. Sem slot escolhido (Play direto na cena, atalhos de teste)
    /// nada é gravado. Carregar é voltar ao último checkpoint, pelo mesmo caminho de quando a Elena morre.
    ///
    /// Tudo é gravado por código permanente (itens, armas e missões pelo <see cref="SaveRegistry"/>; passos de missão
    /// pelo código s1, s2...; eventos e facções pelo nome do arquivo; objetos da cena pelo PersistentId), nunca por
    /// referência. Qualquer mudança no formato do arquivo aumenta <see cref="Version"/>: um arquivo de outra versão,
    /// corrompido ou ilegível conta como slot vazio e deixa um aviso no Console.
    /// </summary>
    public static class SaveSlots
    {
        /// <summary>Versão do formato do arquivo. Aumente a cada mudança no formato.</summary>
        public const int Version = 1;
        public const int SlotCount = 3;

        /// <summary>Área mostrada quando o código do checkpoint não diz uma área conhecida.</summary>
        public const string DefaultArea = "sitio_de_ur";

        /// <summary>Área do "Novo jogo", antes do primeiro checkpoint (a Elena acorda na barraca).</summary>
        public const string NewGameArea = "acampamento";

        const string FinishedQuest = "concluida";

        static string folderOverride;

        /// <summary>Slot da partida em andamento (1 a 3), ou 0 se nenhum foi escolhido.</summary>
        public static int CurrentSlot { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            CurrentSlot = 0;
            folderOverride = null;
        }

        /// <summary>Pasta dos arquivos (persistentDataPath, ou a pasta dos testes).</summary>
        public static string Folder => folderOverride ?? Application.persistentDataPath;

        /// <summary>Só para os testes: grava e lê noutra pasta (null volta ao normal).</summary>
        public static void UseFolder(string path) => folderOverride = path;

        public static string PathOf(int slot) => Path.Combine(Folder, $"save_{slot}.json");

        static string LastSlotPath => Path.Combine(Folder, "ultimo_slot.json");

        /// <summary>A partida deixa de gravar em slot (atalhos de teste, volta ao menu).</summary>
        public static void Detach() => CurrentSlot = 0;

        /// <summary>Escolhe o slot da partida sem gravar (atalhos de teste).</summary>
        public static void Attach(int slot) => CurrentSlot = ValidSlot(slot) ? slot : 0;

        // ---------- Gravar ----------

        /// <summary>Começa uma partida nova no slot: zera o mundo, o relógio e os pontos de retorno e grava o começo.</summary>
        public static void BeginNewGame(int slot, string sceneName)
        {
            Checkpoints.Clear();
            WorldState.Clear();
            PlayTime.Set(0f);
            CurrentSlot = slot;
            Write(slot, new SaveFile
            {
                version = Version,
                summary = NewSummary(NewGameArea, false),
                scene = sceneName,
                world = ToFile(WorldState.Capture()),
            });
        }

        /// <summary>Grava o último checkpoint no slot da partida (chamado ao ativar um checkpoint).</summary>
        public static void WriteCurrent()
        {
            if (CurrentSlot == 0) return;
            if (InMemory())
            {
                Debug.Log("Salvamento: dentro da memória não grava; vale o último checkpoint de antes dela.");
                return;
            }
            Checkpoints.Data data = Checkpoints.Last;
            if (data == null) return;
            Write(CurrentSlot, FromCheckpoint(data, AreaFor(data), false));
        }

        /// <summary>Marca o slot da partida como "demo concluída": ele não pode mais ser carregado, só sobrescrito ou apagado.</summary>
        public static void MarkDemoCompleted()
        {
            if (CurrentSlot == 0) return;
            SaveFile file = Read(CurrentSlot);
            if (file == null) return;
            file.summary.demoCompleted = true;
            file.summary.savedAt = Now();
            file.summary.playSeconds = PlayTime.Seconds;
            Write(CurrentSlot, file);
        }

        public static void Delete(int slot)
        {
            if (!ValidSlot(slot)) return;
            if (File.Exists(PathOf(slot))) File.Delete(PathOf(slot));
        }

        static void Write(int slot, SaveFile file)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                WriteSafely(PathOf(slot), JsonConvert.SerializeObject(file, Formatting.Indented));
                WriteSafely(LastSlotPath, JsonConvert.SerializeObject(new LastSlotFile { slot = slot }));
            }
            catch (Exception e)
            {
                Debug.LogError($"Salvamento: não consegui gravar o slot {slot} ({e.Message}).");
            }
        }

        // Grava ao lado e só depois troca pelo definitivo: se o jogo cair no meio, o arquivo antigo continua inteiro.
        static void WriteSafely(string path, string json)
        {
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        // ---------- Ler ----------

        /// <summary>Resumo de um slot para a tela de slots, ou null se ele está vazio (ou ilegível).</summary>
        public static SlotInfo Info(int slot)
        {
            SaveFile file = Read(slot);
            if (file == null) return null;
            DateTime.TryParse(file.summary.savedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime savedAt);
            return new SlotInfo
            {
                slot = slot,
                area = file.summary.area,
                savedAt = savedAt,
                playSeconds = file.summary.playSeconds,
                demoCompleted = file.summary.demoCompleted,
            };
        }

        /// <summary>O slot usado por último (1 a 3), ou 0 se nenhum.</summary>
        public static int LatestSlot()
        {
            try
            {
                if (!File.Exists(LastSlotPath)) return 0;
                int slot = JsonConvert.DeserializeObject<LastSlotFile>(File.ReadAllText(LastSlotPath))?.slot ?? 0;
                return ValidSlot(slot) ? slot : 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Salvamento: não consegui ler qual slot foi usado por último ({e.Message}).");
                return 0;
            }
        }

        /// <summary>True se há um slot mais recente que pode ser carregado ("Continuar").</summary>
        public static bool CanContinue => Info(LatestSlot())?.CanLoad ?? false;

        /// <summary>Abre o slot mais recente. False se não há o que continuar.</summary>
        public static bool ContinueLatest() => CanContinue && Load(LatestSlot());

        /// <summary>
        /// Carrega o slot: volta ao último checkpoint gravado, como depois de morrer. False (e um aviso no Console) se o
        /// slot está vazio, ilegível ou com a demo concluída.
        /// </summary>
        public static bool Load(int slot)
        {
            SaveFile file = Read(slot);
            if (file == null)
            {
                Debug.LogWarning($"Salvamento: o slot {slot} está vazio ou não pôde ser lido.");
                return false;
            }
            if (file.summary.demoCompleted)
            {
                Debug.LogWarning($"Salvamento: o slot {slot} terminou a demo; ele não pode mais ser carregado, só sobrescrito ou apagado.");
                return false;
            }

            Checkpoints.Data data;
            WorldState.Snapshot world;
            try
            {
                world = FromFile(file.world);
                data = file.checkpoint != null ? ToCheckpoint(file, world) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Salvamento: o slot {slot} tem dados que não consegui usar ({e.Message}); ele conta como vazio.");
                return false;
            }

            CurrentSlot = slot;
            PlayTime.Set(file.summary.playSeconds);
            try
            {
                WriteSafely(LastSlotPath, JsonConvert.SerializeObject(new LastSlotFile { slot = slot }));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Salvamento: não consegui gravar qual slot foi usado por último ({e.Message}).");
            }

            if (data != null)
            {
                Checkpoints.Save(data);
                Checkpoints.Restart(file.scene);
            }
            else
            {
                // Gravado no "Novo jogo", antes do primeiro checkpoint: a região recomeça do zero.
                Checkpoints.Clear();
                WorldState.Restore(world);
                Time.timeScale = 1f;
                SceneManager.LoadScene(file.scene);
            }
            return true;
        }

        /// <summary>Lê o arquivo de um slot. Null se ausente, corrompido ou de outra versão (os dois últimos avisam no Console).</summary>
        static SaveFile Read(int slot)
        {
            if (!ValidSlot(slot)) return null;
            string path = PathOf(slot);
            if (!File.Exists(path)) return null;
            try
            {
                SaveFile file = JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(path));
                if (file == null || file.summary == null || file.world == null || string.IsNullOrEmpty(file.scene))
                {
                    Debug.LogWarning($"Salvamento: o slot {slot} ({path}) está incompleto; ele conta como vazio.");
                    return null;
                }
                if (file.version != Version)
                {
                    Debug.LogWarning($"Salvamento: o slot {slot} ({path}) é da versão {file.version} do formato, e o jogo lê a {Version}; ele conta como vazio.");
                    return null;
                }
                return file;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Salvamento: o slot {slot} ({path}) está corrompido ou ilegível ({e.Message}); ele conta como vazio.");
                return null;
            }
        }

        static bool ValidSlot(int slot) => slot >= 1 && slot <= SlotCount;

        static bool InMemory() =>
            Object.FindObjectsByType<PlayableMemory>(FindObjectsSortMode.None).Any(m => m.IsInMemory);

        // ---------- Área ----------

        /// <summary>A área de um código de checkpoint: o começo dele, antes do ponto ("aldeia.checkpoint_patio" é "aldeia").</summary>
        public static string AreaOf(string checkpointCode)
        {
            if (string.IsNullOrEmpty(checkpointCode)) return "";
            int dot = checkpointCode.IndexOf('.');
            return dot > 0 ? checkpointCode.Substring(0, dot) : checkpointCode;
        }

        public static string AreaKey(string area) => "ui.area." + area;

        /// <summary>Nome da área no idioma atual.</summary>
        public static string AreaName(string area) => Loc.Get(AreaKey(Loc.Has(AreaKey(area)) ? area : DefaultArea));

        // Pelo código do checkpoint; sem ele (atalhos de teste), pelo checkpoint mais perto do ponto de retorno.
        static string AreaFor(Checkpoints.Data data)
        {
            string code = data.checkpointId;
            if (string.IsNullOrEmpty(code))
            {
                Checkpoint nearest = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .OrderBy(c => (c.transform.position - data.position).sqrMagnitude).FirstOrDefault();
                code = nearest != null ? nearest.GetComponent<PersistentId>().Id : "";
            }
            string area = AreaOf(code);
            if (Loc.Has(AreaKey(area))) return area;
            Debug.LogWarning($"Salvamento: o checkpoint \"{code}\" não diz uma área conhecida (falta {AreaKey(area)} no pt-BR.json); o slot mostra \"{Loc.Get(AreaKey(DefaultArea))}\".");
            return DefaultArea;
        }

        // ---------- Conversão para o arquivo ----------

        static Summary NewSummary(string area, bool demoCompleted) => new Summary
        {
            area = area,
            savedAt = Now(),
            playSeconds = PlayTime.Seconds,
            demoCompleted = demoCompleted,
        };

        static string Now() => DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        static SaveFile FromCheckpoint(Checkpoints.Data data, string area, bool demoCompleted)
        {
            var file = new SaveFile
            {
                version = Version,
                summary = NewSummary(area, demoCompleted),
                scene = data.sceneName,
                world = ToFile(data.world),
                checkpoint = new CheckpointFile
                {
                    code = data.checkpointId ?? "",
                    position = new[] { data.position.x, data.position.y, data.position.z },
                    rotation = new[] { data.rotation.x, data.rotation.y, data.rotation.z, data.rotation.w },
                    health = data.health,
                    slots = Stacks(data.slots),
                    keyItems = Stacks(data.keyItems),
                    loaded = new Dictionary<string, int>(),
                },
            };
            if (data.weapons != null)
            {
                file.checkpoint.equipped = data.weapons.equipped != null ? FirearmCode(data.weapons.equipped) : null;
                foreach (KeyValuePair<FirearmDefinition, int> pair in data.weapons.loaded)
                    file.checkpoint.loaded[FirearmCode(pair.Key)] = pair.Value;
            }
            return file;
        }

        static List<StackFile> Stacks(List<ItemStack> stacks) =>
            stacks.Where(s => s.item != null).Select(s => new StackFile { item = s.item.Id, count = s.count }).ToList();

        static string FirearmCode(FirearmDefinition firearm)
        {
            if (string.IsNullOrEmpty(firearm.Id)) Debug.LogError($"Salvamento: a arma {firearm.name} não tem código. Rode Nibiru > Salvamento > Criar registro e códigos.");
            return firearm.Id;
        }

        static WorldFile ToFile(WorldState.Snapshot world)
        {
            SaveRegistry registry = SaveRegistry.Instance;
            var file = new WorldFile
            {
                events = world.storyEvents.OrderBy(e => e, StringComparer.Ordinal).ToList(),
                failedQuests = new List<string>(),
                reputation = new SortedDictionary<string, int>(world.reputation),
                transformation = world.transformation,
                pickups = new SortedDictionary<string, int>(world.pickupsRemaining),
                checkpoints = world.activatedCheckpoints.OrderBy(c => c, StringComparer.Ordinal).ToList(),
                quests = new SortedDictionary<string, string>(),
            };
            foreach (KeyValuePair<string, int> pair in world.questSteps)
            {
                QuestDefinition quest = registry.QuestByName(pair.Key);
                if (quest == null || string.IsNullOrEmpty(quest.StableId))
                {
                    Debug.LogError($"Salvamento: a missão {pair.Key} não está no registro ou não tem código; ela fica fora do slot.");
                    continue;
                }
                file.quests[quest.StableId] = pair.Value >= quest.steps.Length ? FinishedQuest : quest.steps[pair.Value].id;
            }
            foreach (string name in world.failedQuests.OrderBy(q => q, StringComparer.Ordinal))
            {
                QuestDefinition quest = registry.QuestByName(name);
                if (quest != null && !string.IsNullOrEmpty(quest.StableId)) file.failedQuests.Add(quest.StableId);
                else Debug.LogError($"Salvamento: a missão {name} não está no registro ou não tem código; a falha dela fica fora do slot.");
            }
            return file;
        }

        // ---------- Conversão de volta ----------

        static WorldState.Snapshot FromFile(WorldFile file)
        {
            SaveRegistry registry = SaveRegistry.Instance;
            var questSteps = new Dictionary<string, int>();
            foreach (KeyValuePair<string, string> pair in file.quests ?? new SortedDictionary<string, string>())
            {
                QuestDefinition quest = registry.QuestByCode(pair.Key);
                if (quest == null) throw new InvalidDataException($"missão desconhecida \"{pair.Key}\"");
                int step = pair.Value == FinishedQuest ? quest.steps.Length : Array.FindIndex(quest.steps, s => s.id == pair.Value);
                if (step < 0) throw new InvalidDataException($"passo desconhecido \"{pair.Value}\" da missão \"{pair.Key}\"");
                questSteps[quest.Id] = step;
            }
            var failed = new HashSet<string>();
            foreach (string code in file.failedQuests ?? new List<string>())
            {
                QuestDefinition quest = registry.QuestByCode(code);
                if (quest == null) throw new InvalidDataException($"missão desconhecida \"{code}\"");
                failed.Add(quest.Id);
            }
            return new WorldState.Snapshot(
                new Dictionary<string, int>(file.pickups ?? new SortedDictionary<string, int>()),
                new HashSet<string>(file.checkpoints ?? new List<string>()),
                new HashSet<string>(file.events ?? new List<string>()),
                questSteps,
                failed,
                new Dictionary<string, int>(file.reputation ?? new SortedDictionary<string, int>()),
                file.transformation);
        }

        static Checkpoints.Data ToCheckpoint(SaveFile file, WorldState.Snapshot world)
        {
            CheckpointFile c = file.checkpoint;
            SaveRegistry registry = SaveRegistry.Instance;
            var weapons = new PlayerWeapons.Snapshot { loaded = new Dictionary<FirearmDefinition, int>() };
            if (!string.IsNullOrEmpty(c.equipped)) weapons.equipped = registry.Firearm(c.equipped) ?? throw new InvalidDataException($"arma desconhecida \"{c.equipped}\"");
            foreach (KeyValuePair<string, int> pair in c.loaded ?? new Dictionary<string, int>())
                weapons.loaded[registry.Firearm(pair.Key) ?? throw new InvalidDataException($"arma desconhecida \"{pair.Key}\"")] = pair.Value;

            return new Checkpoints.Data
            {
                sceneName = file.scene,
                checkpointId = c.code,
                position = new Vector3(c.position[0], c.position[1], c.position[2]),
                rotation = new Quaternion(c.rotation[0], c.rotation[1], c.rotation[2], c.rotation[3]),
                health = c.health,
                slots = ToStacks(c.slots, registry),
                keyItems = ToStacks(c.keyItems, registry),
                weapons = weapons,
                world = world,
            };
        }

        static List<ItemStack> ToStacks(List<StackFile> stacks, SaveRegistry registry) =>
            (stacks ?? new List<StackFile>()).Select(s => new ItemStack(registry.Item(s.item) ?? throw new InvalidDataException($"item desconhecido \"{s.item}\""), s.count)).ToList();

        // ---------- Formato do arquivo ----------

        /// <summary>O que a tela de slots mostra de um slot.</summary>
        public class SlotInfo
        {
            public int slot;
            public string area;
            public DateTime savedAt;
            public float playSeconds;
            public bool demoCompleted;

            public string AreaName => SaveSlots.AreaName(area);
            public bool CanLoad => !demoCompleted;
        }

        class LastSlotFile
        {
            [JsonProperty("slot")] public int slot;
        }

        class SaveFile
        {
            [JsonProperty("versao")] public int version;
            [JsonProperty("resumo")] public Summary summary;
            [JsonProperty("cena")] public string scene;
            /// <summary>Null no arquivo do "Novo jogo", antes do primeiro checkpoint.</summary>
            [JsonProperty("checkpoint")] public CheckpointFile checkpoint;
            [JsonProperty("mundo")] public WorldFile world;
        }

        class Summary
        {
            [JsonProperty("area")] public string area;
            [JsonProperty("dataHora")] public string savedAt;
            [JsonProperty("tempoJogadoSegundos")] public float playSeconds;
            [JsonProperty("demoConcluida")] public bool demoCompleted;
        }

        class CheckpointFile
        {
            [JsonProperty("codigo")] public string code;
            [JsonProperty("posicao")] public float[] position;
            [JsonProperty("rotacao")] public float[] rotation;
            [JsonProperty("vida")] public float health;
            [JsonProperty("inventario")] public List<StackFile> slots;
            [JsonProperty("itensChave")] public List<StackFile> keyItems;
            [JsonProperty("armaEquipada")] public string equipped;
            [JsonProperty("balasNaArma")] public Dictionary<string, int> loaded;
        }

        class StackFile
        {
            [JsonProperty("item")] public string item;
            [JsonProperty("quantidade")] public int count;
        }

        class WorldFile
        {
            [JsonProperty("eventos")] public List<string> events;
            [JsonProperty("missoes")] public SortedDictionary<string, string> quests;
            [JsonProperty("missoesQueFalharam")] public List<string> failedQuests;
            [JsonProperty("reputacao")] public SortedDictionary<string, int> reputation;
            [JsonProperty("transformacao")] public int transformation;
            [JsonProperty("itensPegos")] public SortedDictionary<string, int> pickups;
            [JsonProperty("checkpointsAtivados")] public List<string> checkpoints;
        }
    }
}
