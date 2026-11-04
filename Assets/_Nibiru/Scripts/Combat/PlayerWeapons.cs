using System.Collections.Generic;
using Nibiru.Localization;
using Nibiru.Cameras;
using Nibiru.Inventory;
using Nibiru.Player;
using Nibiru.Stealth;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Armas de fogo da Elena. Segurando Mirar, a câmera aproxima e ela anda devagar
    /// olhando para a mira; Atirar dispara do centro da tela. A munição carregada fica
    /// em cada arma; a reserva fica no inventário. A primeira arma pega é equipada sozinha;
    /// para trocar: teclas 1 e 2, D-pad esquerda e direita, ou usar a arma no inventário (B6).
    /// A espingarda recarrega cartucho a cartucho (dá para interromper), perde dano com a distância
    /// e empurra o alvo (<see cref="HitReactor"/>).
    /// </summary>
    public class PlayerWeapons : MonoBehaviour
    {
        /// <summary>Cópia do estado das armas, para o checkpoint.</summary>
        public class Snapshot
        {
            public FirearmDefinition equipped;
            public Dictionary<FirearmDefinition, int> loaded;
        }

        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Health ownHealth;
        [SerializeField] PlayerCameraController cameraController;

        [Tooltip("Câmera de onde sai a mira. Vazio = Main Camera.")]
        [SerializeField] Transform aimCamera;

        [Header("Visual")]
        [Tooltip("Objeto da arma na mão, mostrado só enquanto mira.")]
        [SerializeField] GameObject gunVisual;

        [Tooltip("Ponta do cano, de onde sai o rastro do tiro.")]
        [SerializeField] Transform muzzle;

        /// <summary>Modelo e ponta do cano de uma arma, filhos do objeto da arma na mão (B6).</summary>
        [System.Serializable]
        public class WeaponModel
        {
            public FirearmDefinition firearm;
            public GameObject model;
            public Transform muzzle;

            [Tooltip("Onde a mão direita segura a arma (cabo). Posição e giro em relação à arma na mão da Elena.")]
            public Transform rightGrip;

            [Tooltip("Onde a mão esquerda apoia a arma (guarda-mão, ou a frente do cabo da pistola). Vazio = a mão esquerda segue a animação.")]
            public Transform leftGrip;
        }

        [Tooltip("Um modelo por arma, dentro do objeto da arma na mão. Só o da arma equipada fica visível.")]
        [SerializeField] WeaponModel[] models;

        [Tooltip("Ataque de faca: atacar cancela a recarga.")]
        [SerializeField] PlayerMeleeAttack melee;

        [Tooltip("Linha que mostra o rastro do tiro por um instante.")]
        [SerializeField] LineRenderer tracer;

        [Tooltip("Material da marca de impacto.")]
        [SerializeField] Material impactMaterial;

        [Tooltip("Camadas que os tiros acertam.")]
        [SerializeField] LayerMask hitMask = Physics.DefaultRaycastLayers;

        const float TracerTime = 0.05f;
        const float ImpactLifetime = 1.5f;

        readonly Dictionary<FirearmDefinition, int> loaded = new Dictionary<FirearmDefinition, int>();
        readonly RaycastHit[] hits = new RaycastHit[16];
        float nextShotTime;
        float reloadEndTime = -1f;
        float swapEndTime = -1f;
        float tracerHideTime;
        bool autoEquipPending;

        public FirearmDefinition Equipped { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsReloading => reloadEndTime >= 0f;

        /// <summary>Trocando de arma (sem atirar nem recarregar até acabar).</summary>
        public bool IsSwapping => Time.time < swapEndTime;
        /// <summary>Cabo (mão direita) e apoio (mão esquerda) da arma equipada, ou nulos.</summary>
        public Transform RightGrip { get; private set; }
        public Transform LeftGrip { get; private set; }

        public int LoadedRounds => Equipped != null && loaded.TryGetValue(Equipped, out int n) ? n : 0;
        public int ReserveRounds => Equipped != null ? inventory.CountOf(Equipped.ammoItem) : 0;

        /// <summary>True se a mira está sobre algo que pode levar dano (a mira fica vermelha).</summary>
        public bool AimOnTarget { get; private set; }

        /// <summary>Avisa a cada tiro disparado (para o coice da animação, som e efeitos).</summary>
        public event System.Action Fired;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (ownHealth == null) ownHealth = GetComponent<Health>();
            if (melee == null) melee = GetComponent<PlayerMeleeAttack>();
            if (aimCamera == null && Camera.main != null) aimCamera = Camera.main.transform;
            if (gunVisual != null) gunVisual.SetActive(false);
            if (tracer != null) tracer.enabled = false;
        }

        void OnEnable() => inventory.Changed += OnInventoryChanged;

        void OnDisable()
        {
            inventory.Changed -= OnInventoryChanged;
            SetAiming(false);
        }

        void Start() => AutoEquip();

        // Espera o quadro seguinte, para o aviso "equipada" vir depois do aviso do item pego.
        void OnInventoryChanged() => autoEquipPending = true;

        void Update()
        {
            if (autoEquipPending)
            {
                autoEquipPending = false;
                AutoEquip();
            }

            if (tracer != null && tracer.enabled && Time.time >= tracerHideTime) tracer.enabled = false;
            if (IsReloading && Time.time >= reloadEndTime) FinishReload();

            // Esquiva e faca interrompem a recarga (o que já entrou na arma fica).
            if (IsReloading && (motor.IsDodging || (melee != null && melee.IsAttacking))) CancelReload();

            // Troca de arma: teclas 1 e 2 e D-pad. Os dois são lidos todo quadro (o D-pad guarda o toque anterior).
            int slotKey = input.WeaponSlotPressed;
            int padStep = input.WeaponCycleStep;
            if (input.GameplayEnabled && !ownHealth.IsDead && !motor.IsDodging)
            {
                if (slotKey != 0) SwapToSlot(slotKey);
                else if (padStep != 0) Cycle(padStep);
            }

            bool canAim = Equipped != null && input.GameplayEnabled && !motor.IsDodging && !ownHealth.IsDead;
            SetAiming(canAim && input.AimHeld);
            if (Equipped == null || !input.GameplayEnabled) return;

            if (input.ReloadPressed) TryStartReload();
            if (IsAiming && input.FirePressed) TryFire();
            UpdateAimOnTarget();
        }

        /// <summary>Equipa uma arma que esteja no inventário.</summary>
        public bool Equip(FirearmDefinition firearm)
        {
            if (firearm == null || !inventory.Has(firearm.weaponItem)) return false;
            if (firearm == Equipped) return true;

            reloadEndTime = -1f;
            bool swapping = Equipped != null;
            Equipped = firearm;
            // Arma encontrada vem carregada.
            if (!loaded.ContainsKey(firearm)) loaded[firearm] = firearm.magazineSize;
            swapEndTime = swapping ? Time.time + firearm.swapTime : -1f;
            ShowModel();
            Notifications.Post(Loc.Get("ui.weapons.equipped", "{0} equipada").Replace("{0}", firearm.weaponItem.LocalizedName));
            return true;
        }

        /// <summary>As armas que a Elena tem na bolsa, na ordem do lugar (1, 2...).</summary>
        public List<FirearmDefinition> OwnedFirearms()
        {
            var list = new List<FirearmDefinition>();
            foreach (ItemStack stack in inventory.Slots)
            {
                FirearmDefinition firearm = stack.item.firearm;
                if (firearm != null && inventory.Has(firearm.weaponItem) && !list.Contains(firearm)) list.Add(firearm);
            }
            list.Sort((a, b) => a.slot.CompareTo(b.slot));
            return list;
        }

        /// <summary>Tecla 1 ou 2: a arma do lugar pedido. Devolve true se trocou (ou já era a equipada).</summary>
        public bool SwapToSlot(int slot)
        {
            foreach (FirearmDefinition firearm in OwnedFirearms())
            {
                if (firearm.slot == slot) return Equip(firearm);
            }
            Notifications.Post(Loc.Get("ui.weapons.no_slot", "Sem arma neste lugar"));
            return false;
        }

        /// <summary>D-pad: +1 próxima, -1 anterior (dá a volta). Com uma arma só, avisa e não faz nada.</summary>
        public bool Cycle(int direction)
        {
            List<FirearmDefinition> owned = OwnedFirearms();
            if (owned.Count < 2)
            {
                Notifications.Post(Loc.Get("ui.weapons.no_other", "Sem outra arma"));
                return false;
            }
            int index = owned.IndexOf(Equipped);
            int next = ((index < 0 ? 0 : index) + (direction >= 0 ? 1 : -1) + owned.Count) % owned.Count;
            return Equip(owned[next]);
        }

        // Mostra só o modelo da arma equipada (se houver um por arma) e usa a ponta do cano dela.
        void ShowModel()
        {
            if (models == null) return;
            foreach (WeaponModel entry in models)
            {
                if (entry.model == null) continue;
                bool mine = entry.firearm == Equipped;
                entry.model.SetActive(mine);
                if (!mine) continue;
                if (entry.muzzle != null) muzzle = entry.muzzle;
                RightGrip = entry.rightGrip;
                LeftGrip = entry.leftGrip;
            }
        }

        // Sem arma equipada (ou se a equipada saiu do inventário), equipa a primeira que houver.
        void AutoEquip()
        {
            if (Equipped != null && inventory.Has(Equipped.weaponItem)) return;

            Equipped = null;
            reloadEndTime = -1f;
            swapEndTime = -1f;
            foreach (ItemStack stack in inventory.Slots)
            {
                if (stack.item.firearm != null && Equip(stack.item.firearm)) return;
            }
        }

        void SetAiming(bool value)
        {
            if (value == IsAiming) return;
            IsAiming = value;
            motor.Aiming = value;
            if (cameraController != null) cameraController.SetAiming(value);
            if (gunVisual != null) gunVisual.SetActive(value);
            if (!value) AimOnTarget = false;
        }

        void TryFire()
        {
            if (Time.time < nextShotTime || IsSwapping) return;

            // Cartucho a cartucho: atirar interrompe a recarga se já há cartucho no cano.
            if (IsReloading)
            {
                if (!Equipped.reloadPerRound || LoadedRounds == 0) return;
                CancelReload();
            }

            if (LoadedRounds == 0)
            {
                // Arma vazia: recarrega se houver munição, senão avisa.
                if (ReserveRounds > 0) TryStartReload();
                else Notifications.Post("Sem munição");
                return;
            }

            loaded[Equipped] = LoadedRounds - 1;
            nextShotTime = Time.time + Equipped.fireInterval;

            Vector3 firstPoint = Vector3.zero;
            pelletsOnTarget.Clear();
            for (int i = 0; i < Equipped.pellets; i++)
            {
                Vector3 point = FirePellet(SpreadDirection(aimCamera.forward, Equipped.spread));
                if (i == 0) firstPoint = point;
            }
            ApplyHitReactions();
            if (Equipped.fireSound != null) AudioSource.PlayClipAtPoint(Equipped.fireSound, transform.position + Vector3.up, 1f);

            ShowTracer(firstPoint);
            if (cameraController != null) cameraController.AddRecoil(Equipped.recoil);
            Noise.Emit(transform.position, Equipped.noiseRadius, gameObject);
            Fired?.Invoke();
        }

        // Dispara um projétil e devolve onde ele parou.
        Vector3 FirePellet(Vector3 direction)
        {
            Vector3 origin = AimOrigin();
            if (TryRaycast(origin, direction, Equipped.range, out RaycastHit hit))
            {
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target != ownHealth)
                {
                    float damage = Equipped.damagePerPellet * Equipped.DamageFactor(hit.distance);
                    LastPelletDamage = damage;
                    target.TakeDamage(new DamageInfo(damage, hit.point, gameObject));
                    pelletsOnTarget.TryGetValue(target, out int count);
                    pelletsOnTarget[target] = count + 1;
                }
                SpawnImpact(hit.point, hit.normal);
                return hit.point;
            }
            return origin + direction * Equipped.range;
        }

        // Quantos projéteis do tiro atual acertaram cada alvo (para o empurrão).
        readonly Dictionary<Health, int> pelletsOnTarget = new Dictionary<Health, int>();

        /// <summary>Dano do último projétil que acertou algo com vida (para os testes).</summary>
        public float LastPelletDamage { get; private set; }

        /// <summary>Quantos alvos reagiram (cambaleio ou queda) ao último tiro (para os testes).</summary>
        public int LastShotReactions { get; private set; }

        // Depois do tiro: quem levou projéteis perto o bastante cambaleia, e quem levou vários e está bem perto cai.
        void ApplyHitReactions()
        {
            LastShotReactions = 0;
            if (Equipped.staggerRange <= 0f && Equipped.knockdownRange <= 0f) return;
            foreach (KeyValuePair<Health, int> pair in pelletsOnTarget)
            {
                Health target = pair.Key;
                if (target == null || target.IsDead) continue;
                float distance = Vector3.Distance(transform.position, target.transform.position);
                HitReaction wanted = HitReaction.Nenhuma;
                float duration = Equipped.staggerTime;
                if (Equipped.knockdownRange > 0f && distance <= Equipped.knockdownRange && pair.Value >= Equipped.knockdownPellets)
                {
                    wanted = HitReaction.Queda;
                    duration = Equipped.knockdownTime;
                }
                else if (Equipped.staggerRange > 0f && distance <= Equipped.staggerRange) wanted = HitReaction.Cambaleio;
                if (wanted == HitReaction.Nenhuma) continue;
                if (HitReactor.Apply(target, wanted, duration, transform.position) != HitReaction.Nenhuma) LastShotReactions++;
            }
        }

        // O tiro sai do centro da tela, mas a partir da altura do personagem,
        // para não acertar o que estiver entre a câmera e a Elena.
        Vector3 AimOrigin()
        {
            float depth = Vector3.Dot(transform.position - aimCamera.position, aimCamera.forward);
            return aimCamera.position + aimCamera.forward * Mathf.Max(0f, depth);
        }

        bool TryRaycast(Vector3 origin, Vector3 direction, float range, out RaycastHit closest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, range, hitMask, QueryTriggerInteraction.Ignore);
            closest = default;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance < best)
                {
                    best = hits[i].distance;
                    closest = hits[i];
                }
            }
            return best < float.MaxValue;
        }

        static Vector3 SpreadDirection(Vector3 forward, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return forward;
            Vector2 offset = Random.insideUnitCircle * spreadDegrees;
            Quaternion spread = Quaternion.AngleAxis(offset.x, Vector3.up) * Quaternion.AngleAxis(offset.y, Vector3.right);
            return Quaternion.LookRotation(forward) * spread * Vector3.forward;
        }

        void UpdateAimOnTarget()
        {
            AimOnTarget = IsAiming
                && TryRaycast(AimOrigin(), aimCamera.forward, Equipped.range, out RaycastHit hit)
                && hit.collider.GetComponentInParent<Health>() is Health target
                && !target.IsDead;
        }

        void TryStartReload()
        {
            if (IsReloading || IsSwapping || LoadedRounds >= Equipped.magazineSize) return;
            if (ReserveRounds == 0)
            {
                Notifications.Post("Sem munição");
                return;
            }
            reloadEndTime = Time.time + (Equipped.reloadPerRound ? Equipped.reloadOpenTime + Equipped.reloadRoundTime : Equipped.reloadTime);
        }

        /// <summary>Interrompe a recarga. Cartucho a cartucho: o que já entrou fica na arma. Pente inteiro: nada é gasto.</summary>
        public void CancelReload() => reloadEndTime = -1f;

        void FinishReload()
        {
            reloadEndTime = -1f;
            if (Equipped == null) return;
            if (Equipped.reloadPerRound)
            {
                // Um cartucho entra por vez, na hora; continua até encher ou acabar a reserva.
                if (inventory.Remove(Equipped.ammoItem, 1) == 1) loaded[Equipped] = LoadedRounds + 1;
                if (LoadedRounds < Equipped.magazineSize && ReserveRounds > 0)
                    reloadEndTime = Time.time + Equipped.reloadRoundTime;
                return;
            }
            int taken = inventory.Remove(Equipped.ammoItem, Equipped.magazineSize - LoadedRounds);
            loaded[Equipped] = LoadedRounds + taken;
        }

        void ShowTracer(Vector3 end)
        {
            if (tracer == null || muzzle == null) return;
            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerHideTime = Time.time + TracerTime;
        }

        void SpawnImpact(Vector3 point, Vector3 normal)
        {
            if (impactMaterial == null) return;
            GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(mark.GetComponent<Collider>());
            mark.name = "Impacto";
            mark.transform.SetPositionAndRotation(point + normal * 0.01f, Quaternion.LookRotation(normal));
            mark.transform.localScale = Vector3.one * 0.08f;
            mark.GetComponent<Renderer>().sharedMaterial = impactMaterial;
            Destroy(mark, ImpactLifetime);
        }

        public Snapshot Capture() => new Snapshot
        {
            equipped = Equipped,
            loaded = new Dictionary<FirearmDefinition, int>(loaded),
        };

        public void Restore(Snapshot snapshot)
        {
            loaded.Clear();
            foreach (KeyValuePair<FirearmDefinition, int> pair in snapshot.loaded) loaded[pair.Key] = pair.Value;
            reloadEndTime = -1f;
            swapEndTime = -1f;
            autoEquipPending = false;
            Equipped = snapshot.equipped != null && inventory.Has(snapshot.equipped.weaponItem) ? snapshot.equipped : null;
            if (Equipped == null) AutoEquip();
            ShowModel();
        }
    }
}
