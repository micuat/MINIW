using System;

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
