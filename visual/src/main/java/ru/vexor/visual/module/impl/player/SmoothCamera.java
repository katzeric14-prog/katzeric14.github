package ru.vexor.visual.module.impl.player;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.util.ViewState;

public final class SmoothCamera extends Module
{
    private final DoubleSetting smoothness = add(new DoubleSetting("Плавность", 0.45D, 0.05D, 1.0D, 0.05D, 2));

    public SmoothCamera()
    {
        super("Smooth Camera", "Плавное вращение камеры", Category.PLAYER);
        setDisplayName("Плавная камера");
    }

    @Override
    public void onEnable()
    {
        ViewState.smoothSpeed = (float) (double) smoothness.getValue();
        ViewState.smoothActive = true;
    }

    @Override
    public void onTick()
    {
        ViewState.smoothSpeed = (float) (double) smoothness.getValue();
        ViewState.smoothActive = true;
    }

    @Override
    public void onDisable()
    {
        ViewState.stopSmooth();
    }
}
