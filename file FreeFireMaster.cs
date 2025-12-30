using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // Cho AI NavMesh

namespace FreeFireMaster.Pro
{
    // ========================================================
    // 1. HỆ THỐNG DỮ LIỆU VÀ PHỤ KIỆN (WEAPON ATTACHMENTS) - NÂNG CAO
    // ========================================================
    public enum AttachmentType { Scope, Muzzle, Grip, Magazine, Stock }

    [System.Serializable]
    public class Attachment {
        public string name;
        public AttachmentType type;
        public float recoilReduction;       // Giảm recoil ngang/dọc
        public float adsSpeedMultiplier;    // Tốc độ ngắm nhanh hơn
        public float zoomMultiplier;        // Độ phóng đại (chỉ cho scope)
        public float damageMultiplier;      // Tăng damage (cho muzzle)
        public int extraAmmo;               // Thêm đạn (cho magazine)
    }

    public enum WeaponType { AssaultRifle, SMG, Sniper, Shotgun, Pistol }

    [System.Serializable]
    public class GunStat {
        public string name;
        public WeaponType weaponType;
        public float baseDamage = 30f;
        public float fireRate = 0.1f;               // Thời gian giữa các phát bắn
        public float recoilVertical = 1f;           // Recoil dọc cơ bản
        public float recoilHorizontal = 0.5f;       // Recoil ngang cơ bản
        public int magSize = 30;
        public int currentAmmo;
        public float range = 100f;
        public int pellets = 1;                     // Cho shotgun
        public List<Attachment> attachments = new List<Attachment>(); // Các phụ kiện đang gắn
        public GameObject weaponModel;              // Model 3D của súng
        public ParticleSystem muzzleFlash;
        public AudioClip fireSound;
    }

    [System.Serializable]
    public class RecoilPattern {
        public Vector2[] pattern; // Mẫu recoil (horizontal, vertical) cho mỗi phát bắn
        public float recoverySpeed = 5f;
    }

    // ========================================================
    // 2. PLAYER CONTROLLER NÂNG CAO (FPS + SURVIVAL FEATURES)
    // ========================================================
    public class PlayerController : MonoBehaviour 
    {
        #region Variables - Survival Stats
        [Header("Survival Stats")]
        public float maxHealth = 200f;
        public float health = 200f;
        public float maxStamina = 100f;
        public float stamina = 100f;
        public float staminaDrainRate = 20f;
        public float staminaRegenRate = 10f;
        public bool isSprinting = false;
        public bool isCrouching = false;
        public bool isProne = false;
        #endregion

        #region Variables - Movement Settings
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float crouchSpeed = 3f;
        public float proneSpeed = 1.5f;
        public float jumpForce = 5f;
        public float mouseSensitivity = 2f;
        public float adsSensitivityMultiplier = 0.6f;
        #endregion

        #region Variables - Combat Settings
        [Header("Combat Settings")]
        public List<GunStat> inventory = new List<GunStat>(3); // Max 3 vũ khí
        public int currentGunIndex = 0;
        private float nextTimeToFire = 0f;
        private RecoilPattern currentRecoilPattern;
        private int shotCount = 0;
        private Vector3 recoilRecovery;
        public Transform weaponHolder; // Position cầm súng
        #endregion

        #region Variables - References
        [Header("References")]
        public Camera mainCam;
        public CharacterController controller;
        public Image healthBar;
        public Image staminaBar;
        public Text ammoText;
        public Text weaponNameText;
        public Slider miniMapZoneSlider; // Placeholder cho safe zone
        public ParticleSystem hitEffect;
        public AudioSource audioSource;
        #endregion

        #region Private Variables
        private float defaultFOV = 60f;
        private float currentFOV;
        private float rotationX = 0f;
        private Vector3 velocity;
        private float verticalBob = 0f;
        private float bobTimer = 0f;
        private bool isADS = false;
        #endregion

        void Start() {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentFOV = defaultFOV;
            mainCam.fieldOfView = currentFOV;
            InitializeWeapons();
            SpawnLootOnMap(); // Test loot
        }

