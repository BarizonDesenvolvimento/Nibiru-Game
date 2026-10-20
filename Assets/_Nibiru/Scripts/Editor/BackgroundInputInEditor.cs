using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: durante o Play, o jogo continua rodando e recebendo comandos mesmo com a
    /// janela da Unity em segundo plano. Serve para testes automáticos pela ponte MCP.
    /// Nada disso vai para a versão final do jogo: os valores originais voltam ao sair do Play.
    /// Efeito colateral enquanto ligado: teclas digitadas em outras janelas da Unity durante o
    /// Play também chegam ao jogo.
    /// Liga e desliga em Nibiru > Testes > Controles com a Unity em segundo plano.
    /// </summary>
    [InitializeOnLoad]
    static class BackgroundInputInEditor
    {
        const string MenuPath = "Nibiru/Testes/Controles com a Unity em segundo plano";
        const string PrefKey = "Nibiru.BackgroundInputInEditor";

        static bool applied;
        static bool originalRunInBackground;
        static InputSettings.BackgroundBehavior originalBackgroundBehavior;
        static InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior;

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        static BackgroundInputInEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            // Ao recompilar no meio do Play, reaplica.
            if (EditorApplication.isPlaying && Enabled) Apply();
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            if (!EditorApplication.isPlaying) return;
            if (Enabled) Apply();
            else Restore();
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && Enabled) Apply();
            else if (change == PlayModeStateChange.ExitingPlayMode) Restore();
        }

        static void Apply()
        {
            if (applied) return;
            applied = true;

            originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;

            InputSettings settings = InputSystem.settings;
            originalBackgroundBehavior = settings.backgroundBehavior;
            originalEditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        static void Restore()
        {
            if (!applied) return;
            applied = false;

            Application.runInBackground = originalRunInBackground;

            InputSettings settings = InputSystem.settings;
            if (settings == null) return;
            settings.backgroundBehavior = originalBackgroundBehavior;
            settings.editorInputBehaviorInPlayMode = originalEditorBehavior;
        }
    }
}
