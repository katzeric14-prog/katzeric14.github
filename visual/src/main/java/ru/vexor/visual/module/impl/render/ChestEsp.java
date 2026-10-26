package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.renderer.blockentity.state.BlockEntityRenderState;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class ChestEsp extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFFB020));
    private final IntSetting distance = add(new IntSetting("Дистанция", 64, 8, 128, 8));

    public ChestEsp()
    {
        super("Chest Esp", "Подсветка сундуков и бочек", -1);
        setDisplayName("ESP сундуков");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        if (!RenderUtil.ready(context) || context.worldState() == null)
        {
            return;
        }
        Vec3 camera = RenderUtil.camera(context);
        double maxDistance = EspUtil.maxDistanceSq(distance.getValue());
        int rgb = EspUtil.color(color);
        for (BlockEntityRenderState state : context.worldState().blockEntityRenderStates)
        {
            if (state.blockPos == null || !isChest(state.blockEntityType))
            {
                continue;
            }
            BlockPos pos = state.blockPos;
            if (Vec3.atCenterOf(pos).distanceToSqr(camera) > maxDistance)
            {
                continue;
            }
            RenderUtil.box(context, new AABB(pos).inflate(0.002D), rgb, 2.0F);
        }
    }

    static boolean isChest(BlockEntityType<?> type)
    {
        return type == BlockEntityType.CHEST
                || type == BlockEntityType.TRAPPED_CHEST
                || type == BlockEntityType.ENDER_CHEST
                || type == BlockEntityType.BARREL;
    }
}
