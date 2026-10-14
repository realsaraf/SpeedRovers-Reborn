using UnityEngine;

namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        readonly Texture2D[] denseRiders=new Texture2D[4];
        readonly RaceSpriteRegions[] denseRiderRegions=new RaceSpriteRegions[4];
        void InitDenseRiders()
        {
            string[] names={"Red","Blue","Green","Orange"};
            for(int i=0;i<4;i++)
            {
                string path="Painted/RiderMotion"+names[i];
                denseRiders[i]=Resources.Load<Texture2D>(path);
                denseRiderRegions[i]=JsonUtility.FromJson<RaceSpriteRegions>(Resources.Load<TextAsset>(path+"Regions").text);
            }
        }
    }
}
