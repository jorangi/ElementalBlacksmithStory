using System.Collections;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEngine;


namespace ElementalBlacksmithStory.Core
{
    public static class WeaponTreePathFinder
    {
        public static List<uint> FindPath(uint startId, uint destId, SO_WeaponDatabase db)
        {
            if(startId == destId) return new(){startId};
            
            Queue<uint> queue = new();
            HashSet<uint> visited = new();
            Dictionary<uint, uint> parent = new();

            queue.Enqueue(startId);
            visited.Add(startId);
            
            bool found = false;
            while(queue.Count > 0)
            {
                uint cur = queue.Dequeue();
                if(cur == destId)
                {
                    found = true;
                    break;
                }

                SO_WeaponData currentWeapon = db.GetWeapon(cur);
                if(currentWeapon == null || currentWeapon.recipes == null) continue;
                foreach(var recipe in currentWeapon.recipes)
                {
                    if(recipe == null || recipe.recipeOutcome.resultWeapon == null) continue;

                    uint nextId = recipe.recipeOutcome.resultWeapon.Id;

                    if(!visited.Contains(nextId))
                    {
                        visited.Add(nextId);
                        parent[nextId] = cur;
                        queue.Enqueue(nextId);
                    }
                }
            }
            if(!found)
            {
                Debug.Log($"경로를 찾을 수 없습니다. {startId} -> {destId}");
                return null;
            }

            List<uint> path = new();
            uint current = destId;
            while(current!=startId)
            {
                path.Add(current);
                current = parent[current];
            }
            path.Add(startId);
            path.Reverse();
            return path;
        }
    }
}