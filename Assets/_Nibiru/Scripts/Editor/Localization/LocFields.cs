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

        public Func<string> getKey;
        public Action<string> setKey;
        public Func<string> getLegacy;
        public Action<string> setLegacy;
        public Func<string> getResolved;
    }

    /// <summary>Percorre todos os arquivos de dados com texto traduzível.</summary>
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
            foreach (var dialogue in Assets<DialogueDefinition>())
            {
                foreach (DialogueNode node in dialogue.nodes)
                    assigned += FillIds(node.choices.Select(c => c.id).ToList(), "c", (i, id) => { node.choices[i].id = id; EditorUtility.SetDirty(dialogue); });
            }
            foreach (var tape in Assets<TapeDefinition>())
                assigned += FillIds(tape.lines.Select(l => l.id).ToList(), "l", (i, id) => { tape.lines[i].id = id; EditorUtility.SetDirty(tape); });
            foreach (var quest in Assets<QuestDefinition>())
                assigned += FillIds(quest.steps.Select(s => s.id).ToList(), "s", (i, id) => { quest.steps[i].id = id; EditorUtility.SetDirty(quest); });
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

            foreach (var d in Assets<DialogueDefinition>())
            {
                for (int ni = 0; ni < d.nodes.Length; ni++)
                {
                    DialogueNode node = d.nodes[ni];
                    string at = $"{d.name}, fala {node.id}";
                    Add(fields, d, $"dlg:{d.name}:n{ni}:speaker", at + " (quem fala)", "spk." + LocTables.Slug(node.speaker), true,
                        () => node.speakerKey, v => node.speakerKey = v, () => node.speaker, null, () => node.LocalizedSpeaker);
                    Add(fields, d, $"dlg:{d.name}:n{ni}:text", at, $"dlg.{d.name}.{node.id}", false,
                        () => node.textKey, v => node.textKey = v, () => node.text, v => node.text = v, () => node.LocalizedText);
                    for (int ci = 0; ci < node.choices.Length; ci++)
                    {
                        DialogueChoice choice = node.choices[ci];
                        Add(fields, d, $"dlg:{d.name}:n{ni}:c{ci}", $"{at}, resposta {choice.id}", $"dlg.{d.name}.{node.id}.{choice.id}", false,
                            () => choice.textKey, v => choice.textKey = v, () => choice.text, v => choice.text = v, () => choice.LocalizedText);
                    }
                }
            }

            foreach (var doc in Assets<DocumentDefinition>())
            {
                Add(fields, doc, $"doc:{doc.name}:title", doc.name + " (título)", $"doc.{doc.name}.title", false,
                    () => doc.titleKey, v => doc.titleKey = v, () => doc.displayName, v => doc.displayName = v, () => doc.LocalizedTitle);
                Add(fields, doc, $"doc:{doc.name}:text", doc.name + " (texto)", $"doc.{doc.name}.text", false,
                    () => doc.textKey, v => doc.textKey = v, () => doc.text, v => doc.text = v, () => doc.LocalizedText);
                Add(fields, doc, $"doc:{doc.name}:footer", doc.name + " (rodapé)", $"doc.{doc.name}.footer", false,
                    () => doc.footerKey, v => doc.footerKey = v, () => doc.footer, v => doc.footer = v, () => doc.LocalizedFooter);
            }

            foreach (var tape in Assets<TapeDefinition>())
            {
                Add(fields, tape, $"tape:{tape.name}:name", tape.name + " (nome)", $"tape.{tape.name}.name", false,
                    () => tape.displayNameKey, v => tape.displayNameKey = v, () => tape.displayName, v => tape.displayName = v, () => tape.LocalizedName);
                Add(fields, tape, $"tape:{tape.name}:label", tape.name + " (etiqueta)", $"tape.{tape.name}.label", false,
                    () => tape.speakerLabelKey, v => tape.speakerLabelKey = v, () => tape.speakerLabel, v => tape.speakerLabel = v, () => tape.LocalizedSpeakerLabel);
                for (int li = 0; li < tape.lines.Length; li++)
                {
                    TapeDefinition.Line line = tape.lines[li];
                    Add(fields, tape, $"tape:{tape.name}:line{li}", $"{tape.name}, fala {line.id}", $"tape.{tape.name}.{line.id}", false,
                        () => line.textKey, v => line.textKey = v, () => line.text, v => line.text = v, () => line.LocalizedText);
                }
            }

            foreach (var item in Assets<ItemDefinition>())
            {
                Add(fields, item, $"item:{item.name}:name", item.name + " (nome)", $"item.{item.name}.name", false,
                    () => item.nameKey, v => item.nameKey = v, () => item.displayName, v => item.displayName = v, () => item.LocalizedName);
                Add(fields, item, $"item:{item.name}:desc", item.name + " (descrição)", $"item.{item.name}.desc", false,
                    () => item.descriptionKey, v => item.descriptionKey = v, () => item.description, v => item.description = v, () => item.LocalizedDescription);
                Add(fields, item, $"item:{item.name}:hint", item.name + " (dica)", $"item.{item.name}.hint", false,
                    () => item.hintKey, v => item.hintKey = v, () => item.pickupHint, v => item.pickupHint = v, () => item.LocalizedHint);
            }

            foreach (var quest in Assets<QuestDefinition>())
            {
                Add(fields, quest, $"quest:{quest.name}:name", quest.name + " (nome)", $"quest.{quest.name}.name", false,
                    () => quest.nameKey, v => quest.nameKey = v, () => quest.displayName, v => quest.displayName = v, () => quest.LocalizedName);
                for (int si = 0; si < quest.steps.Length; si++)
                {
                    QuestStep step = quest.steps[si];
                    Add(fields, quest, $"quest:{quest.name}:step{si}", $"{quest.name}, passo {step.id}", $"quest.{quest.name}.{step.id}", false,
                        () => step.objectiveKey, v => step.objectiveKey = v, () => step.objective, v => step.objective = v, () => step.LocalizedObjective);
                }
            }

            foreach (var tablet in Assets<TabletDefinition>())
            {
                Add(fields, tablet, $"tablet:{tablet.name}:name", tablet.name + " (nome)", $"tablet.{tablet.name}.name", false,
                    () => tablet.nameKey, v => tablet.nameKey = v, () => tablet.displayName, v => tablet.displayName = v, () => tablet.LocalizedName);
                Add(fields, tablet, $"tablet:{tablet.name}:text", tablet.name + " (texto)", $"tablet.{tablet.name}.text", false,
                    () => tablet.textKey, v => tablet.textKey = v, () => tablet.text, v => tablet.text = v, () => tablet.LocalizedText);
            }

            foreach (var memory in Assets<MemoryDefinition>())
            {
                Add(fields, memory, $"memory:{memory.name}:character", memory.name + " (personagem)", $"memory.{memory.name}.character", false,
                    () => memory.characterNameKey, v => memory.characterNameKey = v, () => memory.characterName, v => memory.characterName = v, () => memory.LocalizedCharacterName);
                Add(fields, memory, $"memory:{memory.name}:place", memory.name + " (lugar)", $"memory.{memory.name}.place", false,
                    () => memory.placeTitleKey, v => memory.placeTitleKey = v, () => memory.placeTitle, v => memory.placeTitle = v, () => memory.LocalizedPlaceTitle);
            }

            return fields;
        }

        static void Add(List<LocField> fields, Object asset, string snapshotId, string where, string suggestedKey, bool identity,
            Func<string> getKey, Action<string> setKey, Func<string> getLegacy, Action<string> setLegacy, Func<string> getResolved)
        {
            // Sem chave e sem texto: nada para traduzir (por exemplo um rodapé vazio).
            if (string.IsNullOrEmpty(getKey()) && string.IsNullOrEmpty(getLegacy())) return;
            fields.Add(new LocField
            {
                asset = asset, snapshotId = snapshotId, where = where, suggestedKey = suggestedKey, identity = identity,
                getKey = getKey, setKey = setKey, getLegacy = getLegacy, setLegacy = setLegacy, getResolved = getResolved,
            });
        }
    }
}
