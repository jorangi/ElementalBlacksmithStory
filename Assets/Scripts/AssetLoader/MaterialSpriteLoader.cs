using UnityEngine;
using VContainer;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class MaterialSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "MaterialAtlas";

        [Inject]
        public MaterialSpriteLoader(SO_SpriteSettings settings = null) 
            : base(settings != null ? settings.DefaultMaterialSprite : null)
        {
        }
    }
}