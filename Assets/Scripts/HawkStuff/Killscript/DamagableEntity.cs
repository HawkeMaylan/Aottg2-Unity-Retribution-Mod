using UnityEngine;
using Photon.Pun;
using System.Collections;
using CustomLogic;
using GameManagers;
using Settings;
using Characters;

namespace Entities
{
    public enum EntityForm
    {
        Human,
        Titan
    }

    [RequireComponent(typeof(Collider))]
    public class DamageableEntity : MonoBehaviourPunCallbacks
    {
        [Header("Entity Setup")]
        public string entityName = "DamageableEntity";
        public int maxHP = 100;
        public string team = "Neutral";

        [Header("GeneralKill Compatibility")]
        public EntityForm entityForm = EntityForm.Human;

        [Header("Options")]
        public bool showKillFeed = true;
        public bool showBillboard = true;

        [Header("Hit Cooldown")]
        public float hitCooldown = 0.2f;

        private int currentHP;
        private bool isDead;
        private float lastHitTime = -999f;

        private GameObject hpBillboard;
        private TextMesh hpText;

        private void Awake()
        {
            currentHP = maxHP;
            if (showBillboard)
                CreateBillboard();
        }

        private void Start()
        {
            UpdateBillboard();
            if (photonView.IsMine)
            {
                photonView.RPC("SyncHealthRPC", RpcTarget.AllBuffered, currentHP, maxHP);
            }
        }

        [PunRPC]
        private void SyncHealthRPC(int hp, int max)
        {
            currentHP = hp;
            maxHP = max;
            UpdateBillboard();
        }

        public void GetHit(string source, int damage, string type = "Collision", string hitbox = "")
        {
            if (!PhotonNetwork.IsMasterClient || isDead)
                return;

            if (Time.time - lastHitTime < hitCooldown)
                return;

            lastHitTime = Time.time;

            Debug.Log($"[{entityName}] hit by {source} for {damage}. HP before hit: {currentHP}");

            currentHP -= damage;
            UpdateBillboard();

            if (currentHP <= 0)
                Die(source, type);
        }

        private void Die(string killerName, string type)
        {
            isDead = true;

            if (showKillFeed && CustomLogicManager.Evaluator != null)
            {
                int damage = Mathf.Clamp(maxHP, 0, maxHP);
                RPCManager.PhotonView.RPC("ShowKillFeedRPC", RpcTarget.All, new object[] { killerName, entityName, damage, type });
            }

            StartCoroutine(SelfDestruct());
        }

        private IEnumerator SelfDestruct()
        {
            yield return new WaitForSeconds(0.1f);

            if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && photonView.ViewID != 0)
                PhotonNetwork.Destroy(gameObject);
            else
                Destroy(gameObject);
        }

        private void CreateBillboard()
        {
            hpBillboard = new GameObject("HPBillboard");
            hpBillboard.transform.SetParent(transform);
            hpBillboard.transform.localPosition = new Vector3(0f, 2f, 0f);
            hpBillboard.transform.localRotation = Quaternion.identity;

            TextMesh textMesh = hpBillboard.AddComponent<TextMesh>();
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.red;
            hpText = textMesh;
        }

        private void UpdateBillboard()
        {
            if (hpText != null)
                hpText.text = currentHP.ToString();
        }

        private void FixedUpdate()
        {
            if (hpBillboard != null && Camera.main != null)
                hpBillboard.transform.rotation = Quaternion.LookRotation(hpBillboard.transform.position - Camera.main.transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHitFromCollider(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryHitFromCollider(collision.collider);
        }

        private void TryHitFromCollider(Collider collider)
        {
            var attacker = collider.GetComponentInParent<BaseCharacter>();
            if (attacker != null && attacker.IsMine())
            {
                
                if (collider.GetComponent<BaseHitbox>() != null)
                {
                    attacker.OnHit(null, this, collider, "Blade", true);
                }
            }

            
        }
    }
}
