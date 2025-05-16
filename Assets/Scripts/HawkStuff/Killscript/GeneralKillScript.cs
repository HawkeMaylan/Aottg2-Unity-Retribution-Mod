using UnityEngine;
using Photon.Pun;
using Characters;

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

    private void OnTriggerEnter(Collider other)
    {
        // Handle human collision
        Human human = other.GetComponentInParent<Human>();
        if (damageHumans && human != null && human.IsMine())
        {
            human.GetHit("", humanDamage, "Collision", other.name);  // Specify correct overload
            return;
        }

        // Handle titan collision
        BaseTitan titan = other.GetComponentInParent<BaseTitan>();
        if (titan == null || titan.Dead) return;

        string hitbox = other.name;
        int localViewId = photonView.ViewID;
        string attackerName = "Environmental";

        // Cast to BasicTitan for BasicCache access
        BasicTitan basicTitan = titan as BasicTitan;

        // Nape damage (Kill)
        if (damageNape && hitbox == titan.BaseTitanCache.NapeHurtbox?.name)
        {
            titan.photonView.RPC("GetHitRPC", RpcTarget.All, localViewId, attackerName, titanNapeDamage, "BladeThrow", hitbox);
        }

        // Disable arms
        else if (disableArms && basicTitan != null)
        {
            if (hitbox == basicTitan.BasicCache.ForearmLHurtbox?.name || hitbox == basicTitan.BasicCache.ForearmRHurtbox?.name)
            {
                titan.photonView.RPC("GetHitRPC", RpcTarget.All, localViewId, attackerName, 0, "BladeThrow", hitbox);
            }
        }

        // Cripple legs
        else if (crippleLegs && titan.BaseTitanCache.LegLHurtbox != null &&
            (hitbox == titan.BaseTitanCache.LegLHurtbox.name || hitbox == titan.BaseTitanCache.LegRHurtbox?.name))
        {
            titan.photonView.RPC("GetHitRPC", RpcTarget.All, localViewId, attackerName, 0, "BladeThrow", hitbox);
        }

        // Blind
        else if (blindEyes && hitbox == titan.BaseTitanCache.EyesHurtbox?.name)
        {
            titan.photonView.RPC("GetHitRPC", RpcTarget.All, localViewId, attackerName, 0, "BladeThrow", hitbox);
        }

        // Directional stun and knockback (applied regardless of hitbox type)
        if (directionalStun)
        {
            Vector3 direction = titan.Cache.Transform.position - transform.position;
            direction.y = 0f;
            titan.Cache.Rigidbody.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
            titan.photonView.RPC("GetHitRPC", RpcTarget.All, localViewId, attackerName, 0, "TitanStun", hitbox);
        }
    }
}
