using System.Collections;
using System.Collections.Generic;
using OctoberStudio;
using UnityEngine;

[System.Serializable]
public class DefaultAbilityEntry
{
    public AbilityType abilityType;
    public int level = 0;
}
namespace OctoberStudio
{
    [System.Serializable]
    public class CharacterData
    {
        [SerializeField] protected string name;
        public string Name => name;

        [SerializeField] protected int cost;
        public int Cost => cost;

        [SerializeField] protected Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] protected GameObject prefab;
        public GameObject Prefab => prefab;

        [Space]
        [SerializeField] protected bool hasStartingAbility = false;
        public bool HasStartingAbility => hasStartingAbility;

        [SerializeField] protected AbilityType startingAbility = AbilityType.Dash;
        public AbilityType StartingAbility => startingAbility;

        [Space]
        [SerializeField, Min(1)] protected float baseHP;
        public float BaseHP => baseHP;

        [SerializeField, Min(1f)] protected float baseDamage;
        public float BaseDamage => baseDamage;
        
        [Header("Default Abilities")]
        [SerializeField] List<DefaultAbilityEntry> startingAbilities = new List<DefaultAbilityEntry>();
        public List<DefaultAbilityEntry> StartingAbilities => startingAbilities;
    }
}