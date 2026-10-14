using UnityEngine;
using System.Runtime.InteropServices;
namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        float nextSoftImpact;
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void SRSoftImpact();
#endif
        void SoftImpactFeedback()
        {
            // Merge simultaneous contacts into one gentle tap, independent of sound mute.
            if(verificationRun || Time.unscaledTime < nextSoftImpact) return;
            nextSoftImpact = Time.unscaledTime + .08f;
#if UNITY_IOS && !UNITY_EDITOR
            SRSoftImpact();
#endif
        }
    }
}
