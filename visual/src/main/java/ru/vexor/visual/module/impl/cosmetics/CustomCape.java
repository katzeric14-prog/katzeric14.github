package ru.vexor.visual.module.impl.cosmetics;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.util.RenderUtil;

public final class CustomCape extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF7A2BFF));
    private final DoubleSetting size = add(new DoubleSetting("Размер", 1.0D, 0.5D, 1.6D, 0.05D, 2));

    public CustomCape()
    {
        super("Custom Cape", "Плащ на вашем персонаже", -1, Category.COSMETICS);
        setDisplayName("Плащ");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        Minecraft minecraft = Minecraft.getInstance();
        LocalPlayer player = minecraft.player;
        if (!RenderUtil.ready(context) || player == null)
        {
            return;
        }
        float partialTick = minecraft.getDeltaTracker().getGameTimeDeltaPartialTick(true);
        Vec3 position = player.getPosition(partialTick);
        float yaw = (float) Math.toRadians(player.getYRot());
        Vec3 back = new Vec3(Math.sin(yaw), 0.0D, -Math.cos(yaw));
        Vec3 right = new Vec3(-Math.cos(yaw), 0.0D, -Math.sin(yaw));
        double width = 0.44D * size.getValue();
        double sway = Math.sin((player.tickCount + partialTick) * 0.08D) * 0.035D;

        Vec3 topCenter = position.add(0.0D, 1.62D, 0.0D).add(back.scale(0.17D));
        Vec3 bottomCenter = position.add(0.0D, 0.55D, 0.0D).add(back.scale(0.30D + sway));
        Vec3 topRight = topCenter.add(right.scale(width));
        Vec3 topLeft = topCenter.add(right.scale(-width));
        Vec3 bottomRight = bottomCenter.add(right.scale(width * 0.85D));
        Vec3 bottomLeft = bottomCenter.add(right.scale(-width * 0.85D));

        int rgb = color.getValue() == null ? 0xFF7A2BFF : color.getValue();
        RenderUtil.quad(context, topLeft, topRight, bottomRight, bottomLeft, rgb);
        RenderUtil.quad(context, bottomLeft, bottomRight, topRight, topLeft, rgb);
    }
}
