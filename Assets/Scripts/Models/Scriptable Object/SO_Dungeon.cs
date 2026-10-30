using System.Collections.Generic;
using UnityEngine;
namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_Dungeon", menuName = "Scriptable Object/SO_Dungeon")]
    public class SO_Dungeon : ScriptableObject, IIdentifiable
    {
        public uint Id => uint.Parse(this.name);
        [SerializeField] string _dungeonName;
        [SerializeField] uint _dungeonLength;
        /// <summary>
        /// 엔티티 배치 번호
        /// </summary>
        private readonly Dictionary<uint, List<uint>> _placeEntity = new();
    }
}