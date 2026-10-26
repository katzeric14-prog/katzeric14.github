package ru.vexor.visual.setting;

public class IntSetting extends Setting<Integer>
{
    private final int min;
    private final int max;
    private final int step;

    public IntSetting(String name, int defaultValue, int min, int max, int step)
    {
        super(name, clamp(defaultValue, min, max));
        this.min = min;
        this.max = max;
        this.step = Math.max(1, step);
    }

    public int getMin()
    {
        return min;
    }

    public int getMax()
    {
        return max;
    }

    public int getStep()
    {
        return step;
    }

    @Override
    public void setValue(Integer newValue)
    {
        int stepped = Math.round((newValue - min) / (float) step) * step + min;
        this.value = clamp(stepped, min, max);
    }

    @Override
    public String displayValue()
    {
        return String.valueOf(value);
    }

    @Override
    protected Integer parse(String text)
    {
        try
        {
            return clamp(Integer.parseInt(text.trim()), min, max);
        }
        catch (NumberFormatException e)
        {
            return defaultValue;
        }
    }

    private static int clamp(int v, int lo, int hi)
    {
        return v < lo ? lo : (v > hi ? hi : v);
    }
}
