package ru.vexor.visual.setting;

import com.google.gson.JsonElement;
import com.google.gson.JsonPrimitive;

import java.util.function.BooleanSupplier;

public abstract class Setting<T>
{
    private final String name;
    protected final T defaultValue;
    protected T value;
    private BooleanSupplier visible = () -> true;

    protected Setting(String name, T defaultValue)
    {
        this.name = name;
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }

    public String getName()
    {
        return name;
    }

    public T getValue()
    {
        return value;
    }

    public void setValue(T newValue)
    {
        this.value = newValue;
    }

    public boolean isVisible()
    {
        return visible.getAsBoolean();
    }

    public Setting<T> visibleWhen(BooleanSupplier condition)
    {
        this.visible = condition;
        return this;
    }

    public abstract String displayValue();

    public void reset()
    {
        this.value = defaultValue;
    }

    public JsonElement toJson()
    {
        return new JsonPrimitive(String.valueOf(value));
    }

    public void fromJson(JsonElement element)
    {
        if (element != null && element.isJsonPrimitive())
        {
            this.value = parse(element.getAsString());
        }
    }

    protected abstract T parse(String text);
}
