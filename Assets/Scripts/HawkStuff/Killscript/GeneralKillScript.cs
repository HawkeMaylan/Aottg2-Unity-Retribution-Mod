using UnityEngine;
using Photon.Pun;
using Characters;
using Effects;

[RequireComponent(typeof(Collider))]
public class GeneralKillScript : MonoBehaviourPun
{
    [Header("General Settings")]
    public float destroyAfterSeconds = 5f;

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
        if (destroyAfterSeconds > 0f)
            Destroy(gameObject, destroyAfterSeconds);
    }

    private void Update()
    {
        Collider[] hits = Physics.OverlapBox(transform.position, transform.localScale / 2f, transform.rotation);
        foreach (var other in hits)
        {
            Human human = other.GetComponentInParent<Human>();
            if (damageHumans && human != null && human.IsMine())
            {
                human.GetHit("", humanDamage, "Collision", other.name);
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
                titan.GetHit("Blade", titanNapeDamage, "BladeThrow", hitboxName);
            }

            if (disableArms && (hitboxName == armL || hitboxName == armR))
            {
                titan.GetHit("Blade", 0, "BladeThrow", hitboxName);
            }

            if (crippleLegs && (hitboxName == legL || hitboxName == legR))
            {
                titan.GetHit("Blade", 0, "BladeThrow", hitboxName);
            }

            if (directionalStun)
            {
                Vector3 dir = (titan.Cache.Transform.position - transform.position).normalized;
                dir.y = 0f;
                titan.Cache.Rigidbody.AddForce(dir * knockbackForce, ForceMode.Impulse);
                titan.GetHit("Blade", 0, "TitanStun", hitboxName);
            }
        }
    }
}
