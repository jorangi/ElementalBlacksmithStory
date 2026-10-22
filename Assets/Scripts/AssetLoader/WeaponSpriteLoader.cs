using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class WeaponSpriteLoader : BaseSpriteLoader
    {
        protected override string[] AtlasAddresses => new[] { "SwordsAtlas", "BowsAtlas" };

        [Inject]
        public WeaponSpriteLoader(SO_SpriteSettings settings = null)
            : base(settings != null ? settings.DefaultWeaponSprite : null)
        {
        }
    }
}