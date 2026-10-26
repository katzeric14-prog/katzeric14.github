package ru.vexor.visual.module.impl.visuals;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.IntSetting;

public final class TimeChanger extends Module
{
    private static TimeChanger instance;

    private final IntSetting time = add(new IntSetting("Время", 6000, 0, 23999, 500));

    public TimeChanger()
    {
        super("Time Changer", "Своё время суток на клиенте", Category.VISUALS);
        setDisplayName("Время");
        instance = this;
    }

    public static long getTime()
    {
        if (instance == null || !instance.isEnabled())
        {
            return 0L;
        }
        return instance.time.getValue();
    }
}
