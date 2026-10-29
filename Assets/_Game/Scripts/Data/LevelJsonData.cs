using System;
using UnityEngine;

namespace SmashPuzzle.Data
{
    [Serializable]
    public class LevelJsonData
    {
        public int    levelId;
        public int    chapterId;
        public int    projectileCount;
        public int    star2Threshold;
        public int    star3Threshold;
        public int    coinRewardBase;
        public ObjectPlacementData[] objects;
    }

    [Serializable]
    public class ObjectPlacementData
    {
        public string type;       // "Jar" / "Stone" / "Pillar" / "Wood" / "TNT"
        public float  x;
        public float  y;
        public float  z;          // Z 오프셋 (TABLE_Z 기준), 기본값 0
        public float  rotation;
        public bool   isTarget;
        public int    groupId;    // -1 = 그룹 없음
    }
}
