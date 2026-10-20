using System.Collections.Generic;
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
    /// para trocar, use a arma no inventário.
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
        float tracerHideTime;
        bool autoEquipPending;

        public FirearmDefinition Equipped { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsReloading => reloadEndTime >= 0f;
        public int LoadedRounds => Equipped != null && loaded.TryGetValue(Equipped, out int n) ? n : 0;
        public int ReserveRounds => Equipped != null ? inventory.CountOf(Equipped.ammoItem) : 0;

        /// <summary>True se a mira está sobre algo que pode levar dano (a mira fica vermelha).</summary>
        public bool AimOnTarget { get; private set; }

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (ownHealth == null) ownHealth = GetComponent<Health>();
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
            Equipped = firearm;
            // Arma encontrada vem carregada.
            if (!loaded.ContainsKey(firearm)) loaded[firearm] = firearm.magazineSize;
            Notifications.Post($"{firearm.weaponItem.displayName} equipada");
            return true;
        }

        // Sem arma equipada (ou se a equipada saiu do inventário), equipa a primeira que houver.
        void AutoEquip()
        {
            if (Equipped != null && inventory.Has(Equipped.weaponItem)) return;

            Equipped = null;
            reloadEndTime = -1f;
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
            if (Time.time < nextShotTime || IsReloading) return;

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
            for (int i = 0; i < Equipped.pellets; i++)
            {
                Vector3 point = FirePellet(SpreadDirection(aimCamera.forward, Equipped.spread));
                if (i == 0) firstPoint = point;
            }

            ShowTracer(firstPoint);
            if (cameraController != null) cameraController.AddRecoil(Equipped.recoil);
            Noise.Emit(transform.position, Equipped.noiseRadius, gameObject);
        }

        // Dispara um projétil e devolve onde ele parou.
        Vector3 FirePellet(Vector3 direction)
        {
            Vector3 origin = AimOrigin();
            if (TryRaycast(origin, direction, Equipped.range, out RaycastHit hit))
            {
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target != ownHealth)
                    target.TakeDamage(new DamageInfo(Equipped.damagePerPellet, hit.point, gameObject));
                SpawnImpact(hit.point, hit.normal);
                return hit.point;
            }
            return origin + direction * Equipped.range;
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
            if (IsReloading || LoadedRounds >= Equipped.magazineSize) return;
            if (ReserveRounds == 0)
            {
                Notifications.Post("Sem munição");
                return;
            }
            reloadEndTime = Time.time + Equipped.reloadTime;
        }

        void FinishReload()
        {
            reloadEndTime = -1f;
            if (Equipped == null) return;
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
            autoEquipPending = false;
            Equipped = snapshot.equipped != null && inventory.Has(snapshot.equipped.weaponItem) ? snapshot.equipped : null;
            if (Equipped == null) AutoEquip();
        }
    }
}
