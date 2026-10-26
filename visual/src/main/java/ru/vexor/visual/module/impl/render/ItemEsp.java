package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class ItemEsp extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFFC857));
    private final IntSetting distance = add(new IntSetting("Дистанция", 48, 8, 128, 8));
    private final BoolSetting dropped = add(new BoolSetting("Только выпавшие", true));

    public ItemEsp()
    {
        super("Item Esp", "Подсветка предметов на земле", -1);
        setDisplayName("ESP предметов");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (!RenderUtil.ready(context) || minecraft.level == null)
        {
            return;
        }
        Vec3 camera = RenderUtil.camera(context);
        float partialTick = EspUtil.partialTick();
        double maxDistance = EspUtil.maxDistanceSq(distance.getValue());
        for (Entity entity : minecraft.level.entitiesForRendering())
        {
            if (!(entity instanceof ItemEntity) || !entity.isAlive())
            {
                continue;
            }
            ItemEntity item = (ItemEntity) entity;
            if (dropped.getValue() && item.getItem().isEmpty())
            {
                continue;
            }
            if (entity.distanceToSqr(camera) > maxDistance)
            {
                continue;
            }
            RenderUtil.box(context, EspUtil.interpolatedBox(entity, partialTick), EspUtil.color(color), 2.0F);
        }
    }
}
