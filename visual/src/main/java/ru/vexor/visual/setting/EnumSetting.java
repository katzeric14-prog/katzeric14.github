package ru.vexor.visual.setting;

import com.google.gson.JsonElement;
import com.google.gson.JsonPrimitive;

public class EnumSetting<E extends Enum<E>> extends Setting<E>
{
    private final E[] values;

    public EnumSetting(String name, E defaultValue)
    {
        super(name, defaultValue);
        this.values = defaultValue.getDeclaringClass().getEnumConstants();
    }

    public E[] getValues()
    {
        return values;
    }

    public void cycle(int direction)
    {
        int index = value.ordinal() + direction;
        if (index < 0) index = values.length - 1;
        if (index >= values.length) index = 0;
        value = values[index];
    }

    @Override
    public String displayValue()
    {
        return value.toString();
    }

    @Override
    public JsonElement toJson()
    {
        return new JsonPrimitive(value.name());
    }

    @Override
    protected E parse(String text)
    {
        String needle = text == null ? "" : text.trim();
        for (E candidate : values)
        {
            if (candidate.name().equalsIgnoreCase(needle) || candidate.toString().equalsIgnoreCase(needle))
            {
                return candidate;
            }
        }
        return defaultValue;
    }
}
