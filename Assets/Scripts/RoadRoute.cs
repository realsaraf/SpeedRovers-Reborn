using UnityEngine;

namespace SpeedRovers.Reborn
{
    // Metres along a continuous route. Every renderer and hazard spawner uses
    // these same district and construction boundaries.
    public static class RoadRoute
    {
        public const float Length = 5000;
        public static readonly float[] Starts={0,700,1250,2100,2800,3500,4250};
        static readonly string[] Names={"DOWNTOWN","RIVER BRIDGE","OUTSKIRTS","CITY DAY","ITALY · COAST","JAPAN · TOKYO","SWITZERLAND · ALPS"};
        public static int District(float metres)
        {
            float m = Mathf.Repeat(metres, Length);
            return m < 700 ? 0 : m < 1250 ? 1 : m < 2100 ? 2 : m < 2800 ? 3 : m < 3500 ? 4 : m < 4250 ? 5 : 6;
        }
        public static string Name(float metres) => Names[District(metres)];
        public static bool Urban(float metres) {int d=District(metres);return d==0 || d==3 || d==5;}
        public static float JunctionOffset(float metres)
        {
            if (!Urban(metres)) return 100;
            return Mathf.Repeat(metres + 70, 180) - 90;
        }
        public static bool Junction(float metres) => Mathf.Abs(JunctionOffset(metres)) < 12;
        public static float WorkStart(int index) => 420 + index * 850;
        public static int WorkLane(int index) => index % 2 == 0 ? 1 : -1;
        public static int WorkIndex(float metres) => Mathf.FloorToInt((metres - 420) / 850);
        public static bool Work(float metres)
        {
            int i = WorkIndex(metres);
            return i >= 0 && metres - WorkStart(i) < 42;
        }
    }
}