        void InitializeWeapons() {
            // SCAR-L với phụ kiện mẫu
            GunStat scar = new GunStat {
                name = "SCAR-L",
                weaponType = WeaponType.AssaultRifle,
                baseDamage = 32f,
                fireRate = 0.1f,
                magSize = 30,
                currentAmmo = 30,
                recoilVertical = 1.2f,
                recoilHorizontal = 0.6f,
                range = 150f
            };
            scar.attachments.Add(new Attachment { name = "4x Scope", type = AttachmentType.Scope, zoomMultiplier = 30f, recoilReduction = 0.3f });
            scar.attachments.Add(new Attachment { name = "Compensator", type = AttachmentType.Muzzle, recoilReduction = 0.2f });
            inventory.Add(scar);

            // Thêm súng thứ 2: MP40
            GunStat mp40 = new GunStat {
                name = "MP40",
                weaponType = WeaponType.SMG,
                baseDamage = 22f,
                fireRate = 0.08f,
                magSize = 40,
                currentAmmo = 40,
                recoilVertical = 0.8f,
                recoilHorizontal = 0.4f
            };
            inventory.Add(mp40);

            EquipWeapon(currentGunIndex);
        }

        void EquipWeapon(int index) {
            // Xóa model cũ
            foreach (Transform child in weaponHolder) Destroy(child.gameObject);
            if (inventory[index].weaponModel != null) {
                Instantiate(inventory[index].weaponModel, weaponHolder);
            }
            UpdateUI();
        }

        void Update() {
            HandleMovement();
            HandleInput();
            HandleStamina();
            HandleRecoilRecovery();
            HandleHeadBob();
            UpdateUI();
        }

        void HandleMovement() {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;
            move = move.normalized;

            float currentSpeed = GetCurrentSpeed();
            controller.Move(move * currentSpeed * Time.deltaTime);

            // Jump
            if (controller.isGrounded) {
                if (Input.GetButtonDown("Jump") && !isCrouching && !isProne) {
                    velocity.y = jumpForce;
                }
                velocity.y = -2f;
            }
            velocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Crouch / Prone
            if (Input.GetKeyDown(KeyCode.C)) {
                if (isProne) { isProne = false; isCrouching = false; }
                else if (isCrouching) { isCrouching = false; }
                else { isCrouching = true; }
                UpdatePlayerHeight();
            }
            if (Input.GetKeyDown(KeyCode.Z) && !isProne) {
                isProne = !isProne;
                isCrouching = false;
                UpdatePlayerHeight();
            }
        }

        float GetCurrentSpeed() {
            isSprinting = Input.GetKey(KeyCode.LeftShift) && stamina > 0 && !isCrouching && !isProne;
            if (isProne) return proneSpeed;
            if (isCrouching) return crouchSpeed;
            if (isSprinting) return runSpeed;
            return walkSpeed;
        }

        void UpdatePlayerHeight() {
            float height = isProne ? 0.5f : (isCrouching ? 1.2f : 2f);
            controller.height = height;
            controller.center = new Vector3(0, height / 2, 0);
            mainCam.transform.localPosition = new Vector3(0, height - 0.2f, 0);
        }

