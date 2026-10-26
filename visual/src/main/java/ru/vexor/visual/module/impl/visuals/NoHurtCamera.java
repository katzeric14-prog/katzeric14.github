package ru.vexor.visual.module.impl.visuals;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;

public final class NoHurtCamera extends Module
{
    public NoHurtCamera()
    {
        super("No Hurt Camera", "Убрать тряску камеры от урона", Category.VISUALS);
        setDisplayName("Без тряски");
    }
}
