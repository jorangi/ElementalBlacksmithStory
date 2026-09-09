using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.UI;

namespace ElementalBlacksmithStory.Core
{
    public class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // view 등록
            builder.RegisterComponentInHierarchy<MoneyView>();
            builder.RegisterComponentInHierarchy<QuickSellView>();
            builder.RegisterComponentInHierarchy<AnvilWeaponView>();
            builder.RegisterComponentInHierarchy<EnhanceButtonView>();
            builder.RegisterComponentInHierarchy<EnhanceChanceView>();
            builder.RegisterComponentInHierarchy<MaterialsView>();
            builder.RegisterComponentInHierarchy<SelectMaterialAmountView>();
            builder.RegisterComponentInHierarchy<GameExitView>();
            builder.RegisterComponentInHierarchy<SettingsView>();
            builder.RegisterComponentInHierarchy<NPCDialogueView>();

            // Presenter 등록
            builder.RegisterEntryPoint<MoneyPresenter>();
            builder.RegisterEntryPoint<QuickSellPresenter>();
            builder.RegisterEntryPoint<AnvilWeaponPresenter>();
            builder.RegisterEntryPoint<EnhanceButtonPresenter>();
            builder.RegisterEntryPoint<EnhanceChancePresenter>();
            builder.RegisterEntryPoint<MaterialsPresenter>();
            builder.RegisterEntryPoint<WeaponTreePresenter>().AsSelf();
            builder.RegisterEntryPoint<GameExitPresenter>().AsSelf();
            builder.RegisterEntryPoint<SettingsPresenter>().AsSelf();
            builder.RegisterEntryPoint<NPCDialoguePresenter>().AsSelf();
        }
    }
}