using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_WeaponDatabase", menuName = "Scriptable Objects/SO_WeaponDatabase")]
    public class SO_WeaponDatabase : ScriptableObject
    {
        [SerializeField] List<SO_WeaponData> weaponList = new();
        private Dictionary<uint, SO_WeaponData> _weaponDict = new();
        public void Init()
        {
            _weaponDict = new();
            foreach(var w in weaponList)
            {
                if (w == null)
                {
                    Debug.LogWarning("[SO_WeaponDatabase] weaponList에 null 항목이 포함되어 있습니다.");
                    continue;
                }
                if (!_weaponDict.ContainsKey(w.id))
                {
                    _weaponDict.Add(w.id, w);
                }
                else
                {
                    Debug.LogWarning($"[SO_WeaponDatabase] ID {w.id} ({w.weaponName})가 이미 딕셔너리에 존재합니다.");
                }
            }
        }
        public SO_WeaponData GetWeapon(uint id)
        {
            if(_weaponDict.ContainsKey(id))
            {
                return _weaponDict[id];
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SO_WeaponDatabase] {id}가 존재하지 않습니다.");
                return null;
            }
        }
        #if UNITY_EDITOR
        [ContextMenu("SO_WeaponData 자동 등록")]
        private void AutoRegisterWeaponData()
        {
            weaponList.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SO_WeaponData");
            foreach(var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var weapon = UnityEditor.AssetDatabase.LoadAssetAtPath<SO_WeaponData>(path);
                if(weapon != null)
                {
                    weaponList.Add(weapon);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SO_WeaponDatabase] 총 {weaponList.Count}개의 무기 데이터를 자동 등록했습니다.");
        }
        #endif
    }
}
