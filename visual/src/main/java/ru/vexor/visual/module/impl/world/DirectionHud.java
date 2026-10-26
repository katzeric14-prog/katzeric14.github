package ru.vexor.visual.module.impl.world;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

public final class DirectionHud extends HudModule
{
    private static final String[] DIRECTIONS = {"Ю", "ЮЗ", "З", "СЗ", "С", "СВ", "В", "ЮВ"};

    public DirectionHud()
    {
        super("Direction Hud", "Направление взгляда", -1, Category.WORLD, Anchor.TOP_LEFT);
        setDisplayName("Направление");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null)
        {
            return 0;
        }
        float yaw = player.getYRot() % 360.0F;
        if (yaw < 0.0F)
        {
            yaw += 360.0F;
        }
        int index = Math.round(yaw / 45.0F) % 8;
        String direction = DIRECTIONS[index];
        String degrees = Math.round(yaw) + "°";
        String label = "Курс";
        int width = Draw.width(label) + Draw.width(direction) + Draw.width(degrees) + 22;
        card(graphics, x, y, width, 15);
        Draw.text(graphics, font(), label, x + 4, y + 4, mutedColor());
        Draw.text(graphics, font(), direction, x + 8 + Draw.width(label), y + 4, accent());
        Draw.textRight(graphics, font(), degrees, x + width - 4, y + 4, 0xFFE9E6F5);
        return 15;
    }
}
