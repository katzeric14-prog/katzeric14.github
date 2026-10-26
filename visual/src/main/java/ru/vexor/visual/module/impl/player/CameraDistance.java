package ru.vexor.visual.module.impl.player;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.DoubleSetting;

public final class CameraDistance extends Module
{
    private static CameraDistance instance;

    private final DoubleSetting multiplier = add(new DoubleSetting("Множитель", 2.0D, 1.0D, 5.0D, 0.1D, 1));

    public CameraDistance()
    {
        super("Camera Distance", "Дистанция камеры от третьего лица", Category.PLAYER);
        setDisplayName("Дистанция камеры");
        instance = this;
    }

    public static float getMultiplier()
    {
        if (instance == null || !instance.isEnabled())
        {
            return 1.0F;
        }
        return (float) (double) instance.multiplier.getValue();
    }
}
