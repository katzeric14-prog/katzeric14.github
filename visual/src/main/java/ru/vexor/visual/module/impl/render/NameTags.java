package ru.vexor.visual.module.impl.render;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.Draw;
import ru.vexor.visual.util.Projection;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

public final class NameTags extends HudModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFFFFFF));
    private final IntSetting distance = add(new IntSetting("Дистанция", 96, 16, 256, 16));
    private final BoolSetting players = add(new BoolSetting("Игроки", true));
    private final BoolSetting mobs = add(new BoolSetting("Мобы", true));
    private final BoolSetting health = add(new BoolSetting("Здоровье", true));
    private final BoolSetting showDistance = add(new BoolSetting("Дистанция до цели", true));
    private final BoolSetting background = add(new BoolSetting("Фон", true));

    private final float[] projected = new float[2];
    private final List<Tag> tags = new ArrayList<>();

    public NameTags()
    {
        super("Name Tags", "Имена, здоровье и дистанция над сущностями", -1, Anchor.OVERLAY);
        setDisplayName("Никнеймы");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (!Projection.isAvailable() || minecraft.level == null || minecraft.player == null)
        {
            return 0;
        }
        Vec3 camera = minecraft.gameRenderer.getMainCamera().position();
        double maxDistance = (double) distance.getValue() * distance.getValue();
        float partialTick = minecraft.getDeltaTracker().getGameTimeDeltaPartialTick(true);
        int rgb = EspUtil.color(color);
        int guiWidth = graphics.guiWidth();
        int guiHeight = graphics.guiHeight();
        tags.clear();

        for (Entity raw : minecraft.level.entitiesForRendering())
        {
            if (!(raw instanceof LivingEntity entity) || entity == minecraft.player || !entity.isAlive())
            {
                continue;
            }
            boolean isPlayer = entity instanceof Player;
            if (isPlayer ? !players.getValue() : !mobs.getValue())
            {
                continue;
            }
            double sqrDistance = entity.distanceToSqr(camera);
            if (sqrDistance > maxDistance)
            {
                continue;
            }

            Vec3 position = entity.getPosition(partialTick).add(0.0D, entity.getBbHeight() + 0.34D, 0.0D);
            if (!Projection.project(position, projected))
            {
                continue;
            }
            if (projected[0] < -1.15F || projected[0] > 1.15F || projected[1] < -1.15F || projected[1] > 1.15F)
            {
                continue;
            }

            int screenX = (int) ((projected[0] * 0.5F + 0.5F) * guiWidth);
            int screenY = (int) ((1.0F - (projected[1] * 0.5F + 0.5F)) * guiHeight);
            tags.add(new Tag(entity, screenX, screenY, Math.sqrt(sqrDistance)));
        }

        tags.sort(Comparator.comparingDouble(Tag::distance).reversed());
        for (Tag tag : tags)
        {
            drawTag(graphics, tag, rgb);
        }
        return 0;
    }

    private void drawTag(GuiGraphics graphics, Tag tag, int rgb)
    {
        LivingEntity living = (LivingEntity) tag.entity;
        String text = Draw.trim(font(), living.getName().getString(), 170);
        if (showDistance.getValue())
        {
            text = text + "  " + String.format(java.util.Locale.ROOT, "%.0fм", tag.distance);
        }
        int textWidth = Draw.width(text);
        int width = Math.max(textWidth, 44) + 8;
        int height = health.getValue() ? 20 : 13;
        int left = tag.x - width / 2;
        int top = tag.y - height - 2;

        if (background.getValue())
        {
            Draw.roundedRect(graphics, left, top, width, height, 4, 0xA0100C1C);
            Draw.roundedRect(graphics, left, top + 1, 2, height - 2, 1, 0xCC7A4BFF);
            Draw.outline(graphics, left, top, width, height, 4, 0x557A4BFF);
        }
        Draw.textShadow(graphics, font(), text, tag.x - textWidth / 2, top + 3, rgb);

        if (health.getValue())
        {
            float ratio = living.getMaxHealth() <= 0.0F
                    ? 0.0F
                    : Math.max(0.0F, Math.min(1.0F, living.getHealth() / living.getMaxHealth()));
            int barLeft = left + 4;
            int barTop = top + height - 5;
            int barWidth = width - 8;
            int fill = Math.round(barWidth * ratio);
            Draw.roundedRect(graphics, barLeft, barTop, barWidth, 2, 1, 0x66000000);
            if (fill > 0)
            {
                int barColor = ratio > 0.6F ? 0xFF44E37E : ratio > 0.3F ? 0xFFFFC947 : 0xFFFF4D6D;
                Draw.roundedRect(graphics, barLeft, barTop, fill, 2, 1, barColor);
            }
        }
    }

    private record Tag(LivingEntity entity, int x, int y, double distance)
    {
    }
}
