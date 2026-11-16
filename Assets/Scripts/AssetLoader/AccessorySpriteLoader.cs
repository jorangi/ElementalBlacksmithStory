using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class AccessorySpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "AccessoryAtlas";

        [Inject]
        public AccessorySpriteLoader(SO_SpriteSettings settings = null)
            : base(settings != null ? settings.DefaultAccessorySprite : null)
        {
        }
    }
}