package ru.vexor.visual.setting;

public class BoolSetting extends Setting<Boolean>
{
    public BoolSetting(String name, boolean defaultValue)
    {
        super(name, defaultValue);
    }

    public void toggle()
    {
        value = !value;
    }

    @Override
    public String displayValue()
    {
        return value ? "Вкл" : "Выкл";
    }

    @Override
    protected Boolean parse(String text)
    {
        return Boolean.parseBoolean(text);
    }
}
