using System;
using System.Collections.Generic;
using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Inventory;
using Nibiru.Memories;
using Nibiru.Quests;
using Nibiru.Tablets;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>Um campo de texto de um arquivo de dados que pode ser traduzido.</summary>
    sealed class LocField
    {
        public Object asset;

        /// <summary>Identificação por posição, para comparar antes e depois da extração (não depende das chaves).</summary>
        public string snapshotId;

        /// <summary>Onde o campo está, para mensagens ("Dialogues/Aldeia_OGuia, fala s1").</summary>
        public string where;

        /// <summary>A chave que o campo recebe se ainda não tiver uma.</summary>
        public string suggestedKey;

        /// <summary>
        /// Campo que também identifica algo (o nome de quem fala é usado para achar o personagem). O texto antigo
        /// fica, e a tabela só guarda a versão para mostrar.
        /// </summary>
        public bool identity;

        /// <summary>O campo precisa de texto na tabela (um rodapé ou uma dica são opcionais).</summary>
        public bool required;

        public Func<string> getKey;
        public Action<string> setKey;
        public Func<string> getLegacy;
        public Action<string> setLegacy;
        public Func<string> getResolved;
    }

    /// <summary>Percorre os arquivos de dados com texto traduzível.</summary>
    static class LocFields
    {
        public static IEnumerable<T> Assets<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Nibiru" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null);

        /// <summary>
        /// Dá código permanente às respostas, às linhas de fita e aos passos de missão que ainda não têm (o maior
        /// número já usado mais um). Os códigos das falas de diálogo são escritos à mão e não são mexidos.
        /// </summary>
        public static int EnsureIds()
        {
            int assigned = 0;
            foreach (var dialogue in Assets<DialogueDefinition>()) assigned += EnsureIds(dialogue);
            foreach (var tape in Assets<TapeDefinition>()) assigned += EnsureIds(tape);
            foreach (var quest in Assets<QuestDefinition>()) assigned += EnsureIds(quest);
            return assigned;
        }

        /// <summary>O mesmo para um arquivo só.</summary>
        public static int EnsureIds(Object asset)
        {
            int assigned = 0;
            switch (asset)
            {
                case DialogueDefinition dialogue:
                    foreach (DialogueNode node in dialogue.nodes)
                        assigned += FillIds(node.choices.Select(c => c.id).ToList(), "c", (i, id) => { node.choices[i].id = id; EditorUtility.SetDirty(dialogue); });
                    break;
                case TapeDefinition tape:
                    assigned += FillIds(tape.lines.Select(l => l.id).ToList(), "l", (i, id) => { tape.lines[i].id = id; EditorUtility.SetDirty(tape); });
                    break;
                case QuestDefinition quest:
                    assigned += FillIds(quest.steps.Select(s => s.id).ToList(), "s", (i, id) => { quest.steps[i].id = id; EditorUtility.SetDirty(quest); });
                    break;
            }
            return assigned;
        }

        // Os que não têm código recebem prefixo + (maior número usado + 1), na ordem.
        static int FillIds(List<string> ids, string prefix, Action<int, string> assign)
        {
            int max = 0;
            foreach (string id in ids)
                if (!string.IsNullOrEmpty(id) && id.StartsWith(prefix) && int.TryParse(id.Substring(prefix.Length), out int n)) max = Math.Max(max, n);
            int count = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i])) continue;
                assign(i, prefix + (++max));
                count++;
            }
            return count;
        }

        /// <summary>Todos os campos que têm texto antigo ou chave (os vazios dos dois lados ficam de fora).</summary>
        public static List<LocField> All()
        {
            var fields = new List<LocField>();
            foreach (var d in Assets<DialogueDefinition>()) Collect(d, fields, false);
            foreach (var doc in Assets<DocumentDefinition>()) Collect(doc, fields, false);
            foreach (var tape in Assets<TapeDefinition>()) Collect(tape, fields, false);
            foreach (var item in Assets<ItemDefinition>()) Collect(item, fields, false);
            foreach (var quest in Assets<QuestDefinition>()) Collect(quest, fields, false);
            foreach (var tablet in Assets<TabletDefinition>()) Collect(tablet, fields, false);
            foreach (var memory in Assets<MemoryDefinition>()) Collect(memory, fields, false);
            return fields;
        }

        /// <summary>
        /// Os campos de um arquivo só. Com includeEmpty, também os que ainda não têm chave nem texto (para dar a
        /// chave padrão a um arquivo recém-criado pelos scripts de conteúdo).
        /// </summary>
        public static List<LocField> Of(Object asset, bool includeEmpty)
        {
            var fields = new List<LocField>();
            Collect(asset, fields, includeEmpty);
            return fields;
        }

        static void Collect(Object asset, List<LocField> fields, bool includeEmpty)
        {
            switch (asset)
            {
                case DialogueDefinition d:
                    for (int ni = 0; ni < d.nodes.Length; ni++)
                    {
                        DialogueNode node = d.nodes[ni];
                        string at = $"{d.name}, fala {node.id}";
                        if (!string.IsNullOrWhiteSpace(node.speaker))
                            Add(fields, includeEmpty, d, $"dlg:{d.name}:n{ni}:speaker", at + " (quem fala)", "spk." + LocTables.Slug(node.speaker), true, true,
                                () => node.speakerKey, v => node.speakerKey = v, () => node.speaker, null, () => node.LocalizedSpeaker);
                        Add(fields, includeEmpty, d, $"dlg:{d.name}:n{ni}:text", at, $"dlg.{d.name}.{node.id}", false, true,
                            () => node.textKey, v => node.textKey = v, () => node.text, v => node.text = v, () => node.LocalizedText);
                        for (int ci = 0; ci < node.choices.Length; ci++)
                        {
                            DialogueChoice choice = node.choices[ci];
                            Add(fields, includeEmpty, d, $"dlg:{d.name}:n{ni}:c{ci}", $"{at}, resposta {choice.id}", $"dlg.{d.name}.{node.id}.{choice.id}", false, true,
                                () => choice.textKey, v => choice.textKey = v, () => choice.text, v => choice.text = v, () => choice.LocalizedText);
                        }
                    }
                    break;

                case DocumentDefinition doc:
                    Add(fields, includeEmpty, doc, $"doc:{doc.name}:title", doc.name + " (título)", $"doc.{doc.name}.title", false, true,
                        () => doc.titleKey, v => doc.titleKey = v, () => doc.displayName, v => doc.displayName = v, () => doc.LocalizedTitle);
                    Add(fields, includeEmpty, doc, $"doc:{doc.name}:text", doc.name + " (texto)", $"doc.{doc.name}.text", false, true,
                        () => doc.textKey, v => doc.textKey = v, () => doc.text, v => doc.text = v, () => doc.LocalizedText);
                    Add(fields, includeEmpty, doc, $"doc:{doc.name}:footer", doc.name + " (rodapé)", $"doc.{doc.name}.footer", false, false,
                        () => doc.footerKey, v => doc.footerKey = v, () => doc.footer, v => doc.footer = v, () => doc.LocalizedFooter);
                    break;

                case TapeDefinition tape:
                    Add(fields, includeEmpty, tape, $"tape:{tape.name}:name", tape.name + " (nome)", $"tape.{tape.name}.name", false, true,
                        () => tape.displayNameKey, v => tape.displayNameKey = v, () => tape.displayName, v => tape.displayName = v, () => tape.LocalizedName);
                    Add(fields, includeEmpty, tape, $"tape:{tape.name}:label", tape.name + " (etiqueta)", $"tape.{tape.name}.label", false, false,
                        () => tape.speakerLabelKey, v => tape.speakerLabelKey = v, () => tape.speakerLabel, v => tape.speakerLabel = v, () => tape.LocalizedSpeakerLabel);
                    for (int li = 0; li < tape.lines.Length; li++)
                    {
                        TapeDefinition.Line line = tape.lines[li];
                        Add(fields, includeEmpty, tape, $"tape:{tape.name}:line{li}", $"{tape.name}, fala {line.id}", $"tape.{tape.name}.{line.id}", false, true,
                            () => line.textKey, v => line.textKey = v, () => line.text, v => line.text = v, () => line.LocalizedText);
                    }
                    break;

                case ItemDefinition item:
                    Add(fields, includeEmpty, item, $"item:{item.name}:name", item.name + " (nome)", $"item.{item.name}.name", false, true,
                        () => item.nameKey, v => item.nameKey = v, () => item.displayName, v => item.displayName = v, () => item.LocalizedName);
                    Add(fields, includeEmpty, item, $"item:{item.name}:desc", item.name + " (descrição)", $"item.{item.name}.desc", false, false,
                        () => item.descriptionKey, v => item.descriptionKey = v, () => item.description, v => item.description = v, () => item.LocalizedDescription);
                    Add(fields, includeEmpty, item, $"item:{item.name}:hint", item.name + " (dica)", $"item.{item.name}.hint", false, false,
                        () => item.hintKey, v => item.hintKey = v, () => item.pickupHint, v => item.pickupHint = v, () => item.LocalizedHint);
                    break;

                case QuestDefinition quest:
                    Add(fields, includeEmpty, quest, $"quest:{quest.name}:name", quest.name + " (nome)", $"quest.{quest.name}.name", false, true,
                        () => quest.nameKey, v => quest.nameKey = v, () => quest.displayName, v => quest.displayName = v, () => quest.LocalizedName);
                    for (int si = 0; si < quest.steps.Length; si++)
                    {
                        QuestStep step = quest.steps[si];
                        Add(fields, includeEmpty, quest, $"quest:{quest.name}:step{si}", $"{quest.name}, passo {step.id}", $"quest.{quest.name}.{step.id}", false, true,
                            () => step.objectiveKey, v => step.objectiveKey = v, () => step.objective, v => step.objective = v, () => step.LocalizedObjective);
                    }
                    break;

                case TabletDefinition tablet:
                    Add(fields, includeEmpty, tablet, $"tablet:{tablet.name}:name", tablet.name + " (nome)", $"tablet.{tablet.name}.name", false, true,
                        () => tablet.nameKey, v => tablet.nameKey = v, () => tablet.displayName, v => tablet.displayName = v, () => tablet.LocalizedName);
                    Add(fields, includeEmpty, tablet, $"tablet:{tablet.name}:text", tablet.name + " (texto)", $"tablet.{tablet.name}.text", false, true,
                        () => tablet.textKey, v => tablet.textKey = v, () => tablet.text, v => tablet.text = v, () => tablet.LocalizedText);
                    break;

                case MemoryDefinition memory:
                    Add(fields, includeEmpty, memory, $"memory:{memory.name}:character", memory.name + " (personagem)", $"memory.{memory.name}.character", false, false,
                        () => memory.characterNameKey, v => memory.characterNameKey = v, () => memory.characterName, v => memory.characterName = v, () => memory.LocalizedCharacterName);
                    Add(fields, includeEmpty, memory, $"memory:{memory.name}:place", memory.name + " (lugar)", $"memory.{memory.name}.place", false, false,
                        () => memory.placeTitleKey, v => memory.placeTitleKey = v, () => memory.placeTitle, v => memory.placeTitle = v, () => memory.LocalizedPlaceTitle);
                    break;
            }
        }

        static void Add(List<LocField> fields, bool includeEmpty, Object asset, string snapshotId, string where, string suggestedKey, bool identity, bool required,
            Func<string> getKey, Action<string> setKey, Func<string> getLegacy, Action<string> setLegacy, Func<string> getResolved)
        {
            // Sem chave e sem texto: nada para traduzir (por exemplo um rodapé vazio), a não ser que se peça tudo.
            if (!includeEmpty && string.IsNullOrEmpty(getKey()) && string.IsNullOrEmpty(getLegacy())) return;
            fields.Add(new LocField
            {
                asset = asset, snapshotId = snapshotId, where = where, suggestedKey = suggestedKey, identity = identity, required = required,
                getKey = getKey, setKey = setKey, getLegacy = getLegacy, setLegacy = setLegacy, getResolved = getResolved,
            });
        }
    }
}
