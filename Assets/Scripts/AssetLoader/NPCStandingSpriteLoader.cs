using VContainer;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class NPCStandingSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "NPCStandingAtlas";

        [Inject]
        public NPCStandingSpriteLoader(SO_SpriteSettings settings = null) 
            : base(settings != null ? settings.DefaultNPCStandingSprite : null)
        {
        }
    }
}