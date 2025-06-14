using UnityEngine;
using Photon.Pun;
using System.Collections;
using CustomLogic;
using GameManagers;
using Settings;
using Characters;

namespace Entities
{
    public enum EntityForm { Human, Titan }

    [RequireComponent(typeof(Collider))]
    public class DamageableEntity : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
    {
        [Header("Entity Setup")]
        public string entityName = "DamageableEntity";
        public int maxHP = 100;
        public int currentHP = -1;
        public string team = "Neutral";

        [Header("GeneralKill Compatibility")]
        public EntityForm entityForm = EntityForm.Human;

        [Header("Options")]
        public bool showKillFeed = true;
        public bool useTextDisplay = true;
        public bool use3DHealthBar = true;
        public bool destroyOnDeath = true;
        public bool onlyShowUIWhenDamaged = false;
        public float hideUIDistance = 50f;
        public Camera referenceCamera;

        [Header("UI Fade Settings")]
        public bool fadeUIAfterDelay = false;
        public float uiFadeDelay = 5f;

        [Header("UI Offsets and Scale")]
        public Vector3 healthBarOffset = new Vector3(0f, 2.5f, 0f);
        public Vector3 textOffset = new Vector3(0f, 2f, 0f);
        public float textScale = 1f;
        public Vector3 healthBarScale = new Vector3(1f, 0.2f, 1f);

        [Header("Hit Cooldown")]
        public float hitCooldown = 0.2f;

        [Header("Damage Settings")]
        public int flatDamageFromUnknown = 100;

        [Header("Effect Prefabs (Resources/HParticles)")]
        public GameObject hitEffectPrefab;
        public Vector3 hitEffectOffset;
        public Vector3 hitEffectRotation;

        public GameObject deathEffectPrefab;
        public Vector3 deathEffectOffset;
        public Vector3 deathEffectRotation;

        [Header("Custom Collider")]
        public Collider customCollider;

        private bool isDead;
        private float lastHitTime = -999f;
        private bool wasDamaged = false;

        private GameObject hpBillboard;
        private TextMesh hpText;

        private GameObject healthBarRoot;
        private Transform foregroundBar;

        private void Awake()
        {
            if (currentHP < 0 || currentHP > maxHP)
                currentHP = maxHP;

            if (useTextDisplay)
                CreateBillboard();
            if (use3DHealthBar)
                CreateHealthBar();
        }

