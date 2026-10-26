package ru.vexor.visual.module.impl.render;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.util.GammaState;

public final class Fullbright extends Module
{
    public Fullbright()
    {
        super("Fullbright", "Максимальная яркость мира", Category.RENDER);
        setDisplayName("Полная яркость");
    }

    @Override
    public void onEnable()
    {
        GammaState.apply("fullbright", 10.0D);
    }

    @Override
    public void onDisable()
    {
        GammaState.release("fullbright");
    }
}
