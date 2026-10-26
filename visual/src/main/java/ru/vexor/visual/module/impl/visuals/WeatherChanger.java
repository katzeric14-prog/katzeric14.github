package ru.vexor.visual.module.impl.visuals;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;

public final class WeatherChanger extends Module
{
    public WeatherChanger()
    {
        super("Weather", "Убрать дождь и грозу", Category.VISUALS);
        setDisplayName("Погода");
    }
}
