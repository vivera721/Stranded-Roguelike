using StrandedRoguelike;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TechnoBladeHitbox : MonoBehaviour
{
    private SurvivorWeaponController owner;

    public void Initialize(SurvivorWeaponController owner)
    {
        this.owner = owner;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHit(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryHit(collision);
    }

    private void TryHit(Collider2D collision)
    {
        if (owner == null) return;
        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;

        owner.TryHitWithTechnoBlade(enemy);
    }
}
