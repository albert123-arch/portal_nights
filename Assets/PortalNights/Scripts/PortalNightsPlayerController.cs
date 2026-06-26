using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PortalNightsHealth))]
    public sealed class PortalNightsPlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8.8f;
        [SerializeField] private float lookSensitivity = 0.09f;
        [SerializeField] private float cameraDistance = 8f;
        [SerializeField] private float cameraHeight = 4.2f;
        [SerializeField] private float cameraPitchMin = -24f;
        [SerializeField] private float cameraPitchMax = 42f;
        [SerializeField] private float arenaRadius = 15.2f;
        [SerializeField] private Vector2 arenaXLimits = new Vector2(-15.5f, 15.5f);
        [SerializeField] private Vector2 arenaZLimits = new Vector2(-14.6f, 30.5f);

        [Header("Combat")]
        [SerializeField] private float damage = 26f;
        [SerializeField] private float shootRange = 42f;
        [SerializeField] private float shootCooldown = 0.18f;
        [SerializeField] private float cameraAimAssistRadius = 0.72f;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float visualYawOffset = 0f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float animationRunSpeed = 8.8f;
        [SerializeField] private float animationDampTime = 0.12f;

        private CharacterController characterController;
        private PortalNightsHealth health;
        private Camera mainCamera;
        private float serverShootTimer;
        private float cameraYaw;
        private float cameraPitch = 14f;
        private Vector3 lastFinalShootDirection = Vector3.forward;
        private Vector3 previousAnimationPosition;
        private bool animationPositionInitialized;
        private float armorBoostTimer;
        private float weaponDamageBoostTimer;
        private float runWeaponDamageMultiplier = 1f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int WeaponTypeIdHash = Animator.StringToHash("WeaponTypeID");
        private static readonly int IsReloadingHash = Animator.StringToHash("IsReloading");
        private static readonly int ReloadSpeedHash = Animator.StringToHash("ReloadSpeed");
        private static readonly int IsSwitchingWeaponHash = Animator.StringToHash("IsSwitchingWeapon");
        private static readonly int StrafeXHash = Animator.StringToHash("StrafeX");
        private static readonly int StrafeYHash = Animator.StringToHash("StrafeY");
        private static readonly int IsCollidingHash = Animator.StringToHash("IsColliding");

        public PortalNightsHealth Health => health;
        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<PortalNightsHealth>();
            if (cameraAnchor == null)
            {
                Transform foundCameraAnchor = transform.Find("CameraAnchor");
                cameraAnchor = foundCameraAnchor == null ? transform : foundCameraAnchor;
            }

            EnsureVisualRoot();
            EnsureAnimator();
            ApplyVisualYawOffset();
        }

        private void OnValidate()
        {
            ApplyVisualYawOffset();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                health.ServerInitialize(health.BaseMaxHealth, true);
                PortalNightsGameController controller = PortalNightsGameController.Instance;
                if (controller != null)
                {
                    transform.SetPositionAndRotation(controller.GetPlayerSpawnPosition(OwnerClientId), controller.GetPlayerSpawnRotation(OwnerClientId));
                }
            }

            if (IsOwner)
            {
                mainCamera = Camera.main;
                Vector3 euler = transform.rotation.eulerAngles;
                cameraYaw = euler.y;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            previousAnimationPosition = transform.position;
            animationPositionInitialized = true;
        }

        private void Update()
        {
            serverShootTimer -= Time.deltaTime;
            UpdateTemporaryBoosts();
            UpdateAnimationState();

            if (!CanReadLocalInput() || health == null || health.IsDead)
            {
                return;
            }

            ReadAndSubmitInput();
        }

        private void LateUpdate()
        {
            if (!CanReadLocalInput())
            {
                return;
            }

            UpdateCamera();
        }

        public void DamageServer(float amount)
        {
            if (!IsServer || health == null)
            {
                return;
            }

            float finalAmount = armorBoostTimer > 0f ? amount * 0.7f : amount;
            health.DamageServer(finalAmount);
            if (health.IsDead)
            {
                Invoke(nameof(RespawnServer), 2.4f);
            }
        }

        private void RespawnServer()
        {
            if (!IsServer || health == null)
            {
                return;
            }

            PortalNightsGameController controller = PortalNightsGameController.Instance;
            if (controller != null)
            {
                transform.SetPositionAndRotation(controller.GetPlayerSpawnPosition(OwnerClientId), controller.GetPlayerSpawnRotation(OwnerClientId));
            }

            health.ServerInitialize(health.BaseMaxHealth, true);
        }

        private void ReadAndSubmitInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Vector2 moveInput = Vector2.zero;
            moveInput.x += keyboard.dKey.isPressed ? 1f : 0f;
            moveInput.x -= keyboard.aKey.isPressed ? 1f : 0f;
            moveInput.y += keyboard.wKey.isPressed ? 1f : 0f;
            moveInput.y -= keyboard.sKey.isPressed ? 1f : 0f;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue();
                cameraYaw += delta.x * lookSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y * lookSensitivity, cameraPitchMin, cameraPitchMax);
            }

            Vector3 aimPoint = GetAimPoint();
            bool fire = mouse != null && mouse.leftButton.isPressed;
            fire |= keyboard.spaceKey.isPressed;
            bool interact = keyboard.eKey.wasPressedThisFrame;
            bool interactHeld = keyboard.eKey.isPressed;
            bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            if (IsServer)
            {
                ProcessInputServer(moveInput, cameraYaw, aimPoint, fire, interact, interactHeld, sprint);
            }
            else if (IsSpawned)
            {
                SubmitInputServerRpc(moveInput, cameraYaw, aimPoint, fire, interact, interactHeld, sprint);
            }
            else
            {
                ProcessInputLocal(moveInput, cameraYaw, aimPoint, fire, interact, interactHeld, sprint);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitInputServerRpc(Vector2 moveInput, float yaw, Vector3 aimPoint, bool fire, bool interact, bool interactHeld, bool sprint)
        {
            ProcessInputServer(moveInput, yaw, aimPoint, fire, interact, interactHeld, sprint);
        }

        private void ProcessInputServer(Vector2 moveInput, float yaw, Vector3 aimPoint, bool fire, bool interact, bool interactHeld, bool sprint)
        {
            if (!IsServer || health == null || health.IsDead)
            {
                return;
            }

            cameraYaw = yaw;
            PortalNightsGameController controller = PortalNightsGameController.Instance;
            Vector3 moveDirection = ApplyDirectMovement(moveInput, yaw, sprint);

            Vector3 lookDirection = fire ? GetFinalFlatShootDirection(transform.position + Vector3.up * 1.2f, aimPoint) : moveDirection;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                RotateGameplayRoot(lookDirection);
            }

            if (interact && controller != null && !controller.GameOver)
            {
                controller.TryInteractNearServer(transform.position, OwnerClientId);
            }

            if (controller != null && !controller.GameOver)
            {
                if (interactHeld)
                {
                    controller.UpdateHeldInteractServer(transform.position, OwnerClientId, Time.deltaTime);
                }
                else
                {
                    controller.CancelHeldInteractServer(OwnerClientId);
                }
            }

            if (fire && (controller == null || !controller.GameOver))
            {
                TryShootServer(aimPoint);
            }
        }

        private void ProcessInputLocal(Vector2 moveInput, float yaw, Vector3 aimPoint, bool fire, bool interact, bool interactHeld, bool sprint)
        {
            cameraYaw = yaw;
            Vector3 moveDirection = ApplyDirectMovement(moveInput, yaw, sprint);

            Vector3 lookDirection = fire ? GetFinalFlatShootDirection(transform.position + Vector3.up * 1.2f, aimPoint) : moveDirection;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                RotateGameplayRoot(lookDirection);
            }

            if (fire)
            {
                TryShootServer(aimPoint);
            }
        }

        private Vector3 ApplyDirectMovement(Vector2 moveInput, float yaw, bool sprint)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 moveDirection = (yawRotation * Vector3.forward) * moveInput.y + (yawRotation * Vector3.right) * moveInput.x;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            float currentMoveSpeed = Mathf.Max(moveSpeed, 8.8f) * (sprint ? 1.35f : 1f);
            Vector3 nextPosition = transform.position + moveDirection * (currentMoveSpeed * Time.deltaTime);
            PortalNightsGameController controller = PortalNightsGameController.Instance;
            if (controller == null || !controller.TryClampPlayerPositionForCurrentArea(ref nextPosition))
            {
                if (arenaXLimits.y > arenaXLimits.x && arenaZLimits.y > arenaZLimits.x)
                {
                    nextPosition.x = Mathf.Clamp(nextPosition.x, arenaXLimits.x, arenaXLimits.y);
                    nextPosition.z = Mathf.Clamp(nextPosition.z, arenaZLimits.x, arenaZLimits.y);
                }
                else
                {
                    Vector2 flat = new Vector2(nextPosition.x, nextPosition.z);
                    if (flat.magnitude > arenaRadius)
                    {
                        flat = flat.normalized * arenaRadius;
                        nextPosition.x = flat.x;
                        nextPosition.z = flat.y;
                    }
                }
            }

            nextPosition.y = Mathf.Max(1.12f, nextPosition.y);
            transform.position = nextPosition;
            return moveDirection;
        }

        private void TryShootServer(Vector3 aimPoint)
        {
            if (serverShootTimer > 0f)
            {
                return;
            }

            serverShootTimer = shootCooldown;
            Vector3 originBase = transform.position + Vector3.up * 1.2f;
            float effectiveShootRange = Mathf.Max(shootRange, 65f);
            Vector3 direction = GetFinalFlatShootDirection(originBase, aimPoint);
            Vector3 origin = originBase + direction * 0.8f;
            lastFinalShootDirection = direction;

            PortalNightsGameController controller = PortalNightsGameController.Instance;
            PortalNightsDamageTarget damageTarget = controller == null ? null : controller.GetShotDamageTarget(origin, direction, effectiveShootRange, 3.15f);
            PortalNightsEnemy target = damageTarget == null && controller != null ? controller.GetShotTarget(origin, direction, effectiveShootRange, 3.15f) : null;
            Vector3 hitPoint = origin + direction * effectiveShootRange;
            if (damageTarget != null && damageTarget.Health != null)
            {
                hitPoint = damageTarget.AimPoint;
                float finalDamage = damage * WeaponDamageMultiplier;
                damageTarget.Health.DamageServer(finalDamage);
                controller?.NotifyDamageTargetHitServer(damageTarget, finalDamage, true);
                PortalNightsVfx.SpawnFloatingText(hitPoint + Vector3.up * 0.5f, Mathf.CeilToInt(finalDamage).ToString(), new Color(0.58f, 0.92f, 1f, 1f));
            }
            else if (target != null)
            {
                hitPoint = target.AimPoint;
                float finalDamage = damage * WeaponDamageMultiplier;
                target.Health.DamageServer(finalDamage);
                PortalNightsVfx.SpawnFloatingText(hitPoint + Vector3.up * 0.5f, Mathf.CeilToInt(finalDamage).ToString(), new Color(0.58f, 0.92f, 1f, 1f));
            }

            if (IsSpawned)
            {
                ShotVfxClientRpc(origin, hitPoint, target != null || damageTarget != null);
            }
            else
            {
                TriggerShootAnimation();
                PlayShotVfx(origin, hitPoint, target != null || damageTarget != null);
            }
        }

        [ClientRpc]
        private void ShotVfxClientRpc(Vector3 origin, Vector3 hitPoint, bool hitEnemy)
        {
            TriggerShootAnimation();
            PlayShotVfx(origin, hitPoint, hitEnemy);
        }

        private static void PlayShotVfx(Vector3 origin, Vector3 hitPoint, bool hitEnemy)
        {
            PortalNightsProjectile.Spawn(origin, hitPoint, new Color(0.2f, 0.84f, 1f, 1f), 0.055f);
            PortalNightsVfx.SpawnBurst(origin, new Color(0.25f, 0.82f, 1f, 1f), 0.42f, 9);
            if (hitEnemy)
            {
                PortalNightsVfx.SpawnBurst(hitPoint, new Color(0.95f, 0.25f, 1f, 1f), 0.75f, 15);
            }
        }

        private bool CanReadLocalInput()
        {
            if (IsOwner)
            {
                return true;
            }

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening || !IsSpawned)
            {
                return true;
            }

            if (manager.IsHost && OwnerClientId == manager.LocalClientId)
            {
                return true;
            }

            NetworkObject localPlayer = manager.SpawnManager == null ? null : manager.SpawnManager.GetLocalPlayerObject();
            if (localPlayer == null)
            {
                return FindFirstObjectByType<PortalNightsPlayerController>() == this;
            }

            return localPlayer.gameObject == gameObject;
        }

        public void ApplyHealServer(float amount)
        {
            if (!IsServer || health == null)
            {
                return;
            }

            health.HealServer(amount);
        }

        public void ResetRunProgressionServer()
        {
            if (!IsServer)
            {
                return;
            }

            armorBoostTimer = 0f;
            weaponDamageBoostTimer = 0f;
            runWeaponDamageMultiplier = 1f;
        }

        public void ResetTemporaryBoostsServer()
        {
            if (!IsServer)
            {
                return;
            }

            armorBoostTimer = 0f;
            weaponDamageBoostTimer = 0f;
        }

        public void ApplyArmorBoostServer(float duration)
        {
            if (!IsServer)
            {
                return;
            }

            armorBoostTimer = Mathf.Max(armorBoostTimer, duration);
        }

        public void ApplyWeaponDamageBoostServer(float duration)
        {
            if (!IsServer)
            {
                return;
            }

            weaponDamageBoostTimer = Mathf.Max(weaponDamageBoostTimer, duration);
        }

        public void ApplyWeaponDamageRunBonusServer(float bonus)
        {
            if (!IsServer)
            {
                return;
            }

            runWeaponDamageMultiplier += Mathf.Max(0f, bonus);
        }

        public float WeaponDamageMultiplier => Mathf.Max(1f, runWeaponDamageMultiplier) * (weaponDamageBoostTimer > 0f ? 1.2f : 1f);

        private void UpdateTemporaryBoosts()
        {
            armorBoostTimer = Mathf.Max(0f, armorBoostTimer - Time.deltaTime);
            weaponDamageBoostTimer = Mathf.Max(0f, weaponDamageBoostTimer - Time.deltaTime);
        }

        private void RotateGameplayRoot(Vector3 flatDirection)
        {
            flatDirection = PortalNightsMath.Flat(flatDirection);
            if (flatDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = targetRotation;
        }

        private Vector3 GetFinalFlatShootDirection(Vector3 origin, Vector3 aimPoint)
        {
            Vector3 direction = PortalNightsMath.Flat(aimPoint - origin);
            if (direction.sqrMagnitude <= 0.05f)
            {
                direction = GetFlatAimForward();
            }

            if (direction.sqrMagnitude <= 0.05f)
            {
                direction = PortalNightsMath.Flat(transform.forward);
            }

            return direction.sqrMagnitude <= 0.05f ? Vector3.forward : direction.normalized;
        }

        private Vector3 GetAimPoint()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return transform.position + transform.forward * shootRange;
            }

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (TryGetCameraEnemyAimPoint(ray, out Vector3 enemyAimPoint))
            {
                AimPoint = enemyAimPoint;
                return AimPoint;
            }

            if (TryRaycastCameraAimPoint(ray, out Vector3 sceneAimPoint))
            {
                AimPoint = sceneAimPoint;
                return AimPoint;
            }

            Plane ground = new Plane(Vector3.up, Vector3.up * 1.2f);
            if (ground.Raycast(ray, out float enter))
            {
                AimPoint = FlattenAimPoint(ray.GetPoint(enter));
                return AimPoint;
            }

            AimPoint = transform.position + Vector3.up * 1.2f + GetFlatAimForward() * shootRange;
            return AimPoint;
        }

        private Vector3 FlattenAimPoint(Vector3 point)
        {
            point.y = transform.position.y + 1.2f;
            return point;
        }

        private bool TryGetCameraEnemyAimPoint(Ray ray, out Vector3 enemyAimPoint)
        {
            enemyAimPoint = Vector3.zero;
            float bestDistance = float.MaxValue;
            float range = Mathf.Max(shootRange, 65f);
            RaycastHit[] hits = Physics.SphereCastAll(ray, cameraAimAssistRadius, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PortalNightsPlayerController>() == this)
                {
                    continue;
                }

                PortalNightsEnemy enemy = hit.collider.GetComponentInParent<PortalNightsEnemy>();
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    enemyAimPoint = enemy.AimPoint;
                }
            }

            return bestDistance < float.MaxValue;
        }

        private bool TryRaycastCameraAimPoint(Ray ray, out Vector3 sceneAimPoint)
        {
            sceneAimPoint = Vector3.zero;
            RaycastHit[] hits = Physics.RaycastAll(ray, 250f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<PortalNightsPlayerController>() == this)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    sceneAimPoint = FlattenAimPoint(hit.point);
                }
            }

            return bestDistance < float.MaxValue;
        }

        private Vector3 GetFlatAimForward()
        {
            Vector3 forward = mainCamera == null ? Vector3.zero : PortalNightsMath.Flat(mainCamera.transform.forward);
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
            }

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = PortalNightsMath.Flat(transform.forward);
            }

            return forward.sqrMagnitude <= 0.001f ? Vector3.forward : forward.normalized;
        }

        private void ApplyVisualYawOffset()
        {
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, visualYawOffset, 0f);
            }
        }

        private void UpdateAnimationState()
        {
            Animator targetAnimator = EnsureAnimator();
            if (targetAnimator == null)
            {
                return;
            }

            if (!animationPositionInitialized)
            {
                previousAnimationPosition = transform.position;
                animationPositionInitialized = true;
            }

            Vector3 delta = PortalNightsMath.Flat(transform.position - previousAnimationPosition);
            previousAnimationPosition = transform.position;

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 velocity = delta / deltaTime;
            float speed01 = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, animationRunSpeed));
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            Vector3 localDirection = localVelocity.sqrMagnitude <= 0.001f ? Vector3.zero : localVelocity.normalized;

            targetAnimator.SetFloat(SpeedHash, speed01, animationDampTime, Time.deltaTime);
            targetAnimator.SetFloat(MotionSpeedHash, Mathf.Lerp(0.85f, 1.18f, speed01), animationDampTime, Time.deltaTime);
            targetAnimator.SetFloat(StrafeXHash, localDirection.x * speed01, animationDampTime, Time.deltaTime);
            targetAnimator.SetFloat(StrafeYHash, localDirection.z * speed01, animationDampTime, Time.deltaTime);
            targetAnimator.SetBool(GroundedHash, true);
            targetAnimator.SetBool(FreeFallHash, false);
            targetAnimator.SetBool(IsAimingHash, true);
            targetAnimator.SetInteger(WeaponTypeIdHash, 0);
            targetAnimator.SetBool(IsReloadingHash, false);
            targetAnimator.SetFloat(ReloadSpeedHash, 1f);
            targetAnimator.SetBool(IsSwitchingWeaponHash, false);
            targetAnimator.SetBool(IsCollidingHash, false);
        }

        private void TriggerShootAnimation()
        {
            Animator targetAnimator = EnsureAnimator();
            if (targetAnimator == null)
            {
                return;
            }

            targetAnimator.SetBool(IsAimingHash, true);
            targetAnimator.SetTrigger(ShootHash);
        }

        private Animator EnsureAnimator()
        {
            if (animator != null)
            {
                return animator;
            }

            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot");
            }

            animator = visualRoot == null ? GetComponentInChildren<Animator>(true) : visualRoot.GetComponentInChildren<Animator>(true);
            return animator;
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot");
            }

            if (visualRoot == null)
            {
                GameObject visualObject = new GameObject("VisualRoot");
                visualRoot = visualObject.transform;
                visualRoot.SetParent(transform, false);
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;

                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (child == visualRoot || child == cameraAnchor || child.name == "CameraAnchor")
                    {
                        continue;
                    }

                    child.SetParent(visualRoot, true);
                }
            }
        }

        private void UpdateCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 anchor = cameraAnchor == null ? transform.position + Vector3.up * 1.45f : cameraAnchor.position;
            Vector3 desiredPosition = anchor - rotation * Vector3.forward * cameraDistance + Vector3.up * cameraHeight;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, 18f * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(anchor + rotation * Vector3.forward * 8f - mainCamera.transform.position, Vector3.up), 18f * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position + Vector3.up * 1.35f;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, PortalNightsMath.Flat(transform.forward).normalized * 2.5f);

            Gizmos.color = Color.green;
            Vector3 shootDirection = lastFinalShootDirection.sqrMagnitude > 0.001f ? lastFinalShootDirection.normalized : PortalNightsMath.Flat(transform.forward).normalized;
            Gizmos.DrawRay(origin + Vector3.up * 0.12f, shootDirection * 3.25f);

            Transform debugVisualRoot = visualRoot == null ? transform.Find("VisualRoot") : visualRoot;
            if (debugVisualRoot != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(origin + Vector3.up * 0.24f, PortalNightsMath.Flat(debugVisualRoot.forward).normalized * 2.1f);
            }
        }
    }
}
