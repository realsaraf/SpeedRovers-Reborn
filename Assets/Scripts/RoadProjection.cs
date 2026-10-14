using UnityEngine;

namespace SpeedRovers.Reborn
{
    // Shared by the asphalt, scenery, pickups and checkpoint gate. The camera
    // follows the rider, as in MainGameScreen's original PlayerX projection.
    public static class RoadProjection
    {
        public const float Horizon = 410f;
        public const float Height = 475f;
        public const float ViewDistance = 470f;
        public const float LaneSpacing = .63f;
        public const float TravelScale = 1.65f;

        public static float Depth(float relativeDistance)
        {
            const float focal = 27f;
            float farScale = focal / (focal + ViewDistance);
            return (focal / (focal + Mathf.Clamp(relativeDistance, -8, ViewDistance)) - farScale) / (1 - farScale);
        }

        public static float DistanceAtDepth(float depth)
        {
            const float focal = 27f;
            float farScale = focal / (focal + ViewDistance);
            return focal / Mathf.LerpUnclamped(farScale, 1, Mathf.Max(0, depth)) - focal;
        }

        public static float HalfWidth(float depth)
        {
            return Mathf.LerpUnclamped(7f, 410f, Mathf.Max(0, depth));
        }

        public static float Center(float runDistance, float relativeDistance, float playerLane)
        {
            float depth = Depth(relativeDistance);
            float bend = (Mathf.Sin((runDistance + relativeDistance * .55f) / 240f) * 60f
                       + Mathf.Sin((runDistance + relativeDistance) / 340f) * 35f) * (1 - depth) * (1 - depth);
            return 270f + bend - playerLane * HalfWidth(depth) * LaneSpacing;
        }

        public static Vector2 Project(float runDistance, float relativeDistance, float objectLane, float playerLane)
        {
            float depth = Depth(relativeDistance);
            return new Vector2(Center(runDistance, relativeDistance, playerLane)
                + objectLane * HalfWidth(depth) * LaneSpacing, ScreenY(runDistance, depth));
        }

        // A gentle change in road grade, shared by surface strips and sprites.
        // This stays monotonic; fully occluding crests need a segment renderer.
        public static float ScreenY(float runDistance, float depth)
        {
            return Horizon + Height * depth + Mathf.Sin(runDistance / 170f) * 32f * (1 - depth);
        }
    }
}