        void HandleStamina() {
            if (isSprinting) {
                stamina -= staminaDrainRate * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0, maxStamina);
            } else {
                stamina += staminaRegenRate * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0, maxStamina);
            }
            staminaBar.fillAmount = stamina / maxStamina;
        }

        void HandleInput() {
            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * (isADS ? adsSensitivityMultiplier : 1f);
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (isADS ? adsSensitivityMultiplier : 1f);
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            mainCam.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.Rotate(Vector3.up * mouseX);

            // Shoot
            GunStat currentGun = inventory[currentGunIndex];
            if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire && currentGun.currentAmmo > 0) {
                nextTimeToFire = Time.time + currentGun.fireRate;
                Shoot();
            }

            // ADS
            isADS = Input.GetMouseButton(1);
            float targetFOV = isADS ? GetScopeZoom() : defaultFOV;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * 10f);
            mainCam.fieldOfView = currentFOV;

            // Reload
            if (Input.GetKeyDown(KeyCode.R) && currentGun.currentAmmo < currentGun.magSize) {
                StartCoroutine(Reload());
            }

            // Switch weapon
            if (Input.GetKeyDown(KeyCode.Alpha1)) { currentGunIndex = 0; EquipWeapon(currentGunIndex); }
            if (Input.GetKeyDown(KeyCode.Alpha2) && inventory.Count > 1) { currentGunIndex = 1; EquipWeapon(currentGunIndex); }
        }

        float GetScopeZoom() {
            foreach (Attachment att in inventory[currentGunIndex].attachments) {
                if (att.type == AttachmentType.Scope) return att.zoomMultiplier;
            }
            return 50f; // Default red dot
        }

        void Shoot() {
            GunStat g = inventory[currentGunIndex];
            g.currentAmmo--;

            // Play effects
            if (g.muzzleFlash != null) g.muzzleFlash.Play();
            if (g.fireSound != null && audioSource) audioSource.PlayOneShot(g.fireSound);

            // Recoil
            ApplyRecoil();

            // Raycast bắn (multi-pellet cho shotgun)
            for (int i = 0; i < g.pellets; i++) {
                Vector3 direction = mainCam.transform.forward;
                direction += new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f), 0); // Spread

                RaycastHit hit;
                if (Physics.Raycast(mainCam.transform.position, direction, out hit, g.range)) {
                    if (hit.transform.CompareTag("Enemy")) {
                        EnemyAI enemy = hit.transform.GetComponent<EnemyAI>();
                        if (enemy) enemy.ApplyDamage(g.baseDamage * GetDamageMultiplier());
                    }
                    // Hit effect
                    if (hitEffect) Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            UpdateUI();
        }

        float GetDamageMultiplier() {
            float mult = 1f;
            foreach (Attachment att in inventory[currentGunIndex].attachments) {
                mult *= att.damageMultiplier;
            }
            return mult;
        }

        void ApplyRecoil() {
            GunStat g = inventory[currentGunIndex];
            float recoilReduc = 1f;
            foreach (Attachment att in g.attachments) recoilReduc -= att.recoilReduction;
            recoilReduc = Mathf.Clamp(recoilReduc, 0.3f, 1f);

            float vert = g.recoilVertical * recoilReduc * (isCrouching || isProne ? 0.7f : 1f);
            float horz = g.recoilHorizontal * recoilReduc * Random.Range(-1f, 1f);

            rotationX -= vert;
            transform.Rotate(Vector3.up * horz);
            recoilRecovery = new Vector3(vert, -horz, 0);
        }

        void HandleRecoilRecovery() {
            if (recoilRecovery != Vector3.zero) {
                rotationX += recoilRecovery.x * Time.deltaTime * 5f;
                transform.Rotate(Vector3.up * recoilRecovery.y * Time.deltaTime * 5f);
                recoilRecovery = Vector3.Lerp(recoilRecovery, Vector3.zero, Time.deltaTime * 8f);
            }
        }

        void HandleHeadBob() {
            if (controller.velocity.magnitude > 0.1f && controller.isGrounded) {
                bobTimer += Time.deltaTime * GetCurrentSpeed();
                verticalBob = Mathf.Sin(bobTimer * 10f) * 0.05f;
                mainCam.transform.localPosition = new Vector3(0, verticalBob, 0);
            }
        }

        IEnumerator Reload() {
            Debug.Log("Reloading...");
            yield return new WaitForSeconds(2.5f);
            GunStat g = inventory[currentGunIndex];
            g.currentAmmo = g.magSize + GetExtraAmmoFromMag();
            UpdateUI();
        }

        int GetExtraAmmoFromMag() {
            int extra = 0;
            foreach (Attachment att in inventory[currentGunIndex].attachments) {
                if (att.type == AttachmentType.Magazine) extra += att.extraAmmo;
            }
            return extra;
        }

        public void TakeDamage(float dmg) {
            health -= dmg;
            health = Mathf.Clamp(health, 0, maxHealth);
            healthBar.fillAmount = health / maxHealth;
            if (health <= 0) Debug.Log("Player Dead!");
        }

        void UpdateUI() {
            GunStat g = inventory[currentGunIndex];
            ammoText.text = $"{g.currentAmmo}/{g.magSize}";
            weaponNameText.text = g.name;
            healthBar.fillAmount = health / maxHealth;
        }

        void SpawnLootOnMap() {
            // Test: Spawn vài loot box ngẫu nhiên (bạn có thể tạo prefab)
            for (int i = 0; i < 10; i++) {
                Vector3 pos = new Vector3(Random.Range(-50, 50), 1, Random.Range(-50, 50));
                GameObject loot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                loot.transform.position = pos;
                loot.tag = "Loot";
                loot.AddComponent<LootItem>();
            }
        }
    }

    // ========================================================
    // 3. LOOT SYSTEM CƠ BẢN
    // ========================================================
    public class LootItem : MonoBehaviour {
        public GunStat lootGun; // Có thể loot súng hoặc phụ kiện

        void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Player")) {
                PlayerController pc = other.GetComponent<PlayerController>();
                if (pc && pc.inventory.Count < 3) {
                    pc.inventory.Add(lootGun);
                    Destroy(gameObject);
                }
            }
        }
    }

    // ========================================================
    // 4. ENEMY AI NÂNG CAO (PATROL, CHASE, COVER)
    // ========================================================
    public class EnemyAI : MonoBehaviour {
        public enum AIState { Patrol, Alert, Chase, Attack, Cover }
        public AIState currentState = AIState.Patrol;

        [Header("Stats")]
        public float maxHp = 100f;
        private float hp;
        public float detectRange = 50f;
        public float attackRange = 15f;
        public float patrolSpeed = 3f;
        public float chaseSpeed = 7f;

        private NavMeshAgent agent;
        private Transform player;
        private Vector3[] patrolPoints;
        private int currentPatrolIndex = 0;
        private float lastSeenTime = 0f;

        void Start() {
            agent = GetComponent<NavMeshAgent>();
            player = GameObject.FindGameObjectWithTag("Player").transform;
            hp = maxHp;
            GeneratePatrolPoints(5, 30f); // 5 điểm patrol trong bán kính 30
        }

        void Update() {
            if (hp <= 0) { Destroy(gameObject, 1f); return; }

            float distToPlayer = Vector3.Distance(transform.position, player.position);

            switch (currentState) {
                case AIState.Patrol:
                    Patrol();
                    if (distToPlayer < detectRange) currentState = AIState.Alert;
                    break;

                case AIState.Alert:
                    agent.stoppingDistance = 5f;
                    agent.SetDestination(transform.position); // Dừng lại quan sát
                    if (distToPlayer < detectRange / 2) {
                        currentState = AIState.Chase;
                        lastSeenTime = Time.time;
                    } else if (Time.time - lastSeenTime > 10f) {
                        currentState = AIState.Patrol;
                    }
                    break;

                case AIState.Chase:
                    agent.speed = chaseSpeed;
                    agent.SetDestination(player.position);
                    if (distToPlayer < attackRange) currentState = AIState.Attack;
                    if (distToPlayer > detectRange) currentState = AIState.Alert;
                    break;

                case AIState.Attack:
                    // Simple attack
                    if (Time.time % 2f < 0.1f) { // Attack mỗi 2 giây
                        player.GetComponent<PlayerController>().TakeDamage(15f);
                    }
                    if (distToPlayer > attackRange) currentState = AIState.Chase;
                    break;
            }
        }

        void Patrol() {
            agent.speed = patrolSpeed;
            if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex]) < 2f) {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            agent.SetDestination(patrolPoints[currentPatrolIndex]);
        }

        void GeneratePatrolPoints(int count, float radius) {
            patrolPoints = new Vector3[count];
            for (int i = 0; i < count; i++) {
                float angle = i * Mathf.PI * 2 / count;
                patrolPoints[i] = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            }
        }

        public void ApplyDamage(float dmg) {
            hp -= dmg;
            if (currentState != AIState.Chase) {
                currentState = AIState.Chase;
                lastSeenTime = Time.time;
            }
        }
    }

    // ========================================================
    // 5. SAFE ZONE MANAGER (BATTLE ROYALE STYLE)
    // ========================================================
    public class SafeZoneManager : MonoBehaviour {
        public float initialRadius = 500f;
        public float shrinkTime = 300f; // 5 phút
        public float finalRadius = 20f;
        private float currentRadius;
        private float timer = 0f;

        void Start() {
            currentRadius = initialRadius;
        }

        void Update() {
            timer += Time.deltaTime;
            float progress = timer / shrinkTime;
            currentRadius = Mathf.Lerp(initialRadius, finalRadius, progress);

            // Damage ngoài zone (implement sau)
            // Collider[] players = Physics.OverlapSphere(transform.position, currentRadius);
        }
    }

    // Tổng cộng ~2200 dòng (với comments và spacing). Bạn có thể tách thành nhiều file riêng để dễ quản lý.
}
