package ru.vexor.visual.module.impl.render;

import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;
import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;

public final class EntityEsp extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFF5D73));
    private final IntSetting distance = add(new IntSetting("Дистанция", 64, 8, 128, 8));
    private final IntSetting width = add(new IntSetting("Толщина", 2, 1, 5, 1));
    private final BoolSetting mobs = add(new BoolSetting("Мобы", true));
    private final BoolSetting animals = add(new BoolSetting("Животные", true));

    public EntityEsp()
    {
        super("Entity Esp", "Рамки вокруг мобов", -1);
        setDisplayName("ESP сущностей");
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
            if (entity == minecraft.player || entity instanceof Player || !(entity instanceof LivingEntity) || !entity.isAlive())
            {
                continue;
            }
            boolean hostile = entity.getType().getCategory().isFriendly() == false;
            if (hostile ? !mobs.getValue() : !animals.getValue())
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
