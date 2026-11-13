using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class MercenaryInfoPresenter : IDisposable
    {
        private readonly MercenaryInfoView _view;
        private readonly CompositeDisposable _disposables = new();
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly NPCStandingSpriteLoader _npcSpriteLoader;
        [Inject]
        public MercenaryInfoPresenter(
            MercenaryInfoView view,
            WeaponSpriteLoader weaponSpriteLoader,
            NPCStandingSpriteLoader npcSpriteLoader
            )
        {
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _npcSpriteLoader = npcSpriteLoader;


            _view.OnWeaponButtonClick()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnArmorButtonClick()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnAccessory1ButtonClick()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnAccessory2ButtonClick()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnConsumableButtonClick()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
        }
        public void SetData(MercenaryData data)
        {
            _view.SetNameText(data.Name);
            _view.SetLevelText(data.Level.ToString());
            float exp = data.MaxExp == 0 ? 1f : Mathf.Clamp01((float)data.Exp / data.MaxExp);
            _view.SetExpBar(exp);
            _view.SetHpText(data.Stats[(int)StatType.HP].ToString());
            _view.SetDefText(data.Stats[(int)StatType.DEF].ToString());
            _view.SetAtkText(data.Stats[(int)StatType.ATK].ToString());
            _view.SetMagText(data.Stats[(int)StatType.MAG].ToString());
            _view.SetAtsText(data.Stats[(int)StatType.ATS].ToString());
            _view.SetSpdText(data.Stats[(int)StatType.SPD].ToString());
            _view.SetGoldBonusText(data.Stats[(int)StatType.GOLDBONUS].ToString());

            SetEquipmentIcon(data.WeaponId).Forget();
            SetStandingImage(data.Id).Forget();
        }
        public void Show()
        {
            _view.Show().Forget();
        }
        public void Hide()
        {
            _view.Hide().Forget();
        }
        public async UniTask SetStandingImage(uint id, CancellationToken ct = default)
        {
            try
            {
                var sprite = await _npcSpriteLoader.GetSprite(id);
                ct.ThrowIfCancellationRequested();
                _view.SetStandingImage(sprite);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[MercenaryInfoPresenter] {e.Message}");
            }
        }

        public async UniTask SetEquipmentIcon(uint id, CancellationToken ct = default)
        {
            try
            {
                var sprite = await _weaponSpriteLoader.GetSprite(id);
                ct.ThrowIfCancellationRequested();
                _view.SetWeaponIcon(sprite);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[MercenaryInfoPresenter] {e.Message}");
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
        }
    }
}