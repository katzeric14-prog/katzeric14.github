package ru.vexor.visual.setting;

import com.mojang.blaze3d.platform.InputConstants;

public class KeySetting extends Setting<Integer>
{
    public KeySetting(String name, int defaultKey)
    {
        super(name, defaultKey);
    }

    public boolean isBound()
    {
        return value != null && value > 0;
    }

    public void setKey(int key)
    {
        setValue(key);
    }

    @Override
    public String displayValue()
    {
        if (value == null || value <= 0)
        {
            return "Не задана";
        }
        return InputConstants.Type.KEYSYM.getOrCreate(value).getDisplayName().getString();
    }

    @Override
    protected Integer parse(String text)
    {
        try
        {
            return Integer.parseInt(text.trim());
        }
        catch (NumberFormatException e)
        {
            return defaultValue;
        }
    }
}
