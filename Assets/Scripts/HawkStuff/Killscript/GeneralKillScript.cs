using UnityEngine;
using Photon.Pun;
using Characters;
using Effects;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class GeneralKillScript : MonoBehaviourPunCallbacks
{
    [Header("General Settings")]
    public float destroyAfterSeconds = 5f;
    public string killSourceName = "Blade";

    [Header("Optional Collision Animation")]
    public bool playAnimationOnCollision = false;
    public AnimationClip collisionAnimation;
    public float animationDelayTime = 0f;
    public bool makeKinematicOnCollision = false;
    public LayerMask animationCollisionLayers = ~0;

    [Header("Optional Particle Effect")]
    public bool spawnParticleOnCollision = false;
    public GameObject collisionParticlePrefab;

    private Animation legacyAnim;
    private bool animationPlayed = false;
    private bool particleSpawned = false;
    private float spawnTime;
    private Rigidbody rb;
    private Collider selfCollider;

    [Header("Human Settings")]
    public bool damageHumans = true;
    public int humanDamage = 100;

    [Header("Titan Settings")]
    public bool damageNape = true;
    public int titanNapeDamage = 1000;
    public bool disableArms = true;
    public bool crippleLegs = true;
    public bool blindEyes = true;
    public bool directionalStun = true;
    public float knockbackForce = 30f;
    public int maxKnockbacksPerTitan = 1;

    private Dictionary<BaseTitan, int> titanKnockbackCounts = new Dictionary<BaseTitan, int>();

    private void Start()
    {
        spawnTime = Time.time;

        if (destroyAfterSeconds > 0f)
            Invoke(nameof(SelfDestruct), destroyAfterSeconds);

        
        if (playAnimationOnCollision)
        {
            if (collisionAnimation != null)
            {
                legacyAnim = GetComponent<Animation>();
                if (legacyAnim == null)
                    legacyAnim = gameObject.AddComponent<Animation>();

                legacyAnim.playAutomatically = false;

                if (!legacyAnim.GetClip(collisionAnimation.name))
                    legacyAnim.AddClip(collisionAnimation, collisionAnimation.name);
            }
            else
            {
                Debug.LogWarning($"[GeneralKillScript] playAnimationOnCollision is enabled, but no animation clip is assigned on {gameObject.name}");
            }
        }

        rb = GetComponent<Rigidbody>();
        selfCollider = GetComponent<Collider>();
    }


    private void Update()
    {
        if (playAnimationOnCollision && Time.time - spawnTime < animationDelayTime)
            return;

        Collider[] hits = Physics.OverlapBox(transform.position, transform.localScale / 2f, transform.rotation);
        foreach (var other in hits)
        {
            if (other == selfCollider)
                continue;

            if (((1 << other.gameObject.layer) & animationCollisionLayers) != 0 && !animationPlayed)
            {
                photonView.RPC("RPC_PlayCollisionAnimation", RpcTarget.All);
                animationPlayed = true;
            }

            if (spawnParticleOnCollision && !particleSpawned && collisionParticlePrefab != null)
            {
                photonView.RPC("RPC_SpawnParticle", RpcTarget.All, transform.position);
                particleSpawned = true;
            }

            if (!PhotonNetwork.IsMasterClient)
                continue;

            Human human = other.GetComponentInParent<Human>();
            if (damageHumans && human != null && human.IsMine())
            {
                human.GetHit(killSourceName, humanDamage, "Collision", other.name);
                continue;
            }

            BaseTitan baseTitan = other.GetComponentInParent<BaseTitan>();
            if (baseTitan == null || baseTitan.Dead || !baseTitan.AI)
                continue;

            BasicTitan titan = baseTitan as BasicTitan;
            if (titan == null) continue;

            string hitboxName = other.name;

            var cache = titan.BaseTitanCache;
            if (blindEyes && hitboxName == cache.EyesHurtbox?.name)
            {
                EffectSpawner.Spawn(EffectPrefabs.CriticalHit, transform.position, Quaternion.Euler(270f, 0f, 0f));
                titan.GetHit("SmokeBomb", 0, "SmokeBomb", hitboxName);
            }
            if (damageNape && hitboxName == cache.NapeHurtbox?.name)
            {
                titan.GetHit(killSourceName, titanNapeDamage, "BladeThrow", hitboxName);
            }
            if (disableArms && (hitboxName == titan.BasicCache.ForearmLHurtbox?.name || hitboxName == titan.BasicCache.ForearmRHurtbox?.name))
            {
                titan.GetHit(killSourceName, 0, "BladeThrow", hitboxName);
            }
            if (crippleLegs && (hitboxName == cache.LegLHurtbox?.name || hitboxName == cache.LegRHurtbox?.name))
            {
                titan.GetHit(killSourceName, 0, "BladeThrow", hitboxName);
            }

            if (directionalStun)
            {
                if (!titanKnockbackCounts.ContainsKey(baseTitan))
                    titanKnockbackCounts[baseTitan] = 0;

                if (titanKnockbackCounts[baseTitan] < maxKnockbacksPerTitan)
                {
                    Vector3 dir = (titan.Cache.Transform.position - transform.position).normalized;
                    dir.y = 0f;

                    titan.GetHit(killSourceName, 0, "TitanStun", hitboxName);
                    titan.Cache.Rigidbody.isKinematic = false;
                    titan.Cache.Rigidbody.AddForce(dir * knockbackForce, ForceMode.Impulse);
                    titanKnockbackCounts[baseTitan]++;
                }
            }
        }
    }

    [PunRPC]
    private void RPC_PlayCollisionAnimation()
    {
        if (legacyAnim != null && collisionAnimation != null)
        {
            legacyAnim.Play(collisionAnimation.name);
            if (makeKinematicOnCollision && rb != null)
                rb.isKinematic = true;
        }
    }

    [PunRPC]
    private void RPC_SpawnParticle(Vector3 position)
    {
        if (collisionParticlePrefab != null)
            Instantiate(collisionParticlePrefab, position, Quaternion.identity);
    }

    private void SelfDestruct()
    {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }
}
