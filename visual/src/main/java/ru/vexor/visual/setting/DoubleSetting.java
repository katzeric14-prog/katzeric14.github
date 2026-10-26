package ru.vexor.visual.setting;

import java.util.Locale;

public class DoubleSetting extends Setting<Double>
{
    private final double min;
    private final double max;
    private final double step;
    private final int decimals;

    public DoubleSetting(String name, double defaultValue, double min, double max, double step, int decimals)
    {
        super(name, clamp(defaultValue, min, max));
        this.min = min;
        this.max = max;
        this.step = step <= 0 ? 0.1 : step;
        this.decimals = Math.max(0, decimals);
    }

    public double getMin()
    {
        return min;
    }

    public double getMax()
    {
        return max;
    }

    public double getStep()
    {
        return step;
    }

    @Override
    public void setValue(Double newValue)
    {
        double snapped = Math.round(newValue / step) * step;
        this.value = clamp(snapped, min, max);
    }

    @Override
    public String displayValue()
    {
        return String.format(Locale.ROOT, "%." + decimals + "f", value);
    }

    @Override
    protected Double parse(String text)
    {
        try
        {
            return clamp(Double.parseDouble(text.trim().replace(',', '.')), min, max);
        }
        catch (NumberFormatException e)
        {
            return defaultValue;
        }
    }

    private static double clamp(double v, double lo, double hi)
    {
        return v < lo ? lo : (v > hi ? hi : v);
    }
}
