package ru.vexor.visual.setting;

import java.util.Locale;

public class ColorSetting extends Setting<Integer>
{
    public ColorSetting(String name, int argb)
    {
        super(name, argb);
    }

    public int getRed()
    {
        return (value >> 16) & 0xFF;
    }

    public int getGreen()
    {
        return (value >> 8) & 0xFF;
    }

    public int getBlue()
    {
        return value & 0xFF;
    }

    public int getAlpha()
    {
        return (value >>> 24) & 0xFF;
    }

    public void setComponent(int index, int component)
    {
        int c = Math.max(0, Math.min(255, component));
        int r = getRed();
        int g = getGreen();
        int b = getBlue();
        int a = getAlpha();
        switch (index)
        {
            case 0: r = c; break;
            case 1: g = c; break;
            case 2: b = c; break;
            default: a = c; break;
        }
        value = (a << 24) | (r << 16) | (g << 8) | b;
    }

    @Override
    public String displayValue()
    {
        return String.format(Locale.ROOT, "#%08X", value);
    }

    @Override
    protected Integer parse(String text)
    {
        try
        {
            String s = text.trim();
            if (s.startsWith("#"))
            {
                s = s.substring(1);
                if (s.length() == 6) s = "FF" + s;
                return (int) Long.parseLong(s, 16);
            }
            if (s.matches("-?\\d+"))
            {
                return Integer.parseInt(s);
            }
            if (s.length() == 6) s = "FF" + s;
            return (int) Long.parseLong(s, 16);
        }
        catch (NumberFormatException e)
        {
            return defaultValue;
        }
    }
}
