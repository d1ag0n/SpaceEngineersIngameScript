using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    class SensorModule : Module<IMySensorBlock> {
        readonly List<MyDetectedEntityInfo> mDetected = new List<MyDetectedEntityInfo>();
        public bool Player(out MyDetectedEntityInfo aPlayer) => detect(MyDetectedEntityType.CharacterHuman, out aPlayer);
        bool detect(MyDetectedEntityType aType, out MyDetectedEntityInfo aEntity) {
            bool result = false;
            int index = 0;
            aEntity = default(MyDetectedEntityInfo);
            foreach (var b in Blocks) {
                b.DetectedEntities(mDetected);
                if (_detect(aType, ref index, ref aEntity)) {
                    result = true;
                    break;
                }
            }
            mDetected.Clear();
            return result;
        }
        bool _detect(MyDetectedEntityType aType, ref int aIndex, ref MyDetectedEntityInfo aEntity) {
            while (aIndex < mDetected.Count) {
                aEntity = mDetected[aIndex];
                aIndex++;
                if (aEntity.Type == aType) {
                    return true;
                }
                
            }
            return false;
        }
    }
}
