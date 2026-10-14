using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.UI
{
    public interface IRuneUIState
    {
        /// <summary>
        /// 룬 교체
        /// </summary>
        /// <param name="runeId">룬 id</param>
        /// <returns></returns>
        public UniTask ChangeRune(uint runeId);
    }
}