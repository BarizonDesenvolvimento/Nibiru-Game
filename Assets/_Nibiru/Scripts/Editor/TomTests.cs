using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Teste automático do final de "O que o Tom viu": com os três rolos a Umm Karim leva a Elena a Abu Faris; ele revela
    /// os filmes, os rolos saem da bolsa e as Fotos do Tom entram (uma vez só); as fotos abrem na tela de leitura; a Umm
    /// Karim volta para casa; tudo volta do checkpoint. Sem os três rolos nada acontece. A missão principal fica livre.
    /// Resumo no Console. Menu: Nibiru > Testes > Secundárias > O que o Tom viu > 6. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class TomTests
    {
        static readonly Vector3 UmmHome = new Vector3(-47.9f, 0f, -30.7f);
        static readonly Vector3 UmmDarkroom = new Vector3(-55.2f, 0f, -27.1f);

        static TomTests() => Register("tom.teste", Begin);

        [MenuItem("Nibiru/Testes/Secundárias/O que o Tom viu/6. Teste automático (resumo no Console)")]
        static void Run() => Go("tom.teste");

        static void Begin() => Start("O que o Tom viu, teste automático", new List<(string, Func<IEnumerator>)>
        {
            ("Fluxo completo: a Umm Karim leva, Abu Faris revela, as fotos entram uma vez, abrem na leitura e tudo volta do checkpoint", FullFlow),
            ("Sem os três rolos nada acontece: a Umm Karim não leva e Abu Faris só espera", NotEnoughRolls),
            ("Código estável do item, textos das fotos e conversas curtas", Data),
        });

        static IEnumerator FullFlow()
        {
            yield return Restart("tom.tres_rolos");
            int mainStep = MainStep();
            Check(Inv("RoloDeFilmeDoTom") == 3, "o atalho deveria dar 3 rolos");
            Check(QuestStep("OQueOTomViu") == 1, $"com os 3 rolos a missão deveria estar no passo 2 (índice 1), está em {QuestStep("OQueOTomViu")}");
            Check(!Obj("Interior_Quarto_Escuro").activeInHierarchy, "o quarto escuro não deveria aparecer antes da Umm Karim levar a Elena");

            // A Umm Karim leva a Elena.
            yield return Face(TomShortcuts.UmmFront, TomShortcuts.UmmYaw);
            yield return Interact("Falar com Umm Karim");
            Check(CurrentDialogue() == "Aldeia_UmmKarimFilmes", $"com 3 rolos a conversa deveria ser a dos filmes (é {CurrentDialogue()})");
            yield return FinishDialogue();
            Check(Raised("UmmKarimLevouAoQuartoEscuro"), "a conversa deveria disparar UmmKarimLevouAoQuartoEscuro");
            Check(Obj("Interior_Quarto_Escuro").activeInHierarchy, "o quarto escuro deveria aparecer");
            Transform ummRoot = Obj("Umm_Karim_Raiz").transform;
            yield return new Until(() => Vector3.Distance(Flat(ummRoot.position), UmmDarkroom) < 1.3f, 40f, "a Umm Karim chegar ao quarto escuro");

            // Abu Faris revela.
            yield return Face(TomShortcuts.AbuFront, TomShortcuts.AbuYaw);
            yield return Interact("Entregar os rolos de filme a Abu Faris");
            Check(Raised("RevelouFilmesDoTom"), "a entrega deveria disparar RevelouFilmesDoTom");
            Check(Inv("RoloDeFilmeDoTom") == 0, "os 3 rolos deveriam sair da bolsa");
            Check(Inv("FotosDoTom") == 1, "as Fotos do Tom deveriam entrar na bolsa");
            Check(QuestStep("OQueOTomViu") == 2, "a missão deveria estar concluída");
            yield return FinishDialogue("Aldeia_QuartoEscuro");
            yield return new Until(() => Vector3.Distance(Flat(ummRoot.position), UmmHome) < 1.3f, 40f, "a Umm Karim voltar para casa");

            // As fotos abrem na leitura.
            var item = Load<ItemDefinition>("Items/FotosDoTom");
            bool used = Object.FindFirstObjectByType<PlayerItemUser>().TryUse(item, out _);
            yield return new Delay(0.3f);
            Check(used && Reading().IsOpen, "usar o item deveria abrir as fotos na tela de leitura");
            Check(ReadingText("titleLabel") == "Fotos do Tom", $"título da leitura: \"{ReadingText("titleLabel")}\"");
            Check(ReadingText("bodyLabel").StartsWith("Foto 1 — A boca do Poço ao entardecer."), "a leitura deveria começar na Foto 1");
            Check(ReadingText("bodyLabel").Contains("Foto 6 — O círculo visto de cima"), "a leitura deveria ter a Foto 6");
            Check(ReadingText("footerLabel").StartsWith("Quem tirou esta foto ainda estava vivo depois de todos."), "a nota da Elena deveria estar no pé");
            Check(Raised("LeuAsFotosDoTom"), "ler deveria disparar LeuAsFotosDoTom");
            CloseReading();
            yield return new Delay(0.3f);

            // Nada em dobro.
            Posted.Clear();
            RaiseEv("RevelouFilmesDoTom");
            RemedyShortcuts.Give("RoloDeFilmeDoTom", 3);
            yield return new Delay(0.4f);
            yield return Face(TomShortcuts.AbuFront, TomShortcuts.AbuYaw);
            Check(Prompt() == "Falar com Abu Faris", $"depois da revelação Abu Faris só conversa (aviso: \"{Prompt()}\")");
            yield return Interact("Falar com Abu Faris");
            yield return FinishDialogue("Aldeia_AbuFarisDepois");
            Check(Inv("FotosDoTom") == 1 && Inv("RoloDeFilmeDoTom") == 3, "uma segunda visita não deveria trocar nem dar nada");
            Check(!Posted.Any(p => p.Contains("Missão concluída")), "a missão não deveria concluir de novo");

            // Volta do checkpoint.
            yield return SaveAndReload();
            Check(Raised("RevelouFilmesDoTom") && Raised("LeuAsFotosDoTom"), "as flags deveriam voltar do checkpoint");
            Check(Inv("FotosDoTom") == 1, "as fotos deveriam continuar na bolsa");
            Check(Obj("Interior_Quarto_Escuro").activeInHierarchy, "o quarto escuro deveria continuar montado");
            Check(Vector3.Distance(Flat(Obj("Umm_Karim_Raiz").transform.position), UmmHome) < 1.3f, "a Umm Karim deveria estar em casa");
            Check(MainStep() == mainStep, "a missão principal não pode ter mudado");
        }

        static IEnumerator NotEnoughRolls()
        {
            yield return Restart("tom.dois_rolos");
            yield return Face(TomShortcuts.UmmFront, TomShortcuts.UmmYaw);
            yield return Interact("Falar com Umm Karim");
            Check(CurrentDialogue() != "Aldeia_UmmKarimFilmes", "com 2 rolos a Umm Karim não deveria levar ninguém");
            yield return FinishDialogue();
            Check(!Raised("UmmKarimLevouAoQuartoEscuro"), "não deveria disparar UmmKarimLevouAoQuartoEscuro");
            Check(!Obj("Interior_Quarto_Escuro").activeInHierarchy, "o quarto escuro não deveria aparecer");

            yield return Restart("tom.quarto_escuro");
            Bag().Remove(Load<ItemDefinition>("Items/RoloDeFilmeDoTom"), 1);
            yield return Face(TomShortcuts.AbuFront, TomShortcuts.AbuYaw);
            Check(Prompt() == "Falar com Abu Faris", $"com 2 rolos Abu Faris só conversa (aviso: \"{Prompt()}\")");
            yield return Interact("Falar com Abu Faris");
            yield return FinishDialogue("Aldeia_AbuFarisEspera");
            Check(!Raised("RevelouFilmesDoTom") && Inv("RoloDeFilmeDoTom") == 2 && Inv("FotosDoTom") == 0, "nada deveria ser trocado com 2 rolos");
        }

        static IEnumerator Data()
        {
            var items = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Nibiru" })
                .Select(g => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(i => i != null).ToList();
            var photos = Load<ItemDefinition>("Items/FotosDoTom");
            Check(photos != null && photos.Id == "fotos_do_tom", $"o código estável de Fotos do Tom deveria ser fotos_do_tom (é \"{photos?.Id}\")");
            Check(photos != null && photos.id == "fotos_do_tom", "o código deveria estar gravado no campo id, e não vir do nome do arquivo");
            Check(photos != null && photos.category == ItemCategory.Chave && photos.document != null, "Fotos do Tom deveria ser item-chave com documento");
            foreach (var dup in items.GroupBy(i => i.Id).Where(g => g.Count() > 1))
                Check(false, $"código de item repetido: {dup.Key}");
            Check(items.All(i => !string.IsNullOrEmpty(i.Id)), "todo item deveria ter um código");

            var scene = Load<DialogueDefinition>("Dialogues/Aldeia_QuartoEscuro");
            Check(scene.nodes.Length <= 8, $"a conversa do quarto escuro deveria ter até 8 falas (tem {scene.nodes.Length})");
            Check(scene.nodes.Select(n => n.speaker).Distinct().OrderBy(s => s).SequenceEqual(new[] { "Abu Faris", "Elena", "Umm Karim" }),
                "só Umm Karim, Abu Faris e Elena deveriam falar");
            Check(Loc.Has("ui.interact.abu_faris_give_rolls") && Loc.Has("ui.interact.abu_faris_talk"), "faltam os avisos de interação no pt-BR.json");
            yield break;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
