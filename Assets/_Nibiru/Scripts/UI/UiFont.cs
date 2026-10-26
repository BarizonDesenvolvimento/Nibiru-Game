using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// A fonte central da interface (veja <see cref="UiTheme"/>). Os textos criados por código usam <see cref="Current"/>;
    /// os que já estão nas cenas e nos prefabs recebem a fonte do tema quando a cena carrega, mas só se o tema tiver uma
    /// fonte própria (sem ela, nada muda).
    /// </summary>
    public static class UiFont
    {
        static Font cached;
        static bool hooked;

        /// <summary>A fonte da interface: a do tema, ou a embutida.</summary>
        public static Font Current
        {
            get
            {
                if (cached == null)
                {
                    var theme = Resources.Load<UiTheme>("UiTheme");
                    cached = theme != null && theme.font != null ? theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            cached = null;
            hooked = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            if (!hooked)
            {
                hooked = true;
                SceneManager.sceneLoaded += (scene, mode) => ApplyToLoadedTexts();
            }
            ApplyToLoadedTexts();
        }

        /// <summary>Põe a fonte do tema em todos os textos já existentes (não faz nada se o tema não tem fonte própria).</summary>
        public static void ApplyToLoadedTexts()
        {
            var theme = Resources.Load<UiTheme>("UiTheme");
            if (theme == null || theme.font == null) return;
            foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
                if (text.gameObject.scene.IsValid() && text.font != theme.font) text.font = theme.font;
        }
    }
}
