using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    static class UtilityClass
    {
        static public float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        static public String GetTimestamp(this DateTime value)
        {
            return value.ToString("yyyy.MM.dd.HH.mm.ss.fff");
        }
    }

    class Data
    {
        public Vector2 endPoint { get; set; }
        public int tot { get; set; }
        public float force { get; set; }
        public bool hasHit { get; set; }
    }

    struct AdaptiveData
    {
        public string startTime;
        public string endTime;
        public Vector2 startPoint;
        public Vector2 endPoint;
        public double distanceTravelled;
        public float force;
        public float forceAccumulated;
        public bool hasHit;
        public List<string> ducksHit;
    }
}
