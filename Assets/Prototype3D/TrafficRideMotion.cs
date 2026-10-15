using System.Runtime.InteropServices;
using UnityEngine;

public partial class TrafficRidePrototype
{
    const string SteeringPreference = "sr2_steering_v2";
    const float TiltDeadZoneDegrees = 2.1f;
    const float TiltFullLockDegrees = 20f;
    Vector3 filteredGravity, neutralGravityTotal;
    float neutralRoll, tiltCalibrationTime, tiltOutput, nextCollisionFeedback;
    int tiltCalibrationSamples;
    bool gravityReady, tiltReady;
    ScreenOrientation tiltOrientation;

    bool TiltAvailable => Application.isMobilePlatform && SystemInfo.supportsAccelerometer;
    bool TiltIsCalibrating => tiltControl && TiltAvailable && !tiltReady;
    float TiltSteering => tiltReady ? tiltOutput : 0;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal", EntryPoint = "SRSoftImpact")] static extern void SRNativeSoftImpact();
    [DllImport("__Internal")] static extern void SRPrepareFeedback();
    [DllImport("__Internal")] static extern void SRSuspendFeedback();
    [DllImport("__Internal")] static extern void SRCollisionImpact(float severity);
#endif

    void InitializeMotionFeedback()
    {
        // Unity remaps acceleration into screen axes in either landscape orientation.
        // Do not rotate it again: that would turn roll into pitch on one side of the phone.
        Input.compensateSensors = true;
        tiltControl = PlayerPrefs.GetInt(SteeringPreference, TiltAvailable ? 1 : 0) == 1 && TiltAvailable;
        // The old sr2_tilt=0 was the shipping default, indistinguishable from an explicit choice.
        // Migrate once to tilt on phones; preserve subsequent manual selection in the new key.
        if (!qaMode && !PlayerPrefs.HasKey(SteeringPreference))
        {
            PlayerPrefs.SetInt(SteeringPreference, tiltControl ? 1 : 0);
            PlayerPrefs.Save();
        }
        RecenterTilt();
        PrepareMotionFeedback();
    }

    void SetTiltControl(bool value)
    {
        tiltControl = value && TiltAvailable;
        if (!qaMode)
        {
            PlayerPrefs.SetInt(SteeringPreference, tiltControl ? 1 : 0);
            PlayerPrefs.Save();
        }
        RecenterTilt();
    }

    void RecenterTilt()
    {
        tiltReady = gravityReady = false;
        tiltOutput = tiltCalibrationTime = 0;
        tiltCalibrationSamples = 0;
        neutralGravityTotal = Vector3.zero;
        tiltOrientation = Screen.orientation;
    }

    void PrepareMotionFeedback()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (hapticsOn && !qaMode) SRPrepareFeedback();
#endif
    }

    void SuspendMotionFeedback()
    {
        touchStarts.Clear();
        throttle = brake = false;
        steerInput = 0;
        RecenterTilt();
#if UNITY_IOS && !UNITY_EDITOR
        if (!qaMode) SRSuspendFeedback();
#endif
    }

    // Sample during countdown/pause too, so the phone settles before GO.
    // Holding throttle never overwrites the sensor output.
    void TickMotionControls()
    {
        if (!tiltControl || !TiltAvailable || qaMode) return;
        if (tiltOrientation != Screen.orientation) RecenterTilt();
        SampleTiltGravity(Input.acceleration, Mathf.Min(Time.unscaledDeltaTime, .05f));
    }

    void SampleTiltGravity(Vector3 gravity, float dt)
    {
        // Ignore startup zeros and abrupt shakes; retain the last stable gravity.
        float magnitude = gravity.magnitude;
        if (magnitude < .45f || magnitude > 1.6f || dt <= 0) return;
        float response = 1 - Mathf.Exp(-dt * 13f);
        filteredGravity = gravityReady ? Vector3.Lerp(filteredGravity, gravity, response) : gravity;
        gravityReady = true;
        if (!tiltReady)
        {
            neutralGravityTotal += filteredGravity.normalized;
            tiltCalibrationSamples++;
            tiltCalibrationTime += dt;
            if (tiltCalibrationTime >= .35f && tiltCalibrationSamples >= 4)
            {
                neutralRoll = ScreenGravityRoll(neutralGravityTotal / tiltCalibrationSamples);
                tiltReady = true;
            }
            return;
        }
        float target = MapTiltSteering(ScreenGravityRoll(filteredGravity), neutralRoll);
        tiltOutput = Mathf.Lerp(tiltOutput, target, 1 - Mathf.Exp(-dt * 18f));
    }

    // Pure mapping functions are exercised by desktop QA with injected sensor values.
    static float ScreenGravityRoll(Vector3 gravity)
    {
        return Mathf.Atan2(gravity.x, Mathf.Sqrt(gravity.y * gravity.y + gravity.z * gravity.z)) * Mathf.Rad2Deg;
    }

    static float MapTiltSteering(float rollDegrees, float neutralDegrees)
    {
        float angle = Mathf.DeltaAngle(neutralDegrees, rollDegrees);
        float amount = Mathf.Clamp01((Mathf.Abs(angle) - TiltDeadZoneDegrees) / (TiltFullLockDegrees - TiltDeadZoneDegrees));
        return Mathf.Sign(angle) * amount;
    }

    void TriggerSoftHaptic()
    {
        if (!hapticsOn || qaMode || Time.unscaledTime < nextFeedback) return;
        nextFeedback = Time.unscaledTime + .1f;
#if UNITY_IOS && !UNITY_EDITOR
        SRNativeSoftImpact();
#elif UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    void TriggerCollisionHaptic(float severity)
    {
        // A reward immediately before contact must never suppress the collision pulse.
        if (!hapticsOn || qaMode || Time.unscaledTime < nextCollisionFeedback) return;
        nextCollisionFeedback = Time.unscaledTime + .18f;
#if UNITY_IOS && !UNITY_EDITOR
        SRCollisionImpact(Mathf.Clamp01(severity));
#elif UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    void TestHaptics()
    {
        if (!hapticsOn) { Announce("TURN HAPTICS ON TO TEST"); return; }
        nextCollisionFeedback = 0;
        PrepareMotionFeedback();
        TriggerCollisionHaptic(.7f);
        Announce(Application.isMobilePlatform ? "HAPTICS TEST / SHORT IMPACT PULSE" : "HAPTICS TEST IS AVAILABLE ON YOUR PHONE");
    }
}
