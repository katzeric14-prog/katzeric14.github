package ru.vexor.visual.module.impl.visuals;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.Draw;

public final class DamageTint extends HudModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFF1E3C));
    private final IntSetting strength = add(new IntSetting("Сила", 55, 5, 100, 5));

    public DamageTint()
    {
        super("Damage Tint", "Красный оттенок при получении урона", -1, Anchor.OVERLAY);
        setDisplayName("Оттенок урона");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null || player.hurtTime <= 0)
        {
            return 0;
        }
        float fade = Math.min(1.0F, player.hurtTime / 10.0F);
        int alpha = (int) (strength.getValue() / 100.0F * 130.0F * fade);
        int rgb = color.getValue() == null ? 0xFFFF1E3C : color.getValue();
        graphics.fill(0, 0, graphics.guiWidth(), graphics.guiHeight(), Draw.withAlpha(rgb, alpha));
        return 0;
    }
}
