using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class WeaponTreePathService
    {
        private readonly SO_WeaponDatabase _weaponDatabase;

        // BFS 탐색용 재사용 버퍼 (GC Allocation 방지)
        private readonly Queue<uint> _queue = new(64);
        private readonly HashSet<uint> _visited = new(64);
        private readonly Dictionary<uint, uint> _parent = new(64);

        [Inject]
        public WeaponTreePathService(SO_WeaponDatabase weaponDatabase)
        {
            _weaponDatabase = weaponDatabase;
        }

        public List<uint> FindPath(uint startId, uint destId)
        {
            if (startId == destId)
            {
                return new List<uint> { startId };
            }

            _queue.Clear();
            _visited.Clear();
            _parent.Clear();

            _queue.Enqueue(startId);
            _visited.Add(startId);

            SO_WeaponData destWeapon = _weaponDatabase.Get(destId);

            bool found = false;
            while (_queue.Count > 0)
            {
                uint cur = _queue.Dequeue();
                if (cur == destId)
                {
                    found = true;
                    break;
                }

                SO_WeaponData currentWeapon = _weaponDatabase.Get(cur);
                if (currentWeapon == null || currentWeapon.recipes == null) continue;

                // 1차: 목표 무기와 속성을 공유하는 레시피 우선 큐잉 (정통 속성 기둥 우선)
                for (int r = 0; r < currentWeapon.recipes.Count; r++)
                {
                    var recipe = currentWeapon.recipes[r];
                    if (recipe == null || recipe.recipeOutcome.resultWeapon == null) continue;

                    var nextWeapon = recipe.recipeOutcome.resultWeapon;
                    if (!_visited.Contains(nextWeapon.Id) && SharesElement(destWeapon, nextWeapon))
                    {
                        _visited.Add(nextWeapon.Id);
                        _parent[nextWeapon.Id] = cur;
                        _queue.Enqueue(nextWeapon.Id);
                    }
                }

                // 2차: 그 외 레시피 큐잉
                for (int r = 0; r < currentWeapon.recipes.Count; r++)
                {
                    var recipe = currentWeapon.recipes[r];
                    if (recipe == null || recipe.recipeOutcome.resultWeapon == null) continue;

                    var nextWeapon = recipe.recipeOutcome.resultWeapon;
                    if (!_visited.Contains(nextWeapon.Id))
                    {
                        _visited.Add(nextWeapon.Id);
                        _parent[nextWeapon.Id] = cur;
                        _queue.Enqueue(nextWeapon.Id);
                    }
                }
            }

            if (!found)
            {
                Debug.Log($"[WeaponTreePathService] 경로를 찾을 수 없습니다: {startId} -> {destId}");
                return null;
            }

            List<uint> path = new(16);
            uint current = destId;
            while (current != startId)
            {
                path.Add(current);
                current = _parent[current];
            }
            path.Add(startId);
            path.Reverse();
            return path;
        }

        private static bool SharesElement(SO_WeaponData a, SO_WeaponData b)
        {
            if (a == null || b == null) return false;
            if (a.elementTypes == null || a.elementTypes.Count == 0) return false;
            if (b.elementTypes == null || b.elementTypes.Count == 0) return false;

            for (int i = 0; i < a.elementTypes.Count; i++)
            {
                for (int j = 0; j < b.elementTypes.Count; j++)
                {
                    if (a.elementTypes[i] == b.elementTypes[j]) return true;
                }
            }
            return false;
        }
    }
}
