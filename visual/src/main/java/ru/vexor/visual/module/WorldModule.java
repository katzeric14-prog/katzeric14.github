package ru.vexor.visual.module;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.renderer.state.BlockOutlineRenderState;

public abstract class WorldModule extends Module
{
    protected WorldModule(String name, String description, int defaultKey)
    {
        super(name, description, Category.RENDER, defaultKey);
    }

    protected WorldModule(String name, String description, int defaultKey, Category category)
    {
        super(name, description, category, defaultKey);
    }

    public void renderWorld(WorldRenderContext context)
    {
    }

    public boolean onBlockOutline(WorldRenderContext context, BlockOutlineRenderState state)
    {
        return true;
    }
}
