package ru.vexor.visual.module.impl.world;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class ChunkBorders extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0x66FFFFFF));
    private final IntSetting radius = add(new IntSetting("Радиус", 3, 1, 8, 1));
    private final IntSetting width = add(new IntSetting("Толщина", 1, 1, 3, 1));

    public ChunkBorders()
    {
        super("Chunk Borders", "Границы чанков вокруг игрока", -1, Category.WORLD);
        setDisplayName("Границы чанков");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (!RenderUtil.ready(context) || player == null)
        {
            return;
        }
        int rgb = color.getValue() == null ? 0x66FFFFFF : color.getValue();
        int chunkX = player.chunkPosition().x;
        int chunkZ = player.chunkPosition().z;
        int r = radius.getValue();
        double y = Math.floor(player.getY()) + 0.04D;
        float lineWidth = width.getValue();

        for (int cx = chunkX - r; cx <= chunkX + r; cx++)
        {
            for (int cz = chunkZ - r; cz <= chunkZ + r; cz++)
            {
                double minX = cx * 16.0D;
                double minZ = cz * 16.0D;
                double maxX = minX + 16.0D;
                double maxZ = minZ + 16.0D;
                Vec3 a = new Vec3(minX, y, minZ);
                Vec3 b = new Vec3(maxX, y, minZ);
                Vec3 c = new Vec3(maxX, y, maxZ);
                Vec3 d = new Vec3(minX, y, maxZ);
                RenderUtil.line(context, a, b, rgb, lineWidth);
                RenderUtil.line(context, b, c, rgb, lineWidth);
                RenderUtil.line(context, c, d, rgb, lineWidth);
                RenderUtil.line(context, d, a, rgb, lineWidth);
            }
        }
    }
}
