package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class Tracers extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF4FE3C1));
    private final IntSetting distance = add(new IntSetting("Дистанция", 96, 16, 256, 16));
    private final IntSetting width = add(new IntSetting("Толщина", 1, 1, 4, 1));
    private final BoolSetting players = add(new BoolSetting("Игроки", true));
    private final BoolSetting mobs = add(new BoolSetting("Мобы", true));

    public Tracers()
    {
        super("Tracers", "Линии к игрокам и мобам", -1);
        setDisplayName("Трассеры");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (!RenderUtil.ready(context) || minecraft.level == null || minecraft.player == null)
        {
            return;
        }
        Vec3 camera = RenderUtil.camera(context);
        float partialTick = EspUtil.partialTick();
        double maxDistance = EspUtil.maxDistanceSq(distance.getValue());
        int rgb = EspUtil.color(color);
        for (Entity entity : minecraft.level.entitiesForRendering())
        {
            if (entity == minecraft.player || !(entity instanceof LivingEntity) || !entity.isAlive())
            {
                continue;
            }
            boolean isPlayer = entity instanceof Player;
            if (isPlayer ? !players.getValue() : !mobs.getValue())
            {
                continue;
            }
            if (entity.distanceToSqr(camera) > maxDistance)
            {
                continue;
            }
            Vec3 target = EspUtil.interpolatedCenter(entity, partialTick);
            RenderUtil.line(context, camera, target, rgb, width.getValue());
        }
    }
}
