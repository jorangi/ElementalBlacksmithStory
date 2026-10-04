using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class RuneSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "RuneAtlas";

        [Inject]
        public RuneSpriteLoader(SO_SpriteSettings settings = null)
            : base(settings != null ? settings.DefaultRuneSprite : null)
        {
        }
    }
}