using System;
using Nibiru.Memories;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 8 da Aldeia (a primeira tabuleta) sem jogar
    /// desde o Acampamento. Cada item liga o Play e põe a Elena no momento escolhido, com um
    /// checkpoint de frente para o nicho. Menu: Nibiru > Testes > Passo 8.
    /// </summary>
    [InitializeOnLoad]
    static class Step8Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 8/";

        /// <summary>Tudo o que já aconteceu até a Elena descansar no nicho (fim do passo 7).</summary>
        public static readonly string[] EventsUntilStep7 =
        {
            "SaiuDaBarraca", "ExaminouOsCorpos", "AchouBolsaDoTom", "FalouComKast", "KastConfiou", "ViuExecucao",
            "ChegouAoMirante", "ChegouAsTrincheiras", "OuviuOrdemNoRadio", "PegouPistola", "HelicopteroPassou",
            "AproximouSeDaAldeia", "ChegouAAldeia", "ChegouAoPatioComSamir", "EntrouNaCasaDoAltar",
            "ConheceuSobreviventes", "SamirMostrouONicho", "DescansouNoNicho",
        };

        // Passo "Leia a tabuleta no nicho" da missão Sobrevivente (os anteriores ficam cumpridos).
        const int ReadTabletStep = 12;

        /// <summary>De frente para o nicho, onde o altar devolve a Elena.</summary>
        public static readonly Vector3 NicheSpot = new Vector3(-47f, 0f, -29.4f);

        // Na memória, de frente para a porta da cabana de junco (a porta fica a oeste dela).
        static readonly Vector3 HutDoorSpot = new Vector3(2004.5f, 1.05f, 2024f);
        const float HutDoorYaw = 90f;

        const string ShuruppakSceneName = "Memoria_Shuruppak";

        static Step8Shortcuts()
        {
            Register("passo8.nicho", () => AtNiche(null, "no nicho. Aperte E (ou A) em \"Ler a tabuleta\"."));
            Register("passo8.memoria", () => AtNiche(StartMemory, "entrando na memória de Shuruppak."));
            Register("passo8.cabana", () => AtNiche(() =>
            {
                StartMemory();
                WaitUntil(() => SceneManager.GetActiveScene().name == ShuruppakSceneName, () =>
                {
                    Teleport(HutDoorSpot, HutDoorYaw);
                    Debug.Log("Passo 8: na porta da cabana de junco. Entre nela para a voz falar.");
                });
            }, null));
            Register("passo8.samir", () => AtNiche(() =>
            {
                // Como se a memória tivesse terminado: a sequência da volta faz o resto.
                foreach (string id in new[] { "TocouATabuleta", "EntrouNaMemoriaDeZiusudra", "AParedeDeJuncoFalou", "OuviuAParedeDeJunco" })
                    Raise(id);
                Find("Conteudo_Aldeia/Primeira_Tabuleta/Sequencia_Volta").GetComponent<TimedSequence>().Play();
            }, "de volta da memória. Feche a tela da tabuleta para ver a marca e a conversa com o Samir."));
            Register("passo8.jogo_novo", () =>
            {
                FreshGame();
                Debug.Log("Passo 8: jogo novo no Acampamento. Abra o inventário e use a Tabuleta do Junco: deve aparecer \"Não aqui.\"");
            });
        }

        [MenuItem(MenuRoot + "1. No nicho, pronto para ler a tabuleta")]
        static void Niche() => Go("passo8.nicho");

        [MenuItem(MenuRoot + "2. Começo da memória de Shuruppak")]
        static void Memory() => Go("passo8.memoria");

        [MenuItem(MenuRoot + "3. Na porta da cabana de junco")]
        static void ReedHut() => Go("passo8.cabana");

        [MenuItem(MenuRoot + "4. Volta da memória: tabuleta, marca e Samir")]
        static void Samir() => Go("passo8.samir");

        [MenuItem(MenuRoot + "5. Jogo novo no Acampamento (trava da tabuleta no inventário)")]
        static void FreshGameMenu() => Go("passo8.jogo_novo");

        // Mundo até o passo 7, checkpoint de frente para o nicho.
        static void AtNiche(Action then, string message)
        {
            StartFrom(EventsUntilStep7, new[] { ("Sobrevivente", ReadTabletStep), ("RemedioParaYusuf", 0), ("OQueOTomViu", 0) }, 0,
                NicheSpot, 180f, then);
            if (message != null) Debug.Log("Passo 8: " + message);
        }

        // Como depois de ler: o aviso de ler fora do ar e a memória começando.
        static void StartMemory()
        {
            Find("Conteudo_Aldeia/Primeira_Tabuleta").transform.Find("Ler_Tabuleta").gameObject.SetActive(false);
            Raise("TocouATabuleta");
            Object.FindFirstObjectByType<PlayableMemory>().Enter();
        }
    }
}
