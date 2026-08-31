using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Events
{
    public readonly struct NextRecipeChangedEvent
    {
        public readonly SO_CraftRecipe Recipe;

        public NextRecipeChangedEvent(SO_CraftRecipe recipe)
        {
            Recipe = recipe;
        }
    }
}
