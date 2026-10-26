package ru.vexor.visual.module.impl.render;

import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.setting.ColorSetting;

final class EspUtil
{
    private EspUtil()
    {
    }

    static float partialTick()
    {
        return Minecraft.getInstance().getDeltaTracker().getGameTimeDeltaPartialTick(true);
    }

    static double maxDistanceSq(int blocks)
    {
        return (double) blocks * (double) blocks;
    }

    static AABB interpolatedBox(Entity entity, float partialTick)
    {
        Vec3 current = entity.getPosition(partialTick);
        Vec3 delta = current.subtract(entity.position());
        return entity.getBoundingBox().move(delta);
    }

    static Vec3 interpolatedCenter(Entity entity, float partialTick)
    {
        return entity.getPosition(partialTick).add(0.0D, entity.getBbHeight() * 0.5D, 0.0D);
    }

    static int color(ColorSetting setting)
    {
        Integer value = setting.getValue();
        return value == null ? 0xFFFFFFFF : value;
    }
}
