using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nibiru.EditorTools.Saving;
using Nibiru.Localization;
using Nibiru.Player;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Mouse
{
    /// <summary>
    /// Teste automático do mouse nos menus e do cursor (A4c). O mouse é simulado pelo <see cref="MenuPointer"/> (posição no
    /// centro da faixa de cada item, calculada pelo gerador de texto), mais um mouse virtual do Input System para provar o
    /// caminho de leitura de verdade. Cobre: hover move o foco (e só quando o mouse se mexe), clique ativa, clique direito
    /// volta ou cancela, roda rola o Arquivo, "Apagar" em cada slot, os botões da confirmação, as Opções (metade esquerda e
    /// direita), a leitura e o fim da demo, e o cursor (preso no jogo, livre nos menus, escondido pelo teclado, de volta ao
    /// mexer o mouse, certo depois do alt-tab e depois de voltar ao menu). Grava só numa pasta de teste.
    /// Menu: Nibiru > Testes > Mouse > Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class MouseTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string MenuSceneName = "MenuPrincipal";

        static string folder;
        static float savedMouseSensitivity, savedPadSensitivity;
        static bool savedInvert;
        static string previousLanguage;

        static MouseTests() => Register("mouse.teste", Begin);

        [MenuItem("Nibiru/Testes/Mouse/Teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("mouse.teste");

        static void Begin()
        {
            folder = Path.Combine(Application.persistentDataPath, "Teste_Mouse");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            SaveSlots.UseFolder(folder);
            SlotShortcuts.FillAll();
            SlotShortcuts.MakeOccupied(1);
            SlotGuard.Begin();
            savedMouseSensitivity = GameOptions.MouseSensitivity;
            savedPadSensitivity = GameOptions.GamepadSensitivity;
            savedInvert = GameOptions.InvertY;
            // Os textos conferidos são os do pt-BR, seja qual for o idioma escolhido nas Opções.
            previousLanguage = Loc.Language;
            Loc.SetLanguage("pt-BR", false);

            Start("Mouse, teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Pausa: hover move o foco, clique ativa, clique direito volta, cursor livre e some com o teclado", PauseMouse),
                ("Opções: metade esquerda diminui e metade direita aumenta; Voltar volta", OptionsMouse),
                ("Arquivo: a roda rola, clique abre a leitura, clique direito fecha a leitura e depois volta", ArchiveMouse),
                ("Pausa: confirmação de Sair para o menu e fechar com o mouse sem o clique atirar", PauseDialogAndClose),
                ("Cursor: preso no jogo, livre na pausa, certo depois do alt-tab, escondido pelo teclado e de volta ao mexer o mouse", CursorStates),
                ("Mouse virtual do Input System: posição, movimento, clique esquerdo, clique direito e roda chegam aos menus", VirtualMouse),
                ("Fim da demo: cursor livre; clique esquerdo volta ao menu com o cursor livre", DemoEnd),
                ("Menu principal: hover, clique, clique em área vazia e o mesmo caminho pelo Update", MainMenuMouse),
                ("Tela de slots: Apagar em cada slot, confirmação por mouse, sobrescrever e voltar", SlotsMouse),
                ("Limpeza: apaga a pasta do teste e devolve as opções", Cleanup),
            });
        }

        // ---------- Ajudas ----------

        static MenuPointer.State Hover(Vector2 at) => new MenuPointer.State { position = at, moved = true };

        static MenuPointer.State Click(Vector2 at) => new MenuPointer.State { position = at, leftClick = true };

        static MenuPointer.State RightClick(Vector2 at) => new MenuPointer.State { position = at, rightClick = true };

        static MenuPointer.State Wheel(Vector2 at, float amount) => new MenuPointer.State { position = at, moved = true, wheel = amount };

        static Until Frames(int count)
        {
            int target = Time.frameCount + count;
            return new Until(() => Time.frameCount >= target, 10f, $"{count} quadros");
        }

        static PauseMenuUI Pause() => Object.FindFirstObjectByType<PauseMenuUI>();

        static PlayerInputReader Input() => Object.FindFirstObjectByType<PlayerInputReader>();

        static void OpenPause() => typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(Pause(), null);

        static Vector2 RowCenter(PauseMenuUI pause, int row)
        {
            bool found = pause.TryGetRowRect(row, out Rect rect);
            Check(found, $"pausa: a linha {row} deveria ter uma faixa na tela");
            return rect.center;
        }

        static Vector2 RowCenter(MainMenuUI menu, int row)
        {
            bool found = menu.TryGetRowRect(row, out Rect rect);
            Check(found, $"menu: a linha {row} deveria ter uma faixa na tela");
            return rect.center;
        }

        static IEnumerator OpenMenu(Action<MainMenuUI> found)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(MenuSceneName);
            yield return new Until(() => Object.FindFirstObjectByType<MainMenuUI>() != null, 30f, "o menu principal abrir");
            yield return Frames(3);
            found(Object.FindFirstObjectByType<MainMenuUI>());
        }

        // ---------- Pausa ----------

        static IEnumerator PauseMouse()
        {
            yield return Restart("arquivo.cheio");
            yield return Frames(3);
            PauseMenuUI pause = Pause();
            Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "no jogo o cursor deveria estar preso e escondido");

            OpenPause();
            yield return Frames(3);
            Check(pause.IsOpen, "a pausa deveria abrir");
            Check(Cursor.lockState == CursorLockMode.None, "pausa aberta: o cursor deveria estar livre");
            Check(!Cursor.visible, "pausa aberta pelo teclado: o cursor deveria ficar escondido até o mouse mexer");

            // Hover move o foco (o mesmo destaque do teclado); parado, não move.
            pause.PointerStep(Hover(RowCenter(pause, 2)));
            Check(pause.FocusedRow == 2, $"passar o mouse no item 2 deveria pôr o foco nele, está em {pause.FocusedRow}");
            pause.PointerStep(new MenuPointer.State { position = RowCenter(pause, 4) });
            Check(pause.FocusedRow == 2, "o mouse parado sobre outro item não deveria mexer o foco");
            pause.PointerStep(Hover(new Vector2(2f, 2f)));
            Check(pause.FocusedRow == 2, "mouse fora dos itens não deveria mexer o foco");
            pause.PointerStep(Click(new Vector2(2f, 2f)));
            Check(pause.IsOpen && pause.PageName == "Principal", "clique fora dos itens não deveria fazer nada");

            // Clique esquerdo ativa: Opções.
            pause.PointerStep(Click(RowCenter(pause, 1)));
            Check(pause.PageName == "Opcoes", $"clicar em Opções deveria abrir as Opções, está em {pause.PageName}");
            // Clique direito volta para a página principal, com o foco em Opções.
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(pause.PageName == "Principal" && pause.IsOpen, "o clique direito nas Opções deveria voltar à pausa");
            Check(pause.FocusedRow == 1, "voltar das Opções deveria deixar o foco em Opções");

            // Fecha pelo clique direito (como Esc): cursor preso de novo.
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!pause.IsOpen, "o clique direito na pausa deveria fechar a pausa");
            Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "despausar: o cursor deveria voltar a ficar preso e escondido");
            yield return Frames(3);
            Check(Input().GameplayEnabled, "depois de despausar o jogo deveria voltar a ouvir o controle");
        }

        static IEnumerator OptionsMouse()
        {
            PauseMenuUI pause = Pause();
            OpenPause();
            yield return Frames(3);
            pause.PointerStep(Click(RowCenter(pause, 1)));
            Check(pause.PageName == "Opcoes", "deveria estar nas Opções");

            float before = GameOptions.MouseSensitivity;
            Check(pause.TryGetOptionHalf(0, 1, out Rect rightHalf) && pause.TryGetOptionHalf(0, -1, out Rect leftHalf) && leftHalf.xMax <= rightHalf.xMin + 0.5f,
                "as duas metades da seta da sensibilidade deveriam existir, esquerda antes da direita");
            pause.TryGetOptionHalf(0, 1, out rightHalf);
            pause.TryGetOptionHalf(0, -1, out leftHalf);
            pause.PointerStep(Click(rightHalf.center));
            Check(Mathf.Abs(GameOptions.MouseSensitivity - (before + 0.1f)) < 0.001f, $"a metade direita deveria aumentar a sensibilidade: {before} → {GameOptions.MouseSensitivity}");
            pause.PointerStep(Click(leftHalf.center));
            pause.PointerStep(Click(leftHalf.center));
            Check(Mathf.Abs(GameOptions.MouseSensitivity - (before - 0.1f)) < 0.001f, $"a metade esquerda deveria diminuir a sensibilidade: {GameOptions.MouseSensitivity}");
            Check(pause.FocusedRow == 0, "clicar na opção deveria pôr o foco nela");

            bool invert = GameOptions.InvertY;
            pause.PointerStep(Click(RowCenter(pause, 2)));
            Check(GameOptions.InvertY != invert, "clicar em Inverter eixo vertical deveria alternar");
            pause.PointerStep(Click(RowCenter(pause, 2)));
            Check(GameOptions.InvertY == invert, "clicar de novo deveria voltar ao valor de antes");

            Check(pause.ScreenText.Contains("‹") && pause.ScreenText.Contains("›"), "a linha das opções deveria mostrar as setas ‹ valor ›");

            pause.PointerStep(Click(RowCenter(pause, 4)));
            Check(pause.PageName == "Principal", "clicar em Voltar deveria voltar à pausa");
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!pause.IsOpen, "o clique direito deveria fechar a pausa");
        }

        static IEnumerator ArchiveMouse()
        {
            PauseMenuUI pause = Pause();
            OpenPause();
            yield return Frames(3);
            pause.PointerStep(Click(RowCenter(pause, 2)));
            Check(pause.PageName == "Arquivo", $"clicar em Arquivo deveria abrir o Arquivo, está em {pause.PageName}");
            Check(!pause.ScreenText.Contains("▲"), "no topo da lista não deveria haver ▲");

            // Roda: para baixo rola a lista, para cima volta.
            Vector2 over = RowCenter(pause, 0);
            pause.PointerStep(Wheel(over, -1f));
            Check(pause.ScreenText.Contains("▲"), "a roda para baixo deveria rolar a lista (▲ aparece)");
            pause.PointerStep(Wheel(over, 1f));
            Check(!pause.ScreenText.Contains("▲"), "a roda para cima deveria voltar ao topo");

            // O marcador ▼ rola com o clique.
            Check(pause.ArchiveMarkerLine(1) >= 0, "o Arquivo cheio deveria ter o marcador ▼");
            Check(pause.TryGetArchiveMarkerRect(1, out Rect down), "o marcador ▼ deveria ter faixa na tela");
            pause.PointerStep(Click(down.center));
            Check(pause.ScreenText.Contains("▲"), "clicar em ▼ deveria rolar a lista");
            pause.PointerStep(Wheel(over, 5f));

            // Hover numa entrada move o foco; clique abre a leitura.
            int row = 1;
            pause.PointerStep(Hover(RowCenter(pause, row)));
            Check(pause.FocusedRow == row, $"passar o mouse na entrada {row} deveria pôr o foco nela, está em {pause.FocusedRow}");
            pause.PointerStep(Click(RowCenter(pause, row)));
            var reading = Object.FindFirstObjectByType<ReadingScreenUI>();
            Check(reading != null && reading.IsOpen, "clicar numa entrada deveria abrir a leitura");
            Check(Cursor.lockState == CursorLockMode.None, "com a leitura aberta o cursor deveria continuar livre");
            yield return Frames(3);

            // Clique direito fecha só a leitura, e a pausa continua no Arquivo.
            reading.PointerStep(RightClick(new Vector2(5f, 5f)));
            Check(!reading.IsOpen, "o clique direito deveria fechar a leitura");
            Check(pause.IsOpen && pause.PageName == "Arquivo", "fechar a leitura deveria devolver à lista do Arquivo");
            Check(Cursor.lockState == CursorLockMode.None, "depois de fechar a leitura a pausa ainda deveria ter o cursor livre");
            yield return Frames(3);

            // Clique esquerdo também fecha.
            pause.PointerStep(Click(RowCenter(pause, row)));
            Check(reading.IsOpen, "deveria abrir a leitura de novo");
            yield return Frames(3);
            reading.PointerStep(Click(new Vector2(5f, 5f)));
            Check(!reading.IsOpen, "o clique esquerdo deveria fechar a leitura");
            yield return Frames(3);

            // Clique direito: Arquivo → pausa → fecha.
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(pause.PageName == "Principal", "o clique direito no Arquivo deveria voltar à pausa");
            Check(pause.FocusedRow == 2, "voltar do Arquivo deveria deixar o foco no Arquivo");
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!pause.IsOpen, "o clique direito deveria fechar a pausa");
        }

        static IEnumerator PauseDialogAndClose()
        {
            PauseMenuUI pause = Pause();
            OpenPause();
            yield return Frames(3);

            // Sair para o menu: confirmação com os botões clicáveis; o foco começa em Cancelar.
            pause.PointerStep(Click(RowCenter(pause, 4)));
            Check(pause.ExitDialog.IsOpen && pause.ExitDialog.FocusOnCancel, "Sair para o menu deveria abrir a confirmação com o foco em Cancelar");
            pause.PointerStep(Hover(RowCenter(pause, 1)));
            Check(!pause.ExitDialog.FocusOnCancel, "passar o mouse no botão de confirmar deveria pôr o foco nele");
            pause.PointerStep(Hover(RowCenter(pause, 0)));
            Check(pause.ExitDialog.FocusOnCancel, "passar o mouse em Cancelar deveria pôr o foco nele");
            pause.PointerStep(Hover(RowCenter(pause, 1)));
            pause.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!pause.ExitDialog.IsOpen && pause.IsOpen, "o clique direito deveria cancelar a confirmação e ficar na pausa");
            pause.PointerStep(Click(RowCenter(pause, 4)));
            pause.PointerStep(Click(new Vector2(2f, 2f)));
            Check(pause.ExitDialog.IsOpen, "clicar fora dos botões não deveria fechar a confirmação");
            pause.PointerStep(Click(RowCenter(pause, 0)));
            Check(!pause.ExitDialog.IsOpen && pause.IsOpen, "clicar em Cancelar deveria fechar a confirmação sem sair");

            // Continuar com o clique: fecha a pausa, e o mesmo clique não chega ao jogo no mesmo quadro.
            pause.PointerStep(Click(RowCenter(pause, 0)));
            Check(!pause.IsOpen, "clicar em Continuar deveria fechar a pausa");
            Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "fechar a pausa deveria prender o cursor");
            Check(!Input().GameplayEnabled, "fechando com o clique, o jogo só deveria ouvir o controle no quadro seguinte");
            yield return Frames(3);
            Check(Input().GameplayEnabled, "no quadro seguinte o jogo deveria voltar a ouvir o controle");
        }

        // ---------- Cursor ----------

        static IEnumerator CursorStates()
        {
            // Alt-tab no jogo: se a Unity soltou o cursor, ele volta a ficar preso.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CursorPolicy.OnFocusRegained();
            Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "alt-tab no jogo: o cursor deveria voltar a ficar preso e escondido");

            // Alt-tab na pausa: o cursor continua livre (e escondido, se o último uso foi o teclado).
            OpenPause();
            yield return Frames(3);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CursorPolicy.OnFocusRegained();
            Check(Cursor.lockState == CursorLockMode.None, "alt-tab na pausa: o cursor deveria ficar livre");

            // Teclado esconde, mexer o mouse mostra (pelo Update da pausa, com o mouse simulado).
            MenuPointer.NoteKeyboardOrPad();
            Check(!Cursor.visible && Cursor.lockState == CursorLockMode.None, "o teclado deveria esconder o cursor sem prendê-lo");
            MenuPointer.Source = () => Hover(new Vector2(2f, 2f));
            try
            {
                yield return Frames(3);
            }
            finally
            {
                MenuPointer.Source = null;
            }
            Check(Cursor.visible, "mexer o mouse deveria mostrar o cursor de novo");
            MenuPointer.NoteKeyboardOrPad();
            Check(!Cursor.visible, "o teclado deveria esconder o cursor de novo");

            Pause().PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!Pause().IsOpen, "a pausa deveria fechar");
            Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "fechar a pausa deveria prender o cursor");
            yield return Frames(3);
        }

        static IEnumerator VirtualMouse()
        {
            UnityEngine.InputSystem.Mouse real = UnityEngine.InputSystem.Mouse.current;
            UnityEngine.InputSystem.Mouse virtualMouse = InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("MouseDeTeste");
            try
            {
                virtualMouse.MakeCurrent();
                // O clique dura um quadro de entrada: lê logo depois de cada atualização do Input System.
                MenuPointer.State state = default;
                var seen = new List<MenuPointer.State>();
                Action probe = () => seen.Add(MenuPointer.Read());
                InputSystem.onAfterUpdate += probe;
                try
                {
                    InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = new Vector2(321f, 123f), delta = new Vector2(4f, 0f) }.WithButton(MouseButton.Left));
                    yield return new Until(() => seen.Exists(s => s.leftClick), 5f, "o clique esquerdo do mouse virtual chegar");
                    state = seen.Find(s => s.leftClick);
                    seen.Clear();
                    InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = new Vector2(10f, 20f), scroll = new Vector2(0f, 120f) }.WithButton(MouseButton.Right));
                    yield return new Until(() => seen.Exists(s => s.rightClick), 5f, "o clique direito do mouse virtual chegar");
                    MenuPointer.State right = seen.Find(s => s.rightClick);
                    Check(right.rightClick && !right.leftClick, "o clique direito deveria chegar, sem o esquerdo");
                    Check(right.wheel > 0f, $"a roda para cima deveria chegar positiva: {right.wheel}");
                }
                finally
                {
                    InputSystem.onAfterUpdate -= probe;
                }
                Check(state.leftClick && !state.rightClick, "o clique esquerdo deveria chegar, sem o direito");
                Check(state.position == new Vector2(321f, 123f), $"a posição deveria chegar: {state.position}");
                Check(state.moved, "o movimento deveria chegar");
            }
            finally
            {
                InputSystem.RemoveDevice(virtualMouse);
                if (real != null) real.MakeCurrent();
            }
        }

        // ---------- Fim da demo ----------

        static IEnumerator DemoEnd()
        {
            var screen = Object.FindFirstObjectByType<DemoEndScreen>(FindObjectsInactive.Include);
            Check(screen != null, "a tela de fim da demo deveria estar na região");
            screen.Show();
            Check(Cursor.lockState == CursorLockMode.None, "o fim da demo deveria soltar o cursor");
            screen.PointerStep(Click(new Vector2(5f, 5f)));
            Check(screen.IsOpen, "antes de 20 quadros o clique não deveria sair da tela");
            yield return Frames(25);
            screen.PointerStep(RightClick(new Vector2(5f, 5f)));
            Check(screen.IsOpen, "só o clique esquerdo deveria sair do fim da demo");
            screen.PointerStep(Click(new Vector2(5f, 5f)));
            yield return new Until(() => Object.FindFirstObjectByType<MainMenuUI>() != null, 30f, "o menu principal abrir");
            yield return Frames(3);
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "voltar ao menu pelo fim da demo deveria deixar o cursor livre e visível");
        }

        // ---------- Menu principal ----------

        static IEnumerator MainMenuMouse()
        {
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>();
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "menu principal: o cursor deveria estar livre e visível");
            Check(menu.ShowsContinue && menu.ShowsLoad, "com os slots do teste o menu deveria ter Continuar e Carregar");

            menu.PointerStep(Hover(RowCenter(menu, 2)));
            Check(menu.FocusedRow == 2, $"passar o mouse no item 2 deveria pôr o foco nele, está em {menu.FocusedRow}");
            menu.PointerStep(new MenuPointer.State { position = RowCenter(menu, 3) });
            Check(menu.FocusedRow == 2, "o mouse parado sobre outro item não deveria mexer o foco");
            menu.PointerStep(Click(new Vector2(2f, 2f)));
            Check(menu.FocusedRow == 2 && !menu.InSlotScreen, "clique em área vazia não deveria fazer nada");
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!menu.InSlotScreen, "o clique direito no menu principal não deveria fazer nada");

            // O mesmo caminho pelo Update (o menu lê o MenuPointer sozinho).
            MenuPointer.Source = () => Hover(RowCenter(menu, 1));
            try
            {
                yield return Frames(3);
            }
            finally
            {
                MenuPointer.Source = null;
            }
            Check(menu.FocusedRow == 1, "pelo Update, o mouse sobre o item 1 deveria pôr o foco nele");

            // Teclado esconde o cursor; mexer o mouse traz de volta.
            MenuPointer.NoteKeyboardOrPad();
            Check(!Cursor.visible && Cursor.lockState == CursorLockMode.None, "no menu o teclado deveria esconder o cursor sem prendê-lo");
            MenuPointer.Source = () => Hover(RowCenter(menu, 1));
            try
            {
                yield return Frames(3);
            }
            finally
            {
                MenuPointer.Source = null;
            }
            Check(Cursor.visible, "no menu mexer o mouse deveria mostrar o cursor");

            // Clique em Novo jogo (item 1) abre a tela de slots; clique direito volta.
            menu.PointerStep(Click(RowCenter(menu, 1)));
            Check(menu.InSlotScreen && menu.IsNewGameScreen, "clicar em Novo jogo deveria abrir a tela de slots do Novo jogo");
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!menu.InSlotScreen, "o clique direito na tela de slots deveria voltar ao menu");
            // Clique em Carregar (item 2).
            menu.PointerStep(Click(RowCenter(menu, 2)));
            Check(menu.InSlotScreen && !menu.IsNewGameScreen, "clicar em Carregar deveria abrir a tela de slots do Carregar");
            // Um slot que não carrega (a demo concluída, slot 2) mostra o aviso e nada acontece.
            menu.PointerStep(Click(RowCenter(menu, 1) + new Vector2(-300f, 0f)));
            Check(menu.InSlotScreen && menu.ScreenText.Contains("não pode ser carregado"), "clicar num slot que não carrega deveria mostrar o aviso");
            // Voltar (linha 3) pelo clique.
            menu.PointerStep(Click(RowCenter(menu, 3)));
            Check(!menu.InSlotScreen, "clicar em Voltar deveria voltar ao menu");
        }

        static IEnumerator SlotsMouse()
        {
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>();
            menu.OpenSlots(true);
            yield return Frames(2);

            Check(menu.RenderedLineCount == menu.ScreenText.Split((char)10).Length, $"nenhuma linha da tela de slots deveria quebrar sozinha: {menu.RenderedLineCount} linhas desenhadas x {menu.ScreenText.Split((char)10).Length} no texto");

            // Apagar em cada slot que tem arquivo.
            for (int row = 0; row < SaveSlots.SlotCount; row++)
                Check(menu.TryGetDeleteRect(row, out Rect rect) && rect.width > 10f, $"o slot {row + 1} tem arquivo e deveria ter o botão Apagar");

            // Hover sobre o Apagar do slot 2 move o foco para o slot 2.
            menu.TryGetDeleteRect(1, out Rect delete2);
            menu.PointerStep(Hover(delete2.center));
            Check(menu.FocusedSlot == 2, $"passar o mouse no Apagar do slot 2 deveria focar o slot 2, está em {menu.FocusedSlot}");

            // Apagar: abre a confirmação do slot certo, com o foco em Cancelar; clique direito cancela.
            menu.TryGetDeleteRect(0, out Rect delete1);
            menu.PointerStep(Click(delete1.center));
            Check(menu.IsDialogOpen && menu.DialogFocusOnCancel, "clicar em Apagar deveria abrir a confirmação com o foco em Cancelar");
            Check(menu.ScreenText.Contains("slot 1"), "a confirmação deveria ser a do slot 1");
            menu.PointerStep(Hover(RowCenter(menu, 1)));
            Check(!menu.DialogFocusOnCancel, "passar o mouse no botão de confirmar deveria pôr o foco nele");
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!menu.IsDialogOpen && SaveSlots.Inspect(1).state != SaveSlots.SlotState.Empty, "o clique direito deveria cancelar sem apagar");

            // Cancelar pelo clique não apaga.
            menu.PointerStep(Click(delete1.center));
            menu.PointerStep(Click(RowCenter(menu, 0)));
            Check(!menu.IsDialogOpen && SaveSlots.Inspect(1).state != SaveSlots.SlotState.Empty, "clicar em Cancelar não deveria apagar");

            // Clicar fora dos botões não fecha nem confirma.
            menu.PointerStep(Click(delete1.center));
            menu.PointerStep(Click(new Vector2(2f, 2f)));
            Check(menu.IsDialogOpen && SaveSlots.Inspect(1).state != SaveSlots.SlotState.Empty, "clique fora dos botões não deveria fazer nada");
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));

            // Sobrescrever: clicar no slot ocupado (fora do Apagar) pergunta; o foco começa em Cancelar.
            menu.TryGetRowRect(1, out Rect row2);
            menu.PointerStep(Click(new Vector2(row2.xMin + 40f, row2.center.y)));
            Check(menu.IsDialogOpen && menu.DialogFocusOnCancel && menu.ScreenText.Contains("slot 2"), "clicar num slot ocupado do Novo jogo deveria pedir para sobrescrever o slot 2");
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!menu.IsDialogOpen && SaveSlots.Inspect(2).state != SaveSlots.SlotState.Empty, "cancelar não deveria sobrescrever");

            // Confirmar apaga de verdade (slot 3, o ilegível).
            menu.TryGetDeleteRect(2, out Rect delete3);
            menu.PointerStep(Click(delete3.center));
            Check(menu.IsDialogOpen && menu.ScreenText.Contains("slot 3"), "deveria abrir a confirmação do slot 3");
            menu.PointerStep(Click(RowCenter(menu, 1)));
            Check(!menu.IsDialogOpen && SaveSlots.Inspect(3).state == SaveSlots.SlotState.Empty, "confirmar pelo clique deveria apagar o slot 3");
            Check(!menu.TryGetDeleteRect(2, out _), "o slot 3 vazio não deveria ter o botão Apagar");

            // Clicar num slot vazio no Novo jogo começa a partida: só confere a pergunta (vazio não pergunta) sem carregar a cena.
            menu.PointerStep(RightClick(new Vector2(2f, 2f)));
            Check(!menu.InSlotScreen, "o clique direito deveria voltar ao menu principal");
        }

        static IEnumerator Cleanup()
        {
            SlotGuard.End();
            MenuPointer.Source = null;
            GameOptions.MouseSensitivity = savedMouseSensitivity;
            GameOptions.GamepadSensitivity = savedPadSensitivity;
            GameOptions.InvertY = savedInvert;
            if (!string.IsNullOrEmpty(previousLanguage)) Loc.SetLanguage(previousLanguage, false);
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder != null && folder.EndsWith("Teste_Mouse") && Directory.Exists(folder)) Directory.Delete(folder, true);
            yield break;
        }
    }
}
