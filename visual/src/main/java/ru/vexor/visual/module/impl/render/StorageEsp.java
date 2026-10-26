package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.renderer.blockentity.state.BlockEntityRenderState;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.entity.BlockEntityType;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class StorageEsp extends WorldModule
{
    private static final int STORAGE_COLOR = 0xFF6CE3FF;
    private static final int MACHINE_COLOR = 0xFFB388FF;

    private final IntSetting distance = add(new IntSetting("Дистанция", 64, 8, 128, 8));

    public StorageEsp()
    {
        super("Storage Esp", "Подсветка контейнеров и механизмов", -1);
        setDisplayName("ESP хранилищ");
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
        for (BlockEntityRenderState state : context.worldState().blockEntityRenderStates)
        {
            if (state.blockPos == null)
            {
                continue;
            }
            int color = colorFor(state.blockEntityType);
            if (color == 0)
            {
                continue;
            }
            BlockPos pos = state.blockPos;
            if (Vec3.atCenterOf(pos).distanceToSqr(camera) > maxDistance)
            {
                continue;
            }
            RenderUtil.box(context, new AABB(pos).inflate(0.002D), color, 2.0F);
        }
    }

    private static int colorFor(BlockEntityType<?> type)
    {
        if (type == null)
        {
            return 0;
        }
        if (type == BlockEntityType.SHULKER_BOX || type == BlockEntityType.HOPPER
                || type == BlockEntityType.DISPENSER || type == BlockEntityType.DROPPER)
        {
            return STORAGE_COLOR;
        }
        if (type == BlockEntityType.FURNACE || type == BlockEntityType.BLAST_FURNACE
                || type == BlockEntityType.BREWING_STAND || type == BlockEntityType.CAMPFIRE
                || type == BlockEntityType.JUKEBOX)
        {
            return MACHINE_COLOR;
        }
        return 0;
    }
}
