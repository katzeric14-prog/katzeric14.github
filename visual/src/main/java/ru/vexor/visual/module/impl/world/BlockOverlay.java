package ru.vexor.visual.module.impl.world;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.renderer.state.BlockOutlineRenderState;
import net.minecraft.world.phys.AABB;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class BlockOverlay extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF8B5CF6));
    private final IntSetting width = add(new IntSetting("Толщина", 2, 1, 5, 1));

    private AABB target;
    private long seenAt;

    public BlockOverlay()
    {
        super("Block Overlay", "Своя обводка блока под прицелом", -1, Category.WORLD);
        setDisplayName("Обводка блока");
    }

    @Override
    public boolean onBlockOutline(WorldRenderContext context, BlockOutlineRenderState state)
    {
        if (state == null || state.pos() == null || state.shape() == null)
        {
            return true;
        }
        target = state.shape().bounds().move(state.pos());
        seenAt = System.currentTimeMillis();
        return false;
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        if (target == null || System.currentTimeMillis() - seenAt > 250L || !RenderUtil.ready(context))
        {
            return;
        }
        RenderUtil.box(context, target.inflate(0.002D),
                color.getValue() == null ? 0xFF8B5CF6 : color.getValue(), width.getValue());
    }
}
