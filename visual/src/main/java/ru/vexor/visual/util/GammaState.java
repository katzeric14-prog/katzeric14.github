package ru.vexor.visual.util;

import java.util.LinkedHashMap;
import java.util.Map;

public final class GammaState
{
    private static final Map<String, Double> owners = new LinkedHashMap<>();

    private GammaState()
    {
    }

    public static void apply(String owner, double value)
    {
        owners.put(owner, value);
    }

    public static void release(String owner)
    {
        owners.remove(owner);
    }

    public static boolean active()
    {
        return !owners.isEmpty();
    }

    public static double value()
    {
        double max = 0.0D;
        for (double value : owners.values())
        {
            max = Math.max(max, value);
        }
        return max;
    }
}
