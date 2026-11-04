using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nibiru.Player;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using InputMouse = UnityEngine.InputSystem.Mouse;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Cameras
{
    /// <summary>
    /// Teste automático da câmera livre de conferência (nível 1). Teclado e mouse virtuais do Input System.
    /// Cobre: ligar pausa o jogo e começa na pose da câmera do jogo; desligar devolve tudo igual (tempo, cursor, controles,
    /// Elena, câmera do jogo, preferências); ligar duas vezes não duplica; voo em todas as direções, Shift mais rápido;
    /// giro do mouse e limite de inclinação; a Elena e a câmera do jogo não se mexem durante o voo; a ferramenta mora na
    /// montagem do editor (fora da build). Menu: Nibiru > Testes > Câmera livre (teste automático).
    /// </summary>
    [InitializeOnLoad]
    static class FreeCameraTests
    {
        static string folder;
        static Keyboard realKeyboard, keyboard;
        static InputMouse realMouse, mouse;

        static FreeCameraTests() => Register("camera_livre.teste", Begin);

        [MenuItem("Nibiru/Testes/Câmera livre (teste automático)", priority = 102)]
        static void Run() => Go("camera_livre.teste");

        static void Begin()
        {
            folder = SaveStorage.TestFolder("Teste_CameraLivre");
            SaveStorage.DeleteTestFolder(folder);
            SaveSlots.UseFolder(folder);

            realKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("TecladoDeTesteCameraLivre");
            realMouse = InputMouse.current;
            mouse = InputSystem.AddDevice<InputMouse>("MouseDeTesteCameraLivre");
            mouse.MakeCurrent();
            keyboard.MakeCurrent();

            Start("Câmera livre de conferência, teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Ligar e desligar: pausa o jogo, começa na pose da câmera do jogo e devolve tudo igual (tempo, cursor, controles, Elena, câmera, preferências)", OnOff),
                ("Ligar duas vezes não duplica; desligar duas vezes não estraga", Twice),
                ("Voo: W, S, A, D, E e Q andam para o lado certo e Shift acelera", Flight),
                ("Mouse gira a câmera e a inclinação é limitada; Elena e câmera do jogo não se mexem; o jogo continua parado", Look),
                ("Cena recarregada com a câmera ligada (atalho de teste, morrer e voltar): continua parado, a Elena nova sem controles, e desligar devolve tudo", Reload),
                ("Fora da build: a ferramenta mora na montagem do editor", NotInBuild),
                ("Limpeza: tira os aparelhos de teste e apaga a pasta do teste", Cleanup),
            });
        }

        // ---------- Ajudas ----------

        static Until Frames(int count)
        {
            int target = Time.frameCount + count;
            return new Until(() => Time.frameCount >= target, 10f, $"{count} quadros");
        }

        static IEnumerator ToMoment()
        {
            if (FreeCameraCheck.IsOn) FreeCameraCheck.Disable();
            int old = Motor() != null ? Motor().GetInstanceID() : 0;
            Go("armas.casa_sem_chave");
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 60f, "o atalho recarregar a região");
            yield return new Delay(3.5f);
        }

        // Aperta as teclas na ferramenta (o teclado virtual do Input System perde a tecla entre os quadros; a leitura do teclado de
        // verdade fica para o teste do dono) e espera o tempo pedido em tempo do editor, porque o tempo do jogo está parado.
        static IEnumerator Fly(float seconds, params Key[] keys)
        {
            FreeCameraCheck.SimulatedKeys.Clear();
            foreach (Key key in keys) FreeCameraCheck.SimulatedKeys.Add(key);
            yield return new Delay(seconds);
            FreeCameraCheck.SimulatedKeys.Clear();
            yield return Frames(2);
        }

        static Transform Cam() => FreeCameraCheck.FreeCamera.transform;

        sealed class Snapshot
        {
            public float timeScale;
            public CursorLockMode cursor;
            public bool cursorVisible, gameplay;
            public Vector3 elenaPosition, mainPosition;
            public Quaternion elenaRotation, mainRotation;
            public float mainFov;
            public float zoomPref;
            public int cameras;

            public static Snapshot Take()
            {
                Camera main = Camera.main;
                return new Snapshot
                {
                    timeScale = Time.timeScale,
                    cursor = Cursor.lockState,
                    cursorVisible = Cursor.visible,
                    gameplay = Motor().GetComponent<PlayerInputReader>().GameplayEnabled,
                    elenaPosition = Motor().transform.position,
                    elenaRotation = Motor().transform.rotation,
                    mainPosition = main.transform.position,
                    mainRotation = main.transform.rotation,
                    mainFov = main.fieldOfView,
                    zoomPref = PlayerPrefs.GetFloat(GameOptions.CameraDistanceKey, -1f),
                    cameras = Camera.allCamerasCount,
                };
            }
        }

        static bool Same(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.0001f;

        // ---------- 1. Ligar e desligar ----------

        static IEnumerator OnOff()
        {
            yield return ToMoment();
            Snapshot before = Snapshot.Take();
            Check(before.timeScale == 1f && before.gameplay, "antes de ligar o jogo deveria rodar normalmente");

            Check(FreeCameraCheck.Enable(), "ligar no Play deveria funcionar");
            yield return Frames(3);
            Check(FreeCameraCheck.IsOn && FreeCameraCheck.FreeCamera != null, "a câmera livre deveria existir");
            Check(Time.timeScale == 0f, $"ligada, o jogo deveria estar parado: timeScale {Time.timeScale}");
            Check(!Motor().GetComponent<PlayerInputReader>().GameplayEnabled, "ligada, os controles do jogador deveriam estar desligados");
            Check(Same(Cam().position, before.mainPosition) && Quaternion.Angle(Cam().rotation, before.mainRotation) < 0.01f,
                $"a câmera livre deveria nascer na pose da câmera do jogo: {Cam().position} contra {before.mainPosition}");
            Check(Camera.main != null && Camera.main.enabled, "a câmera do jogo deveria continuar ligada");

            FreeCameraCheck.Disable();
            yield return Frames(3);
            Snapshot after = Snapshot.Take();
            Check(!FreeCameraCheck.IsOn && FreeCameraCheck.FreeCamera == null, "desligada, a câmera livre deveria sumir");
            Check(after.timeScale == before.timeScale, $"o tempo deveria voltar: {after.timeScale}");
            Check(after.cursor == before.cursor && after.cursorVisible == before.cursorVisible, "o cursor deveria voltar como estava");
            Check(after.gameplay == before.gameplay, "os controles deveriam voltar");
            Check(Same(after.elenaPosition, before.elenaPosition) && Quaternion.Angle(after.elenaRotation, before.elenaRotation) < 0.01f, "a Elena não deveria ter se mexido");
            Check(Same(after.mainPosition, before.mainPosition) && Quaternion.Angle(after.mainRotation, before.mainRotation) < 0.01f && Mathf.Approximately(after.mainFov, before.mainFov),
                "a câmera do jogo não deveria ter se mexido");
            Check(Mathf.Approximately(after.zoomPref, before.zoomPref), "a preferência de distância da câmera não deveria mudar");
            Check(after.cameras == before.cameras, $"o número de câmeras deveria voltar: {after.cameras} contra {before.cameras}");
            yield return new Delay(0.5f);
            Check(Time.timeScale == 1f, "o jogo deveria voltar a andar");
        }

        static IEnumerator Twice()
        {
            yield return ToMoment();
            int cameras = Camera.allCamerasCount;
            Check(FreeCameraCheck.Enable(), "ligar deveria funcionar");
            Check(FreeCameraCheck.Enable(), "ligar de novo deveria só dizer que está ligada");
            yield return Frames(2);
            Check(Camera.allCamerasCount == cameras + 1, $"deveria haver uma câmera a mais, não duas: {Camera.allCamerasCount} (eram {cameras})");
            Check(Time.timeScale == 0f, "continua parado");
            FreeCameraCheck.Disable();
            FreeCameraCheck.Disable();
            yield return Frames(3);
            Check(Time.timeScale == 1f && Camera.allCamerasCount == cameras, "depois de desligar duas vezes tudo deveria estar normal");
            Check(Motor().GetComponent<PlayerInputReader>().GameplayEnabled, "os controles deveriam estar ligados de novo");
        }

        // ---------- 2. Voo ----------

        static IEnumerator Flight()
        {
            yield return ToMoment();
            Check(FreeCameraCheck.Enable(), "ligar deveria funcionar");
            yield return Frames(3);
            Transform cam = Cam();

            Vector3 from = cam.position;
            yield return Fly(0.5f, Key.W);
            Vector3 moved = cam.position - from;
            Check(moved.magnitude > 0.05f && Vector3.Dot(moved.normalized, cam.forward) > 0.99f, $"W deveria andar para a frente: {moved.magnitude:0.00} m, alinhamento {Vector3.Dot(moved.normalized, cam.forward):0.00}");
            float normal = moved.magnitude;

            from = cam.position;
            yield return Fly(0.5f, Key.S);
            moved = cam.position - from;
            Check(moved.magnitude > 0.05f && Vector3.Dot(moved.normalized, -cam.forward) > 0.99f, "S deveria andar para trás");

            from = cam.position;
            yield return Fly(0.5f, Key.D);
            moved = cam.position - from;
            Check(moved.magnitude > 0.05f && Vector3.Dot(moved.normalized, cam.right) > 0.99f, "D deveria andar para a direita");

            from = cam.position;
            yield return Fly(0.5f, Key.A);
            moved = cam.position - from;
            Check(moved.magnitude > 0.05f && Vector3.Dot(moved.normalized, -cam.right) > 0.99f, "A deveria andar para a esquerda");

            from = cam.position;
            yield return Fly(0.5f, Key.E);
            moved = cam.position - from;
            Check(moved.y > 0.05f && Mathf.Abs(moved.x) + Mathf.Abs(moved.z) < 0.005f, $"E deveria subir na vertical: {moved}");

            from = cam.position;
            yield return Fly(0.5f, Key.Q);
            moved = cam.position - from;
            Check(moved.y < -0.05f && Mathf.Abs(moved.x) + Mathf.Abs(moved.z) < 0.005f, $"Q deveria descer na vertical: {moved}");

            from = cam.position;
            yield return Fly(0.5f, Key.W, Key.LeftShift);
            moved = cam.position - from;
            Check(moved.magnitude > normal * 1.8f, $"Shift deveria acelerar bem: {moved.magnitude:0.00} m contra {normal:0.00} m sem Shift");

            from = cam.position;
            yield return new Delay(0.4f);
            Check(Same(cam.position, from), "sem tecla apertada a câmera deveria ficar parada");
            FreeCameraCheck.Disable();
        }

        // ---------- 3. Mouse e isolamento ----------

        static IEnumerator Look()
        {
            yield return ToMoment();
            Snapshot start = Snapshot.Take();
            float gameTime = Time.time;
            Check(FreeCameraCheck.Enable(), "ligar deveria funcionar");
            yield return Frames(3);
            Transform cam = Cam();

            float yaw = cam.eulerAngles.y;
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(200f, 0f) });
            yield return Frames(3);
            float turned = Mathf.DeltaAngle(yaw, cam.eulerAngles.y);
            Check(turned > 10f, $"mouse para a direita deveria girar a câmera para a direita: {turned:0.0}°");

            // Muito para cima e muito para baixo: a inclinação para em 89°.
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(0f, 50000f) });
            yield return Frames(3);
            float up = cam.eulerAngles.x > 180f ? cam.eulerAngles.x - 360f : cam.eulerAngles.x;
            Check(up <= -88f && up >= -89.5f, $"olhando para cima a inclinação deveria parar perto de -89°: {up:0.0}°");
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(0f, -50000f) });
            yield return Frames(3);
            float down = cam.eulerAngles.x > 180f ? cam.eulerAngles.x - 360f : cam.eulerAngles.x;
            Check(down >= 88f && down <= 89.5f, $"olhando para baixo a inclinação deveria parar perto de 89°: {down:0.0}°");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return Frames(2);

            // Voando e girando, a Elena e a câmera do jogo ficam onde estavam e o tempo do jogo não anda.
            yield return Fly(0.5f, Key.W, Key.D);
            yield return new Delay(0.6f);
            Snapshot now = Snapshot.Take();
            Check(Same(now.elenaPosition, start.elenaPosition) && Quaternion.Angle(now.elenaRotation, start.elenaRotation) < 0.01f, "a Elena não deveria se mexer durante o voo");
            Check(Same(now.mainPosition, start.mainPosition) && Quaternion.Angle(now.mainRotation, start.mainRotation) < 0.01f, "a câmera do jogo não deveria se mexer durante o voo");
            Check(Mathf.Approximately(Time.time, gameTime), $"o tempo do jogo deveria ficar parado: {Time.time - gameTime:0.000} s");
            FreeCameraCheck.Disable();
            yield return Frames(3);
            Check(Mathf.Approximately(PlayerPrefs.GetFloat(GameOptions.CameraDistanceKey, -1f), start.zoomPref), "as preferências não deveriam mudar");
        }

        static IEnumerator Reload()
        {
            yield return ToMoment();
            Check(FreeCameraCheck.Enable(), "ligar deveria funcionar");
            yield return Frames(3);
            int old = Motor().GetInstanceID();
            Checkpoints.Restart(RegionSceneName);
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 40f, "a cena recarregar");
            yield return new Delay(2f);
            Check(FreeCameraCheck.IsOn && FreeCameraCheck.FreeCamera != null, "a câmera livre deveria continuar ligada depois da recarga");
            Check(Time.timeScale == 0f, $"depois da recarga o jogo deveria continuar parado: {Time.timeScale}");
            Check(!Motor().GetComponent<PlayerInputReader>().GameplayEnabled, "a Elena nova não deveria ter os controles ligados");
            FreeCameraCheck.Disable();
            yield return Frames(3);
            Check(Time.timeScale == 1f, "desligada, o jogo deveria voltar a andar");
            Check(Motor().GetComponent<PlayerInputReader>().GameplayEnabled, "desligada, a Elena nova deveria ter os controles de volta");
        }

        static IEnumerator NotInBuild()
        {
            string assembly = typeof(FreeCameraCheck).Assembly.GetName().Name;
            Check(assembly.Contains("Editor"), $"a ferramenta deveria estar na montagem do editor: {assembly}");
            Check(!typeof(PlayerInputReader).Assembly.GetName().Name.Contains("Editor"), "o jogo não deveria estar na montagem do editor");
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("FreeCameraCheck t:MonoScript").FirstOrDefault() ?? "");
            Check(path.Contains("/Editor/"), $"o arquivo deveria estar numa pasta Editor: {path}");
            yield break;
        }

        static IEnumerator Cleanup()
        {
            if (FreeCameraCheck.IsOn) FreeCameraCheck.Disable();
            Check(SaveStorage.RealFingerprint() == AutomatedRun.StartFingerprint, "os slots reais não deveriam mudar durante o teste");
            foreach (InputDevice device in new InputDevice[] { keyboard, mouse })
                if (device != null) InputSystem.RemoveDevice(device);
            keyboard = null;
            mouse = null;
            if (realKeyboard != null) realKeyboard.MakeCurrent();
            if (realMouse != null) realMouse.MakeCurrent();
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder != null && folder.EndsWith("Teste_CameraLivre")) SaveStorage.DeleteTestFolder(folder);
            yield break;
        }
    }
}
