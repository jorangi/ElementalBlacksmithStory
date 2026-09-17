using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class RecipeUnlockService
    {
        private readonly HashSet<uint> recipeUnlockData = new();
        private readonly Subject<uint> _onRecipeUnlockedSubject = new();
        public Observable<uint> OnRecipeUnlockedAsObservable => _onRecipeUnlockedSubject;

        [Inject]
        public RecipeUnlockService()
        {
        }
        public bool IsUnlocked(uint recipeId) => recipeUnlockData.Contains(recipeId);
        /// <summary>
        /// 레시피 해금
        /// </summary>
        /// <param name="recipeId"></param>
        public void Unlock(uint recipeId)
        {
            if (recipeUnlockData.Contains(recipeId))
            {
                Debug.Log($"[RecipeUnlockService] 이미 해금된 레시피 id={recipeId}입니다.");
                return;
            }
            recipeUnlockData.Add(recipeId);
            Debug.Log($"[RecipeUnlockService] 레시피 id={recipeId}를 해금했습니다.");
            _onRecipeUnlockedSubject.OnNext(recipeId);
            return;
        }
        //추후 해금 알림 같은 건 이벤트로 발송
    }
}