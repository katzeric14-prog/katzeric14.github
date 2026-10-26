package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.effect.MobEffectInstance;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public final class PotionStatus extends HudModule
{
    public PotionStatus()
    {
        super("Potion Status", "Активные эффекты", -1);
        setDisplayName("Эффекты");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null || player.getActiveEffects().isEmpty())
        {
            return 0;
        }
        List<MobEffectInstance> effects = new ArrayList<>(player.getActiveEffects());
        effects.sort((a, b) -> Integer.compare(b.getDuration(), a.getDuration()));
        int count = Math.min(6, effects.size());
        int width = 132;
        int height = 5 + count * 11;
        card(graphics, x, y, width, height);

        for (int i = 0; i < count; i++)
        {
            MobEffectInstance effect = effects.get(i);
            int rowY = y + 4 + i * 11;
            String name = effect.getEffect().value().getDisplayName().getString();
            if (effect.getAmplifier() > 0)
            {
                name = name + " " + roman(effect.getAmplifier() + 1);
            }
            String time = effect.isInfiniteDuration() ? "∞" : duration(effect.getDuration());
            int color = effect.getEffect().value().isBeneficial() ? 0xFF4CFF8A : 0xFFFF7A8A;
            Draw.text(graphics, font(), Draw.trim(font(), name, 88), x + 4, rowY, color);
            Draw.textRight(graphics, font(), time, x + width - 4, rowY, mutedColor());
        }
        return height;
    }

    private static String duration(int ticks)
    {
        int seconds = Math.max(0, ticks / 20);
        return String.format(Locale.ROOT, "%d:%02d", seconds / 60, seconds % 60);
    }

    private static String roman(int value)
    {
        switch (value)
        {
            case 2:
                return "II";
            case 3:
                return "III";
            case 4:
                return "IV";
            case 5:
                return "V";
            default:
                return String.valueOf(value);
        }
    }
}
