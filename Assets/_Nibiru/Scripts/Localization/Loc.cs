using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Nibiru.Localization
{
    /// <summary>
    /// Textos do jogo por chave, em vários idiomas. Cada idioma é um arquivo JSON em
    /// Resources/Localization (pt-BR.json, en.json): um objeto simples "chave": "texto". O idioma escolhido fica
    /// guardado nas preferências do jogador. Falta de texto no idioma escolhido cai no pt-BR (o inglês pode ficar
    /// vazio); falta no pt-BR cai no texto antigo do campo, se houver, e por último no nome da chave entre colchetes.
    ///
    /// Convenção das chaves (veja o CLAUDE.md): dlg.&lt;conversa&gt;.&lt;fala&gt;[.&lt;escolha&gt;], spk.&lt;nome&gt;,
    /// doc.&lt;nome&gt;.title|text|footer, tape.&lt;nome&gt;.name|label|&lt;linha&gt;, item.&lt;nome&gt;.name|desc|hint,
    /// quest.&lt;nome&gt;.name|&lt;passo&gt;, tablet.&lt;nome&gt;.name|text, memory.&lt;nome&gt;.character|place.
    /// </summary>
    public static class Loc
    {
        public const string DefaultLanguage = "pt-BR";
        public const string PrefsKey = "Nibiru.Language";

        /// <summary>Idiomas do jogo, na ordem em que aparecem no seletor.</summary>
        public static readonly string[] Languages = { "pt-BR", "en" };

        static string language;
        static Dictionary<string, string> current, fallback;

        /// <summary>Avisa a interface quando o idioma muda.</summary>
        public static event Action LanguageChanged;

        // Começa limpo a cada Play, mesmo com o recarregamento de domínio desligado no editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            InvalidateCache();
            LanguageChanged = null;
        }

        /// <summary>Código do idioma atual (pt-BR ou en).</summary>
        public static string Language
        {
            get
            {
                EnsureLoaded();
                return language;
            }
        }

        /// <summary>Nome do idioma como ele mesmo se chama (não é traduzido).</summary>
        public static string DisplayName(string code) => code == "en" ? "English" : "Português (Brasil)";

        /// <summary>Troca o idioma. Com save, guarda a escolha para a próxima vez que o jogo abrir.</summary>
        public static void SetLanguage(string code, bool save = true)
        {
            if (Array.IndexOf(Languages, code) < 0) code = DefaultLanguage;
            EnsureLoaded();
            if (code == language) return;
            language = code;
            current = code == DefaultLanguage ? fallback : Load(code);
            if (save)
            {
                PlayerPrefs.SetString(PrefsKey, code);
                PlayerPrefs.Save();
            }
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// O texto da chave no idioma atual. Sem a chave (campo ainda não migrado), devolve o texto antigo.
        /// </summary>
        public static string Get(string key, string legacy = null)
        {
            if (string.IsNullOrEmpty(key)) return legacy ?? string.Empty;
            EnsureLoaded();
            if (current.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value)) return value;
            if (fallback.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return value;
            return !string.IsNullOrEmpty(legacy) ? legacy : "[" + key + "]";
        }

        /// <summary>True se a chave tem texto no pt-BR (o idioma de referência).</summary>
        public static bool Has(string key)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(key) && fallback.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value);
        }

        /// <summary>True se a chave tem texto neste idioma (sem cair no pt-BR).</summary>
        public static bool HasIn(string code, string key)
        {
            return !string.IsNullOrEmpty(key) && Table(code).TryGetValue(key, out string value) && !string.IsNullOrEmpty(value);
        }

        /// <summary>Esquece as tabelas lidas, para a próxima consulta ler os arquivos de novo (o editor chama depois de gravar).</summary>
        public static void InvalidateCache()
        {
            current = null;
            fallback = null;
            language = null;
            loaded.Clear();
        }

        static readonly Dictionary<string, Dictionary<string, string>> loaded = new Dictionary<string, Dictionary<string, string>>();

        static Dictionary<string, string> Table(string code)
        {
            EnsureLoaded();
            if (!loaded.TryGetValue(code, out Dictionary<string, string> table))
                loaded[code] = table = Load(code);
            return table;
        }

        static void EnsureLoaded()
        {
            if (current != null) return;
            loaded.Clear();
            string saved = PlayerPrefs.GetString(PrefsKey, DefaultLanguage);
            language = Array.IndexOf(Languages, saved) >= 0 ? saved : DefaultLanguage;
            fallback = Load(DefaultLanguage);
            current = language == DefaultLanguage ? fallback : Load(language);
        }

        static Dictionary<string, string> Load(string code)
        {
            var asset = Resources.Load<TextAsset>("Localization/" + code);
            if (asset == null) return new Dictionary<string, string>();
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text) ?? new Dictionary<string, string>();
            }
            catch (JsonException e)
            {
                Debug.LogError($"Localização: o arquivo {code}.json está com erro: {e.Message}");
                return new Dictionary<string, string>();
            }
        }
    }
}
