using UnityEngine;
using VContainer;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class WeaponSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "WeaponAtlas";

        [Inject]
        public WeaponSpriteLoader(SO_SpriteSettings settings = null) 
            : base(settings != null ? settings.DefaultWeaponSprite : null)
        {
        }
    }
}