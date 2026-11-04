using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nibiru.Cameras;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Localization;
using Nibiru.Memories;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using InputMouse = UnityEngine.InputSystem.Mouse;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Cameras
{
    /// <summary>
    /// Teste automático do zoom da câmera (roda do mouse e opção "Distância da câmera"). O mouse é um mouse virtual do
    /// Input System, então a roda chega ao jogo pelo mesmo caminho da roda de verdade. Cobre: direção, passo e inversão da
    /// roda; limites mínimo e máximo; mira ignorando o zoom e voltando ao soltar; diálogo, pausa, Arquivo, inventário e
    /// leitura bloqueando a roda; colisão com o zoom nos extremos (túnel, antecâmara, Poço, quarto escuro); pulos e tremor ao
    /// rolar a roda junto de paredes (só registrados); persistência e limites do valor guardado; a opção das Opções igual à
    /// roda, inclusive depois de recarregar; memória jogável e viagem de caminhonete sem zoom; Modo livre.
    /// Mexe só numa pasta de teste e devolve as preferências do jogador. Menu: Nibiru > Testes > Câmera > Zoom.
    /// </summary>
    [InitializeOnLoad]
    static class ZoomTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const float FeetToCenter = 1.05f;
        const float Tol = 0.03f;

        static string folder;
        static InputMouse realMouse, virtualMouse;
        static bool rightHeld;

        static bool hadStored;
        static float storedDistance;
        static bool savedInvertWheel;
        static float savedMouseSensitivity;

        static Vector3 clearFeet;
        static float clearYaw;
        static bool haveClear;
        static readonly StringBuilder notes = new StringBuilder();

        static ZoomTests() => Register("zoom.teste", Begin);

        [MenuItem("Nibiru/Testes/Câmera/Zoom: teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("zoom.teste");

        static void Begin()
        {
            folder = Nibiru.Save.SaveStorage.TestFolder("Teste_Zoom");
            Nibiru.Save.SaveStorage.DeleteTestFolder(folder);
            SaveSlots.UseFolder(folder);
            hadStored = PlayerPrefs.HasKey(GameOptions.CameraDistanceKey);
            storedDistance = PlayerPrefs.GetFloat(GameOptions.CameraDistanceKey, 0f);
            savedMouseSensitivity = GameOptions.MouseSensitivity;
            notes.Clear();
            haveClear = false;
            rightHeld = false;

            realMouse = InputMouse.current;
            virtualMouse = InputSystem.AddDevice<InputMouse>("MouseDeTesteZoom");
            virtualMouse.MakeCurrent();
            // Os textos conferidos são os do pt-BR, seja qual for o idioma escolhido nas Opções.
            Loc.SetLanguage("pt-BR", false);

            Start("Zoom da câmera, teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Roda: para a frente aproxima, passo de 0,25 m por clique, frações e inversão", WheelDirection),
                ("Limites: mínimo 1,8 m e máximo 4,0 m (medidos na câmera, em campo aberto) e a câmera fora da cabeça", Limits),
                ("Mira ignora o zoom (1,6 m e 0,9 m) e volta à distância escolhida ao soltar", AimIgnoresZoom),
                ("Diálogo: a roda não faz zoom e a distância é a mesma depois", Dialogue),
                ("Menus: pausa, Arquivo, inventário e leitura bloqueiam a roda", Menus),
                ("Opção \"Distância da câmera\": igual à roda, nos dois sentidos, inclusive depois de recarregar", OptionMatchesWheel),
                ("Persistência: grava 1 s depois de parar, volta ao recarregar e limita valores fora da faixa", Persistence),
                ("Colisão com o zoom nos extremos: antecâmara, Poço, quarto escuro (nunca dentro de parede)", CollisionHall),
                ("Pulos e tremor ao rolar a roda junto de paredes (só registra)", Jitter),
                ("Colisão com o zoom nos extremos: túnel e rampa dos Guardiões (passo 13)", CollisionTunnel),
                ("Modo livre: a roda funciona na boca do Poço e no wadi", FreeRoam),
                ("Memória jogável: sem zoom, e o zoom volta ao voltar ao presente", Memory),
                ("Viagem de caminhonete: sem zoom", Truck),
                ("Limpeza: devolve as preferências e apaga a pasta do teste", Cleanup),
            });
        }

        // ---------- Ajudas ----------

        static Until Frames(int count)
        {
            int target = Time.frameCount + count;
            return new Until(() => Time.frameCount >= target, 10f, $"{count} quadros");
        }

        static PlayerCameraController Ctl() => Object.FindFirstObjectByType<PlayerCameraController>();

        static ThirdPersonCameraMode Mode() => Object.FindFirstObjectByType<ThirdPersonCameraMode>();

        static PauseMenuUI Pause() => Object.FindFirstObjectByType<PauseMenuUI>();

        static bool Approx(float a, float b, float tolerance = Tol) => Mathf.Abs(a - b) <= tolerance;

        // Vai para um atalho (que recarrega a região) e espera o mundo arrumar.
        static IEnumerator ToMoment(string id, float settle = 3.5f)
        {
            int old = Motor() != null ? Motor().GetInstanceID() : 0;
            Go(id);
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 60f, $"o atalho {id} recarregar a região");
            yield return new Delay(settle);
        }

        static void SendMouse(float notches)
        {
            var state = new MouseState { scroll = new Vector2(0f, notches * PlayerCameraController.WheelUnitsPerNotch) };
            if (rightHeld) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(virtualMouse, state);
        }

        // Um movimento da roda (positivo = para a frente). Depois zera, para a roda não repetir.
        static IEnumerator Wheel(float notches)
        {
            SendMouse(notches);
            yield return Frames(2);
            SendMouse(0f);
            yield return Frames(2);
        }

        static IEnumerator HoldRight(bool held)
        {
            rightHeld = held;
            SendMouse(0f);
            yield return Frames(2);
        }

        // Distância da câmera ao ponto do ombro (de onde ela recua), como o ThirdPersonCameraMode a calcula.
        static float BackDistance(bool aiming)
        {
            CameraSettings s = Ctl().Settings;
            Transform cam = Ctl().transform;
            float shoulder = aiming ? s.aimShoulderOffset : s.shoulderOffset;
            Vector3 point = Motor().transform.position + Vector3.up * s.pivotHeight + cam.right * shoulder;
            return Vector3.Distance(cam.position, point);
        }

        static bool IsCharacter(Collider c) => c.GetComponentInParent<Health>() != null || c.GetComponentInParent<CharacterController>() != null;

        // Há 4,3 m livres atrás do ombro nesta posição e direção? (campo aberto para medir o zoom no máximo)
        static bool ClearBehind(Vector3 feet, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(Ctl().Settings.startPitch, yaw, 0f);
            Vector3 pivot = feet + Vector3.up * (FeetToCenter + Ctl().Settings.pivotHeight);
            Vector3 shoulder = pivot + rotation * Vector3.right * Ctl().Settings.shoulderOffset;
            Vector3 back = rotation * Vector3.back;
            foreach (RaycastHit hit in Physics.SphereCastAll(shoulder, 0.35f, back, 4.4f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(Motor().transform.root) && !IsCharacter(hit.collider)) return false;
            foreach (RaycastHit hit in Physics.SphereCastAll(pivot, 0.35f, rotation * Vector3.right, Ctl().Settings.shoulderOffset + 0.2f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(Motor().transform.root) && !IsCharacter(hit.collider)) return false;
            return true;
        }

        static Vector3 OnGround(Vector3 spot) =>
            UnityEngine.AI.NavMesh.SamplePosition(new Vector3(spot.x, spot.y + 2f, spot.z), out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : spot;

        // Procura, entre alguns lugares abertos do mapa, um com espaço atrás da Elena para medir o zoom máximo.
        static bool FindClearSpot()
        {
            if (haveClear) return true;
            var spots = new[]
            {
                Step8Shortcuts.NicheSpot, new Vector3(8f, 0f, -108f), new Vector3(60f, 0f, 170.2f), new Vector3(-26f, 0f, -5f),
                new Vector3(44f, 0f, 62f), new Vector3(0f, 0f, -80f), new Vector3(-40f, 0f, -60f), new Vector3(30f, 0f, 120f),
            };
            foreach (Vector3 spot in spots)
            {
                Vector3 feet = OnGround(spot);
                for (int yaw = 0; yaw < 360; yaw += 45)
                    if (ClearBehind(feet, yaw))
                    {
                        clearFeet = feet;
                        clearYaw = yaw;
                        haveClear = true;
                        return true;
                    }
            }
            return false;
        }

        static IEnumerator GoToClearSpot()
        {
            Check(FindClearSpot(), "não achei nenhum lugar aberto (4,4 m livres atrás do ombro) para medir o zoom");
            if (!haveClear) yield break;
            Teleport(clearFeet + Vector3.up * FeetToCenter, clearYaw);
            yield return new Delay(0.4f);
        }

        // ---------- 1. Roda ----------

        static IEnumerator WheelDirection()
        {
            yield return ToMoment("livre.aldeia");
            PlayerCameraController c = Ctl();
            savedInvertWheel = c.Settings.zoomInvertWheel;
            Check(Cursor.lockState == CursorLockMode.Locked, $"o cursor deveria estar preso no jogo para a roda valer ({Cursor.lockState})");
            Check(Approx(c.Settings.zoomMinDistance, 1.8f) && Approx(c.Settings.zoomMaxDistance, 4f) && Approx(c.Settings.distance, 2.5f),
                $"faixa do asset deveria ser 1,8 / 2,5 / 4,0: {c.Settings.zoomMinDistance} / {c.Settings.distance} / {c.Settings.zoomMaxDistance}");
            Check(c.ZoomAllowed, "com a Elena livre a roda deveria valer");
            // O valor real da roda: o padrão do projeto normaliza para 1 por clique (o teste antigo mandava 120 e não via isso).
            Check(InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms,
                "o projeto deveria usar o padrão do Input System (roda normalizada, 1 por clique)");
            Check(Approx(PlayerCameraController.WheelUnitsPerNotch, 1f), $"com a roda normalizada, 1 clique deveria valer 1: {PlayerCameraController.WheelUnitsPerNotch}");
            try
            {
                c.Settings.zoomInvertWheel = false;
                c.SetZoomTarget(2.5f);
                yield return Frames(2);
                InputSystem.QueueStateEvent(virtualMouse, new MouseState { scroll = new Vector2(0f, 1f) }); // um clique de roda de verdade
                yield return Frames(2);
                InputSystem.QueueStateEvent(virtualMouse, new MouseState());
                yield return Frames(2);
                Check(Approx(c.ZoomTarget, 2.25f), $"um clique de roda real (valor 1) deveria aproximar 0,25 m, de 2,5 para 2,25: {c.ZoomTarget}");
                c.Settings.zoomInvertWheel = false;
                c.SetZoomTarget(2.5f);
                yield return Frames(2);
                yield return Wheel(1f);
                Check(Approx(c.ZoomTarget, 2.25f), $"1 clique para a frente deveria aproximar para 2,25: {c.ZoomTarget}");
                yield return Wheel(-2f);
                Check(Approx(c.ZoomTarget, 2.75f), $"2 cliques para trás deveriam afastar para 2,75: {c.ZoomTarget}");
                yield return Wheel(1f);
                Check(Approx(c.ZoomTarget, 2.5f), $"de volta a 2,5: {c.ZoomTarget}");
                yield return Wheel(0.5f);
                Check(Approx(c.ZoomTarget, 2.375f), $"meio clique (roda de alta resolução) deveria dar 2,375: {c.ZoomTarget}");

                c.SetZoomTarget(2.5f);
                c.Settings.zoomInvertWheel = true;
                yield return Wheel(1f);
                Check(Approx(c.ZoomTarget, 2.75f), $"invertida, 1 clique para a frente deveria afastar para 2,75: {c.ZoomTarget}");
            }
            finally
            {
                c.Settings.zoomInvertWheel = savedInvertWheel;
            }
            c.SetZoomTarget(2.5f);
        }

        // ---------- 2. Limites ----------

        static IEnumerator Limits()
        {
            yield return GoToClearSpot();
            if (!haveClear) yield break;
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(2.5f);

            yield return Wheel(30f);
            Check(Approx(c.ZoomTarget, 1.8f, 0.001f), $"30 cliques para a frente deveriam parar no mínimo 1,8: {c.ZoomTarget}");
            yield return new Delay(1.4f);
            float near = BackDistance(false);
            Vector3 pivot = Motor().transform.position + Vector3.up * c.Settings.pivotHeight;
            float toHead = Vector3.Distance(c.transform.position, pivot);
            Check(Approx(near, 1.8f, 0.1f), $"no mínimo a câmera deveria ficar a 1,8 m do ombro: {near:0.00}");
            Check(toHead >= 1.7f, $"a câmera deveria ficar longe da cabeça: {toHead:0.00} m do ponto seguido");

            yield return Wheel(-30f);
            Check(Approx(c.ZoomTarget, 4f, 0.001f), $"30 cliques para trás deveriam parar no máximo 4,0: {c.ZoomTarget}");
            yield return new Delay(1.6f);
            float far = BackDistance(false);
            Check(Approx(far, 4f, 0.1f), $"no máximo a câmera deveria ficar a 4,0 m do ombro: {far:0.00}");
        }

        // ---------- 3. Mira ----------

        static IEnumerator AimIgnoresZoom()
        {
            yield return GoToClearSpot();
            if (!haveClear) yield break;
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(4f);
            yield return new Delay(1.5f);
            Check(Approx(c.ZoomTarget, 4f, 0.001f), $"antes de mirar o zoom escolhido deveria ser 4,0: {c.ZoomTarget}");
            Check(Approx(BackDistance(false), 4f, 0.1f), $"antes de mirar a câmera deveria estar a 4,0: {BackDistance(false):0.00}");

            yield return HoldRight(true);
            yield return new Delay(1.4f);
            Check(c.IsAiming, "com o botão direito apertado a Elena deveria estar mirando");
            Check(Approx(BackDistance(true), c.Settings.aimDistance, 0.1f), $"mirando, a câmera deveria ir para {c.Settings.aimDistance} m (a mira ignora o zoom): {BackDistance(true):0.00}");
            Check(!c.ZoomAllowed, "mirando, a roda não deveria valer");
            float beforeWheel = c.ZoomTarget;
            yield return Wheel(-3f);
            Check(Approx(c.ZoomTarget, beforeWheel, 0.0001f), $"mirando, a roda não deveria mudar o zoom escolhido: {beforeWheel} → {c.ZoomTarget} (mouse atual: {InputMouse.current?.name})");
            yield return new Delay(0.3f);
            Check(Approx(BackDistance(true), c.Settings.aimDistance, 0.1f), $"mirando, rolar a roda não deveria mexer na câmera: {BackDistance(true):0.00}");

            yield return HoldRight(false);
            yield return new Delay(1.6f);
            Check(!c.IsAiming, "soltando, a Elena deveria parar de mirar");
            Check(Approx(BackDistance(false), 4f, 0.1f), $"soltando, a câmera deveria voltar a 4,0: {BackDistance(false):0.00}");
            c.SetZoomTarget(2.5f);
        }

        // ---------- 4. Diálogo ----------

        static IEnumerator Dialogue()
        {
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(3f);
            yield return Frames(2);
            DialogueRunner runner = Runner();
            var dialogue = Load<DialogueDefinition>("Dialogues/Aldeia_AMarca");
            Check(dialogue != null && runner.Play(dialogue), "a conversa de teste deveria começar");
            yield return Frames(3);
            Check(runner.IsRunning, "a conversa deveria estar aberta");
            Check(!c.ZoomAllowed, "em conversa a roda não deveria valer");
            yield return Wheel(3f);
            yield return Wheel(-5f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"em conversa a roda não deveria mudar o zoom: {c.ZoomTarget}");
            yield return FinishDialogue();
            yield return new Delay(0.5f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"depois da conversa a distância escolhida deveria ser a mesma: {c.ZoomTarget}");
            Check(c.ZoomAllowed, "depois da conversa a roda deveria voltar a valer");
            yield return Wheel(1f);
            Check(Approx(c.ZoomTarget, 2.75f), $"depois da conversa a roda deveria funcionar: {c.ZoomTarget}");
            c.SetZoomTarget(2.5f);
        }

        // ---------- 5. Menus ----------

        static Vector2 RowCenter(PauseMenuUI pause, int row)
        {
            pause.TryGetRowRect(row, out Rect rect);
            return rect.center;
        }

        static void OpenPause() => typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(Pause(), null);

        static void ClosePause() => typeof(PauseMenuUI).GetMethod("CloseMenu", Any).Invoke(Pause(), new object[] { false });

        static IEnumerator Menus()
        {
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(3f);

            // Pausa, e o Arquivo dentro dela (lá a roda é da lista).
            PauseMenuUI pause = Pause();
            OpenPause();
            yield return Frames(3);
            Check(!c.ZoomAllowed, "com a pausa aberta a roda não deveria valer");
            yield return Wheel(2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"na pausa a roda não deveria mudar o zoom: {c.ZoomTarget}");
            pause.PointerStep(new MenuPointer.State { position = RowCenter(pause, 2), leftClick = true });
            Check(pause.PageName == "Arquivo", $"deveria estar no Arquivo: {pause.PageName}");
            yield return Wheel(-2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"no Arquivo a roda é da lista, não do zoom: {c.ZoomTarget}");
            ClosePause();
            yield return new Delay(0.4f);
            Check(c.ZoomAllowed, "fechando a pausa a roda deveria voltar a valer");

            // Inventário.
            var inventory = Object.FindFirstObjectByType<InventoryScreenUI>();
            inventory.SetOpen(true);
            yield return Frames(3);
            Check(!c.ZoomAllowed, "com o inventário aberto a roda não deveria valer");
            yield return Wheel(2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"no inventário a roda não deveria mudar o zoom: {c.ZoomTarget}");
            inventory.SetOpen(false);
            yield return new Delay(0.4f);

            // Leitura (documento, tabuleta, relevo).
            Reading().Show("Teste", "Texto de teste.", "");
            yield return Frames(3);
            Check(!c.ZoomAllowed, "com a leitura aberta a roda não deveria valer");
            yield return Wheel(2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"na leitura a roda não deveria mudar o zoom: {c.ZoomTarget}");
            CloseReading();
            yield return new Delay(0.6f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"depois dos menus a distância escolhida deveria ser a mesma: {c.ZoomTarget}");
            c.SetZoomTarget(2.5f);
        }

        // ---------- 6. Opção das Opções ----------

        static IEnumerator OptionMatchesWheel()
        {
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(2.5f);
            yield return Wheel(-2f); // roda: 3,0 m
            Check(Approx(c.ZoomTarget, 3f), $"a roda deveria deixar em 3,0: {c.ZoomTarget}");

            PauseMenuUI pause = Pause();
            OpenPause();
            yield return Frames(3);
            pause.PointerStep(new MenuPointer.State { position = RowCenter(pause, 1), leftClick = true });
            Check(pause.PageName == "Opcoes", $"deveria estar nas Opções: {pause.PageName}");
            Check(Approx(pause.CameraDistanceNow(), 3f), $"a opção deveria mostrar o valor da roda (3,0): {pause.CameraDistanceNow()}");
            Check(pause.ScreenText.Contains("Distância da câmera") && pause.ScreenText.Contains("3,00 m"), "a linha da opção deveria mostrar \"Distância da câmera\" e \"3,00 m\"");

            // Opção → roda: o lado direito da seta afasta 0,25 m.
            Check(pause.TryGetOptionHalf(3, 1, out Rect right) && pause.TryGetOptionHalf(3, -1, out Rect left), "as duas metades da seta da distância deveriam existir");
            pause.PointerStep(new MenuPointer.State { position = right.center, leftClick = true });
            Check(Approx(c.ZoomTarget, 3.25f), $"a seta direita deveria afastar para 3,25: {c.ZoomTarget}");
            pause.TryGetOptionHalf(3, -1, out left);
            pause.PointerStep(new MenuPointer.State { position = left.center, leftClick = true });
            pause.PointerStep(new MenuPointer.State { position = left.center, leftClick = true });
            Check(Approx(c.ZoomTarget, 2.75f), $"duas setas esquerdas deveriam aproximar para 2,75: {c.ZoomTarget}");
            Check(pause.ScreenText.Contains("2,75 m"), "a tela da opção deveria mostrar 2,75 m");

            // Limites da opção.
            for (int i = 0; i < 12; i++) pause.PointerStep(new MenuPointer.State { position = left.center, leftClick = true });
            Check(Approx(c.ZoomTarget, 1.8f, 0.001f), $"a opção não deveria passar do mínimo: {c.ZoomTarget}");
            // Fechar a pausa e rolar a roda: a opção reflete na hora.
            ClosePause();
            yield return new Delay(0.4f);
            yield return Wheel(-4f); // 1,8 + 1,0 = 2,8
            OpenPause();
            yield return Frames(3);
            pause.PointerStep(new MenuPointer.State { position = RowCenter(pause, 1), leftClick = true });
            Check(Approx(pause.CameraDistanceNow(), 2.8f), $"a opção deveria refletir a roda na hora (2,8): {pause.CameraDistanceNow()}");
            ClosePause();
            yield return new Delay(0.3f);

            // Depois de recarregar, a opção e a roda continuam iguais.
            yield return SaveAndReload();
            c = Ctl();
            Check(Approx(c.ZoomTarget, 2.8f), $"depois de recarregar o zoom deveria ser 2,8: {c.ZoomTarget}");
            pause = Pause();
            OpenPause();
            yield return Frames(3);
            pause.PointerStep(new MenuPointer.State { position = RowCenter(pause, 1), leftClick = true });
            Check(Approx(pause.CameraDistanceNow(), c.ZoomTarget), $"depois de recarregar a opção ({pause.CameraDistanceNow()}) deveria ser igual à roda ({c.ZoomTarget})");
            ClosePause();
            yield return new Delay(0.3f);
            c.SetZoomTarget(2.5f);
        }

        // ---------- 7. Persistência ----------

        static IEnumerator Persistence()
        {
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(2.5f);
            c.FlushZoom();
            yield return Wheel(-1.5f); // 2,875
            float chosen = c.ZoomTarget;
            Check(Approx(chosen, 2.875f), $"a roda deveria deixar em 2,875: {chosen}");
            Check(Approx(PlayerPrefs.GetFloat(GameOptions.CameraDistanceKey, 0f), chosen, 0.001f), "a preferência deveria ter o valor na hora");
            FieldInfo pending = typeof(PlayerCameraController).GetField("saveAt", Any);
            Check((float)pending.GetValue(c) >= 0f, "logo depois da roda a gravação em disco deveria estar pendente");
            yield return new Delay(1.4f);
            Check((float)pending.GetValue(c) < 0f, "1 s depois de parar a roda a gravação em disco deveria ter acontecido");

            yield return SaveAndReload();
            Check(Approx(Ctl().ZoomTarget, chosen), $"depois de recarregar o zoom deveria ser o escolhido ({chosen}): {Ctl().ZoomTarget}");

            // Valores fora da faixa ou inválidos guardados nas preferências.
            GameOptions.CameraDistance = 99f;
            yield return SaveAndReload();
            Check(Approx(Ctl().ZoomTarget, 4f, 0.001f), $"valor guardado 99 deveria virar o máximo 4,0: {Ctl().ZoomTarget}");
            GameOptions.CameraDistance = -5f;
            yield return SaveAndReload();
            Check(Approx(Ctl().ZoomTarget, 1.8f, 0.001f), $"valor guardado -5 deveria virar o mínimo 1,8: {Ctl().ZoomTarget}");
            GameOptions.CameraDistance = float.NaN;
            yield return SaveAndReload();
            Check(Approx(Ctl().ZoomTarget, 2.5f, 0.05f), $"valor guardado inválido deveria virar o padrão 2,5 (com folga de 0,05 m: rodas de alta resolução do computador do dono mandam micro-rolagens sozinhas): {Ctl().ZoomTarget}");
            PlayerPrefs.DeleteKey(GameOptions.CameraDistanceKey);
            yield return SaveAndReload();
            Check(Approx(Ctl().ZoomTarget, 2.5f, 0.05f), $"sem nada guardado deveria valer o padrão 2,5: {Ctl().ZoomTarget}");
        }

        // ---------- 8, 10. Colisão ----------

        static string ViolationAt(Vector3 feet, float[] zooms, ref int samples)
        {
            var mode = Mode();
            PlayerMotor player = Motor();
            var tmp = new GameObject("CameraDeTesteZoom").transform;
            var bad = new List<string>();
            Teleport(feet + Vector3.up * FeetToCenter, 0f);
            Physics.SyncTransforms();
            Vector3 pivot = player.transform.position + Vector3.up * 0.6f;
            foreach (float zoom in zooms)
            for (int yaw = 0; yaw < 360; yaw += 30)
            foreach (int pitch in new[] { -35, -10, 20, 60 })
            foreach (bool aiming in new[] { false, true })
            {
                mode.SetZoom(zoom);
                mode.Enter();
                mode.SetAiming(aiming);
                for (int i = 0; i < 20; i++) mode.UpdateCamera(tmp, yaw, pitch, 0.1f);
                samples++;
                Vector3 cam = tmp.position;
                Collider inside = Physics.OverlapSphere(cam, 0.12f, ~0, QueryTriggerInteraction.Ignore)
                    .FirstOrDefault(col => !col.transform.IsChildOf(player.transform.root) && !IsCharacter(col));
                RaycastHit[] between = Physics.RaycastAll(pivot, cam - pivot, Vector3.Distance(pivot, cam), ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => !h.collider.transform.IsChildOf(player.transform.root) && !IsCharacter(h.collider)).ToArray();
                if (inside != null || between.Length > 0)
                    bad.Add($"{feet} zoom {zoom} yaw {yaw} pitch {pitch}{(aiming ? " mirando" : "")}: {(inside != null ? "dentro de " + inside.name : "atravessou " + between[0].collider.name)}");
            }
            Object.Destroy(tmp.gameObject);
            return bad.Count == 0 ? null : string.Join("; ", bad.Take(3)) + (bad.Count > 3 ? $" (+{bad.Count - 3})" : "");
        }

        static void SweepReport(List<(string name, Vector3 feet)> stations, string where)
        {
            var violations = new List<string>();
            int samples = 0;
            float[] zooms = { 1.8f, 4f };
            foreach (var (name, feet) in stations)
            {
                string bad = ViolationAt(feet, zooms, ref samples);
                if (bad != null) violations.Add($"{name}: {bad}");
            }
            PlayerCameraController c = Ctl();
            Mode().SetZoom(c.ZoomCurrent);
            Mode().SetAiming(c.IsAiming);
            Check(violations.Count == 0, $"{where}: {violations.Count} lugares com a câmera dentro de parede, por exemplo: {string.Join(" | ", violations.Take(3))}");
            notes.AppendLine($"Varredura {where}: {samples} posições de câmera em {stations.Count} lugares (zoom 1,8 e 4,0, 12 direções, 4 inclinações, mirando ou não): {(violations.Count == 0 ? "nenhuma dentro de parede" : violations.Count + " lugares ruins")}");
        }

        static List<(string, Vector3)> HallStations() => new List<(string, Vector3)>
        {
            ("antecâmara, luta", new Vector3(45f, -14f, 83f)),
            ("antecâmara, depois da luta", new Vector3(42f, -14f, 93f)),
            ("antecâmara, vigia do atirador", new Vector3(46f, -14f, 85.5f)),
            ("antecâmara, inscrição", new Vector3(52.6f, -14f, 83.9f)),
            ("antecâmara, junto do flash", new Vector3(45f, -14f, 88.8f)),
            ("Poço, andaime", new Vector3(41f, -3.5f, 79f)),
            ("Poço, ponto de retorno", new Vector3(48.4f, -10.5f, 71.4f)),
            ("Poço, boca", new Vector3(45f, 0f, 64f)),
            ("corredor da boca", new Vector3(52f, 0f, 40f)),
            ("quarto escuro", OnGround(new Vector3(-55.2f, 0f, -27.1f))),
            ("nicho da Aldeia", Step8Shortcuts.NicheSpot),
        };

        static IEnumerator CollisionHall()
        {
            yield return null;
            SweepReport(HallStations(), "antecâmara, Poço e quarto escuro");
        }

        static IEnumerator CollisionTunnel()
        {
            yield return ToMoment("passo13.aberta");
            float RampY(float z) => -14f + (z - 110f) * 14f / 30f;
            var stations = new List<(string, Vector3)>
            {
                ("antecâmara", new Vector3(36.5f, -14f, 96f)), ("antecâmara, canto", new Vector3(35.4f, -14f, 94.6f)), ("antecâmara, canto 2", new Vector3(35.4f, -14f, 97.4f)),
                ("sob a parede", new Vector3(31f, -14f, 94.7f)), ("túnel 1", new Vector3(30.7f, -14f, 97.4f)), ("túnel 2", new Vector3(32f, -14f, 100f)),
                ("túnel 3", new Vector3(30.7f, -14f, 100.5f)), ("túnel 4", new Vector3(33.3f, -14f, 104.5f)), ("túnel 5", new Vector3(32f, -14f, 106f)),
                ("túnel 6", new Vector3(30.7f, -14f, 108f)), ("túnel 7", new Vector3(33.3f, -14f, 109.4f)),
                ("rampa 1", new Vector3(30.7f, RampY(112f), 112f)), ("rampa 2", new Vector3(33.3f, RampY(118f), 118f)), ("rampa 3", new Vector3(32f, RampY(125f), 125f)),
                ("rampa 4", new Vector3(30.7f, RampY(132f), 132f)), ("rampa 5", new Vector3(33.3f, RampY(138f), 138f)),
            };
            SweepReport(stations, "túnel e rampa dos Guardiões");
        }

        // ---------- 9. Pulos e tremor ----------

        struct Sample
        {
            public float time, distance, zoom, target;
        }

        static IEnumerator Jitter()
        {
            yield return ToMoment("livre.aldeia");
            PlayerCameraController c = Ctl();
            var spots = new List<(string name, Vector3 feet)>
            {
                ("antecâmara, luta", new Vector3(45f, -14f, 83f)),
                ("antecâmara, vigia", new Vector3(46f, -14f, 85.5f)),
                ("antecâmara, inscrição", new Vector3(52.6f, -14f, 83.9f)),
                ("Poço, andaime", new Vector3(41f, -3.5f, 79f)),
                ("quarto escuro", OnGround(new Vector3(-55.2f, 0f, -27.1f))),
            };
            var flagged = new List<string>();
            int runs = 0;
            float worstSpeed = 0f;
            string worstWhere = "";
            foreach (var (name, feet) in spots)
            {
                for (int yaw = 0; yaw < 360; yaw += 90)
                {
                    Teleport(feet + Vector3.up * FeetToCenter, yaw);
                    c.SetZoomTarget(1.8f);
                    yield return new Delay(0.9f);
                    var samples = new List<Sample>();
                    Vector3 startPos = Motor().transform.position;
                    float moved = 0f;
                    int lastFrame = -1;
                    Action sample = () =>
                    {
                        if (Time.frameCount == lastFrame) return;
                        lastFrame = Time.frameCount;
                        Vector3 pivot = Motor().transform.position + Vector3.up * c.Settings.pivotHeight;
                        moved = Mathf.Max(moved, Vector3.Distance(Motor().transform.position, startPos));
                        samples.Add(new Sample { time = Time.realtimeSinceStartup, distance = Vector3.Distance(c.transform.position, pivot), zoom = c.ZoomCurrent, target = c.ZoomTarget });
                    };
                    // 9 cliques afastando e 9 aproximando, um a cada 0,15 s, com a câmera sendo amostrada a cada quadro.
                    for (int i = 0; i < 18; i++)
                    {
                        SendMouse(i < 9 ? -1f : 1f);
                        double end = Now + 0.15;
                        bool zeroed = false;
                        int sentFrame = Time.frameCount;
                        yield return new Until(() =>
                        {
                            sample();
                            if (!zeroed && Time.frameCount > sentFrame) { SendMouse(0f); zeroed = true; }
                            return Now >= end;
                        }, 5f, "o ciclo da roda");
                    }
                    int settleFrom = Time.frameCount;
                    yield return new Until(() => { sample(); return Time.frameCount > settleFrom + 20; }, 5f, "a câmera assentar");

                    runs++;
                    float maxSpeed = 0f;
                    string maxAt = "";
                    int flips = 0;
                    float lastSign = 0f;
                    for (int i = 1; i < samples.Count; i++)
                    {
                        float dt = Mathf.Max(0.0001f, samples[i].time - samples[i - 1].time);
                        float delta = samples[i].distance - samples[i - 1].distance;
                        if (Mathf.Abs(delta) / dt > maxSpeed)
                        {
                            maxSpeed = Mathf.Abs(delta) / dt;
                            maxAt = $"d {samples[i - 1].distance:0.00}→{samples[i].distance:0.00}, zoom {samples[i].zoom:0.00}, alvo {samples[i].target:0.00}, dt {dt * 1000f:0} ms";
                        }
                        if (Mathf.Abs(delta) > 0.01f)
                        {
                            float sign = Mathf.Sign(delta);
                            if (lastSign != 0f && sign != lastSign) flips++;
                            lastSign = sign;
                        }
                    }
                    if (maxSpeed > worstSpeed) { worstSpeed = maxSpeed; worstWhere = $"{name}, direção {yaw}°"; }
                    if (moved > 0.1f) notes.AppendLine($"  (Elena se mexeu {moved:0.00} m em {name}, direção {yaw}°: a medida não vale para pulos da câmera)");
                    // Salto: mais rápido que o zoom (3,5 m/s) com folga; tremor: ida e volta de mais de 1 cm várias vezes seguidas.
                    if (maxSpeed > 8f || flips > 3) flagged.Add($"{name}, direção {yaw}°: velocidade máxima {maxSpeed:0.0} m/s ({maxAt}), {flips} inversões, Elena se mexeu {moved:0.00} m");
                }
            }
            c.SetZoomTarget(2.5f);
            notes.AppendLine($"Pulos e tremor: {runs} rolagens junto de paredes (ida e volta, 9 cliques cada); pior velocidade da câmera {worstSpeed:0.0} m/s em {worstWhere}; " +
                (flagged.Count == 0 ? "nenhum pulo nem tremor." : $"{flagged.Count} com pulo ou tremor: {string.Join(" | ", flagged)}"));
            // O dono pediu só o registro: não derruba o teste.
        }

        // ---------- 11. Modo livre ----------

        static IEnumerator FreeRoam()
        {
            foreach (string id in new[] { "livre.poco", "livre.wadi" })
            {
                yield return ToMoment(id, 4f);
                PlayerCameraController c = Ctl();
                c.SetZoomTarget(2.5f);
                // Uma conversa de chegada (o Poço tem uma) bloqueia a roda de verdade: passa por ela antes.
                if (Runner().IsRunning) yield return FinishDialogue();
                yield return new Delay(0.5f);
                Check(c.ZoomAllowed, $"{id}: a roda deveria valer (conversa={Runner().IsRunning}, controles={Motor().GetComponent<PlayerInputReader>().GameplayEnabled}, menu={CursorPolicy.AnyFree}, mirando={c.IsAiming})");
                yield return Wheel(-1f);
                Check(Approx(c.ZoomTarget, 2.75f), $"{id}: a roda deveria afastar para 2,75: {c.ZoomTarget}");
                yield return Wheel(2f);
                Check(Approx(c.ZoomTarget, 2.25f), $"{id}: a roda deveria aproximar para 2,25: {c.ZoomTarget}");
                c.SetZoomTarget(2.5f);
            }
        }

        // ---------- 12. Memória e viagem ----------

        static string MemoryState(PlayableMemory memory) => typeof(PlayableMemory).GetField("state", Any).GetValue(memory).ToString();

        static IEnumerator Memory()
        {
            yield return ToMoment("passo8.memoria", 1f);
            var memory = Object.FindFirstObjectByType<PlayableMemory>();
            yield return new Until(() => MemoryState(memory) == "InMemory", 40f, "a memória começar");
            yield return new Delay(4.5f);
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(3f);
            Check(Motor().GetComponent<PlayerInputReader>().GameplayEnabled, "na memória a Elena deveria poder andar (o bloqueio do zoom é o que importa)");
            Check(!c.ZoomAllowed, "na memória a roda não deveria valer");
            yield return Wheel(2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"na memória a roda não deveria mudar o zoom: {c.ZoomTarget}");

            // Termina a memória pelo evento dela e vê o zoom voltar.
            var definition = (MemoryDefinition)typeof(PlayableMemory).GetField("memory", Any).GetValue(memory);
            StoryEvents.Raise(definition.endOn);
            yield return new Until(() => MemoryState(memory) == "Present", 40f, "a volta ao presente");
            yield return new Delay(1f);
            Check(c.ZoomAllowed, "depois de voltar ao presente a roda deveria voltar a valer");
            yield return Wheel(1f);
            Check(Approx(c.ZoomTarget, 2.75f), $"depois da memória a roda deveria funcionar: {c.ZoomTarget}");
            c.SetZoomTarget(2.5f);
        }

        static IEnumerator Truck()
        {
            yield return ToMoment("passo15.meio_ficou", 2.5f);
            PlayerCameraController c = Ctl();
            c.SetZoomTarget(3f);
            Check(!c.ZoomAllowed, "na viagem a roda não deveria valer");
            yield return Wheel(2f);
            Check(Approx(c.ZoomTarget, 3f, 0.001f), $"na viagem a roda não deveria mudar o zoom: {c.ZoomTarget}");
            c.SetZoomTarget(2.5f);
        }

        // ---------- Limpeza ----------

        static IEnumerator Cleanup()
        {
            Check(Nibiru.Save.SaveStorage.RealFingerprint() == AutomatedRun.StartFingerprint, "os slots reais não deveriam mudar durante o teste");
            PlayerCameraController c = Ctl();
            if (c != null) c.Settings.zoomInvertWheel = savedInvertWheel;
            if (virtualMouse != null)
            {
                InputSystem.RemoveDevice(virtualMouse);
                virtualMouse = null;
            }
            if (realMouse != null) realMouse.MakeCurrent();
            if (hadStored) PlayerPrefs.SetFloat(GameOptions.CameraDistanceKey, storedDistance);
            else PlayerPrefs.DeleteKey(GameOptions.CameraDistanceKey);
            GameOptions.MouseSensitivity = savedMouseSensitivity;
            GameOptions.Save();
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder != null && folder.EndsWith("Teste_Zoom")) Nibiru.Save.SaveStorage.DeleteTestFolder(folder);
            Debug.Log("Zoom, registros da varredura e dos pulos:\n" + notes);
            yield break;
        }
    }
}
