package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

import java.util.Locale;

public final class TargetHud extends HudModule
{
    public TargetHud()
    {
        super("Target Hud", "Информация о цели под прицелом", -1, Anchor.CENTER);
        setDisplayName("Цель");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        Minecraft minecraft = Minecraft.getInstance();
        LocalPlayer player = minecraft.player;
        if (player == null || minecraft.level == null)
        {
            return 0;
        }
        Vec3 eye = player.getEyePosition();
        Vec3 look = player.getLookAngle();
        LivingEntity target = null;
        double best = 0.965D;
        for (Entity entity : minecraft.level.entitiesForRendering())
        {
            if (entity == player || !(entity instanceof LivingEntity) || !entity.isAlive())
            {
                continue;
            }
            Vec3 offset = entity.getEyePosition().subtract(eye);
            double distance = offset.length();
            if (distance > 8.0D || distance < 0.1D)
            {
                continue;
            }
            double dot = offset.normalize().dot(look);
            if (dot > best)
            {
                best = dot;
                target = (LivingEntity) entity;
            }
        }
        if (target == null)
        {
            return 0;
        }

        int width = 150;
        int height = 34;
        int left = x - width / 2;
        card(graphics, left, y, width, height);

        String name = Draw.trim(font(), target.getName().getString(), width - 54);
        Draw.text(graphics, font(), name, left + 5, y + 5, 0xFFFFFFFF);
        String distance = String.format(Locale.ROOT, "%.1f м", target.distanceTo(player));
        Draw.textRight(graphics, font(), distance, left + width - 5, y + 5, mutedColor());

        float health = Math.max(0.0F, target.getHealth());
        float maxHealth = Math.max(1.0F, target.getMaxHealth());
        float ratio = Math.min(1.0F, health / maxHealth);
        int barX = left + 5;
        int barY = y + 20;
        int barWidth = width - 10;
        graphics.fill(barX, barY, barX + barWidth, barY + 6, 0xAA000000);
        int barColor = ratio > 0.5F ? 0xFF4CFF8A : (ratio > 0.25F ? 0xFFFFC857 : 0xFFFF5D73);
        graphics.fill(barX, barY, barX + Math.max(1, Math.round(barWidth * ratio)), barY + 6, barColor);
        String hp = Math.round(health) + " / " + Math.round(maxHealth);
        Draw.textCentered(graphics, font(), hp, x, y + 26, 0xFFE9E6F5);
        return height;
    }
}
