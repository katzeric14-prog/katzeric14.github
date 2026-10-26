package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.multiplayer.PlayerInfo;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

public final class PingHud extends HudModule
{
    public PingHud()
    {
        super("Ping", "Задержка соединения", -1);
        setDisplayName("Пинг");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        Minecraft minecraft = Minecraft.getInstance();
        int ping = -1;
        if (minecraft.player != null && minecraft.getConnection() != null)
        {
            PlayerInfo info = minecraft.getConnection().getPlayerInfo(minecraft.player.getUUID());
            if (info != null)
            {
                ping = info.getLatency();
            }
        }
        String label = "Пинг";
        String value = ping < 0 ? "—" : ping + " мс";
        int width = Draw.width(label) + Draw.width(value) + 14;
        card(graphics, x, y, width, 18);
        Draw.text(graphics, font(), label, x + 4, y + 5, mutedColor());
        int color = ping < 0 ? mutedColor() : (ping < 120 ? 0xFF4CFF8A : (ping < 300 ? 0xFFFFC857 : 0xFFFF5D73));
        Draw.text(graphics, font(), value, x + width - 4 - Draw.width(value), y + 5, color);
        return 18;
    }
}
