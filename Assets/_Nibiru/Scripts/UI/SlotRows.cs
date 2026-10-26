using System;
using System.Globalization;
using Nibiru.Localization;
using Nibiru.Save;

namespace Nibiru.UI
{
    /// <summary>O texto de cada slot na tela de slots (tudo pelo sistema de localização).</summary>
    public static class SlotRows
    {
        /// <summary>Tempo jogado como hh:mm, ou "—" quando o arquivo não tem o tempo.</summary>
        public static string PlayTimeText(SaveSlots.SlotInfo info)
        {
            if (!info.hasPlayTime) return "—";
            int minutes = (int)(info.playSeconds / 60f);
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }

        /// <summary>Data e hora de gravação no formato do computador do jogador.</summary>
        public static string DateText(SaveSlots.SlotInfo info) => info.savedAt.ToString("g", CultureInfo.CurrentCulture);

        /// <summary>Texto de uma linha de slot: "Slot 1   Aldeia   26/10/2026 14:32   Tempo 01:23".</summary>
        public static string Format(SaveSlots.SlotInfo info)
        {
            string name = string.Format(Loc.Get("ui.slots.slot", "Slot {0}"), info.slot);
            switch (info.state)
            {
                case SaveSlots.SlotState.Empty:
                    return $"{name}     {Loc.Get("ui.slots.vazio", "Vazio")}";
                case SaveSlots.SlotState.Unreadable:
                    return $"{name}     {Loc.Get("ui.slots.ilegivel", "Dados do slot ilegíveis")}";
                case SaveSlots.SlotState.DemoCompleted:
                    return $"{name}     {info.AreaName}     {DateText(info)}     {string.Format(Loc.Get("ui.slots.tempo", "Tempo {0}"), PlayTimeText(info))}     {Loc.Get("ui.slots.demo", "Demo concluída")}";
                default:
                    return $"{name}     {info.AreaName}     {DateText(info)}     {string.Format(Loc.Get("ui.slots.tempo", "Tempo {0}"), PlayTimeText(info))}";
            }
        }
    }
}
