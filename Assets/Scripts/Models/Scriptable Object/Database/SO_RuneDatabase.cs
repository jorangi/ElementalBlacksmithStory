using System;
using System.Collections.Generic;
using UnityEngine;
namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_RuneDatabase", menuName = "Scriptable Objects/Database/SO_RuneDatabase")]
    public class SO_RuneDatabase : SO_BaseDatabase<SO_RuneData>
    {
        private readonly Dictionary<RuneGrade, List<SO_RuneData>> _runeDict = new();
        public override void Init()
        {
            _itemDict = new();
            _runeDict.Clear();
            foreach (var g in Enum.GetValues(typeof(RuneGrade)))
            {
                _runeDict[(RuneGrade)g] = new();
            }

            foreach (var i in _itemList)
            {
                if (i == null)
                {
                    Debug.LogWarning($"[SO_BaseDatabase<SO_RuneData>] 리스트에 null이 포함되어 있습니다.");
                    continue;
                }
                if (!_itemDict.ContainsKey(i.Id))
                {
                    _itemDict.Add(i.Id, i);
                }

                if (_runeDict.TryGetValue(i.grade, out var list))
                {
                    if (!list.Contains(i))
                    {
                        list.Add(i);
                    }
                    else
                    {
                        Debug.LogWarning($"[SO_BaseDatabase<SO_RuneData>] Id가 {i.Id}인 데이터가 이미 딕셔너리에 포함되어 있습니다.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SO_BaseDatabase<SO_RuneData>] 정의되지 않은 RuneGrade({i.grade})를 가진 데이터({i.Id})가 있습니다.");
                }
            }
        }
        public SO_RuneData[] GetRunesByGrade(RuneGrade grade)
        {
            return _runeDict.TryGetValue(grade, out var list) ? list.ToArray() : Array.Empty<SO_RuneData>();
        }
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}