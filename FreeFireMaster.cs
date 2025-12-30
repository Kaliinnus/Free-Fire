using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ========================================================
// FREE FIRE ULTIMATE SYSTEM - ALL-IN-ONE SCRIPT
// ========================================================

namespace FreeFireUltimate
{
    // --- 1. DATA MODELS (DỮ LIỆU SÚNG & SKIN) ---
    [System.Serializable]
    public class WeaponSkin {
        public string skinName;
        public float bonusDamage;
        public float fireRateBoost; // Giá trị âm để bắn nhanh hơn
        public Color skinColor;
    }

    [System.Serializable]
    public class GunData {
        public string gunName;
        public float damage;
        public float fireRate;
        public float range;
        public int ammo;
        public WeaponSkin activeSkin;
    }

    // --- 2. THE MASTER CONTROLLER (NGƯỜI CHƠI) ---
    public class FreeFireManager : MonoBehaviour
    {
        [Header("Chỉ số Sinh tồn")]
        public float health = 200f;
        public bool isDead = false;
        public bool hasLanded = false;
        public bool isLobby = true;

        [Header("Điều khiển PC (WASD & Mouse)")]
        public float moveSpeed = 8f;
        public float mouseSensitivity = 150f;
        private float xRotation = 0f;

        [Header("Hệ thống Chiến đấu")]
        public List<GunData> inventory = new List<GunData>();
        public int currentSlot = 0;
        private float nextFireTime = 0f;

        [Header("Tham chiếu Unity")]
        public Camera playerCam;
        public GameObject parachuteModel;
        public Text hudText;
        public GameObject lobbyUI;
        public Transform mapCenter;

        private CharacterController controller;
        private Vector3 velocity;

        void Awake() {
            controller = GetComponent<CharacterController>();
            if (isLobby) {
                Cursor.lockState = CursorLockMode.None;
                lobbyUI.SetActive(true);
            }
        }

        // HÀM BẮT ĐẦU TRẬN ĐẤU (GỌI TỪ NÚT LOBBY)
        public void StartMatch() {
            isLobby = false;
            lobbyUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            
            // Khởi tạo vũ khí mẫu với Skin
            GunData scar = new GunData { gunName = "SCAR", damage = 30, fireRate = 0.15f, range = 120, ammo = 30 };
            scar.activeSkin = new WeaponSkin { skinName = "Đẳng Cấp Titan", bonusDamage = 7, fireRateBoost = -0.03f, skinColor = Color.yellow };
            inventory.Add(scar);

            // Đưa người chơi lên trời để nhảy dù
            transform.position = new Vector3(Random.Range(-50, 50), 300, Random.Range(-50, 50));
        }

        void Update() {
            if (isDead || isLobby) return;

            if (!hasLanded) {
                HandleParachute();
            } else {
                HandleLook();
                HandleMovement();
                HandleCombat();
            }
            UpdateHUD();
        }

        // --- ĐIỀU KHIỂN CHUỘT ---
        void HandleLook() {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        // --- ĐIỀU KHIỂN WASD ---
        void HandleMovement() {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += -18f * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        // --- NHẢY DÙ ---
        void HandleParachute() {
            controller.Move(Vector3.down * 20f * Time.deltaTime);
            if (controller.isGrounded) {
                hasLanded = true;
                if (parachuteModel) parachuteModel.SetActive(false);
            }
        }

        // --- CHIẾN ĐẤU (CLICK TRÁI/PHẢI) ---
        void HandleCombat() {
            if (inventory.Count == 0) return;
            GunData current = inventory[currentSlot];

            // Chuột trái bắn
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime) {
                nextFireTime = Time.time + (current.fireRate + current.activeSkin.fireRateBoost);
                Fire(current);
            }

            // Chuột phải ngắm
            playerCam.fieldOfView = Input.GetMouseButton(1) ? 
                Mathf.Lerp(playerCam.fieldOfView, 30f, 0.1f) : 
                Mathf.Lerp(playerCam.fieldOfView, 60f, 0.1f);
        }

        void Fire(GunData g) {
            if (g.ammo <= 0) return;
            g.ammo--;

            RaycastHit hit;
            if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, g.range)) {
                if (hit.transform.CompareTag("Enemy")) {
                    float totalDmg = g.damage + g.activeSkin.bonusDamage;
                    hit.transform.GetComponent<EnemyAI>().TakeDamage(totalDmg);
                }
            }
        }

        public void TakeDamage(float amount) {
            health -= amount;
            if (health <= 0) Die();
        }

        void Die() {
            isDead = true;
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Chơi lại
        }

        void UpdateHUD() {
            if (hudText && inventory.Count > 0) {
                hudText.text = $"HP: {Mathf.Ceil(health)} | {inventory[currentSlot].gunName}\n" +
                               $"Skin: {inventory[currentSlot].activeSkin.skinName} | Đạn: {inventory[currentSlot].ammo}";
            }
        }
    }

    // --- 3. BOT AI (KẺ ĐỊCH) ---
    public class EnemyAI : MonoBehaviour {
        public float hp = 100f;
        private NavMeshAgent agent;
        private Transform target;

        void Start() {
            agent = GetComponent<NavMeshAgent>();
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }

        void Update() {
            if (hp <= 0) return;
            float d = Vector3.Distance(transform.position, target.position);
            if (d < 40f) agent.SetDestination(target.position);
            if (d < 15f && Random.value < 0.02f) {
                target.GetComponent<FreeFireManager>().TakeDamage(3f);
            }
        }

        public void TakeDamage(float dmg) {
            hp -= dmg;
            if (hp <= 0) Destroy(gameObject, 0.5f);
        }
    }

    // --- 4. WORLD & MAP SYSTEM (BO & LOOT) ---
    public class WorldSystem : MonoBehaviour {
        public float zoneRadius = 500f;
        public Transform zoneVisual;

        void Update() {
            zoneRadius -= 0.5f * Time.deltaTime;
            if (zoneVisual) zoneVisual.localScale = new Vector3(zoneRadius, 100, zoneRadius);

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (Vector3.Distance(p.transform.position, Vector3.zero) > zoneRadius / 2) {
                p.GetComponent<FreeFireManager>().TakeDamage(2f * Time.deltaTime);
            }
        }
    }

    public class LootAmmo : MonoBehaviour {
        void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Player")) {
                other.GetComponent<FreeFireManager>().inventory[0].ammo += 30;
                Destroy(gameObject);
            }
        }
    }
}