        private void Start()
        {
            if (referenceCamera == null)
                referenceCamera = Camera.main;

            UpdateBillboard();
            UpdateHealthBar();

            if (customCollider != null && customCollider != GetComponent<Collider>())
            {
                ColliderEventForwarder forwarder = customCollider.gameObject.AddComponent<ColliderEventForwarder>();
                forwarder.Setup(this);
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            photonView.RPC("UpdateHealthRPC", RpcTarget.All, currentHP);
        }

        [PunRPC]
        private void UpdateHealthRPC(int hp)
        {
            currentHP = hp;
            UpdateBillboard();
            UpdateHealthBar();
        }

        public void GetHit(string source, int damage, string type = "Collision", string hitbox = "")
        {
            if (!PhotonNetwork.IsMasterClient || isDead)
                return;

            if (Time.time - lastHitTime < hitCooldown)
                return;

            lastHitTime = Time.time;
            wasDamaged = true;
            currentHP -= damage;

            photonView.RPC("UpdateHealthRPC", RpcTarget.All, currentHP);

            if (hitEffectPrefab != null)
                photonView.RPC("SpawnEffectRPC", RpcTarget.All, hitEffectPrefab.name, transform.position + hitEffectOffset, Quaternion.Euler(hitEffectRotation));

            if (currentHP <= 0)
                Die(source, type);
        }

        [PunRPC]
        private void RequestHitRPC(string source, int damage, string type, string hitbox, int viewID)
        {
            if (!PhotonNetwork.IsMasterClient || isDead || photonView.ViewID != viewID)
                return;

            GetHit(source, damage, type, hitbox);
        }

        private void Die(string killerName, string type)
        {
            isDead = true;

            if (deathEffectPrefab != null)
                photonView.RPC("SpawnEffectRPC", RpcTarget.All, deathEffectPrefab.name, transform.position + deathEffectOffset, Quaternion.Euler(deathEffectRotation));

            if (showKillFeed && CustomLogicManager.Evaluator != null)
            {
                int damage = Mathf.Clamp(maxHP, 0, maxHP);
                RPCManager.PhotonView?.RPC("ShowKillFeedRPC", RpcTarget.All, new object[] { killerName, entityName, damage, type });
            }

            if (destroyOnDeath)
                photonView.RPC("RequestDestroyRPC", RpcTarget.MasterClient);
        }

        [PunRPC]
        private void RequestDestroyRPC()
        {
            if (PhotonNetwork.IsMasterClient && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC]
        private void SpawnEffectRPC(string resourceName, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = Resources.Load<GameObject>($"HParticles/{resourceName}");
            if (prefab != null)
                Instantiate(prefab, position, rotation); // Local-only visual effect
        }

        private void CreateBillboard()
        {
            hpBillboard = new GameObject("HPBillboard");
            hpBillboard.transform.SetParent(transform);
            hpBillboard.transform.localPosition = textOffset;
            hpBillboard.transform.localRotation = Quaternion.identity;
            hpBillboard.transform.localScale = Vector3.one * textScale;

            TextMesh textMesh = hpBillboard.AddComponent<TextMesh>();
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;
            hpText = textMesh;
        }

        private void CreateHealthBar()
        {
            healthBarRoot = new GameObject("HealthBarRoot");
            healthBarRoot.transform.SetParent(transform);
            healthBarRoot.transform.localPosition = healthBarOffset;
            healthBarRoot.transform.localScale = healthBarScale;

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BarBackground";
            bg.transform.SetParent(healthBarRoot.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = Vector3.one;
            bg.GetComponent<Renderer>().material.color = Color.black;

            GameObject fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fg.name = "BarForeground";
            fg.transform.SetParent(healthBarRoot.transform);
            fg.transform.localPosition = new Vector3(-0.5f, 0f, -0.01f);
            fg.transform.localScale = Vector3.one;
            fg.GetComponent<Renderer>().material.color = Color.green;
            foregroundBar = fg.transform;
        }

        private void UpdateBillboard()
        {
            if (hpText != null)
                hpText.text = currentHP.ToString();
        }

        private void UpdateHealthBar()
        {
            if (foregroundBar != null && maxHP > 0)
            {
                float ratio = Mathf.Clamp01((float)currentHP / maxHP);
                foregroundBar.localScale = new Vector3(ratio, 1f, 1f);
                foregroundBar.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.01f);

                var color = Color.green;
                if (ratio <= 0.25f)
                    color = Color.red;
                else if (ratio <= 0.5f)
                    color = new Color(1f, 0.65f, 0f);

                foregroundBar.GetComponent<Renderer>().material.color = color;
            }
        }

        private void FixedUpdate()
        {
            if (referenceCamera == null)
                referenceCamera = Camera.main;
            if (referenceCamera == null)
                return;

            float dist = Vector3.Distance(referenceCamera.transform.position, transform.position);
            bool recentlyHit = Time.time - lastHitTime < uiFadeDelay;
            bool showUI = (!onlyShowUIWhenDamaged || (wasDamaged && (!fadeUIAfterDelay || recentlyHit))) && dist < hideUIDistance;

            if (useTextDisplay && hpBillboard != null)
            {
                hpBillboard.transform.rotation = Quaternion.LookRotation(hpBillboard.transform.position - referenceCamera.transform.position);
                hpBillboard.SetActive(showUI);
            }

            if (use3DHealthBar && healthBarRoot != null)
            {
                healthBarRoot.transform.rotation = Quaternion.LookRotation(healthBarRoot.transform.position - referenceCamera.transform.position);
                healthBarRoot.SetActive(showUI);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (customCollider == null)
                TryHitFromCollider(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (customCollider == null)
                TryHitFromCollider(collision.collider);
        }

        public void TryHitFromCollider(Collider collider)
        {
            var hitbox = collider.GetComponent<BaseHitbox>();
            var attacker = hitbox?.Owner;

            if (hitbox == null || attacker == null || !hitbox.IsActive())
                return;

            string attackerName = attacker.Name ?? "Unknown";

            if (attacker is Human human && human.IsMine())
            {
                photonView.RPC("RequestHitRPC", RpcTarget.MasterClient, attackerName, flatDamageFromUnknown, "Blade", collider.name, photonView.ViewID);
            }
            else if (attacker == null)
            {
                photonView.RPC("RequestHitRPC", RpcTarget.MasterClient, "Unknown", flatDamageFromUnknown, "Collision", collider.name, photonView.ViewID);
            }
        }
    }

    public class ColliderEventForwarder : MonoBehaviour
    {
        private DamageableEntity damageable;

        public void Setup(DamageableEntity entity)
        {
            damageable = entity;
        }

        private void OnTriggerEnter(Collider other)
        {
            damageable?.TryHitFromCollider(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            damageable?.TryHitFromCollider(collision.collider);
        }
    }
}
