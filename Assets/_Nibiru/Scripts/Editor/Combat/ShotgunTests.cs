using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Interaction;
using Nibiru.Inventory;
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

namespace Nibiru.EditorTools.Weapons
{
    /// <summary>
    /// B6, a espingarda: atalhos de Play (Nibiru > Testes > Armas (B6)) e o teste automático. O teste cobre: a casa
    /// trancada (sem chave, chave dada pela Umm Karim, abrir, pegar a arma e os 6 cartuchos da região), a troca por 1/2 e
    /// pelo D-pad (também mirando), o dano perto e longe (espingarda e pistola), a recarga cartucho a cartucho
    /// interrompida sem perder munição, o cambaleio e a queda por tipo de inimigo (soldado, Lullu, Primeiro Servo, que leva
    /// o dano e não reage) e a arma e a munição guardadas no checkpoint e no slot. Controle, teclado e mouse virtuais.
    /// Mexe só numa pasta de teste. Menu: Nibiru > Testes > Armas (B6) > Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class ShotgunTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string MenuRoot = "Nibiru/Testes/Armas (B6)/";

        // Na rua, a leste da casa trancada (Casa_8), de frente para a porta (oeste).
        static readonly Vector3 DoorSpot = new Vector3(-42.0f, 0f, -12f);
        static readonly Vector3 TableSpot = new Vector3(-45.4f, 0f, -12.3f);

        static string folder;
        static Gamepad pad;
        static Keyboard realKeyboard, keyboard;
        static GameObject aimCam;
        static GameObject wall;

