using System.Collections.Generic;
using Cysharp.Text;
using System.Linq;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public static class RecipeKeyHelper
    {
        public static string GenerateKey(List<RecipeMaterial> materials)
        {
            if(materials == null || materials.Count == 0) return string.Empty;
            var sorted = materials.OrderBy(m => m.materialId);
            using(var sb = ZString.CreateStringBuilder())
            {
                foreach(var m in sorted)
                {
                    sb.Append(m.materialId);
                    sb.Append(':');
                    sb.Append(m.count);
                    sb.Append('|');
                }
                return sb.ToString();
            }
        }
        public static string GenerateKey(Dictionary<uint, uint> currentInputs)
        {
            if(currentInputs == null || currentInputs.Count == 0) return string.Empty;
            var sorted = currentInputs.OrderBy(kv => kv.Key);
            using(var sb = ZString.CreateStringBuilder())
            {
                foreach(var kv in sorted)
                {
                    sb.Append(kv.Key);
                    sb.Append(':');
                    sb.Append(kv.Value);
                    sb.Append('|');
                }
                return sb.ToString();
            }
        }
    }
}