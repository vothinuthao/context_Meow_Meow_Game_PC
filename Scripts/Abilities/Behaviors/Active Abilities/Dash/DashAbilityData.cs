using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Dash Ability Data", menuName = "October/Abilities/Active/Dash")]
    public class DashAbilityData : GenericAbilityData<DashAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.Dash;
        }

        private void OnValidate()
        {
            type = AbilityType.Dash;
        }
    }
}