        static ShotgunTests()
        {
            Register("armas.teste", Begin);
            Register("armas.casa", () => House(withKey: true));
            Register("armas.casa_sem_chave", () => House(withKey: false));
            Register("armas.soldados", () => RemedyShortcuts.Base(new Vector3(-19.5f, 0f, 3f), 45f, GiveKit,
                "Armas (B6): na entrada da base, com a pistola, a espingarda carregada e 4 cartuchos de reserva. Tecla 1/2 ou D-pad trocam de arma. A menos de 3 m o tiro mata; de 3 a 6 m o soldado cambaleia."));
            Register("armas.lullu", () => Step11Shortcuts.Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco" }, 23, Step11Shortcuts.NearFlashSpot, 0f,
                "Armas (B6): um passo antes do flash, com o Lullu vivo, a pistola e a espingarda. O Lullu cambaleia com o tiro (não cai).",
                thenAlso: GiveKit));
            Register("armas.servo", () => Step11Shortcuts.Begin(Step12Shortcuts.Woke, Step12Shortcuts.FightStep, Step12Shortcuts.FightSpot, 0f,
                "Armas (B6): no ponto de retorno da luta, com a espingarda. O Primeiro Servo leva o dano (um quarto) e nunca cambaleia.",
                thenAlso: GiveKit));
        }

        [MenuItem(MenuRoot + "1. Diante da casa trancada, com a chave", priority = 1)]
        static void Menu1() => Go("armas.casa");

        [MenuItem(MenuRoot + "2. Diante da casa trancada, sem a chave", priority = 2)]
        static void Menu2() => Go("armas.casa_sem_chave");

        [MenuItem(MenuRoot + "3. Entrada da base: soldados, com a espingarda", priority = 3)]
        static void Menu3() => Go("armas.soldados");

        [MenuItem(MenuRoot + "4. Diante do Lullu, com a espingarda", priority = 4)]
        static void Menu4() => Go("armas.lullu");

        [MenuItem(MenuRoot + "5. Luta do Primeiro Servo, com a espingarda", priority = 5)]
        static void Menu5() => Go("armas.servo");

        [MenuItem(MenuRoot + "6. Teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("armas.teste");

        // ---------- Montagem dos atalhos ----------

        static readonly string[] B6Items = { "Espingarda", "Cartucho", "ChaveDaCasa" };

        static void ClearB6()
        {
            PlayerInventory bag = Object.FindFirstObjectByType<PlayerInventory>();
            if (bag == null) return;
            foreach (string item in B6Items)
            {
                ItemDefinition definition = Load<ItemDefinition>("Items/" + item);
                bag.Remove(definition, bag.CountOf(definition));
            }
        }

        // A pistola e a faca de sempre, mais a espingarda (carregada ao ser equipada) e 4 cartuchos de reserva.
        static void GiveKit()
        {
            ElenaShortcuts.GiveWeapons();
            PlayerInventory bag = Object.FindFirstObjectByType<PlayerInventory>();
            if (bag == null) return;
            bag.Add(Load<ItemDefinition>("Items/Espingarda"), 1);
            bag.Add(Load<ItemDefinition>("Items/Cartucho"), 4);
        }

        static void House(bool withKey)
        {
            ClearB6();
            Step9Shortcuts.Start(revealed: true, extra: null, 15, DoorSpot, 270f, () =>
            {
                ElenaShortcuts.GiveWeapons();
                ClearB6();
                if (withKey) Object.FindFirstObjectByType<PlayerInventory>().Add(Load<ItemDefinition>("Items/ChaveDaCasa"), 1);
                Debug.Log(withKey
                    ? "Armas (B6): diante da porta da casa trancada (Casa_8, a leste do pátio do altar), com a chave. Abra, pegue a espingarda e os 4 cartuchos da mesa."
                    : "Armas (B6): diante da porta da casa trancada, sem a chave: ela diz que está trancada. A chave vem da Umm Karim quando o Yusuf recebe os antibióticos.");
            });
        }

        // ---------- Teste automático ----------

        static void Begin()
        {
            folder = SaveStorage.TestFolder("Teste_Armas");
            SaveStorage.DeleteTestFolder(folder);
            SaveSlots.UseFolder(folder);
            Loc.SetLanguage("pt-BR", false);

            pad = InputSystem.AddDevice<Gamepad>("ControleDeTesteArmas");
            realKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("TecladoDeTesteArmas");

            Start("Espingarda (B6), teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Casa trancada: sem chave avisa; a Umm Karim dá a chave com o remédio; abrir gasta a chave; ela fica aberta depois de voltar do checkpoint", HouseAndKey),
                ("Pegar: a espingarda vem com 2 no cano, são 6 cartuchos na região e nenhum inimigo larga cartucho", PickUp),
                ("Troca de arma: teclas 1 e 2 e D-pad, também mirando; sem outra arma só avisa; bloqueada em menu; RB continua faca", Swap),
                ("Dano por distância: cheio até 5 m, cai até 35% aos 14 m e some além; a pistola não perde dano", Damage),
                ("Recarga cartucho a cartucho: interrompida por esquiva, faca e tiro sem perder munição; pistola cancelada não gasta", Reload),
                ("Soldado: de perto o tiro mata; vivo, cambaleia até 6 m e cai de perto com 6 projéteis; além de 6 m nada", SoldierReactions),
                ("Lullu: cambaleia e nunca cai", LulluReactions),
                ("Primeiro Servo: leva o dano e não cambaleia nem cai", ServoReactions),
                ("Salvamento: arma equipada, cartucho no pente e na reserva voltam do checkpoint e do slot", SaveAndSlot),
                ("Limpeza: tira os aparelhos de teste e apaga a pasta do teste", Cleanup),
            });
        }

        // ---------- Ajudas ----------

        static Until Frames(int count)
        {
            int target = Time.frameCount + count;
            return new Until(() => Time.frameCount >= target, 10f, $"{count} quadros");
        }

        static PlayerWeapons W() => Object.FindFirstObjectByType<PlayerWeapons>();

        static FirearmDefinition Gun(string name) => Load<FirearmDefinition>("Combat/" + name);

        static ItemDefinition Item(string name) => Load<ItemDefinition>("Items/" + name);

        static int Reserve(string item) => Bag().CountOf(Item(item));

        static Dictionary<FirearmDefinition, int> Loaded() =>
            (Dictionary<FirearmDefinition, int>)typeof(PlayerWeapons).GetField("loaded", Any).GetValue(W());

        static void SetLoaded(FirearmDefinition gun, int count) => Loaded()[gun] = count;

        static void SetReserve(string item, int count)
        {
            PlayerInventory bag = Bag();
            bag.Remove(Item(item), bag.CountOf(Item(item)));
            if (count > 0) bag.Add(Item(item), count);
        }

        static IEnumerator ToMoment(string id, float settle = 3f)
        {
            ClearB6();
            int old = Motor() != null ? Motor().GetInstanceID() : 0;
            Go(id);
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 60f, $"o atalho {id} recarregar a região");
            yield return new Delay(settle);
        }

        // Pistola e espingarda na bolsa (a espingarda carregada), com a pistola equipada.
        static IEnumerator ArmedInVillage()
        {
            yield return ToMoment("armas.casa_sem_chave", 3.5f);
            ClearB6();
            GiveKit();
            yield return Frames(3);
            W().Equip(Gun("Pistola"));
            W().CancelReload();
            yield return new Delay(0.6f);
        }

        static void Pad(float leftTrigger, params GamepadButton[] held)
        {
            var state = new GamepadState { leftTrigger = leftTrigger };
            foreach (GamepadButton b in held) state = state.WithButton(b);
            InputSystem.QueueStateEvent(pad, state);
        }

        static IEnumerator PadTap(params GamepadButton[] buttons)
        {
            Pad(0f, buttons);
            yield return Frames(2);
            Pad(0f);
            yield return Frames(3);
        }

        static IEnumerator KeyTap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return Frames(2);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
        }

        static void OpenPause() => typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), null);

        static void ClosePause() => typeof(PauseMenuUI).GetMethod("CloseMenu", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), new object[] { false });

        // Dispara como se o gatilho fosse apertado (a entrada de verdade é conferida no teste de troca e de recarga).
        static void Fire() => typeof(PlayerWeapons).GetMethod("TryFire", Any).Invoke(W(), null);

        // A câmera de mira passa a ser um ponto na altura do peito da Elena olhando para o alvo: o tiro acerta onde o teste quer.
        static void AimAt(Vector3 point)
        {
            if (aimCam == null) aimCam = new GameObject("Teste_CameraDeMira");
            aimCam.transform.position = Motor().transform.position + Vector3.up * 1.4f;
            aimCam.transform.rotation = Quaternion.LookRotation((point - aimCam.transform.position).normalized);
            typeof(PlayerWeapons).GetField("aimCamera", Any).SetValue(W(), aimCam.transform);
        }

        static Vector3 ChestOf(Component enemy)
        {
            Collider body = enemy.GetComponentsInChildren<Collider>().FirstOrDefault(c => !c.isTrigger && c.enabled);
            return body != null ? body.bounds.center : enemy.transform.position + Vector3.up * 1.1f;
        }

        // Uma parede larga e firme no ar (para o tiro não achar mais nada), a esta distância da Elena, olhando para cima e para leste.
        static Health MakeWall(float distance)
        {
            if (wall != null) Object.Destroy(wall);
            Vector3 direction = Quaternion.Euler(-35f, 90f, 0f) * Vector3.forward;
            Vector3 origin = Motor().transform.position + Vector3.up * 1.4f;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Teste_ParedeDeTiro";
            wall.transform.position = origin + direction * distance;
            wall.transform.rotation = Quaternion.LookRotation(direction);
            wall.transform.localScale = new Vector3(12f, 12f, 0.2f);
            var health = wall.AddComponent<Health>();
            health.SetMax(1000000f);
            AimAt(origin + direction * distance);
            return health;
        }

        // Atira (esperando o intervalo) e devolve o dano que a parede levou.
        static IEnumerator ShootWall(float distance, Action<float> result)
        {
            Health target = MakeWall(distance);
            yield return new Delay(0.5f);
            float before = target.Current;
            Fire();
            yield return Frames(2);
            result(before - target.Current);
        }

        // ---------- 1. Casa trancada e chave ----------

        static IEnumerator HouseAndKey()
        {
            yield return ToMoment("armas.casa_sem_chave", 3.5f);
            LockedDoor door = Obj("Porta_Trancada") != null ? Obj("Porta_Trancada").GetComponent<LockedDoor>() : null;
            Check(door != null, "a porta trancada deveria existir na cena (menu Povoar a espingarda (B6))");
            if (door == null) yield break;

            Teleport(DoorSpot, 270f);
            yield return new Delay(0.8f);
            Check(Item("ChaveDaCasa") != null && Bag().CountOf(Item("ChaveDaCasa")) == 0, "a Elena não deveria ter a chave no começo");
            Check(Prompt() == Loc.Get("ui.interact.porta_trancada"), $"sem a chave o aviso deveria ser \"Porta trancada\": \"{Prompt()}\"");
            int posted = Posted.Count;
            PlayerInteractor interactor = Motor().GetComponent<PlayerInteractor>();
            interactor.Current?.Interact(interactor);
            yield return new Delay(0.3f);
            Check(Posted.Skip(posted).Contains(Loc.Get("ui.interact.porta_trancada_aviso")), "sem a chave, a porta deveria avisar que está trancada");
            Check(!door.IsOpen && !Raised("AbriuACasaTrancada"), "sem a chave a porta não deveria abrir");

            // A Umm Karim dá a chave quando o Yusuf recebe os antibióticos.
            Raise("EntregouAntibioticos");
            yield return new Delay(0.5f);
            Check(Bag().CountOf(Item("ChaveDaCasa")) == 1, $"com o remédio entregue a Elena deveria ter 1 chave: {Bag().CountOf(Item("ChaveDaCasa"))}");
            Check(Prompt() == Loc.Get("ui.interact.porta_abrir"), $"com a chave o aviso deveria ser \"Abrir a porta\": \"{Prompt()}\"");
            interactor.Current?.Interact(interactor);
            yield return new Delay(0.5f);
            Check(Raised("AbriuACasaTrancada") && door.IsOpen, "com a chave a porta deveria abrir e disparar o evento");
            Check(Bag().CountOf(Item("ChaveDaCasa")) == 0, "abrir deveria gastar a chave");
            Check(!door.GetComponent<Collider>().enabled, "aberta, a porta não deveria mais ter colisor");

            // Volta do checkpoint: a porta continua aberta e a chave não é dada de novo.
            yield return SaveAndReload();
            LockedDoor again = Obj("Porta_Trancada").GetComponent<LockedDoor>();
            Check(again.IsOpen, "depois de voltar do checkpoint a porta deveria continuar aberta");
            Check(Bag().CountOf(Item("ChaveDaCasa")) == 0, "depois de voltar do checkpoint a chave não deveria ser dada de novo");
        }

        // ---------- 2. Pegar ----------

        static IEnumerator PickUp()
        {
            yield return ToMoment("armas.casa_sem_chave", 3.5f);

            // Contas do mapa: 4 cartuchos na casa, mais 2 no cano = 6 na região; ninguém larga cartucho.
            ItemDefinition cartridge = Item("Cartucho");
            var pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int lying = pickups.Where(p => (ItemDefinition)typeof(ItemPickup).GetField("item", Any).GetValue(p) == cartridge)
                .Sum(p => (int)typeof(ItemPickup).GetField("amount", Any).GetValue(p));
            Check(lying == 4, $"deveria haver 4 cartuchos no chão da região: {lying}");
            Check(Gun("Espingarda").magazineSize == 2, "a espingarda deveria vir com 2 no cano");
            var drops = Resources.FindObjectsOfTypeAll<DropOnDeath>();
            foreach (DropOnDeath drop in drops)
            {
                var serialized = new SerializedObject(drop);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == cartridge)
                        Check(false, $"{drop.name} larga cartucho; soldado não pode largar");
            }

            // Pegar a arma e os cartuchos da mesa.
            Teleport(TableSpot, 270f);
            yield return new Delay(0.8f);
            PlayerInteractor interactor = Motor().GetComponent<PlayerInteractor>();
            Check(Obj("Espingarda_Casa") != null && Obj("Municao_Cartuchos_Casa") != null, "a mesa deveria ter a espingarda e os cartuchos");
            Obj("Espingarda_Casa").GetComponent<ItemPickup>().Interact(interactor);
            Obj("Municao_Cartuchos_Casa").GetComponent<ItemPickup>().Interact(interactor);
            yield return new Delay(0.6f);
            Check(Bag().CountOf(Item("Espingarda")) == 1, "a Elena deveria ter pegado a espingarda");
            Check(Reserve("Cartucho") == 4, $"a reserva deveria ser 4 cartuchos: {Reserve("Cartucho")}");
            Check(W().OwnedFirearms().Count == 2, $"a Elena deveria ter as duas armas: {W().OwnedFirearms().Count}");
            Check(W().Equipped == Gun("Pistola"), "a pistola, que ela já tinha, deveria continuar equipada");
            W().Equip(Gun("Espingarda"));
            Check(W().LoadedRounds == 2 && Reserve("Cartucho") == 4, $"a espingarda deveria vir com 2 no cano e 4 de reserva: {W().LoadedRounds}/{Reserve("Cartucho")}");
        }

        // ---------- 3. Troca ----------

        static IEnumerator Swap()
        {
            yield return ArmedInVillage();
            PlayerWeapons weapons = W();
            FirearmDefinition pistol = Gun("Pistola"), shotgun = Gun("Espingarda");
            Check(weapons.Equipped == pistol, "no começo a pistola deveria estar equipada");

            yield return KeyTap(Key.Digit2);
            Check(weapons.Equipped == shotgun, $"a tecla 2 deveria equipar a espingarda: {weapons.Equipped?.id}");
            Check(weapons.IsSwapping, "logo depois da tecla 2 a troca deveria estar em curso (0,4 s)");
            Check(Posted.Any(m => m == "Espingarda de cano duplo equipada"), "a troca deveria avisar \"Espingarda de cano duplo equipada\"");
            yield return new Delay(0.5f);
            Check(!weapons.IsSwapping, "depois de 0,5 s a troca deveria ter acabado");

            yield return KeyTap(Key.Digit1);
            Check(weapons.Equipped == pistol, "a tecla 1 deveria equipar a pistola");
            yield return new Delay(0.5f);

            yield return PadTap(GamepadButton.DpadRight);
            Check(weapons.Equipped == shotgun, "D-pad direita deveria ir para a próxima arma (a espingarda)");
            yield return new Delay(0.5f);
            yield return PadTap(GamepadButton.DpadLeft);
            Check(weapons.Equipped == pistol, "D-pad esquerda deveria voltar para a pistola");
            yield return new Delay(0.5f);
            yield return PadTap(GamepadButton.DpadLeft);
            Check(weapons.Equipped == shotgun, "D-pad esquerda na primeira arma deveria dar a volta para a última");
            yield return new Delay(0.5f);

            // Mirando (LT seguro): troca sem largar a mira, e não atira durante a troca de 0,4 s.
            Pad(1f);
            yield return Frames(6);
            Check(weapons.IsAiming, "com o gatilho esquerdo seguro a Elena deveria estar mirando");
            Pad(1f, GamepadButton.DpadLeft);
            yield return Frames(2);
            Pad(1f);
            yield return Frames(2);
            Check(weapons.Equipped == pistol, "mirando, D-pad esquerda deveria trocar para a pistola");
            Check(weapons.IsAiming, "a troca mirando não deveria largar a mira");
            Check(weapons.IsSwapping, "a troca mirando também leva 0,4 s");
            int loadedBefore = weapons.LoadedRounds;
            Fire();
            Check(weapons.LoadedRounds == loadedBefore, "durante a troca não se atira");
            yield return new Delay(0.6f);
            Check(!weapons.IsSwapping, "a troca mirando deveria ter acabado");
            Pad(0f);
            yield return Frames(4);

            // RB sozinho continua faca, com a espingarda na mão.
            weapons.Equip(shotgun);
            yield return new Delay(0.6f);
            PlayerMeleeAttack melee = Motor().GetComponent<PlayerMeleeAttack>();
            Pad(0f, GamepadButton.RightShoulder);
            yield return Frames(4);
            Check(melee.IsAttacking, "RB sozinho deveria ser a faca, mesmo com a espingarda");
            Pad(0f);
            yield return new Delay(0.8f);

            // Menus bloqueiam a troca.
            OpenPause();
            yield return new Delay(0.3f);
            yield return KeyTap(Key.Digit1);
            Check(weapons.Equipped == shotgun, "com a pausa aberta a tecla 1 não deveria trocar");
            ClosePause();
            yield return new Delay(0.4f);

            // Só uma arma: o D-pad avisa e a tecla do lugar vazio avisa.
            Bag().Remove(Item("Espingarda"), 1);
            yield return new Delay(0.3f);
            Check(weapons.Equipped == pistol, "sem a espingarda na bolsa a pistola deveria ser a equipada");
            int posted = Posted.Count;
            yield return PadTap(GamepadButton.DpadRight);
            Check(Posted.Skip(posted).Contains("Sem outra arma"), "com uma arma só o D-pad deveria avisar \"Sem outra arma\"");
            yield return KeyTap(Key.Digit2);
            Check(Posted.Skip(posted).Contains("Sem arma neste lugar"), "a tecla 2 sem a espingarda deveria avisar \"Sem arma neste lugar\"");
            Check(weapons.Equipped == pistol, "a pistola deveria continuar equipada");
        }

        // ---------- 4. Dano por distância ----------

        static IEnumerator Damage()
        {
            yield return ArmedInVillage();
            PlayerWeapons weapons = W();
            FirearmDefinition shotgun = Gun("Espingarda"), pistol = Gun("Pistola");
            weapons.Equip(shotgun);
            yield return new Delay(0.6f);

            foreach (float distance in new[] { 3f, 5f, 9.5f, 13.5f })
            {
                SetLoaded(shotgun, 2);
                float taken = 0f;
                yield return ShootWall(distance, d => taken = d);
                float hitDistance = distance - 0.1f; // a face da parede fica 0,1 m mais perto
                float expected = 8 * shotgun.damagePerPellet * shotgun.DamageFactor(hitDistance);
                Check(Mathf.Abs(taken - expected) < 0.5f, $"a {distance:0.0} m a espingarda deveria tirar {expected:0.0} (8 projéteis): tirou {taken:0.0}");
                Check(Mathf.Abs(weapons.LastPelletDamage - shotgun.damagePerPellet * shotgun.DamageFactor(hitDistance)) < 0.4f,
                    $"a {distance:0.0} m cada projétil deveria valer {shotgun.damagePerPellet * shotgun.DamageFactor(hitDistance):0.0}: {weapons.LastPelletDamage:0.0}");
            }
            // Cheio até 5 m, 35% no alcance máximo, além do alcance nada.
            Check(Mathf.Approximately(shotgun.DamageFactor(5f), 1f), "até 5 m o dano deveria ser cheio");
            Check(Mathf.Abs(shotgun.DamageFactor(14f) - 0.35f) < 0.001f, $"aos 14 m o dano deveria ser 35%: {shotgun.DamageFactor(14f):0.00}");
            SetLoaded(shotgun, 2);
            float beyond = -1f;
            yield return ShootWall(15f, d => beyond = d);
            Check(beyond == 0f, $"além do alcance (15 m) o tiro não deveria ferir: {beyond:0.0}");

            // A pistola não perde dano com a distância.
            weapons.Equip(pistol);
            yield return new Delay(0.6f);
            foreach (float distance in new[] { 3f, 40f })
            {
                SetLoaded(pistol, 8);
                float taken = 0f;
                yield return ShootWall(distance, d => taken = d);
                Check(Mathf.Abs(taken - pistol.damagePerPellet) < 0.5f, $"a {distance:0} m a pistola deveria tirar {pistol.damagePerPellet:0}: tirou {taken:0.0}");
            }
        }

        // ---------- 5. Recarga ----------

        static IEnumerator Reload()
        {
            yield return ArmedInVillage();
            PlayerWeapons weapons = W();
            FirearmDefinition shotgun = Gun("Espingarda"), pistol = Gun("Pistola");
            weapons.Equip(shotgun);
            yield return new Delay(0.6f);

            // Recarga inteira: um cartucho a cada 0,8 s depois de 0,5 s para abrir.
            SetLoaded(shotgun, 0);
            SetReserve("Cartucho", 4);
            yield return KeyTap(Key.R);
            Check(weapons.IsReloading, "a tecla R deveria começar a recarga");
            yield return new Delay(1.0f);
            Check(weapons.LoadedRounds == 0, $"depois de 1,0 s ainda nenhum cartucho deveria ter entrado: {weapons.LoadedRounds}");
            yield return new Delay(0.7f);
            Check(weapons.LoadedRounds == 1 && Reserve("Cartucho") == 3, $"com ~1,5 s deveria ter entrado 1 cartucho (1 no cano, 3 de reserva): {weapons.LoadedRounds}/{Reserve("Cartucho")}");
            Check(weapons.IsReloading, "com 1 no cano e reserva a recarga deveria continuar");
            yield return new Delay(0.9f);
            Check(weapons.LoadedRounds == 2 && Reserve("Cartucho") == 2 && !weapons.IsReloading,
                $"com ~2,4 s os 2 cartuchos deveriam estar dentro e a recarga acabada: {weapons.LoadedRounds}/{Reserve("Cartucho")}, recarregando {weapons.IsReloading}");

            // Interrompida pela esquiva antes do primeiro cartucho: nada se perde.
            SetLoaded(shotgun, 0);
            SetReserve("Cartucho", 4);
            yield return KeyTap(Key.R);
            yield return new Delay(0.3f);
            Check(weapons.IsReloading, "a recarga deveria estar em curso");
            yield return PadTap(GamepadButton.East);
            Check(!weapons.IsReloading, "a esquiva deveria interromper a recarga");
            Check(weapons.LoadedRounds == 0 && Reserve("Cartucho") == 4, $"interrompida antes do primeiro cartucho nada se perde: {weapons.LoadedRounds}/{Reserve("Cartucho")}");
            yield return new Delay(1.0f);

            // Interrompida pela esquiva depois do primeiro cartucho: o que entrou fica.
            SetLoaded(shotgun, 0);
            SetReserve("Cartucho", 4);
            yield return KeyTap(Key.R);
            yield return new Delay(1.6f);
            Check(weapons.LoadedRounds == 1, $"deveria ter entrado 1 cartucho: {weapons.LoadedRounds}");
            yield return PadTap(GamepadButton.East);
            Check(!weapons.IsReloading, "a esquiva deveria interromper a recarga");
            Check(weapons.LoadedRounds == 1 && Reserve("Cartucho") == 3, $"o cartucho que entrou fica: {weapons.LoadedRounds}/{Reserve("Cartucho")} (total {weapons.LoadedRounds + Reserve("Cartucho")} de 4)");
            yield return new Delay(1.0f);

            // Interrompida pela faca (RB).
            SetLoaded(shotgun, 0);
            SetReserve("Cartucho", 4);
            yield return KeyTap(Key.R);
            yield return new Delay(1.6f);
            Pad(0f, GamepadButton.RightShoulder);
            yield return Frames(4);
            Pad(0f);
            Check(!weapons.IsReloading, "a faca deveria interromper a recarga");
            Check(weapons.LoadedRounds + Reserve("Cartucho") == 4, $"a faca não gasta munição: {weapons.LoadedRounds}+{Reserve("Cartucho")}");
            yield return new Delay(1.0f);

            // Interrompida pelo tiro (com cartucho no cano); sem cartucho no cano o tiro não interrompe.
            SetLoaded(shotgun, 0);
            SetReserve("Cartucho", 4);
            yield return KeyTap(Key.R);
            yield return new Delay(0.6f);
            AimAt(Motor().transform.position + Vector3.up * 30f);
            Fire();
            Check(weapons.IsReloading && weapons.LoadedRounds == 0, "sem cartucho no cano o tiro não deveria interromper nem disparar");
            yield return new Delay(1.2f);
            Check(weapons.LoadedRounds == 1 && weapons.IsReloading, $"com 1 no cano a recarga deveria seguir: {weapons.LoadedRounds}");
            Fire();
            Check(!weapons.IsReloading, "atirar com cartucho no cano deveria interromper a recarga");
            Check(weapons.LoadedRounds == 0 && Reserve("Cartucho") == 3, $"o tiro gasta o cartucho do cano e a reserva fica: {weapons.LoadedRounds}/{Reserve("Cartucho")}");

            // Pistola: pente inteiro; cancelar não gasta nada.
            weapons.Equip(pistol);
            yield return new Delay(0.6f);
            SetLoaded(pistol, 3);
            int reserve = Reserve("Municao9mm");
            yield return KeyTap(Key.R);
            yield return new Delay(0.4f);
            Check(weapons.IsReloading, "a recarga da pistola deveria estar em curso");
            yield return PadTap(GamepadButton.East);
            Check(!weapons.IsReloading && weapons.LoadedRounds == 3 && Reserve("Municao9mm") == reserve,
                $"cancelar a recarga da pistola não gasta nada: {weapons.LoadedRounds}/{Reserve("Municao9mm")} (reserva era {reserve})");
        }

        // ---------- 6 a 8. Inimigos ----------

        // Põe a Elena a esta distância do inimigo, num lugar livre em volta dele (chão da malha de navegação e linha de tiro limpa),
        // de frente para ele, e mira no peito dele. O inimigo fica congelado onde está.
        static void FaceEnemy(Component enemy, float distance)
        {
            SetFrozen(enemy, true);
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            Vector3 chest = ChestOf(enemy);
            Vector3 foot = enemy.transform.position;
            // O pé do inimigo é o chão embaixo dele (o ponto de origem pode estar no meio do corpo).
            if (Physics.Raycast(foot + Vector3.up * 0.3f, Vector3.down, out RaycastHit under, 4f, ~0, QueryTriggerInteraction.Ignore)) foot = under.point;
            for (int step = 0; step < 24; step++)
            {
                // Começa pelo sul (longe do gatilho do flash, que fica ao norte do Lullu) e dá a volta.
                float angle = ((187f + step * 15f) % 360f) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 spot = foot + direction * distance;
                // Chão firme na mesma altura do inimigo.
                if (!Physics.Raycast(spot + Vector3.up * 1.5f, Vector3.down, out RaycastHit floor, 3f, ~0, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(floor.point.y - foot.y) > 0.4f) continue;
                // Linha de tiro limpa, e espaço para a Elena em pé.
                Vector3 eye = floor.point + Vector3.up * 1.4f;
                if (Physics.Linecast(eye, chest, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(enemy.transform)) continue;
                if (Physics.CheckCapsule(floor.point + Vector3.up * 0.4f, floor.point + Vector3.up * 1.7f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vector3 look = foot - floor.point;
                look.y = 0f;
                Teleport(floor.point + Vector3.up * 1.05f, Quaternion.LookRotation(look).eulerAngles.y);
                Physics.SyncTransforms();
                AimAt(chest);
                return;
            }
            Check(false, $"não achei um lugar livre a {distance:0.0} m de {enemy.name} para atirar");
        }

        // Congela a IA do inimigo entre os tiros (o HitReactor usa o mesmo travão durante a reação).
        static void SetFrozen(Component enemy, bool frozen)
        {
            var soldier = enemy.GetComponent<SoldierAI>();
            if (soldier != null) soldier.ReactionLock = frozen;
            var sniper = enemy.GetComponent<SniperAI>();
            if (sniper != null) sniper.ReactionLock = frozen;
            var lullu = enemy.GetComponent<LulluAI>();
            if (lullu != null) lullu.ReactionLock = frozen;
        }

        static IEnumerator ShootReaction(Health target, Component enemy, float distance, Action<HitReaction, float, int> result)
        {
            // Espera o inimigo ficar livre de uma reação anterior.
            HitReactor old = enemy.GetComponent<HitReactor>();
            if (old != null) yield return new Until(() => !old.Busy, 10f, "a reação anterior acabar");
            yield return null;
            target.Restore(target.Max);
            SetFrozen(enemy, true);
            FaceEnemy(enemy, distance);
            yield return Frames(2);
            AimAt(ChestOf(enemy));
            SetLoaded(Gun("Espingarda"), 2);
            yield return new Delay(0.5f);
            AimAt(ChestOf(enemy));
            float before = target.Current;
            Fire();
            yield return Frames(3);
            HitReactor reactor = enemy.GetComponent<HitReactor>();
            result(reactor != null ? reactor.Current : HitReaction.Nenhuma, before - target.Current, W().LastShotReactions);
            // Solta o travão que o teste pôs (se o HitReactor travou, ele mesmo solta ao acabar).
            if (reactor == null || !reactor.Busy) SetFrozen(enemy, false);
        }

        static IEnumerator SoldierReactions()
        {
            yield return ToMoment("armas.soldados", 4f);
            W().Equip(Gun("Espingarda"));
            yield return new Delay(0.6f);
            SoldierAI soldier = Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None).FirstOrDefault(s => s.CurrentState != SoldierAI.State.Morto);
            Check(soldier != null, "deveria haver um soldado vivo na entrada da base");
            if (soldier == null) yield break;
            var health = soldier.GetComponent<Health>();

            // A queima-roupa o tiro mata um soldado (8 x 16 = 128 contra 100).
            FaceEnemy(soldier, 2.5f);
            yield return Frames(2);
            AimAt(ChestOf(soldier));
            yield return new Delay(0.5f);
            AimAt(ChestOf(soldier));
            SetLoaded(Gun("Espingarda"), 2);
            Fire();
            yield return new Delay(0.5f);
            Check(health.IsDead, $"a 2,5 m um tiro deveria matar o soldado (vida {health.Current:0})");

            // Os soldados seguintes ficam vivos para o teste (mais vida), para ver a reação.
            SoldierAI other = Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None).FirstOrDefault(s => s != soldier && s.CurrentState != SoldierAI.State.Morto);
            Check(other != null, "deveria haver um segundo soldado vivo");
            if (other == null) yield break;
            var otherHealth = other.GetComponent<Health>();
            otherHealth.SetMax(2000f);

            HitReaction kind = HitReaction.Nenhuma; float taken = 0f; int count = 0;
            yield return ShootReaction(otherHealth, other, 2.5f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Queda, $"a 2,5 m com 6 ou mais projéteis o soldado deveria cair: {kind} (tirou {taken:0})");
            Check(other.ReactionLock, "caído, a IA do soldado deveria estar parada");
            yield return new Until(() => !other.GetComponent<HitReactor>().Busy, 6f, "o soldado levantar");
            Check(!other.ReactionLock, "depois de levantar a IA deveria voltar");

            yield return ShootReaction(otherHealth, other, 5f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Cambaleio, $"a 5 m o soldado deveria cambalear (não cair): {kind} (tirou {taken:0})");
            yield return new Until(() => !other.GetComponent<HitReactor>().Busy, 6f, "o soldado se recompor");
            Check(!other.ReactionLock, "depois do cambaleio a IA deveria voltar");

            yield return ShootReaction(otherHealth, other, 8f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Nenhuma, $"a 8 m o soldado não deveria reagir: {kind} (tirou {taken:0})");

            // O atirador de elite (se estiver por perto) também reage.
            SniperAI sniper = Object.FindObjectsByType<SniperAI>(FindObjectsSortMode.None).FirstOrDefault(s => s.CurrentState != SoldierAI.State.Morto);
            if (sniper != null)
            {
                var sniperHealth = sniper.GetComponent<Health>();
                sniperHealth.SetMax(2000f);
                // O atirador fica em lugar alto: confere o tipo (aceita queda e cambaleio), sem depender do lugar onde ele está.
                Check(HitReactor.Allowed(sniper.gameObject, HitReaction.Queda) == HitReaction.Queda, "o atirador de elite deveria aceitar a queda");
                Check(HitReactor.Allowed(sniper.gameObject, HitReaction.Cambaleio) == HitReaction.Cambaleio, "o atirador de elite deveria aceitar o cambaleio");
            }
        }

        static IEnumerator LulluReactions()
        {
            yield return ToMoment("armas.lullu", 4f);
            W().Equip(Gun("Espingarda"));
            yield return new Delay(0.6f);
            LulluAI lullu = Object.FindObjectsByType<LulluAI>(FindObjectsSortMode.None).FirstOrDefault(l => l.CurrentState != LulluAI.State.Morto && l.GetComponent<FirstServantFight>() == null);
            Check(lullu != null, "deveria haver um Lullu vivo perto do flash");
            if (lullu == null) yield break;
            var health = lullu.GetComponent<Health>();
            health.SetMax(2000f);

            HitReaction kind = HitReaction.Nenhuma; float taken = 0f; int count = 0;
            yield return ShootReaction(health, lullu, 2.5f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Cambaleio, $"o Lullu deveria cambalear e nunca cair, mesmo a 2,5 m: {kind} (tirou {taken:0})");
            Check(taken > 0f, "o Lullu deveria levar o dano do tiro");
            Check(lullu.ReactionLock, "cambaleando, a IA do Lullu deveria estar parada");
            yield return new Until(() => !lullu.GetComponent<HitReactor>().Busy, 6f, $"o Lullu se recompor (escala de tempo {Time.timeScale}, flash {Raised("FlashDoPoco")})");
            Check(!lullu.ReactionLock, "depois do cambaleio a IA do Lullu deveria voltar");

            yield return ShootReaction(health, lullu, 8f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Nenhuma, $"a 8 m o Lullu não deveria reagir: {kind}");
        }

        static IEnumerator ServoReactions()
        {
            yield return ToMoment("armas.servo", 4f);
            W().Equip(Gun("Espingarda"));
            yield return new Delay(0.6f);
            FirstServantFight fight = Step12Shortcuts.Servant();
            Check(fight != null, "o Primeiro Servo deveria estar na antecâmara");
            if (fight == null) yield break;
            var health = fight.GetComponent<Health>();
            Check(HitReactor.Allowed(fight.gameObject, HitReaction.Queda) == HitReaction.Nenhuma, "o Servo não deveria aceitar queda");
            Check(HitReactor.Allowed(fight.gameObject, HitReaction.Cambaleio) == HitReaction.Nenhuma, "o Servo não deveria aceitar cambaleio");

            HitReaction kind = HitReaction.Nenhuma; float taken = 0f; int count = 0;
            yield return ShootReaction(health, fight, 2.5f, (k, t, n) => { kind = k; taken = t; count = n; });
            Check(kind == HitReaction.Nenhuma && count == 0, $"o Servo não deveria reagir: {kind}, reações {count}");
            Check(taken > 0f, $"o Servo deveria levar o dano da espingarda (tirou {taken:0.0})");
            Check(fight.GetComponent<HitReactor>() == null, "o Servo não deveria ganhar um HitReactor");
            Check(fight.GetComponent<LulluAI>() != null, "o Servo é uma LulluAI com a luta por cima");
        }

        // ---------- 9. Salvamento ----------

        static IEnumerator SaveAndSlot()
        {
            yield return ArmedInVillage();
            PlayerWeapons weapons = W();
            FirearmDefinition shotgun = Gun("Espingarda");
            weapons.Equip(shotgun);
            yield return new Delay(0.6f);
            SetLoaded(shotgun, 1);
            SetReserve("Cartucho", 3);
            SetLoaded(Gun("Pistola"), 5);

            // Checkpoint na memória (como morrer e voltar).
            yield return SaveAndReload();
            weapons = W();
            Check(weapons.Equipped == shotgun, $"a espingarda deveria voltar equipada: {weapons.Equipped?.id}");
            Check(weapons.LoadedRounds == 1 && Reserve("Cartucho") == 3, $"deveria voltar 1 no cano e 3 de reserva: {weapons.LoadedRounds}/{Reserve("Cartucho")}");
            Check(Loaded()[Gun("Pistola")] == 5, "o pente da pistola deveria voltar com 5");

            // Slot em disco: grava, limpa a memória e carrega.
            SaveSlots.BeginNewGame(1, RegionSceneName);
            Motor().GetComponent<PlayerRespawn>().SaveCheckpointHere();
            SaveSlots.WriteCurrent();
            WorldStateClear();
            int old = Motor().GetInstanceID();
            bool ok = SaveSlots.Load(1);
            Check(ok, "carregar o slot 1 deveria funcionar");
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 40f, "a cena recarregar");
            yield return new Delay(2f);
            weapons = W();
            Check(weapons.Equipped == shotgun, $"do slot: a espingarda deveria voltar equipada: {weapons.Equipped?.id}");
            Check(weapons.LoadedRounds == 1 && Reserve("Cartucho") == 3, $"do slot: 1 no cano e 3 de reserva: {weapons.LoadedRounds}/{Reserve("Cartucho")}");
            Check(Bag().CountOf(Item("Espingarda")) == 1, "do slot: a espingarda deveria estar na bolsa");
        }

        static void WorldStateClear()
        {
            WorldState.Clear();
            Checkpoints.Clear();
            SaveSlots.Detach();
        }

        // ---------- Limpeza ----------

        static IEnumerator Cleanup()
        {
            Check(SaveStorage.RealFingerprint() == AutomatedRun.StartFingerprint, "os slots reais não deveriam mudar durante o teste");
            if (wall != null) Object.Destroy(wall);
            if (aimCam != null) Object.Destroy(aimCam);
            ClearB6();
            foreach (InputDevice device in new InputDevice[] { pad, keyboard })
                if (device != null) InputSystem.RemoveDevice(device);
            pad = null;
            keyboard = null;
            if (realKeyboard != null) realKeyboard.MakeCurrent();
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder != null && folder.EndsWith("Teste_Armas")) SaveStorage.DeleteTestFolder(folder);
            yield break;
        }
    }
}
