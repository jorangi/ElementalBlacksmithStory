using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ElementalBlacksmithStory.Test
{
    public class MercenaryTest
    {
        private SO_MercenaryDatabase _database;
        private PlayerData _playerData;
        [SetUp]
        public void SetUp()
        {
            string[] guids = AssetDatabase.FindAssets("t:SO_MercenaryDatabase");
            Assert.IsNotEmpty(guids, "SO_MercenaryDatabase 에셋을 찾을 수 없습니다.");

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _database = AssetDatabase.LoadAssetAtPath<SO_MercenaryDatabase>(path);
            Assert.IsNotNull(_database);

            _playerData = new PlayerData(null, null, null, null, null);

        }
        [Test]
        public void Database_GetAll_ReturnsMercenaries()
        {
            var all = _database.GetAll();
            Assert.IsNotNull(all);
            Assert.Greater(all.Count, 0);
        }
        [Test]
        public void MercenaryData_Init_LoadBaseStats()
        {
            var so = _database.GetAll()[0];
            var mercenary = new MercenaryData(so);
            Assert.AreEqual(so.GetBaseStat(StatType.ATK), mercenary.GetStatValue(StatType.ATK));
        }
        [Test]
        public void PlayerData_AddMercenary_AddsAndGetsById()
        {
            var so = _database.Get(90003);
            var mercenary = new MercenaryData(so);
            bool added = _playerData.AddMercenary(mercenary);
            Assert.IsTrue(added);
            Assert.AreSame(mercenary, _playerData.GetMercenary(so.Id));
        }
        [Test]
        public void PlayerData_GetMercenary_ReturnsMercenary()
        {
            var so = _database.Get(90003);
            var expected = new MercenaryData(so);
            _playerData.AddMercenary(expected);

            var actual = _playerData.GetMercenary(90003);
            Assert.IsNotNull(actual);
            Assert.AreSame(expected, actual);
            Assert.IsNull(_playerData.GetMercenary(999999));
        }
        [Test]
        public void MercenaryData_EquipAndUnequipWeapon_UpdatesStats()
        {
            var so = _database.Get(90003);
            var mercenary = new MercenaryData(so);
            float baseAtk = mercenary.GetStatValue(StatType.ATK);

            var weaponData = ScriptableObject.CreateInstance<SO_WeaponData>();
            weaponData.baseStats.Add(new BaseStat { StatType = StatType.ATK, Value = 25f });
            var weapon = new Weapon(weaponData);

            Assert.IsTrue(mercenary.EquipWeapon(weapon));
            Assert.AreEqual(baseAtk + 25f, mercenary.GetStatValue(StatType.ATK));

            Assert.IsTrue(mercenary.UnequipWeapon());
            Assert.AreEqual(baseAtk, mercenary.GetStatValue(StatType.ATK));

            Object.DestroyImmediate(weaponData);
        }
        [Test]
        public void MercenaryData_EquipWeaponWithModifiers_AppliesFinalWeaponStats()
        {
            var so = _database.Get(90003);
            var mercenary = new MercenaryData(so);
            float baseAtk = mercenary.GetStatValue(StatType.ATK);

            var weaponData = ScriptableObject.CreateInstance<SO_WeaponData>();
            weaponData.baseStats.Add(new BaseStat { StatType = StatType.ATK, Value = 20f });
            var weapon = new Weapon(weaponData);
            var flatHandle = weapon.AddStatModifier(StatType.ATK, new StatModifier(StatType.ATK, StatModifierType.Flat, 10f));
            var addHandle = weapon.AddStatModifier(StatType.ATK, new StatModifier(StatType.ATK, StatModifierType.Add, 0.5f)); // (20 + 10) * 1.5 = 45

            float weaponAtk = weapon.GetStatValue(StatType.ATK);
            Debug.Log($"[모디파이어 적용] 무기 ATK - 예측: 45, 실제: {weaponAtk}");
            Assert.AreEqual(45f, weaponAtk);

            Assert.IsTrue(mercenary.EquipWeapon(weapon));
            float mercEquippedAtk = mercenary.GetStatValue(StatType.ATK);
            Debug.Log($"[무기 장착] 용병 ATK - 예측: {baseAtk + 45f}, 실제: {mercEquippedAtk}");
            Assert.AreEqual(baseAtk + 45f, mercEquippedAtk);

            flatHandle.Dispose();
            float weaponAfterRemove = weapon.GetStatValue(StatType.ATK);
            Debug.Log($"[Flat 모디파이어 제거] 무기 ATK - 예측: 30, 실제: {weaponAfterRemove}");
            Assert.AreEqual(30f, weaponAfterRemove);

            addHandle.Dispose();
            float weaponBaseOnly = weapon.GetStatValue(StatType.ATK);
            Debug.Log($"[Add 모디파이어 제거] 무기 ATK - 예측: 20, 실제: {weaponBaseOnly}");
            Assert.AreEqual(20f, weaponBaseOnly);

            Assert.IsTrue(mercenary.UnequipWeapon());
            float mercUnequippedAtk = mercenary.GetStatValue(StatType.ATK);
            Debug.Log($"[무기 해제] 용병 ATK - 예측: {baseAtk}, 실제: {mercUnequippedAtk}");
            Assert.AreEqual(baseAtk, mercUnequippedAtk);

            Object.DestroyImmediate(weaponData);
        }
    }
}