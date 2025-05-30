using UnityEngine;
using Photon.Pun;
using Characters;
using Effects;

[RequireComponent(typeof(Collider))]
public class GeneralKillScript : MonoBehaviourPun
{
    [Header("General Settings")]
    public float destroyAfterSeconds = 5f;
    public string killSourceName = "Blade";

    [Header("Optional Collision Animation")]
    public bool playAnimationOnCollision = false;
    public AnimationClip collisionAnimation;
    public float animationDelayTime = 0f;
    public bool makeKinematicOnCollision = false;

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

    private void Start()
    {
        spawnTime = Time.time;

        if (destroyAfterSeconds > 0f)
            Invoke(nameof(SelfDestruct), destroyAfterSeconds);

        if (playAnimationOnCollision && collisionAnimation != null)
        {
            legacyAnim = GetComponent<Animation>();
            if (legacyAnim == null)
                legacyAnim = gameObject.AddComponent<Animation>();

            legacyAnim.playAutomatically = false;
            if (!legacyAnim.GetClip(collisionAnimation.name))
                legacyAnim.AddClip(collisionAnimation, collisionAnimation.name);
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

            if (playAnimationOnCollision && !animationPlayed && legacyAnim != null && collisionAnimation != null)
            {
                legacyAnim.Play(collisionAnimation.name);
                animationPlayed = true;

                if (makeKinematicOnCollision && rb != null)
                    rb.isKinematic = true;

                CancelInvoke(nameof(SelfDestruct));
                Invoke(nameof(SelfDestruct), collisionAnimation.length);
            }

            if (spawnParticleOnCollision && !particleSpawned && collisionParticlePrefab != null)
            {
                Instantiate(collisionParticlePrefab, transform.position, Quaternion.identity);
                particleSpawned = true;
            }

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
            if (titan == null)
                continue;

            string hitboxName = other.name;

            var eyes = titan.BaseTitanCache.EyesHurtbox?.name;
            var nape = titan.BaseTitanCache.NapeHurtbox?.name;
            var legL = titan.BaseTitanCache.LegLHurtbox?.name;
            var legR = titan.BaseTitanCache.LegRHurtbox?.name;
            var armL = titan.BasicCache.ForearmLHurtbox?.name;
            var armR = titan.BasicCache.ForearmRHurtbox?.name;

            if (blindEyes && hitboxName == eyes)
            {
                EffectSpawner.Spawn(EffectPrefabs.CriticalHit, transform.position, Quaternion.Euler(270f, 0f, 0f));
                titan.GetHit("SmokeBomb", 0, "SmokeBomb", hitboxName);
            }

            if (damageNape && hitboxName == nape)
            {
                titan.GetHit(killSourceName, titanNapeDamage, "BladeThrow", hitboxName);
            }

            if (disableArms && (hitboxName == armL || hitboxName == armR))
            {
                titan.GetHit(killSourceName, 0, "BladeThrow", hitboxName);
            }

            if (crippleLegs && (hitboxName == legL || hitboxName == legR))
            {
                titan.GetHit(killSourceName, 0, "BladeThrow", hitboxName);
            }

            if (directionalStun)
            {
                Vector3 dir = (titan.Cache.Transform.position - transform.position).normalized;
                dir.y = 0f;
                titan.Cache.Rigidbody.AddForce(dir * knockbackForce, ForceMode.Impulse);
                titan.GetHit(killSourceName, 0, "TitanStun", hitboxName);
            }
        }
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }
}
