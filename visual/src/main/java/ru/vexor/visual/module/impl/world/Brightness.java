package ru.vexor.visual.module.impl.world;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.util.GammaState;

public final class Brightness extends Module
{
    private final DoubleSetting gamma = add(new DoubleSetting("Гамма", 3.0D, 0.0D, 10.0D, 0.1D, 1));
    private double applied = -1.0D;

    public Brightness()
    {
        super("Brightness", "Настраиваемая яркость мира", Category.WORLD);
        setDisplayName("Гамма");
    }

    @Override
    public void onEnable()
    {
        apply();
    }

    @Override
    public void onTick()
    {
        if (Math.abs(applied - (double) gamma.getValue()) > 0.001D)
        {
            apply();
        }
    }

    private void apply()
    {
        applied = gamma.getValue();
        GammaState.apply("brightness", applied);
    }

    @Override
    public void onDisable()
    {
        applied = -1.0D;
        GammaState.release("brightness");
    }
}
