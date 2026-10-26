using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nibiru.Documents;
using Nibiru.Localization;
using Nibiru.Player;
using Nibiru.Powers;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Teste automático do Arquivo (etapa A3): códigos estáveis e catálogo completo; só aparece o que foi lido ou ouvido
    /// (sem "???"); o aviso "Adicionado ao Arquivo" só na primeira vez; rever cada entrada pela pausa abre a tela de
    /// leitura sem repetir evento nem recompensa e a leitura devolve a vez ao menu (sem soltar a Elena); a inscrição da
    /// Aldeia só pode ser lida depois do Eco; a carta das Trincheiras e a inscrição do Poço; tudo volta do checkpoint.
    /// Resumo no Console. Menu: Nibiru > Testes > Arquivo (A3) > 7. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class ArchiveTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static ArchiveTests() => Register("arquivo.teste", Begin);

        [MenuItem("Nibiru/Testes/Arquivo (A3)/7. Teste automático (resumo no Console)")]
        static void Run() => Go("arquivo.teste");

        static void Begin() => Start("Arquivo (A3), teste automático", new List<(string, Func<IEnumerator>)>
        {
            ("Códigos estáveis, catálogo completo e textos", Data),
            ("Arquivo vazio, itens escondidos e o aviso só na primeira vez", EmptyAndNotice),
            ("Tudo coletado: rever cada entrada pela pausa, sem evento nem recompensa repetidos, e voltar do checkpoint", ReviewAll),
            ("Inscrição da Aldeia: só se lê depois do Eco", VillageInscription),
            ("Carta do soldado nas Trincheiras e inscrição do Poço entram no Arquivo", LetterAndWell),
            ("Achados da base: registro do guincho (protegido do atirador) e nota do selo (sem atrapalhar o 3º rolo)", BaseFinds),
        });

        // ---------- Ajudas ----------

        static PauseMenuUI Pause() => UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>();

        static PlayerInputReader Input() => UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();

        static object ArchivePage() => Enum.ToObject(typeof(PauseMenuUI).GetNestedType("Page", BindingFlags.NonPublic), 2);

        static void PauseCall(string method, params object[] args) => typeof(PauseMenuUI).GetMethod(method, Any).Invoke(Pause(), args);

        static T PauseField<T>(string field) => (T)typeof(PauseMenuUI).GetField(field, Any).GetValue(Pause());

        static void OpenArchive()
        {
            PauseCall("Open");
            PauseCall("ShowPage", ArchivePage(), 0);
        }

        static List<ArchiveCatalog.Entry> Rows() => PauseField<List<ArchiveCatalog.Entry>>("archiveRows");

        static string PauseText() => PauseField<UnityEngine.UI.Text>("content").text;

        static int Selected() => PauseField<int>("selected");

        static void SetSelected(int value) => typeof(PauseMenuUI).GetField("selected", Any).SetValue(Pause(), value);

        static int ArchiveTop() => PauseField<int>("archiveTop");

        static int Window() => (int)typeof(PauseMenuUI).GetField("ArchiveWindow", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        static int LineCount() => PauseField<System.Collections.IList>("archiveLines").Count;

        static int LineOfRowIndex(int row) => (int)typeof(PauseMenuUI).GetMethod("LineOfRow", Any).Invoke(Pause(), new object[] { row });

        static void ClosePause()
        {
            if (Pause().IsOpen) PauseCall("Close");
        }

        // ---------- Dados ----------

        static IEnumerator Data()
        {
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            Check(catalog != null, "o catálogo deveria estar em Resources/Arquivo/Catalogo");
            if (catalog == null) yield break;

            ArchiveCatalog.Entry[] entries = catalog.AllEntries().ToArray();
            Check(entries.Length == 11, $"o catálogo deveria ter 11 entradas (10 documentos e 1 fita), tem {entries.Length}");

            foreach (var duplicate in entries.GroupBy(e => e.Id).Where(g => g.Count() > 1))
                Check(false, $"código repetido no catálogo: {duplicate.Key}");
            foreach (ArchiveCatalog.Entry entry in entries)
            {
                string name = entry.document != null ? entry.document.name : entry.tape?.name;
                Check(!string.IsNullOrEmpty(entry.Id), $"{name}: sem código");
                Check(entry.Id != name, $"{name}: o código não pode ser o nome do arquivo");
                Check(entry.IsTape ? entry.Id.StartsWith("tape.") : entry.Id.StartsWith("doc."), $"{name}: código \"{entry.Id}\" sem o prefixo doc. ou tape.");
                Check(entry.UnlockEvent != null, $"{name}: sem evento de leitura ou coleta");
                Check(entry.document == null || entry.document.id == entry.Id, $"{name}: o código deveria estar no campo id");
                if (entry.document != null)
                {
                    Check(Loc.Has(entry.document.titleKey) && Loc.Has(entry.document.textKey), $"{name}: título ou texto sem chave no pt-BR.json");
                    Check(!string.IsNullOrEmpty(entry.Body), $"{name}: texto vazio");
                }
            }

            // Os códigos da tabela do menu são os gravados nos arquivos (nunca mudam).
            foreach ((string asset, string id) in ArchiveContent_DocumentIds())
            {
                var doc = Load<DocumentDefinition>("Documents/" + asset);
                Check(doc != null && doc.id == id, $"{asset}: o código gravado deveria ser {id} (é \"{doc?.id}\")");
            }
            var tape = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            Check(tape != null && tape.id == "tape.irena_1", "Fita_Irena_1 deveria ter o código tape.irena_1");

            // Todo documento e toda fita dos dados está no catálogo, menos o diário (fica fora da demo).
            foreach (DocumentDefinition doc in AssetDatabase.FindAssets("t:DocumentDefinition", new[] { "Assets/_Nibiru/Data" })
                         .Select(g => AssetDatabase.LoadAssetAtPath<DocumentDefinition>(AssetDatabase.GUIDToAssetPath(g))))
            {
                bool listed = entries.Any(e => e.document == doc);
                if (doc.name == "Diario_Entrada_1") Check(!listed, "o diário não deve entrar no Arquivo");
                else Check(listed, $"o documento {doc.name} não está no catálogo");
            }
            foreach (TapeDefinition t in AssetDatabase.FindAssets("t:TapeDefinition", new[] { "Assets/_Nibiru/Data" })
                         .Select(g => AssetDatabase.LoadAssetAtPath<TapeDefinition>(AssetDatabase.GUIDToAssetPath(g))))
                Check(entries.Any(e => e.tape == t), $"a fita {t.name} não está no catálogo");

            // O documento da passagem usa o texto que a Elena já lê em voz alta.
            var passage = Load<DocumentDefinition>("Documents/Passagem_Inscricao");
            Check(passage != null && passage.LocalizedText == Loc.Get("dlg.Passagem_Inscricao.e2"),
                "o documento da passagem deveria ter o mesmo texto da fala dela");
            Check(Loc.Has(ArchiveNotifier.NoticeKey), "falta a chave do aviso do Arquivo");
            Check(catalog.noticeEvent != null, "o catálogo precisa do evento do aviso");
            yield return null;
        }

        static (string asset, string id)[] ArchiveContent_DocumentIds() => Content.ArchiveContent.DocumentIds;

        // ---------- Vazio, escondido e aviso ----------

        static IEnumerator EmptyAndNotice()
        {
            yield return Restart("arquivo.vazio");
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            Check(catalog.UnlockedCount() == 0, $"num jogo novo o Arquivo deveria estar vazio (tem {catalog.UnlockedCount()})");
            OpenArchive();
            Check(Rows().Count == 0, "sem nada lido a lista deveria estar vazia");
            Check(PauseText().Contains(Loc.Get("ui.arquivo.vazio")), "a página deveria dizer que não há nada ainda");
            Check(!PauseText().Contains("?"), "itens não coletados ficam escondidos, sem \"???\"");
            ClosePause();

            // Outro evento qualquer não avisa nada.
            Posted.Clear();
            RaiseEv("ChegouAAldeia");
            yield return new Delay(0.2f);
            Check(!Posted.Contains(Loc.Get(ArchiveNotifier.NoticeKey)), "um evento fora do Arquivo não deveria avisar");

            // O primeiro documento lido avisa; o seguinte não.
            RaiseEv("LeuOEmailDoKast");
            yield return new Delay(0.2f);
            string notice = Loc.Get(ArchiveNotifier.NoticeKey);
            Check(notice == "Adicionado ao Arquivo (menu de pausa)", $"texto do aviso: \"{notice}\"");
            Check(Posted.Count(p => p == notice) == 1, "o primeiro item deveria avisar uma vez");
            Check(Raised("AvisouDoArquivo"), "o aviso deveria ficar registrado");
            RaiseEv("ViuAFotoDe1979");
            yield return new Delay(0.2f);
            Check(Posted.Count(p => p == notice) == 1, "o segundo item não deveria avisar de novo");

            // Só o lido aparece, sem os outros nem contador.
            OpenArchive();
            Check(Rows().Count == 2, $"deveria listar 2 entradas, lista {Rows().Count}");
            string text = PauseText();
            Check(text.Contains(Load<DocumentDefinition>("Documents/Base_EmailDoKast").LocalizedTitle), "o e-mail lido deveria aparecer");
            Check(!text.Contains(Load<DocumentDefinition>("Documents/Poco_Inscricao").LocalizedTitle), "a inscrição do Poço (não lida) deveria estar escondida");
            Check(!text.Contains(Load<TapeDefinition>("Tapes/Fita_Irena_1").LocalizedName), "a fita (não pega) deveria estar escondida");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(text, @"\d\s*/\s*\d|\(\d"), "não deve haver contador");
            ClosePause();

            // O aviso não repete depois de voltar do checkpoint.
            yield return SaveAndReload();
            Posted.Clear();
            RaiseEv("LeuAInscricaoDoPoco");
            yield return new Delay(0.2f);
            Check(!Posted.Contains(notice), "depois de voltar do checkpoint o aviso não deveria repetir");
        }

        // ---------- Rever tudo ----------

        static IEnumerator ReviewAll()
        {
            yield return Restart("arquivo.cheio");
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            Check(catalog.UnlockedCount() == 11, $"com tudo coletado o Arquivo deveria ter 11 entradas (tem {catalog.UnlockedCount()})");
            int fundacao = Rep("FundacaoEnlil");
            int guardioes = Rep("Guardioes");
            int transformation = Save.WorldState.Transformation;
            int mainStep = MainStep();

            OpenArchive();
            List<ArchiveCatalog.Entry> rows = Rows();
            Check(rows.Count == 11, $"a lista deveria ter 11 linhas (tem {rows.Count})");
            Check(Time.timeScale == 0f && !Input().GameplayEnabled, "com a pausa aberta o jogo deveria estar parado e a Elena sem controle");
            yield return Scrolling(rows);

            for (int i = 0; i < rows.Count; i++)
            {
                Posted.Clear();
                SetSelected(i);
                PauseCall("KeepArchiveSelectionVisible");
                PauseCall("Refresh");
                int topBefore = ArchiveTop();
                PauseCall("OpenArchiveEntry");
                yield return new Delay(0.15f);
                ArchiveCatalog.Entry entry = rows[i];
                Check(Reading().IsOpen, $"{entry.Id}: a leitura deveria abrir");
                Check(ReadingText("titleLabel") == entry.Title, $"{entry.Id}: título da leitura \"{ReadingText("titleLabel")}\"");
                if (entry.IsTape)
                    Check(ReadingText("bodyLabel").Contains(entry.tape.lines[0].LocalizedText) && ReadingText("bodyLabel").Contains(entry.tape.lines[entry.tape.lines.Length - 1].LocalizedText),
                        "a fita deveria aparecer em texto, da primeira à última fala");
                else
                    Check(ReadingText("bodyLabel") == entry.document.LocalizedText, $"{entry.Id}: texto da leitura diferente do documento");
                Check(Posted.Count == 0, $"{entry.Id}: rever não deveria avisar nada ({string.Join("; ", Posted)})");

                CloseReading();
                yield return new Delay(0.15f);
                Check(!Reading().IsOpen, $"{entry.Id}: a leitura deveria fechar");
                Check(Pause().IsOpen, $"{entry.Id}: ao fechar a leitura a pausa deveria continuar aberta");
                Check(Time.timeScale == 0f, $"{entry.Id}: o jogo deveria continuar parado");
                Check(!Input().GameplayEnabled, $"{entry.Id}: a Elena não pode ser solta com a pausa aberta");
                Check(PauseField<UnityEngine.UI.Text>("content").gameObject.activeInHierarchy, $"{entry.Id}: a lista deveria voltar");
                Check(Selected() == i, $"{entry.Id}: ao voltar da leitura a seleção deveria continuar em {i} (está em {Selected()})");
                Check(ArchiveTop() == topBefore, $"{entry.Id}: ao voltar da leitura a rolagem deveria continuar em {topBefore} (está em {ArchiveTop()})");
                Check(PauseText().Contains(entry.Title), $"{entry.Id}: o item escolhido deveria continuar visível depois de voltar da leitura");
            }

            ClosePause();
            yield return new Delay(0.15f);
            Check(Time.timeScale == 1f && Input().GameplayEnabled, "fechar a pausa deveria devolver o tempo e o controle");
            Check(Rep("FundacaoEnlil") == fundacao && Rep("Guardioes") == guardioes, "rever não pode mudar reputação");
            Check(Save.WorldState.Transformation == transformation, "rever não pode mudar a transformação");
            Check(MainStep() == mainStep, "rever não pode mexer na missão");

            // Tudo volta do checkpoint (o código estável é o que o salvamento do A4 vai gravar).
            yield return SaveAndReload();
            Check(ArchiveCatalog.Current.UnlockedCount() == 11, $"depois do checkpoint o Arquivo deveria ter 11 entradas (tem {ArchiveCatalog.Current.UnlockedCount()})");
        }

        // ---------- Rolagem ----------

        // A lista rola: a seleção (setas, direcional) mantém o item visível, a roda do mouse rola e leva a seleção, e a
        // letra dos itens é a normal.
        static IEnumerator Scrolling(List<ArchiveCatalog.Entry> rows)
        {
            int window = Window();
            int lines = LineCount();
            Check(lines > window, $"o teste precisa de mais linhas ({lines}) que a janela ({window})");
            string text = PauseText();
            Check(ArchiveTop() == 0 && Selected() == 0, "a página deveria abrir no topo, com o primeiro item escolhido");
            Check(!text.Contains("\u25B2") && text.Contains("\u25BC"), "no topo só deveria haver o marcador de mais abaixo");
            Check(!text.Contains(rows[rows.Count - 1].Title), "a última entrada não deveria caber na primeira tela");
            Check(!text.Contains(rows[0].Title + "</size>"), "a letra dos itens deveria ser a normal, sem tamanho reduzido");
            Font font = PauseField<UnityEngine.UI.Text>("content").font;
            Check(font != null && font.HasCharacter('\u25B2') && font.HasCharacter('\u25BC'), "a fonte da interface deveria ter os glifos de seta");

            // Descer um a um pelo teclado: o escolhido nunca sai da janela.
            for (int i = 1; i < rows.Count; i++)
            {
                SetSelected(i);
                PauseCall("KeepArchiveSelectionVisible");
                PauseCall("Refresh");
                Check(PauseText().Contains(rows[i].Title), $"o item {i} deveria estar visível ao escolher ({rows[i].Id})");
            }
            text = PauseText();
            Check(ArchiveTop() == lines - window, $"na última entrada a janela deveria estar no fim ({lines - window}), está em {ArchiveTop()}");
            Check(text.Contains("\u25B2") && !text.Contains("\u25BC"), "no fim só deveria haver o marcador de mais acima");
            Check(text.Contains(rows[rows.Count - 1].Title), "a última entrada deveria aparecer");

            // Subir até a primeira: o título do primeiro grupo volta a aparecer.
            for (int i = rows.Count - 2; i >= 0; i--)
            {
                SetSelected(i);
                PauseCall("KeepArchiveSelectionVisible");
                PauseCall("Refresh");
                Check(PauseText().Contains(rows[i].Title), $"subindo, o item {i} deveria estar visível ({rows[i].Id})");
            }
            Check(ArchiveTop() == 0 && PauseText().Contains(ArchiveCatalog.Current.groups[0].LocalizedTitle), "voltando ao primeiro, a janela deveria estar no topo, com o título do grupo");

            // Roda do mouse: rola e leva a seleção para dentro da janela.
            PauseCall("ScrollArchive", 3);
            Check(ArchiveTop() == 3, $"3 linhas da roda deveriam levar a janela a 3 (está em {ArchiveTop()})");
            int selectedLine = LineOfRowIndex(Selected());
            Check(selectedLine >= 3 && selectedLine <= 3 + window - 1, $"a seleção deveria seguir para dentro da janela (linha {selectedLine})");
            PauseCall("ScrollArchive", 99);
            Check(ArchiveTop() == lines - window, "rolar além do fim deveria parar no fim");
            PauseCall("ScrollArchive", -99);
            Check(ArchiveTop() == 0, "rolar além do topo deveria parar no topo");

            // Rolar até a última entrada, abrir e fechar: a lista volta na mesma posição e com o mesmo item.
            SetSelected(rows.Count - 1);
            PauseCall("KeepArchiveSelectionVisible");
            PauseCall("Refresh");
            int top = ArchiveTop();
            PauseCall("OpenArchiveEntry");
            yield return new Delay(0.15f);
            Check(Reading().IsOpen && ReadingText("titleLabel") == rows[rows.Count - 1].Title, "a última entrada deveria abrir");
            CloseReading();
            yield return new Delay(0.15f);
            Check(Selected() == rows.Count - 1 && ArchiveTop() == top, $"depois de fechar, seleção {Selected()} (era {rows.Count - 1}) e rolagem {ArchiveTop()} (era {top})");
            Check(PauseText().Contains(rows[rows.Count - 1].Title), "depois de fechar, a última entrada deveria continuar à vista");
            SetSelected(0);
            PauseCall("KeepArchiveSelectionVisible");
            PauseCall("Refresh");
        }

        // ---------- Aldeia ----------

        // A zona "Ler a inscrição" da Aldeia (o Poço tem outra com o mesmo nome), mesmo desligada.
        static GameObject VillageZone() => Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(o =>
            o.name == "Ler_Inscricao" && o.scene.IsValid() && o.transform.parent != null && o.transform.parent.name == "Inscricao_Escondida");

        static IEnumerator VillageInscription()
        {
            yield return Restart("arquivo.aldeia_antes");
            Check(!Raised("RevelouAInscricaoDaAldeia"), "antes do Eco a escrita não deveria estar revelada");
            GameObject zone = VillageZone();
            Check(zone != null, "a zona \"Ler_Inscricao\" da Aldeia deveria existir");
            if (zone == null) yield break;
            Check(!zone.activeSelf, "antes do Eco a zona de leitura deveria estar desligada");
            yield return Face(ArchiveShortcuts.VillageWallSpot, 180f);
            Check(Prompt() != "Ler a inscrição", "antes do Eco não pode aparecer \"Ler a inscrição\"");

            // O Eco acende a escrita.
            EchoHiddenMark mark = zone.transform.parent.GetComponent<EchoHiddenMark>();
            Check(mark != null, "a escrita escondida deveria ter o EchoHiddenMark");
            mark.Reveal(3f, null);
            yield return new Delay(0.4f);
            Check(Raised("RevelouAInscricaoDaAldeia"), "o Eco deveria disparar RevelouAInscricaoDaAldeia");
            Check(zone.activeSelf, "depois do Eco a zona de leitura deveria ligar");
            yield return Face(ArchiveShortcuts.VillageWallSpot, 180f);
            Posted.Clear();
            yield return Interact("Ler a inscrição");
            Check(Reading().IsOpen, "ler deveria abrir a tela de leitura");
            Check(ReadingText("titleLabel") == Load<DocumentDefinition>("Documents/Aldeia_Inscricao").LocalizedTitle, "título da inscrição da Aldeia");
            Check(Raised("LeuAInscricaoDaAldeia"), "ler deveria disparar LeuAInscricaoDaAldeia");
            Check(Posted.Contains(Loc.Get(ArchiveNotifier.NoticeKey)), "a primeira entrada no Arquivo deveria avisar");
            CloseReading();
            yield return new Delay(0.3f);
            Check(ArchiveCatalog.Current.AllEntries().Any(e => e.Id == "doc.aldeia_inscricao" && e.IsUnlocked), "a inscrição da Aldeia deveria estar no Arquivo");

            // Depois de voltar do checkpoint, a leitura continua liberada.
            yield return SaveAndReload();
            Check(Raised("RevelouAInscricaoDaAldeia") && Raised("LeuAInscricaoDaAldeia"), "os eventos deveriam voltar do checkpoint");
            GameObject again = VillageZone();
            Check(again != null && again.activeSelf, "depois do checkpoint a zona de leitura deveria continuar ligada");
        }

        // ---------- Carta e Poço ----------

        static IEnumerator LetterAndWell()
        {
            yield return Restart("arquivo.carta");
            yield return Face(ArchiveShortcuts.LetterApproach, ArchiveShortcuts.LetterYaw);
            Check(Obj("Carta_Do_Soldado") != null, "a carta deveria estar nas Trincheiras");
            Posted.Clear();
            yield return Interact("Ler a carta");
            Check(Reading().IsOpen, "ler a carta deveria abrir a tela de leitura");
            Check(ReadingText("titleLabel") == Load<DocumentDefinition>("Documents/Trincheiras_CartaDoSoldado").LocalizedTitle, "título da carta");
            Check(Raised("LeuACartaDoSoldado"), "ler deveria disparar LeuACartaDoSoldado");
            Check(Posted.Contains(Loc.Get(ArchiveNotifier.NoticeKey)), "a primeira entrada no Arquivo deveria avisar");
            CloseReading();
            yield return new Delay(0.3f);
            Check(ArchiveCatalog.Current.AllEntries().Any(e => e.Id == "doc.trincheiras_carta_soldado" && e.IsUnlocked), "a carta deveria estar no Arquivo");

            // A carta está fora do caminho principal, mas a pé a partir da entrada.
            var path = new UnityEngine.AI.NavMeshPath();
            bool reachable = UnityEngine.AI.NavMesh.CalculatePath(new Vector3(0f, 0f, -77f), ArchiveShortcuts.LetterApproach, UnityEngine.AI.NavMesh.AllAreas, path);
            Check(reachable && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "a carta deveria ser alcançável a pé desde a entrada das trincheiras");

            // Poço: a inscrição entra no Arquivo.
            yield return Restart("arquivo.poco");
            Posted.Clear();
            yield return Interact("Ler a inscrição");
            Check(Reading().IsOpen && Raised("LeuAInscricaoDoPoco"), "ler a inscrição do Poço deveria abrir a leitura e disparar o evento");
            CloseReading();
            yield return new Delay(0.3f);
            Check(ArchiveCatalog.Current.AllEntries().Any(e => e.Id == "doc.poco_inscricao" && e.IsUnlocked), "a inscrição do Poço deveria estar no Arquivo");

            // A fita entra quando é pega e se relê em texto.
            RaiseEv("ColetouAFitaDeIrena1");
            yield return new Delay(0.2f);
            ArchiveCatalog.Entry tapeEntry = ArchiveCatalog.Current.AllEntries().First(e => e.IsTape);
            Check(tapeEntry.IsUnlocked && tapeEntry.Id == "tape.irena_1", "a fita deveria estar no Arquivo");
            OpenArchive();
            Check(Rows().Any(e => e.IsTape), "a fita pega deveria aparecer na lista");
            ClosePause();
        }

        // ---------- Achados da base ----------

        static IEnumerator BaseFinds()
        {
            // Registro do guincho: mochila ao pé sul do contêiner 5, fora da linha de tiro do atirador do zigurate.
            yield return Restart("arquivo.registro");
            yield return Face(ArchiveShortcuts.RegisterSpot, 0f);
            Posted.Clear();
            var sniper = UnityEngine.Object.FindFirstObjectByType<AI.SniperAI>();
            Vector3 eye = sniper.transform.position + Vector3.up * 1.6f;
            foreach (float height in new[] { 0.4f, 1.0f, 1.7f })
            {
                Vector3 target = ArchiveShortcuts.RegisterSpot + Vector3.up * height;
                bool blocked = Physics.Raycast(eye, (target - eye).normalized, out RaycastHit hit, (target - eye).magnitude - 0.05f, ~0, QueryTriggerInteraction.Ignore);
                Check(blocked, $"o atirador não deveria ver a Elena lendo o registro (altura {height:0.0} m, nada no caminho)");
            }
            yield return Interact("Ler o registro");
            Check(Reading().IsOpen, "ler o registro deveria abrir a tela de leitura");
            Check(ReadingText("titleLabel") == Load<DocumentDefinition>("Documents/Base_RegistroDoGuincho").LocalizedTitle, "título do registro");
            Check(Raised("LeuORegistroDoGuincho"), "ler deveria disparar LeuORegistroDoGuincho");
            Check(Posted.Contains(Loc.Get(ArchiveNotifier.NoticeKey)), "a primeira entrada no Arquivo deveria avisar");
            CloseReading();
            yield return new Delay(0.3f);
            Check(ArchiveCatalog.Current.AllEntries().Any(e => e.Id == "doc.base_registro_guincho" && e.IsUnlocked), "o registro deveria estar no Arquivo");
            Check(!sniper.IsLockedOn, "o atirador não deveria ter travado a mira na Elena durante a leitura");
            yield return Face(ArchiveShortcuts.RegisterSpot, 0f);
            Check(Prompt() == "Ler o registro", "a leitura é repetível");
            yield return SaveAndReload();
            Check(Raised("LeuORegistroDoGuincho"), "o evento deveria voltar do checkpoint");

            // Nota do selo: na ponta leste da mesa da tenda de provas, sem pegar nem esconder o 3º rolo do Tom.
            yield return Restart("arquivo.nota");
            GameObject roll = Obj("Rolo_De_Filme_3");
            GameObject note = Obj("Nota_Do_Selo_1927");
            Check(roll != null && roll.activeInHierarchy, "o 3º rolo do Tom deveria continuar na mesa");
            Check(note != null && roll != null && Vector3.Distance(roll.transform.position, note.transform.position) > 0.5f, "a nota não pode ficar em cima do rolo");
            yield return Face(ArchiveShortcuts.NoteSpot, 0f);
            Posted.Clear();
            yield return Interact("Ler a nota");
            Check(Reading().IsOpen, "ler a nota deveria abrir a tela de leitura");
            Check(ReadingText("titleLabel") == Load<DocumentDefinition>("Documents/Base_NotaDoSelo1927").LocalizedTitle, "título da nota");
            Check(Raised("LeuANotaDoSelo"), "ler deveria disparar LeuANotaDoSelo");
            CloseReading();
            yield return new Delay(0.3f);
            Check(ArchiveCatalog.Current.AllEntries().Any(e => e.Id == "doc.base_nota_selo_1927" && e.IsUnlocked), "a nota deveria estar no Arquivo");
            Check(roll.activeInHierarchy && Inv("RoloDeFilmeDoTom") == 0, "ler a nota não pode pegar o rolo");

            // Do lado do rolo (oeste da mesa) o aviso não é o da nota.
            yield return Face(new Vector3(31.2f, 0f, 22.6f), 0f);
            Check(Prompt() != "Ler a nota", $"junto do rolo o aviso não deveria ser o da nota (é \"{Prompt()}\")");

            // Nenhum dos dois fecha passo de missão: não estão no caminho principal.
            foreach (string name in new[] { "LeuORegistroDoGuincho", "LeuANotaDoSelo" })
            {
                var asset = Load<StoryEvent>("Events/" + name);
                bool used = AssetDatabase.FindAssets("t:QuestDefinition", new[] { "Assets/_Nibiru/Data" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                    .Any(q => q.steps != null && q.steps.Any(st => st.completeOn == asset));
                Check(!used, $"{name} não pode fechar passo de missão");
            }
        }
    }
}
