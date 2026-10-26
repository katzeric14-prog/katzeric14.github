package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class PlayerEsp extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF8B5CF6));
    private final IntSetting distance = add(new IntSetting("Дистанция", 96, 16, 256, 16));
    private final IntSetting width = add(new IntSetting("Толщина", 2, 1, 5, 1));

    public PlayerEsp()
    {
        super("Player Esp", "Рамки вокруг игроков", -1);
        setDisplayName("ESP игроков");
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
        for (Entity entity : minecraft.level.entitiesForRendering())
        {
            if (!(entity instanceof Player) || entity == minecraft.player || !entity.isAlive())
            {
                continue;
            }
            if (entity.distanceToSqr(camera) > maxDistance)
            {
                continue;
            }
            RenderUtil.box(context, EspUtil.interpolatedBox(entity, partialTick), EspUtil.color(color), width.getValue());
        }
    }
}
