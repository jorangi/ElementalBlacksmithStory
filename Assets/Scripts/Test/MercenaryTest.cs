using ElementalBlacksmithStory.Data;
using UnityEngine;

namespace ElementalBlacksmithStory.Test
{
    public class MercenaryTest : ITest
    {
        private readonly PlayerData _playerData;
        private readonly SO_MercenaryDatabase _mercenaryDatabase;
        public MercenaryTest(
            PlayerData playerData,
            SO_MercenaryDatabase mercenaryDatabase
        )
        {
            _playerData = playerData;
            _mercenaryDatabase = mercenaryDatabase;
        }
        public void ExecuteTest()
        {
        }
        public bool ReadAllMercenary()
        {
            Debug.Log("[MercenaryTest]용병 데이터베이스에 있는 모든 데이터를 읽어오기");
            var allData = _mercenaryDatabase.GetAll();
            if (allData == null)
            {
                Debug.Log("[MercenaryTest]용병 데이터베이스에서 데이터를 읽어오지 못했습니다.");
                return false;
            }
            Debug.Log("[MercenaryTest]용병 데이터베이스에 있는 모든 데이터를 읽어오기");
            return true;

        }
        public bool AddMercenaryTest()
        {
            var mercenary = _mercenaryDatabase.GetAll();
            return true;
        }
    }
}