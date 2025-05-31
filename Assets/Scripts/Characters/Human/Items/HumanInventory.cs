using UnityEngine;

namespace Characters
{
    public class HumanInventory : MonoBehaviour
    {
        [Header("Deployable Counts")]
        public int cannonCount = 0;
        public int wagon1Count = 0;
        public int wagon2Count = 0;

        //  increment/decrement helper methods
        public void AddCannon() => cannonCount++;
        public void RemoveCannon() => cannonCount = Mathf.Max(0, cannonCount - 1);

        public void AddWagon1() => wagon1Count++;
        public void RemoveWagon1() => wagon1Count = Mathf.Max(0, wagon1Count - 1);

        public void AddWagon2() => wagon2Count++;
        public void RemoveWagon2() => wagon2Count = Mathf.Max(0, wagon2Count - 1);
    }
}
