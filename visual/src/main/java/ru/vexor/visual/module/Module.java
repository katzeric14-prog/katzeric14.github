package ru.vexor.visual.module;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import ru.vexor.visual.VexorVisual;
import ru.vexor.visual.config.ConfigManager;
import ru.vexor.visual.setting.KeySetting;
import ru.vexor.visual.setting.Setting;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public abstract class Module
{
    private final String name;
    private final String description;
    private final Category category;
    private final KeySetting keyBind;
    private final List<Setting<?>> settings = new ArrayList<>();
    private String displayName;
    private boolean enabled;

    protected Module(String name, String description, Category category)
    {
        this(name, description, category, -1);
    }

    protected Module(String name, String description, Category category, int defaultKey)
    {
        this.name = name;
        this.description = description;
        this.category = category;
        this.keyBind = new KeySetting("Клавиша", defaultKey);
    }

    public String getName()
    {
        return name;
    }

    public String getDescription()
    {
        return description;
    }

    public String getDisplayName()
    {
        return displayName != null ? displayName : name;
    }

    protected void setDisplayName(String displayName)
    {
        this.displayName = displayName;
    }

    public Category getCategory()
    {
        return category;
    }

    public KeySetting getKeyBind()
    {
        return keyBind;
    }

    public boolean isEnabled()
    {
        return enabled;
    }

    public List<Setting<?>> getSettings()
    {
        return Collections.unmodifiableList(settings);
    }

    public boolean hasSettings()
    {
        return !settings.isEmpty();
    }

    public void toggle()
    {
        setEnabled(!enabled);
    }

    public void setEnabled(boolean value)
    {
        if (enabled == value)
        {
            return;
        }
        enabled = value;
        try
        {
            if (value)
            {
                onEnable();
            }
            else
            {
                onDisable();
            }
        }
        catch (Throwable t)
        {
            VexorVisual.LOGGER.error("Ошибка переключения модуля " + name, t);
        }
        ConfigManager.markDirty();
    }

    public void onTick()
    {
    }

    public void onEnable()
    {
    }

    public void onDisable()
    {
    }

    protected <S extends Setting<?>> S add(S setting)
    {
        settings.add(setting);
        return setting;
    }

    public void resetSettings()
    {
        for (Setting<?> setting : settings)
        {
            setting.reset();
        }
        ConfigManager.markDirty();
    }

    public JsonObject toJson()
    {
        JsonObject object = new JsonObject();
        object.addProperty("enabled", enabled);
        object.addProperty("key", keyBind.getValue());
        JsonObject values = new JsonObject();
        for (Setting<?> setting : settings)
        {
            values.add(setting.getName(), setting.toJson());
        }
        object.add("settings", values);
        return object;
    }

    public void fromJson(JsonObject object)
    {
        if (object.has("key"))
        {
            keyBind.setKey(object.get("key").getAsInt());
        }
        if (object.has("settings") && object.get("settings").isJsonObject())
        {
            JsonObject values = object.getAsJsonObject("settings");
            for (Setting<?> setting : settings)
            {
                JsonElement element = values.get(setting.getName());
                if (element != null)
                {
                    try
                    {
                        setting.fromJson(element);
                    }
                    catch (RuntimeException e)
                    {
                        VexorVisual.LOGGER.warn("Не удалось прочитать настройку " + setting.getName(), e);
                    }
                }
            }
        }
        if (object.has("enabled") && object.get("enabled").getAsBoolean() && !enabled)
        {
            setEnabled(true);
        }
    }
}
