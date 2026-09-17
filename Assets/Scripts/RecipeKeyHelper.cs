using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class RecipeKeyHelper
    {
        private readonly EquipmentInventory _equipmentInventory;
        [Inject]
        public RecipeKeyHelper(EquipmentInventory equipmentInventory)
        {
            _equipmentInventory = equipmentInventory;
        }
        /// <summary>
        /// 해당 id가 무기인지
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsWeaponId(uint id) => id >= 10000 && id < 20000;
        /// <summary>
        /// 해당 id가 재료인지
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsMaterialId(uint id) => id >= 30000 && id < 40000;

        /// <summary>
        /// 레시피(SO_CraftRecipe)의 고유 키를 생성
        /// 형식: {baseWeaponId}|{재료Id}:{개수}|... (재료Id 오름차순 정렬), 재료에는 무기가 들어갈 수도 있음.
        /// 예: 10004|10001:3|30001:1|30002:2
        /// </summary>
        public string GenerateKey(uint baseWeaponId, List<RecipeMaterial> materials)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append(baseWeaponId);
            if (materials != null && materials.Count > 0)
            {
                //오름차순 정렬
                sb.Append('|');
                var sorted = materials
                    .Where(m => m.material != null && m.count > 0)
                    .OrderBy(m => m.material.Id);

                foreach (var m in sorted)
                {
                    sb.Append(m.material.Id);
                    sb.Append(':');
                    sb.Append(m.count);
                    sb.Append('|');
                }
            }
            return sb.ToString();
        }

        public string GenerateKey(uint baseWeaponId, SO_CraftRecipe recipe)
        {
            if (recipe == null) return baseWeaponId.ToString();
            return GenerateKey(baseWeaponId, recipe.recipeMaterials);
        }

        /// <summary>
        /// 사용자가 선택/투입한 재료 딕셔너리를 기반으로 키 문자열을 생성합니다.
        /// 형식: {baseWeaponId}|{재료Id}:{개수}|... (재료Id 오름차순 정렬)
        /// </summary>
        public string GenerateKey(uint baseWeaponId, IReadOnlyDictionary<uint, uint> currentInputs)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append(baseWeaponId);
            if (currentInputs != null && currentInputs.Count > 0)
            {
                sb.Append('|');
                var sorted = currentInputs
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Key);

                foreach (var kv in sorted)
                {
                    sb.Append(kv.Key);
                    sb.Append(':');
                    sb.Append(kv.Value);
                    sb.Append('|');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 베이스 무기와 사용자가 선택한 재료데이터(BaseMaterialData)를 기반으로 레시피를 후보레시피로부터 탐색
        /// </summary>
        public SO_CraftRecipe FindMatchingRecipe(
            IReadOnlyDictionary<BaseMaterialData, uint> selectedMaterials,
            IEnumerable<SO_CraftRecipe> candidateRecipes)
        {
            if (candidateRecipes == null) return null;

            int selectedMaterialCount = 0;
            if (selectedMaterials != null)
            {
                foreach (var kv in selectedMaterials)
                {
                    if (kv.Value > 0) selectedMaterialCount++;
                }
            }

            foreach (var recipe in candidateRecipes)
            {
                if (recipe == null || recipe.recipeMaterials == null) continue;

                if (IsMatch(recipe, selectedMaterials))
                {
                    return recipe;
                }
            }

            return null;
        }
        /// <summary>
        /// 베이스 무기와 사용자가 선택한 재료(3xxxx), 그리고 무기 인벤토리에 보유 중인 무기(1xxxx)를 검사하여
        /// 후보 레시피 중 완벽히 일치(Exact Match)하는 레시피를 탐색
        /// - 1xxxx 무기 재료: 무기 인벤토리에 요구 수량 이상 보유하고 있으면 충족
        /// - 3xxxx 일반 재료: 사용자가 선택한 재료와 종류 및 수량이 정확히 일치해야 충족
        /// </summary>
        public SO_CraftRecipe FindMatchingRecipe(
            IReadOnlyDictionary<uint, uint> selectedMaterials,
            IEnumerable<SO_CraftRecipe> candidateRecipes)
        {
            if (candidateRecipes == null) return null;

            int selectedMaterialCount = 0;
            if (selectedMaterials != null)
            {
                foreach (var kv in selectedMaterials)
                {
                    if (kv.Value > 0) selectedMaterialCount++;
                }
            }

            foreach (var recipe in candidateRecipes)
            {
                if (recipe == null || recipe.recipeMaterials == null) continue;

                if (IsMatch(recipe, selectedMaterials))
                {
                    return recipe;
                }
            }

            return null;
        }
        /// <summary>
        /// 선택된 재료들 레시피 매칭 여부 판단
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="selectedMaterials">재료 데이터와 개수</param>
        /// <param name="selectedMaterialCount">선택된 재료 개수</param>
        /// <returns></returns>
        private bool IsMatch(SO_CraftRecipe recipe, IReadOnlyDictionary<BaseMaterialData, uint> selectedMaterials)
        {
            if (recipe == null || recipe.recipeMaterials == null) return false;

            int requiredMaterialCount = 0;

            foreach (var req in recipe.recipeMaterials)
            {
                if (req.material == null || req.count == 0) continue;
                uint id = req.material.Id;

                // 무기 재료(베이스 무기 아님)
                if (IsWeaponId(id))
                {
                    int ownedCount = _equipmentInventory.GetCountByWeaponId(id);
                    if (ownedCount < req.count)
                    {
                        return false;
                    }
                }
                // 일반 재료
                else
                {
                    requiredMaterialCount++;
                    if (selectedMaterials == null ||
                        !selectedMaterials.TryGetValue(req.material, out uint count) ||
                        count != req.count)
                    {
                        return false;
                    }
                }
            }

            int selectedMaterialCount = 0;
            if (selectedMaterials != null)
            {
                foreach (var kv in selectedMaterials)
                {
                    if (kv.Value > 0) selectedMaterialCount++;
                }
            }

            // 플레이어가 레시피에 필요하지 않은 추가 일반 재료를 선택한 경우 불일치
            return selectedMaterialCount == requiredMaterialCount;
        }
        /// <summary>
        /// 선택된 재료들 레시피 매칭 여부 판단
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="selectedMaterials"></param>
        /// <param name="selectedMaterialCount"></param>
        /// <returns></returns>
        private bool IsMatch(SO_CraftRecipe recipe, IReadOnlyDictionary<uint, uint> selectedMaterials)
        {
            if (recipe == null || recipe.recipeMaterials == null) return false;

            int requiredMaterialCount = 0;

            foreach (var req in recipe.recipeMaterials)
            {
                if (req.material == null || req.count == 0) continue;
                uint id = req.material.Id;

                // 무기 재료(베이스 무기 아님)
                if (IsWeaponId(id))
                {
                    int ownedCount = _equipmentInventory.GetCountByWeaponId(id);
                    if (ownedCount < req.count)
                    {
                        return false;
                    }
                }
                // 일반 재료
                else
                {
                    requiredMaterialCount++;
                    if (selectedMaterials == null ||
                        !selectedMaterials.TryGetValue(id, out uint count) ||
                        count != req.count)
                    {
                        return false;
                    }
                }
            }

            int selectedMaterialCount = 0;
            if (selectedMaterials != null)
            {
                foreach (var kv in selectedMaterials)
                {
                    if (kv.Value > 0) selectedMaterialCount++;
                }
            }

            // 플레이어가 레시피에 필요하지 않은 추가 일반 재료를 선택한 경우 불일치
            return selectedMaterialCount == requiredMaterialCount;
        }
    }
}