package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.gui.GuiGraphics;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.util.Draw;

public final class Watermark extends HudModule
{
    private final BoolSetting shadow = add(new BoolSetting("Тень", true));

    public Watermark()
    {
        super("Watermark", "Логотип клиента на экране", -1);
        setDisplayName("Вотермарка");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        String first = "VEXOR";
        String second = " VISUAL";
        int width = Draw.width(first + second) + 8;
        card(graphics, x, y, width, 18);
        int textY = y + 5;
        if (shadow.getValue())
        {
            Draw.textShadow(graphics, font(), first, x + 4, textY, accent());
            Draw.textShadow(graphics, font(), second, x + 4 + Draw.width(first), textY, 0xFFFFFFFF);
        }
        else
        {
            Draw.text(graphics, font(), first, x + 4, textY, accent());
            Draw.text(graphics, font(), second, x + 4 + Draw.width(first), textY, 0xFFFFFFFF);
        }
        return 18;
    }
}
