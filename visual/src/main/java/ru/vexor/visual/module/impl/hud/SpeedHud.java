package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

import java.util.Locale;

public final class SpeedHud extends HudModule
{
    public SpeedHud()
    {
        super("Speed", "Скорость перемещения", -1);
        setDisplayName("Скорость");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null)
        {
            return 0;
        }
        double speed = Math.sqrt(player.getDeltaMovement().x * player.getDeltaMovement().x
                + player.getDeltaMovement().z * player.getDeltaMovement().z) * 20.0D;
        String label = "Скорость";
        String value = String.format(Locale.ROOT, "%.2f б/с", speed);
        int width = Draw.width(label) + Draw.width(value) + 14;
        card(graphics, x, y, width, 18);
        Draw.text(graphics, font(), label, x + 4, y + 5, mutedColor());
        Draw.text(graphics, font(), value, x + width - 4 - Draw.width(value), y + 5, accent());
        return 18;
    }
}
