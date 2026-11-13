using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [System.Flags]
    public enum WeaponType
    {
        None = 0,
        Sword = 1 << 0,
        Bow = 1 << 1,
        Blunt = 1 << 2,
        Spear = 1 << 3,
        Grimoire = 1 << 4
    }
    [CreateAssetMenu(fileName = "SO_Mercenary", menuName = "Scriptable Objects/SO_Mercenary")]
    public class SO_Mercenary : ScriptableObject, IIdentifiable
    {
        public uint Id => uint.Parse(name);


        [Header("용병 기본 데이터")]
        public string MercName;
        public uint SpriteId;
        [TextArea(3, 5)]
        public string Description;
        public WeaponType WeaponType;

        [Header("스탯 데이터")]
        public List<BaseStat> BaseStats = new();

        public float GetBaseStat(StatType statType)
        {
            for (int i = 0; i < BaseStats.Count; i++)
            {
                if (BaseStats[i].StatType == statType)
                    return BaseStats[i].Value;
            }
            return 0f;
        }

        [Header("스킬 데이터")]
        private List<SkillData> Skills = new(); // 스킬 데이터
    }
}