using System;
using System.Collections.Generic;
using Nibiru.Localization;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// A lista do Arquivo (menu de pausa): quais documentos e fitas existem, em que grupos e em que ordem.
    /// O que já foi lido ou ouvido vem dos eventos de cada um (<c>raiseOnRead</c>, <c>raiseOnCollected</c>); este
    /// arquivo não guarda estado. Quem salva o jogo (A4) deve usar o <see cref="Entry.Id"/> de cada entrada
    /// (campo <c>id</c> do documento ou da fita), nunca o nome do arquivo.
    /// Fica em Resources/Arquivo/Catalogo; criado por Nibiru > Arquivo > Criar catálogo e códigos.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Catálogo do Arquivo", fileName = "Catalogo")]
    public class ArchiveCatalog : ScriptableObject
    {
        public const string ResourcePath = "Arquivo/Catalogo";

        /// <summary>Uma entrada: um documento ou uma fita.</summary>
        [Serializable]
        public class Entry
        {
            public DocumentDefinition document;
            public TapeDefinition tape;

            public bool IsTape => tape != null;

            /// <summary>Código estável usado pelo salvamento.</summary>
            public string Id => tape != null ? tape.Id : document != null ? document.Id : "";

            public StoryEvent UnlockEvent => tape != null ? tape.raiseOnCollected : document != null ? document.raiseOnRead : null;

            /// <summary>Já foi lido (documento) ou coletado (fita) nesta partida.</summary>
            public bool IsUnlocked => StoryEvents.HasBeenRaised(UnlockEvent);

            public string Title => tape != null ? tape.LocalizedName : document != null ? document.LocalizedTitle : "";

            public string Body => tape != null ? tape.TranscriptText : document != null ? document.LocalizedText : "";

            public string Footer => tape != null ? tape.LocalizedSpeakerLabel : document != null ? document.LocalizedFooter : "";
        }

        [Serializable]
        public class Group
        {
            [Tooltip("Chave do título do grupo (ui.arquivo.grupo_...).")]
            [LocKey] public string titleKey;
            public Entry[] entries = new Entry[0];

            public string LocalizedTitle => Loc.Get(titleKey);
        }

        public Group[] groups = new Group[0];

        [Tooltip("Disparado na primeira vez que algo entra no Arquivo: marca que o aviso \"Adicionado ao Arquivo\" já foi dado.")]
        public StoryEvent noticeEvent;

        static ArchiveCatalog cached;

        /// <summary>O catálogo do jogo (vem de Resources). Nulo se ainda não foi criado.</summary>
        public static ArchiveCatalog Current
        {
            get
            {
                if (cached == null) cached = Resources.Load<ArchiveCatalog>(ResourcePath);
                return cached;
            }
        }

        public IEnumerable<Entry> AllEntries()
        {
            foreach (Group group in groups)
                foreach (Entry entry in group.entries)
                    if (entry != null) yield return entry;
        }

        /// <summary>Só o que já foi lido ou coletado, no grupo dado.</summary>
        public List<Entry> UnlockedIn(Group group)
        {
            var list = new List<Entry>();
            foreach (Entry entry in group.entries)
                if (entry != null && entry.IsUnlocked) list.Add(entry);
            return list;
        }

        public int UnlockedCount()
        {
            int count = 0;
            foreach (Entry entry in AllEntries())
                if (entry.IsUnlocked) count++;
            return count;
        }
    }
}
