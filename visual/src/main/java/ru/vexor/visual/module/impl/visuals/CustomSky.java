package ru.vexor.visual.module.impl.visuals;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.ColorSetting;

public final class CustomSky extends Module
{
    private static CustomSky instance;

    private final ColorSetting skyColor = add(new ColorSetting("Цвет неба", 0xFF5B4BFF));

    public CustomSky()
    {
        super("Custom Sky", "Свой цвет неба", Category.VISUALS);
        setDisplayName("Своё небо");
        instance = this;
    }

    public static int getSkyColor()
    {
        if (instance == null)
        {
            return 0xFF5B4BFF;
        }
        Integer value = instance.skyColor.getValue();
        return value == null ? 0xFF5B4BFF : value;
    }
}
