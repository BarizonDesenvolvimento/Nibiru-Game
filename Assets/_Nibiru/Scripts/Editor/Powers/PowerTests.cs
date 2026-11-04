using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nibiru.Cameras;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Localization;
using Nibiru.Memories;
using Nibiru.Player;
using Nibiru.Powers;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Tablets;
using Nibiru.Testes;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using InputMouse = UnityEngine.InputSystem.Mouse;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.PowerTools
{
    /// <summary>
    /// Teste automático do ME1 (posse, recarga e mapa de controles dos poderes), no Play, com controle, teclado e mouse
    /// virtuais do Input System (os comandos chegam ao jogo pelo mesmo caminho dos de verdade). Cobre: a tabuleta dá o Eco
    /// uma vez; checkpoint e recarga guardada; recarga só em tempo de jogo; a regra do LB (Eco ao soltar, LB+RB, LB+Y, RB
    /// sozinho é faca, outro botão cancela, segurar mais de 0,5 s cancela); mira; teclado e mouse; poder que ela não tem;
    /// bloqueios (diálogo, pausa, inventário, leitura, memória, viagem); zoom no D-pad; plano B (Eco no Y sozinho).
    /// Mexe só numa pasta de teste. Menu: Nibiru > Testes > Poderes > Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class PowerTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string EcoCode = "poder.eco_do_abzu";

        static string folder;
        static Gamepad pad;
        static Keyboard realKeyboard, keyboard;
        static InputMouse realMouse, mouse;
        static readonly List<(PowerDefinition power, int frame, float latency)> fired = new List<(PowerDefinition, int, float)>();
        static PlayerPowers hooked;

        static PowerTests() => Register("poderes.teste", Begin);

        [MenuItem("Nibiru/Testes/Poderes/Teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("poderes.teste");

        static void Begin()
        {
            folder = SaveStorage.TestFolder("Teste_Poderes");
            SaveStorage.DeleteTestFolder(folder);
            SaveSlots.UseFolder(folder);
            PlayerInputReader.SchemeOverride = null;
            Loc.SetLanguage("pt-BR", false);

            pad = InputSystem.AddDevice<Gamepad>("ControleDeTestePoderes");
            realKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("TecladoDeTestePoderes");
            realMouse = InputMouse.current;
            mouse = InputSystem.AddDevice<InputMouse>("MouseDeTestePoderes");

            Start("Poderes (ME1), teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Tabuleta: ler no nicho dá o Eco uma vez, com o rodapé com chave; ler de novo não dá de novo", TabletGrants),
                ("Checkpoint: o Eco e a recarga restante voltam como estavam ao guardar", CheckpointKeeps),
                ("Recarga só em tempo de jogo: para na pausa, no inventário, na leitura e no diálogo", GameTimeOnly),
                ("Controle: LB tocado dispara o Eco ao soltar, nunca ao apertar (mede o atraso)", LbTap),
                ("Controle: LB+RB dispara a vaga 2, sem faca e sem Eco", LbRb),
                ("Controle: LB+Y dispara a vaga 3, sem Eco", LbY),
                ("Controle: RB sozinho é a faca, sem poder", RbAlone),
                ("Controle: LB seguro + B esquiva e cancela o Eco", LbDodge),
                ("Controle: LB seguro mais de 0,5 s e solto não faz nada", LbLongHold),
                ("Mira: LB solto dá Eco, LB+RB dá a vaga 2, RB sozinho não faz nada", Aiming),
                ("Teclado e mouse: Q, Z, X e os botões laterais 4 e 5 disparam na hora", KeyboardMouse),
                ("Poder que ela não tem: LB+RB não faz nada e não vira faca", NotOwned),
                ("Recarga: dentro dela o poder não dispara de novo", CooldownBlocks),
                ("Bloqueios: diálogo, pausa, inventário e leitura não disparam poder", Blocked),
                ("Zoom no D-pad: cima aproxima, baixo afasta (0,25 m), segurar repete; mirando e em menu não", DpadZoom),
                ("Plano B (configuração): Y sozinho dispara o Eco na hora, LB sozinho nada, combinações iguais", SchemeB),
                ("Memória jogável e viagem de caminhonete: nenhum poder dispara", MemoryAndTruck),
                ("Limpeza: tira os aparelhos de teste e apaga a pasta do teste", Cleanup),
            });
        }

        // ---------- Ajudas ----------

        static Until Frames(int count)
        {
            int target = Time.frameCount + count;
            return new Until(() => Time.frameCount >= target, 10f, $"{count} quadros");
        }

        static PlayerPowers Powers() => Object.FindFirstObjectByType<PlayerPowers>();

        static PlayerInputReader Reader() => Motor().GetComponent<PlayerInputReader>();

        static PowerDefinition Power(string code) => SaveRegistry.Instance.Power(code);

        static PowerDefinition Eco => Power(EcoCode);
        static PowerDefinition Slot2 => Power("poder.teste_poder_vaga_2");
        static PowerDefinition Slot3 => Power("poder.teste_poder_vaga_3");

        static int Count(PowerDefinition power) => fired.Count(f => f.power == power);

        // Liga a escuta dos disparos (a Elena muda a cada recarga de cena) e zera as recargas.
        static void Hook()
        {
            PlayerPowers powers = Powers();
            if (powers != hooked)
            {
                if (hooked != null) hooked.Activated -= OnFired;
                hooked = powers;
                if (hooked != null) hooked.Activated += OnFired;
            }
            fired.Clear();
            foreach (PowerDefinition p in new[] { Eco, Slot2, Slot3 }) WorldState.SetPowerCooldown(p.Id, 0f);
        }

        static void OnFired(PowerDefinition power) => fired.Add((power, Time.frameCount, hooked.Input.LastPadPowerLatency));

        // Dá os dois poderes de teste (vagas 2 e 3) e põe o efeito deles na Elena.
        static void AddTestPowers()
        {
            PlayerPowers powers = Powers();
            foreach (PowerDefinition p in new[] { Slot2, Slot3 })
            {
                PowerBook.Grant(p);
                if (powers.GetComponents<TestPowerBehaviour>().All(b => b.definition != p))
                    powers.gameObject.AddComponent<TestPowerBehaviour>().definition = p;
            }
            powers.RefreshBehaviours();
        }

        static IEnumerator ToMoment(string id, float settle = 3f)
        {
            int old = Motor() != null ? Motor().GetInstanceID() : 0;
            Go(id);
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 60f, $"o atalho {id} recarregar a região");
            yield return new Delay(settle);
            Hook();
        }

        static IEnumerator ToVillage()
        {
            yield return ToMoment("livre.aldeia", 3.5f);
            AddTestPowers();
            Hook();
        }

        static void Pad(params GamepadButton[] held)
        {
            var state = new GamepadState();
            foreach (GamepadButton b in held)
            {
                // Os gatilhos são eixos no estado do controle, não bits de botão.
                if (b == GamepadButton.LeftTrigger) state.leftTrigger = 1f;
                else if (b == GamepadButton.RightTrigger) state.rightTrigger = 1f;
                else state = state.WithButton(b);
            }
            InputSystem.QueueStateEvent(pad, state);
        }

        static IEnumerator PadPress(params GamepadButton[] held)
        {
            Pad(held);
            yield return Frames(2);
        }

        static IEnumerator PadRelease()
        {
            Pad();
            yield return Frames(3);
        }

        static IEnumerator Tap(UnityEngine.InputSystem.Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return Frames(2);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
        }

        static IEnumerator MouseButtonTap(MouseButton button)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(button));
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return Frames(2);
        }

        static void OpenPause() => typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), null);

        static void ClosePause() => typeof(PauseMenuUI).GetMethod("CloseMenu", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), new object[] { false });

        // ---------- 1 a 3: posse e recarga ----------

        static IEnumerator TabletGrants()
        {
            yield return ToMoment("passo8.nicho");
            Check(!PowerBook.Has(Eco), "antes de ler a tabuleta a Elena não deveria ter o Eco");
            var tablet = Load<TabletDefinition>("Tablets/TabuletaDoJunco");
            var reader = Motor().GetComponent<TabletReader>();
            int changes = 0;
            Action count = () => changes++;
            PowerBook.Changed += count;
            reader.Read(tablet);
            yield return Frames(3);
            string footer = ReadingText("footerLabel");
            Check(PowerBook.Has(Eco), "depois de ler a tabuleta a Elena deveria ter o Eco");
            Check(footer.Contains("Novo poder: Eco do Abzu"), $"o rodapé deveria anunciar o Eco: {footer}");
            Check(footer.Contains("Transformação +5"), $"o rodapé deveria mostrar a transformação: {footer}");
            CloseReading();
            yield return new Delay(0.5f);
            reader.Read(tablet);
            yield return Frames(3);
            Check(!ReadingText("footerLabel").Contains("Novo poder"), "relendo, o rodapé não deveria anunciar o poder de novo");
            CloseReading();
            PowerBook.Changed -= count;
            Check(changes == 1, $"o Eco deveria ser dado uma vez só: {changes} avisos");
            Check(WorldState.PowerCodes.Count() == 1, $"a Elena deveria ter só o Eco: {string.Join(", ", WorldState.PowerCodes)}");
            Check(!WorldState.HasStoryEvent(Eco.LegacyEventId), "o evento antigo Poder:EcoDoAbzu não deveria ser marcado");
            yield return new Delay(0.5f);
        }

        static IEnumerator CheckpointKeeps()
        {
            // Continua no nicho, com o Eco do cenário anterior.
            Hook();
            PowerBook.StartCooldown(Eco);
            yield return new Delay(1f);
            float atSave = PowerBook.CooldownLeft(Eco);
            yield return SaveAndReload();
            Hook();
            Check(PowerBook.Has(Eco), "depois de voltar do checkpoint a Elena deveria ter o Eco");
            // Hook zera as recargas; confere pelo checkpoint guardado.
            WorldState.Snapshot saved = Checkpoints.Last.world;
            string described = WorldState.Describe(saved);
            Check(described.Contains("poder " + EcoCode), "o checkpoint deveria guardar o Eco");
            Check(described.Contains("recarga " + EcoCode), $"o checkpoint deveria guardar a recarga do Eco ({atSave:0.0} s): {described.Split('\n').Where(l => l.StartsWith("recarga") || l.StartsWith("poder")).FirstOrDefault()}");
        }

        static IEnumerator GameTimeOnly()
        {
            Hook();
            PowerBook.StartCooldown(Eco);
            float start = PowerBook.CooldownLeft(Eco);
            OpenPause();
            yield return new Delay(1.2f);
            Check(Mathf.Approximately(PowerBook.CooldownLeft(Eco), start), $"na pausa a recarga não deveria andar: {start:0.00} → {PowerBook.CooldownLeft(Eco):0.00}");
            ClosePause();
            yield return new Delay(0.3f);

            var inventory = Object.FindFirstObjectByType<InventoryScreenUI>();
            float before = PowerBook.CooldownLeft(Eco);
            inventory.SetOpen(true);
            yield return new Delay(1.2f);
            Check(PowerBook.CooldownLeft(Eco) >= before - 0.05f, $"no inventário a recarga não deveria andar: {before:0.00} → {PowerBook.CooldownLeft(Eco):0.00}");
            inventory.SetOpen(false);
            yield return new Delay(0.3f);

            before = PowerBook.CooldownLeft(Eco);
            Reading().Show("Teste", "Texto de teste.", "");
            yield return new Delay(1.2f);
            Check(PowerBook.CooldownLeft(Eco) >= before - 0.05f, $"na leitura a recarga não deveria andar: {before:0.00} → {PowerBook.CooldownLeft(Eco):0.00}");
            CloseReading();
            yield return new Delay(0.3f);

            before = PowerBook.CooldownLeft(Eco);
            DialogueRunner runner = Runner();
            Check(runner.Play(Load<DialogueDefinition>("Dialogues/Aldeia_AMarca")), "a conversa de teste deveria começar");
            yield return new Delay(1.2f);
            Check(PowerBook.CooldownLeft(Eco) >= before - 0.05f, $"no diálogo a recarga não deveria andar: {before:0.00} → {PowerBook.CooldownLeft(Eco):0.00}");
            yield return FinishDialogue();

            before = PowerBook.CooldownLeft(Eco);
            yield return new Delay(1f);
            float walked = before - PowerBook.CooldownLeft(Eco);
            Check(walked > 0.7f && walked < 1.3f, $"jogando, a recarga deveria andar ~1 s em 1 s: andou {walked:0.00} s");
        }

        // ---------- 4 a 10: a regra do LB ----------

        static IEnumerator LbTap()
        {
            yield return ToVillage();
            Pad(GamepadButton.LeftShoulder);
            yield return Frames(6);
            Check(Count(Eco) == 0, "apertar o LB não deveria disparar o Eco (só ao soltar)");
            Check(Reader().PowerModifierHeldSeconds > 0f, "com o LB seguro o leitor deveria contar o tempo");
            yield return PadRelease();
            Check(Count(Eco) == 1, $"soltar o LB deveria disparar o Eco uma vez: {Count(Eco)}");
            Check(Count(Slot2) + Count(Slot3) == 0, "o toque no LB não deveria disparar outra vaga");
            if (fired.Count > 0)
            {
                float ms = fired[0].latency * 1000f;
                Check(ms > 0f && ms < 500f, $"o atraso do Eco deveria ser o tempo do toque: {ms:0} ms");
                Debug.Log($"Poderes: atraso medido do Eco ao soltar o LB (toque de 6 quadros): {ms:0} ms.");
            }
        }

        static IEnumerator LbRb()
        {
            Hook();
            var melee = Motor().GetComponent<PlayerMeleeAttack>();
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.RightShoulder);
            Check(Count(Slot2) == 1, $"LB+RB deveria disparar a vaga 2: {Count(Slot2)}");
            Check(!melee.IsAttacking, "com o LB seguro o RB não deveria virar faca");
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadRelease();
            Check(Count(Eco) == 0, "depois de LB+RB, soltar o LB não deveria disparar o Eco");
            Check(!melee.IsAttacking, "soltar os botões não deveria dar facada");
        }

        static IEnumerator LbY()
        {
            Hook();
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.North);
            Check(Count(Slot3) == 1, $"LB+Y deveria disparar a vaga 3: {Count(Slot3)}");
            yield return PadRelease();
            Check(Count(Eco) == 0, "depois de LB+Y, soltar o LB não deveria disparar o Eco");
        }

        static IEnumerator RbAlone()
        {
            Hook();
            yield return new Delay(0.8f);
            var melee = Motor().GetComponent<PlayerMeleeAttack>();
            yield return PadPress(GamepadButton.RightShoulder);
            Check(melee.IsAttacking, "RB sozinho deveria dar a facada");
            yield return PadRelease();
            Check(fired.Count == 0, $"RB sozinho não deveria disparar poder: {fired.Count}");
            yield return new Delay(1f);
        }

        static IEnumerator LbDodge()
        {
            Hook();
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.East);
            Check(Motor().IsDodging, "LB seguro + B deveria esquivar");
            yield return PadRelease();
            Check(Count(Eco) == 0, "depois de outro botão, soltar o LB não deveria disparar o Eco");
            yield return new Delay(1f);
        }

        static IEnumerator LbLongHold()
        {
            Hook();
            Pad(GamepadButton.LeftShoulder);
            yield return new Delay(0.7f);
            yield return PadRelease();
            Check(Count(Eco) == 0, "segurar o LB 0,7 s e soltar não deveria disparar o Eco");
            Pad(GamepadButton.LeftShoulder);
            yield return new Delay(0.3f);
            yield return PadRelease();
            Check(Count(Eco) == 1, "segurar 0,3 s e soltar deveria disparar o Eco (dentro do limite de 0,5 s)");
        }

        static IEnumerator Aiming()
        {
            Hook();
            yield return PadPress(GamepadButton.LeftTrigger);
            yield return new Delay(0.4f);
            Check(Motor().Aiming, "com o LT seguro e a pistola, a Elena deveria mirar");
            yield return PadPress(GamepadButton.LeftTrigger, GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftTrigger);
            Check(Count(Eco) == 1, $"mirando, o toque no LB deveria disparar o Eco: {Count(Eco)}");
            yield return PadPress(GamepadButton.LeftTrigger, GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftTrigger, GamepadButton.LeftShoulder, GamepadButton.RightShoulder);
            Check(Count(Slot2) == 1, $"mirando, LB+RB deveria disparar a vaga 2: {Count(Slot2)}");
            yield return PadPress(GamepadButton.LeftTrigger);
            int before = fired.Count;
            var melee = Motor().GetComponent<PlayerMeleeAttack>();
            yield return PadPress(GamepadButton.LeftTrigger, GamepadButton.RightShoulder);
            Check(fired.Count == before && !melee.IsAttacking, "mirando, RB sozinho não deveria fazer nada");
            yield return PadRelease();
            yield return new Delay(0.5f);
        }

        // ---------- 11 a 14 ----------

        static IEnumerator KeyboardMouse()
        {
            Hook();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Q));
            yield return Frames(2);
            Check(Count(Eco) == 1, $"Q deveria disparar o Eco ao apertar: {Count(Eco)}");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
            yield return Tap(UnityEngine.InputSystem.Key.Z);
            Check(Count(Slot2) == 1, $"Z deveria disparar a vaga 2: {Count(Slot2)}");
            yield return Tap(UnityEngine.InputSystem.Key.X);
            Check(Count(Slot3) == 1, $"X deveria disparar a vaga 3: {Count(Slot3)}");
            Hook();
            yield return MouseButtonTap(MouseButton.Back);
            Check(Count(Slot2) == 1, $"o botão lateral 4 do mouse deveria disparar a vaga 2: {Count(Slot2)}");
            yield return MouseButtonTap(MouseButton.Forward);
            Check(Count(Slot3) == 1, $"o botão lateral 5 do mouse deveria disparar a vaga 3: {Count(Slot3)}");
            Check(Reader().PowerSlotLabel(PowerSlot.Poder1) == "Q", $"o rótulo do Eco no teclado deveria ser Q: {Reader().PowerSlotLabel(PowerSlot.Poder1)}");
        }

        static IEnumerator NotOwned()
        {
            Hook();
            PowerBook.Revoke(Slot2);
            var melee = Motor().GetComponent<PlayerMeleeAttack>();
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.RightShoulder);
            Check(fired.Count == 0, $"sem o poder da vaga 2, LB+RB não deveria disparar nada: {fired.Count}");
            Check(!melee.IsAttacking, "sem o poder da vaga 2, LB+RB não deveria virar faca");
            yield return PadRelease();
            Check(Count(Eco) == 0, "e soltar o LB não deveria disparar o Eco");
            PowerBook.Grant(Slot2);
            yield return new Delay(0.5f);
        }

        static IEnumerator CooldownBlocks()
        {
            Hook();
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            yield return new Delay(0.3f);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            Check(Count(Eco) == 1, $"dentro da recarga o Eco não deveria disparar de novo: {Count(Eco)}");
            Check(PowerBook.CooldownLeft(Eco) > Eco.cooldown - 1f, $"a recarga deveria ter começado cheia: {PowerBook.CooldownLeft(Eco):0.0}");
        }

        static IEnumerator Blocked()
        {
            Hook();
            DialogueRunner runner = Runner();
            Check(runner.Play(Load<DialogueDefinition>("Dialogues/Aldeia_AMarca")), "a conversa de teste deveria começar");
            yield return Frames(3);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadRelease();
            Check(fired.Count == 0, "no diálogo nenhum poder deveria disparar");
            yield return FinishDialogue();
            yield return new Delay(0.5f);

            OpenPause();
            yield return Frames(3);
            yield return Tap(UnityEngine.InputSystem.Key.Z);
            Check(fired.Count == 0, "na pausa nenhum poder deveria disparar");
            ClosePause();
            yield return new Delay(0.4f);

            var inventory = Object.FindFirstObjectByType<InventoryScreenUI>();
            inventory.SetOpen(true);
            yield return Frames(3);
            yield return Tap(UnityEngine.InputSystem.Key.X);
            Check(fired.Count == 0, "no inventário nenhum poder deveria disparar");
            inventory.SetOpen(false);
            yield return new Delay(0.4f);

            Reading().Show("Teste", "Texto de teste.", "");
            yield return Frames(3);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            Check(fired.Count == 0, "na leitura nenhum poder deveria disparar");
            CloseReading();
            yield return new Delay(0.6f);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            Check(Count(Eco) == 1, "depois dos menus o Eco deveria voltar a funcionar");
        }

        // ---------- 15: zoom no D-pad ----------

        static IEnumerator DpadZoom()
        {
            Hook();
            PlayerCameraController c = Object.FindFirstObjectByType<PlayerCameraController>();
            c.SetZoomTarget(3f);
            yield return Frames(2);
            yield return PadPress(GamepadButton.DpadUp);
            yield return PadRelease();
            Check(Mathf.Abs(c.ZoomTarget - 2.75f) < 0.01f, $"D-pad para cima deveria aproximar 0,25 m: {c.ZoomTarget}");
            yield return PadPress(GamepadButton.DpadDown);
            yield return PadRelease();
            Check(Mathf.Abs(c.ZoomTarget - 3f) < 0.01f, $"D-pad para baixo deveria afastar 0,25 m: {c.ZoomTarget}");
            Pad(GamepadButton.DpadDown);
            yield return new Delay(0.5f);
            yield return PadRelease();
            Check(c.ZoomTarget >= 3.5f - 0.01f, $"segurar o D-pad deveria repetir (a cada 0,15 s): {c.ZoomTarget}");

            c.SetZoomTarget(3f);
            yield return PadPress(GamepadButton.LeftTrigger);
            yield return new Delay(0.3f);
            yield return PadPress(GamepadButton.LeftTrigger, GamepadButton.DpadUp);
            yield return PadPress(GamepadButton.LeftTrigger);
            Check(Mathf.Abs(c.ZoomTarget - 3f) < 0.01f, $"mirando o D-pad não deveria mudar o zoom: {c.ZoomTarget}");
            yield return PadRelease();

            OpenPause();
            yield return Frames(3);
            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            string before = pause.ScreenText;
            yield return PadPress(GamepadButton.DpadDown);
            yield return PadRelease();
            Check(Mathf.Abs(c.ZoomTarget - 3f) < 0.01f, $"na pausa o D-pad não deveria mudar o zoom: {c.ZoomTarget}");
            Check(pause.ScreenText != before, "na pausa o D-pad deveria continuar navegando o menu");
            ClosePause();
            yield return new Delay(0.4f);
            c.SetZoomTarget(2.5f);
        }

        // ---------- 16: plano B ----------

        static IEnumerator SchemeB()
        {
            PlayerInputReader.SchemeOverride = PlayerInputReader.EcoScheme.YSozinho;
            try
            {
                Hook();
                Check(Reader().CurrentEcoScheme == PlayerInputReader.EcoScheme.YSozinho, "o plano B deveria estar valendo");
                yield return PadPress(GamepadButton.North);
                Check(Count(Eco) == 1, $"no plano B, Y sozinho deveria disparar o Eco ao apertar: {Count(Eco)}");
                if (fired.Count > 0) Check(fired[0].latency < 0.001f, $"no plano B o Eco sai na hora: {fired[0].latency * 1000f:0} ms");
                yield return PadRelease();
                Hook();
                yield return PadPress(GamepadButton.LeftShoulder);
                yield return PadRelease();
                Check(fired.Count == 0, "no plano B, LB sozinho não deveria fazer nada");
                yield return PadPress(GamepadButton.LeftShoulder);
                yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.North);
                Check(Count(Slot3) == 1 && Count(Eco) == 0, "no plano B, LB+Y deveria disparar a vaga 3 e não o Eco");
                yield return PadPress(GamepadButton.LeftShoulder);
                yield return PadPress(GamepadButton.LeftShoulder, GamepadButton.RightShoulder);
                Check(Count(Slot2) == 1, "no plano B, LB+RB deveria disparar a vaga 2");
                yield return PadRelease();
                Check(Reader().PowerSlotLabel(PowerSlot.Poder1) != "LB", "no plano B o rótulo do Eco não deveria ser LB");
            }
            finally
            {
                PlayerInputReader.SchemeOverride = null;
            }
        }

        // ---------- 17: memória e viagem ----------

        static IEnumerator MemoryAndTruck()
        {
            yield return ToMoment("passo8.memoria", 1f);
            var memory = Object.FindFirstObjectByType<PlayableMemory>();
            yield return new Until(() => memory != null && typeof(PlayableMemory).GetField("state", Any).GetValue(memory).ToString() == "InMemory", 40f, "a memória começar");
            yield return new Delay(4f);
            Hook();
            PowerBook.Grant(Eco);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadRelease();
            Check(fired.Count == 0, "na memória nenhum poder deveria disparar");

            // Termina a memória pelo evento dela (como o teste do zoom), senão o atalho seguinte não recarrega a região.
            var definition = (MemoryDefinition)typeof(PlayableMemory).GetField("memory", Any).GetValue(memory);
            StoryEvents.Raise(definition.endOn);
            yield return new Until(() => typeof(PlayableMemory).GetField("state", Any).GetValue(memory).ToString() == "Present", 40f, "a volta ao presente");
            yield return new Delay(1f);

            yield return ToMoment("passo15.meio_ficou", 2.5f);
            yield return Tap(UnityEngine.InputSystem.Key.Q);
            yield return PadPress(GamepadButton.LeftShoulder);
            yield return PadRelease();
            Check(fired.Count == 0, "na viagem de caminhonete nenhum poder deveria disparar");
        }

        static IEnumerator Cleanup()
        {
            Check(SaveStorage.RealFingerprint() == AutomatedRun.StartFingerprint, "os slots reais não deveriam mudar durante o teste");
            PlayerInputReader.SchemeOverride = null;
            if (hooked != null) hooked.Activated -= OnFired;
            hooked = null;
            foreach (InputDevice device in new InputDevice[] { pad, keyboard, mouse })
                if (device != null) InputSystem.RemoveDevice(device);
            pad = null;
            keyboard = null;
            mouse = null;
            if (realKeyboard != null) realKeyboard.MakeCurrent();
            if (realMouse != null) realMouse.MakeCurrent();
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder != null && folder.EndsWith("Teste_Poderes")) SaveStorage.DeleteTestFolder(folder);
            yield break;
        }
    }
}
