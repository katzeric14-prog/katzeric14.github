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

public final class Wings extends WorldModule
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF35E0FF));
    private final DoubleSetting size = add(new DoubleSetting("Размер", 1.0D, 0.5D, 1.6D, 0.05D, 2));
    private final DoubleSetting speed = add(new DoubleSetting("Взмах", 1.0D, 0.0D, 3.0D, 0.1D, 1));

    public Wings()
    {
        super("Wings", "Крылья за спиной", -1, Category.COSMETICS);
        setDisplayName("Крылья");
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
        double time = (player.tickCount + partialTick) / 20.0D;
        double flap = Math.sin(time * 3.0D * speed.getValue()) * 0.22D;
        int rgb = color.getValue() == null ? 0xFF35E0FF : color.getValue();
        double scale = size.getValue();

        Vec3 shoulder = position.add(0.0D, 1.52D, 0.0D).add(back.scale(0.16D));
        for (int side = -1; side <= 1; side += 2)
        {
            Vec3 root = shoulder.add(right.scale(side * 0.10D));
            Vec3 topRoot = root.add(0.0D, 0.38D, 0.0D);
            Vec3 bottomRoot = root.add(0.0D, -0.42D, 0.0D).add(back.scale(0.05D));
            Vec3 tip = root
                    .add(right.scale(side * 1.15D * scale))
                    .add(0.0D, 0.35D + flap, 0.0D)
                    .add(back.scale(0.30D));
            Vec3 tipLower = tip.add(0.0D, -0.32D, 0.0D).add(right.scale(side * -0.12D));
            RenderUtil.quad(context, topRoot, tip, tipLower, bottomRoot, rgb);
            RenderUtil.quad(context, bottomRoot, tipLower, tip, topRoot, rgb);
        }
    }
}
