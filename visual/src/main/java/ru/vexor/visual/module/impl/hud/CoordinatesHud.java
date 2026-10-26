package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

import java.util.Locale;

public final class CoordinatesHud extends HudModule
{
    public CoordinatesHud()
    {
        super("Coordinates", "Координаты игрока", -1);
        setDisplayName("Координаты");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null)
        {
            return 0;
        }
        String text = String.format(Locale.ROOT, "XYZ %d %d %d",
                (int) Math.floor(player.getX()), (int) Math.floor(player.getY()), (int) Math.floor(player.getZ()));
        int width = Draw.width(text) + 10;
        card(graphics, x, y, width, 18);
        Draw.text(graphics, font(), "XYZ ", x + 4, y + 5, mutedColor());
        Draw.text(graphics, font(), text.substring(4), x + 4 + Draw.width("XYZ "), y + 5, accent());
        return 18;
    }
}
