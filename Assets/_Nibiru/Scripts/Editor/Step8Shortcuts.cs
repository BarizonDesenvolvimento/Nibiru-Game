using Nibiru.Cameras;
using Nibiru.Memories;
using Nibiru.Player;
using Nibiru.Save;
using Nibiru.Sequences;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 8 da Aldeia sem jogar desde o Acampamento.
    /// Cada item do menu abre a região, liga o Play (se ainda não estiver ligado) e põe a Elena
    /// no momento escolhido, com o mundo como se ela tivesse jogado até ali. Pelo caminho, o
    /// atalho marca um checkpoint de frente para o nicho: morrer ou recomeçar volta para lá.
    /// Menu: Nibiru > Testes > Passo 8.
    /// </summary>
    [InitializeOnLoad]
    static class Step8Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 8/";
        const string PendingKey = "Nibiru.Step8Shortcuts.Pending";
        const string ScenePath = "Assets/_Nibiru/Scenes/Regiao1_SitioDeUr.unity";
        const string SceneName = "Regiao1_SitioDeUr";

        enum Moment { None, Niche, Memory, ReedHut, Samir, FreshGame }

        // Tudo o que já aconteceu até a Elena descansar no nicho (fim do passo 7).
        static readonly string[] EventsUntilStep7 =
        {
            "SaiuDaBarraca", "ExaminouOsCorpos", "AchouBolsaDoTom", "FalouComKast", "KastConfiou", "ViuExecucao",
            "ChegouAoMirante", "ChegouAsTrincheiras", "OuviuOrdemNoRadio", "PegouPistola", "HelicopteroPassou",
            "AproximouSeDaAldeia", "ChegouAAldeia", "ChegouAoPatioComSamir", "EntrouNaCasaDoAltar",
            "ConheceuSobreviventes", "SamirMostrouONicho", "DescansouNoNicho",
        };

        // Passo "Leia a tabuleta no nicho" da missão Sobrevivente (os anteriores ficam cumpridos).
        const int ReadTabletStep = 12;

        // De frente para o nicho, onde o altar devolve a Elena.
        static readonly Vector3 NicheSpot = new Vector3(-47f, 0f, -29.4f);

        // Na memória, de frente para a porta da cabana de junco (a porta fica a oeste dela).
        static readonly Vector3 HutDoorSpot = new Vector3(2004.5f, 1.05f, 2024f);
        const float HutDoorYaw = 90f;

        static Moment waitingFor;
        static int readyFrame;
        static int oldMemoryId;

        static Step8Shortcuts() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(MenuRoot + "1. No nicho, pronto para ler a tabuleta")]
        static void Niche() => Go(Moment.Niche);

        [MenuItem(MenuRoot + "2. Começo da memória de Shuruppak")]
        static void Memory() => Go(Moment.Memory);

        [MenuItem(MenuRoot + "3. Na porta da cabana de junco")]
        static void ReedHut() => Go(Moment.ReedHut);

        [MenuItem(MenuRoot + "4. Volta da memória: tabuleta, marca e Samir")]
        static void Samir() => Go(Moment.Samir);

        [MenuItem(MenuRoot + "5. Jogo novo no Acampamento (trava da tabuleta no inventário)")]
        static void FreshGame() => Go(Moment.FreshGame);

        static void Go(Moment moment)
        {
            if (EditorApplication.isPlaying)
            {
                if (SceneManager.GetActiveScene().name != SceneName && !IsInMemory())
                {
                    Debug.LogWarning("Passo 8: saia do Play e use o atalho de novo (a cena aberta não é a região).");
                    return;
                }
                Begin(moment);
                return;
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            SessionState.SetInt(PendingKey, (int)moment);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                StopWaiting();
                return;
            }
            if (change != PlayModeStateChange.EnteredPlayMode) return;

            var moment = (Moment)SessionState.GetInt(PendingKey, 0);
            SessionState.EraseInt(PendingKey);
            if (moment != Moment.None) Begin(moment);
        }

        static void Begin(Moment moment)
        {
            if (moment == Moment.FreshGame)
            {
                WorldState.Clear();
                Checkpoints.Save(null);
                Restart(Moment.None);
                Debug.Log("Passo 8: jogo novo no Acampamento. Abra o inventário e use a Tabuleta do Junco: deve aparecer \"Não aqui.\"");
                return;
            }

            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (motor == null)
            {
                Debug.LogWarning("Passo 8: a Elena não está na cena.");
                return;
            }

            // Mundo até o passo 7 e um checkpoint de frente para o nicho; a cena recomeça dali.
            WorldState.Clear();
            foreach (string id in EventsUntilStep7) WorldState.MarkStoryEvent(id);
            WorldState.SetQuestStep("Sobrevivente", ReadTabletStep);
            WorldState.SetQuestStep("RemedioParaYusuf", 0);
            WorldState.SetQuestStep("OQueOTomViu", 0);

            var spawn = new GameObject("Passo8_Retorno").transform;
            spawn.SetPositionAndRotation(NicheSpot, Quaternion.Euler(0f, 180f, 0f));
            motor.GetComponent<PlayerRespawn>().SaveCheckpoint(spawn);
            Object.Destroy(spawn.gameObject);

            Restart(moment);
            if (moment == Moment.Niche)
                Debug.Log("Passo 8: no nicho. Aperte E (ou A) em \"Ler a tabuleta\".");
        }

        static void Restart(Moment next)
        {
            StopWaiting();
            var before = FindMemory();
            oldMemoryId = before != null ? before.GetInstanceID() : 0;
            Checkpoints.Restart(SceneName);
            if (next == Moment.None || next == Moment.Niche) return;
            waitingFor = next;
            readyFrame = -1;
            EditorApplication.update += Tick;
        }

        static void StopWaiting()
        {
            EditorApplication.update -= Tick;
            waitingFor = Moment.None;
        }

        // Espera a cena recomeçar (e, para a cabana, a memória carregar) antes do próximo passo.
        static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                StopWaiting();
                return;
            }

            if (readyFrame < 0)
            {
                // Alguns quadros depois de recarregar, para os Start e os eventos refeitos rodarem.
                var memory = FindMemory();
                if (memory == null || memory.GetInstanceID() == oldMemoryId) return;
                readyFrame = Time.frameCount + 5;
                return;
            }
            if (Time.frameCount < readyFrame) return;

            switch (waitingFor)
            {
                case Moment.Memory:
                    StartMemory();
                    StopWaiting();
                    Debug.Log("Passo 8: entrando na memória de Shuruppak.");
                    break;

                case Moment.ReedHut:
                    if (!FindMemory().IsInMemory) StartMemory();
                    if (!IsInMemory()) return;
                    PlaceAtHutDoor();
                    StopWaiting();
                    Debug.Log("Passo 8: na porta da cabana de junco. Entre nela para a voz falar.");
                    break;

                case Moment.Samir:
                    // Como se a memória tivesse terminado: a sequência da volta faz o resto.
                    foreach (string id in new[] { "TocouATabuleta", "EntrouNaMemoriaDeZiusudra", "AParedeDeJuncoFalou", "OuviuAParedeDeJunco" })
                        Quests.StoryEvents.Raise(Load<Quests.StoryEvent>("Events/" + id));
                    Find("Conteudo_Aldeia/Primeira_Tabuleta/Sequencia_Volta").GetComponent<TimedSequence>().Play();
                    StopWaiting();
                    Debug.Log("Passo 8: de volta da memória. Feche a tela da tabuleta para ver a marca e a conversa com o Samir.");
                    break;

                default:
                    StopWaiting();
                    break;
            }
        }

        // Como depois de ler: a pedra no nicho, o aviso de ler fora do ar e a memória começando.
        static void StartMemory()
        {
            Transform tablet = Find("Conteudo_Aldeia/Primeira_Tabuleta").transform;
            tablet.Find("Ler_Tabuleta").gameObject.SetActive(false);
            Quests.StoryEvents.Raise(Load<Quests.StoryEvent>("Events/TocouATabuleta"));
            FindMemory().Enter();
        }

        static void PlaceAtHutDoor()
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(HutDoorSpot, Quaternion.Euler(0f, HutDoorYaw, 0f));
            controller.enabled = true;
            Physics.SyncTransforms();
            var cameraController = Object.FindFirstObjectByType<PlayerCameraController>();
            if (cameraController != null) cameraController.AlignTo(motor.transform);
        }

        static bool IsInMemory() => SceneManager.GetActiveScene().name == ShuruppakSceneName;

        const string ShuruppakSceneName = "Memoria_Shuruppak";

        static PlayableMemory FindMemory() => Object.FindFirstObjectByType<PlayableMemory>();

        static GameObject Find(string path) => GameObject.Find(path);

        static T Load<T>(string relativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>($"Assets/_Nibiru/Data/{relativePath}.asset");
    }
}
