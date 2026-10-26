package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

public final class FpsHud extends HudModule
{
    public FpsHud()
    {
        super("FPS", "Счётчик кадров", -1);
        setDisplayName("FPS");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        int fps = Minecraft.getInstance().getFps();
        String label = "FPS";
        String value = String.valueOf(fps);
        int width = Draw.width(label) + Draw.width(value) + 14;
        card(graphics, x, y, width, 18);
        Draw.text(graphics, font(), label, x + 4, y + 5, mutedColor());
        Draw.text(graphics, font(), value, x + width - 4 - Draw.width(value), y + 5, accent());
        return 18;
    }
}
