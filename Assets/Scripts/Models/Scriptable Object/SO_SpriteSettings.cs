using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_SpriteSettings", menuName = "Scriptable Objects/SO_SpriteSettings")]
    public class SO_SpriteSettings : ScriptableObject
    {
        [Header("Default Fallback Sprites")]
        [SerializeField] private Sprite defaultMaterialSprite;
        [SerializeField] private Sprite defaultWeaponSprite;
        [SerializeField] private Sprite defaultNPCStandingSprite;
        [SerializeField] private Sprite defaultRuneSprite;

        public Sprite DefaultMaterialSprite => defaultMaterialSprite;
        public Sprite DefaultWeaponSprite => defaultWeaponSprite;
        public Sprite DefaultNPCStandingSprite => defaultNPCStandingSprite;
        public Sprite DefaultRuneSprite => defaultRuneSprite;
    }
}
