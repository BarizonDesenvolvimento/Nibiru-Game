using System.Linq;
using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Player;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para ver os soldados da Fundação animados onde eles aparecem (a
    /// execução no Acampamento, as patrulhas, as Trincheiras, o encontro da Aldeia, o cerco), o piloto
    /// no helicóptero, o combate no laboratório e uma vitrine com todas as animações lado a lado.
    /// Menu: Nibiru > Testes > Soldados.
    /// </summary>
    [InitializeOnLoad]
    static class SoldierShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Soldados/";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";

        // Acampamento: o que já aconteceu até o rádio (o resgate vem a seguir) e depois da execução.
        static readonly string[] EventsUntilRadio = { "SaiuDaBarraca", "ExaminouOsCorpos", "AchouBolsaDoTom", "FalouComKast", "KastConfiou" };
        static readonly string[] EventsAfterExecution = EventsUntilRadio.Concat(new[] { "ChegouAoMirante", "ViuExecucao" }).ToArray();
        // Trincheiras, com a pistola e o helicóptero já passado.
        static readonly string[] EventsInTrenches = EventsAfterExecution.Concat(new[] { "ChegouAsTrincheiras", "OuviuOrdemNoRadio", "PegouPistola", "HelicopteroPassou" }).ToArray();

        // Passos da missão Sobrevivente: "Espere o resgate", "Fuja para as trincheiras" e "Atravesse as trincheiras".
        const int WaitRescueStep = 5;
        const int FleeStep = 6;
        const int CrossTrenchesStep = 8;

        // No mirante do Acampamento, de frente para o heliponto (onde o checkpoint do mirante devolve a Elena).
        static readonly Vector3 LookoutSpot = new Vector3(19f, 3f, -97f);
        // Na frente e à direita do helicóptero pousado no heliponto, olhando o piloto pelo vidro.
        static readonly Vector3 BesideHelicopterSpot = new Vector3(28.2f, 0.1f, -78f);
        const float BesideHelicopterYaw = 158f;
        // Na entrada das trincheiras, olhando para o norte. Em z = -75 ficava dentro do talude Talude_H1_B
        // (z de -75,75 a -74,25): a Elena caía pelo chão. O teste geral dos atalhos achou.
        static readonly Vector3 TrenchEntranceSpot = new Vector3(0f, 0f, -77.5f);
        // Na Aldeia, de onde se vê a barreira do norte com o líder de guarda (fora do alcance da visão dele).
        static readonly Vector3 BarrierViewSpot = new Vector3(-50f, 0f, 22f);

        // Vitrine: um chão vazio longe de tudo, no laboratório.
        static readonly Vector3 ShowcaseSpot = new Vector3(300f, 0f, 300f);
        static SoldierAnimator firing, reloading;
        static Animator hurting, dyingBack, dyingForward;
        static double nextFire, nextReload, nextHurt, nextDeath;

        static SoldierShortcuts()
        {
            Register("soldados.execucao", () =>
            {
                StartFrom(EventsUntilRadio, new[] { ("Sobrevivente", WaitRescueStep), ("OQueOTomViu", 0) }, 0, LookoutSpot, 45f);
                Debug.Log("Soldados: no mirante do Acampamento. Em uns 15 s o helicóptero (com o piloto) pousa; o soldado aponta o fuzil para o trabalhador e atira. O líder de esquadra fica no heliponto.");
            });
            Register("soldados.acampamento", () =>
            {
                // O líder de guarda no heliponto veria a Elena ali do lado: os soldados ficam parados nesta partida.
                StartFrom(EventsAfterExecution, new[] { ("Sobrevivente", FleeStep), ("OQueOTomViu", 0) }, 0, BesideHelicopterSpot, BesideHelicopterYaw,
                    () => After(0.5f, ElenaShortcuts.StopEnemies));
                Debug.Log("Soldados: na frente do helicóptero pousado, olhando o piloto pelo vidro da cabine. Os soldados ficam parados nesta partida (o líder de esquadra está de guarda logo ao lado).");
            });
            Register("soldados.trincheiras", () =>
            {
                StartFrom(EventsInTrenches, new[] { ("Sobrevivente", CrossTrenchesStep), ("OQueOTomViu", 0) }, 0, TrenchEntranceSpot, 0f, ElenaShortcuts.GiveWeapons);
                Debug.Log("Soldados: na entrada das trincheiras, com pistola. Dois soldados patrulham e o líder de esquadra está de guarda no canto nordeste. Faça barulho, atire, deixe que eles atirem e recarreguem.");
            });
            Register("soldados.cerco", () =>
            {
                Step9Shortcuts.Start(revealed: true, extra: new[] { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga" },
                    Step9Shortcuts.CrossRoadStep, BarrierViewSpot, 20f, ElenaShortcuts.GiveWeapons);
                Debug.Log("Soldados: na Aldeia, com pistola. Ao norte, o líder de esquadra guarda a barreira; a leste, a patrulha anda rente ao muro; o helicóptero pousado na base tem o piloto na cabine.");
            });
            Register("soldados.laboratorio", () => After(0.3f, () =>
            {
                ElenaShortcuts.GiveWeapons();
                Debug.Log("Soldados: laboratório com pistola, munição e faca, e os soldados com IA. Eles patrulham, ouvem, perseguem, miram, atiram e recarregam a cada 8 tiros. Use \"Ferir\" e \"Matar o soldado mais perto\" para ver dano e morte.");
            }));
            Register("soldados.vitrine", () => After(0.3f, BuildShowcase));
        }

        [MenuItem(MenuRoot + "1. Acampamento: a execução vista do mirante")]
        static void Execution() => Go("soldados.execucao");

        [MenuItem(MenuRoot + "2. Acampamento: o piloto no helicóptero pousado")]
        static void Camp() => Go("soldados.acampamento");

        [MenuItem(MenuRoot + "3. Trincheiras: patrulha e o líder, com pistola")]
        static void Trenches() => Go("soldados.trincheiras");

        [MenuItem(MenuRoot + "4. Aldeia: o soldado do encontro (mira e cai com a facada)")]
        static void Encounter() => Go("samir.encontro");

        [MenuItem(MenuRoot + "5. Aldeia: volta de checkpoint com o soldado caído")]
        static void FallenSoldier() => Go("samir.seguir");

        [MenuItem(MenuRoot + "6. Cerco: o líder na barreira, a patrulha e o piloto")]
        static void Siege() => Go("soldados.cerco");

        [MenuItem(MenuRoot + "7. Laboratório: combate com os soldados")]
        static void Lab() => Go("soldados.laboratorio", LabScenePath);

        [MenuItem(MenuRoot + "8. Vitrine: os 4 soldados e as animações lado a lado")]
        static void Showcase() => Go("soldados.vitrine", LabScenePath);

        [MenuItem(MenuRoot + "9. Ferir o soldado mais perto")]
        static void HurtNearest() => HitNearest(10f);

        [MenuItem(MenuRoot + "10. Matar o soldado mais perto")]
        static void KillNearest() => HitNearest(10000f);

        // O golpe vem da Elena: o soldado ferido sabe de onde veio e entra em combate.
        static void HitNearest(float amount)
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (!EditorApplication.isPlaying || motor == null)
            {
                Debug.LogWarning("Ligue o Play com a Elena na cena antes deste atalho.");
                return;
            }
            var soldier = Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None)
                .Where(s => !s.GetComponent<Health>().IsDead)
                .OrderBy(s => Vector3.Distance(s.transform.position, motor.transform.position)).FirstOrDefault();
            if (soldier == null)
            {
                Debug.LogWarning("Nenhum soldado vivo na cena.");
                return;
            }
            soldier.GetComponent<Health>().TakeDamage(new DamageInfo(amount, soldier.transform.position + Vector3.up * 0.4f, motor.gameObject));
            Debug.Log($"Soldados: {soldier.name} levou {amount:0} de dano.");
        }

        // ---------- Vitrine ----------

        // Uma cópia por animação, em fila, de frente para a Elena, com o nome em cima. Andar e correr
        // ficam no lugar (quem anda no jogo é o NavMeshAgent). Tiro, recarga, dano e mortes se repetem.
        static void BuildShowcase()
        {
            ElenaShortcuts.StopEnemies();
            var group = new GameObject("Vitrine_Soldados").transform;
            group.position = ShowcaseSpot;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Chao";
            floor.transform.SetParent(group, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(34f, 0.2f, 30f);

            var settings = AssetDatabase.LoadAssetAtPath<SoldierSettings>("Assets/_Nibiru/Data/AI/SoldadoContencao.asset");
            var poses = new (string label, string character, float speed)[]
            {
                ("Parado", "Soldado", 0f),
                ("Patrulha", "Soldado", settings.patrolSpeed),
                ("Correndo", "Soldado", settings.chaseSpeed),
                ("Mirando", "Soldado", 0f),
                ("Atirando", "Soldado", 0f),
                ("Recarregando", "Soldado", 0f),
                ("Dano", "Soldado", 0f),
                ("Morte", "Soldado", 0f),
                ("Morte de frente", "Soldado", 0f),
                ("Líder de esquadra", "LiderDeEsquadra", settings.patrolSpeed),
                ("Atirador de elite", "AtiradorDeElite", 0f),
                ("Piloto", "Piloto", 0f),
            };
            const float spacing = 1.6f;
            for (int i = 0; i < poses.Length; i++)
            {
                var (label, character, speed) = poses[i];
                var copy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SoldiersSetup.VisualPrefab(character)), group);
                copy.name = label;
                copy.transform.localPosition = new Vector3((i - (poses.Length - 1) * 0.5f) * spacing, character == "Piloto" ? 0.5f : 0f, 0f);
                copy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var animator = copy.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var soldier = copy.GetComponent<SoldierAnimator>();

                // Mira, tiro e recarga ficam com o SoldierAnimator (ele gira o tronco e cuida das camadas);
                // nas outras, a pose é posta direto no Animator.
                if (label is "Mirando" or "Atirando" or "Recarregando")
                {
                    if (label != "Recarregando") soldier.AimAt(true);
                    if (label == "Atirando") firing = soldier;
                    if (label == "Recarregando") reloading = soldier;
                }
                else
                {
                    if (soldier != null) Object.Destroy(soldier);
                    if (character != "Piloto") animator.SetFloat(SoldierAnimator.SpeedParam, speed);
                    if (label == "Dano") hurting = animator;
                    if (label == "Morte") dyingBack = animator;
                    if (label == "Morte de frente") dyingForward = animator;
                }
                if (character == "Piloto")
                {
                    // O banco do piloto, para ele não ficar sentado no ar.
                    var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seat.name = "Banco";
                    seat.transform.SetParent(copy.transform, false);
                    seat.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                    seat.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }

                var text = new GameObject("Nome").AddComponent<TextMesh>();
                text.transform.SetParent(copy.transform, false);
                // Alturas alternadas, para os nomes vizinhos não se encostarem.
                text.transform.localPosition = new Vector3(0f, (character == "Piloto" ? 1.6f : 2.1f) + (i % 2 == 0 ? 0f : 0.2f), 0f);
                text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                text.text = label;
                text.characterSize = 0.035f;
                text.fontSize = 48;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            Teleport(ShowcaseSpot + new Vector3(0f, 0f, -7.5f), 0f);
            nextFire = nextReload = nextHurt = nextDeath = 0;
            EditorApplication.update -= Repeat;
            EditorApplication.update += Repeat;
            Debug.Log("Soldados: vitrine com todas as animações e os 4 modelos. Ande ao longo da fila; tiro, recarga, dano e mortes se repetem.");
        }

        static void Repeat()
        {
            if (!EditorApplication.isPlaying || firing == null)
            {
                EditorApplication.update -= Repeat;
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            if (now >= nextFire)
            {
                firing.Fire();
                nextFire = now + 1.3;
            }
            if (now >= nextReload && reloading != null)
            {
                reloading.Reload();
                nextReload = now + 3.4;
            }
            if (now >= nextHurt && hurting != null)
            {
                hurting.SetTrigger(SoldierAnimator.HurtParam);
                nextHurt = now + 1.5;
            }
            if (now >= nextDeath && dyingBack != null && dyingForward != null)
            {
                dyingBack.Play(SoldierAnimator.DeathState, 0, 0f);
                dyingForward.Play(SoldierAnimator.ForwardDeathState, 0, 0f);
                nextDeath = now + 4.5;
            }
        }
    }
}
