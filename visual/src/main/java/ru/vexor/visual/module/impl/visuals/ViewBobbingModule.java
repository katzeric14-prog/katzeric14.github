package ru.vexor.visual.module.impl.visuals;

import net.minecraft.client.Minecraft;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;

public final class ViewBobbingModule extends Module
{
    private boolean previous = true;

    public ViewBobbingModule()
    {
        super("View Bobbing", "Убрать покачивание камеры", Category.VISUALS);
        setDisplayName("Без покачивания");
    }

    @Override
    public void onEnable()
    {
        previous = Minecraft.getInstance().options.bobView().get();
        Minecraft.getInstance().options.bobView().set(false);
    }

    @Override
    public void onDisable()
    {
        Minecraft.getInstance().options.bobView().set(previous);
    }
}
