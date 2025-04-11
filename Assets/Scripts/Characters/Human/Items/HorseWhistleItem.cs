using Characters;
using UnityEngine;

namespace Characters
{
    class HorseWhistleItem : SimpleUseable
    {
        public HorseWhistleItem(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;

            if (human != null && human.Horse != null)
            {
                human.Horse.HorseWhistle(); // 
                human.PlaySound(HumanSounds.FlareLaunch); // Optional sound effect
            }
        }
    }
}
