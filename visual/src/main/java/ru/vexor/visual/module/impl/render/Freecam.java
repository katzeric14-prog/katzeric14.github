package ru.vexor.visual.module.impl.render;

import com.mojang.blaze3d.platform.InputConstants;
import net.minecraft.client.Minecraft;
import net.minecraft.world.phys.Vec3;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.util.ViewState;

public final class Freecam extends Module
{
    private final DoubleSetting speed = add(new DoubleSetting("Скорость", 0.45D, 0.05D, 2.0D, 0.05D, 2));

    public Freecam()
    {
        super("Freecam", "Свободная камера", Category.RENDER);
        setDisplayName("Свободная камера");
    }

    @Override
    public void onEnable()
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player != null)
        {
            ViewState.startFreecam(minecraft.gameRenderer.getMainCamera());
        }
    }

    @Override
    public void onDisable()
    {
        ViewState.stopFreecam();
    }

    @Override
    public void onTick()
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null)
        {
            setEnabled(false);
            return;
        }
        if (!ViewState.freecamActive)
        {
            ViewState.startFreecam(minecraft.gameRenderer.getMainCamera());
        }

        com.mojang.blaze3d.platform.Window window = minecraft.getWindow();
        double value = speed.getValue();
        float yaw = (float) Math.toRadians(ViewState.freecamYaw);
        float pitch = (float) Math.toRadians(ViewState.freecamPitch);
        Vec3 forward = new Vec3(-Math.sin(yaw) * Math.cos(pitch), -Math.sin(pitch), Math.cos(yaw) * Math.cos(pitch));
        Vec3 right = new Vec3(-Math.cos(yaw), 0.0D, -Math.sin(yaw));
        Vec3 move = Vec3.ZERO;

        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_W))
        {
            move = move.add(forward);
        }
        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_S))
        {
            move = move.subtract(forward);
        }
        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_D))
        {
            move = move.add(right);
        }
        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_A))
        {
            move = move.subtract(right);
        }
        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_SPACE))
        {
            move = move.add(0.0D, 1.0D, 0.0D);
        }
        if (InputConstants.isKeyDown(window, GLFW.GLFW_KEY_LEFT_SHIFT))
        {
            move = move.add(0.0D, -1.0D, 0.0D);
        }

        double sprint = InputConstants.isKeyDown(window, GLFW.GLFW_KEY_LEFT_CONTROL) ? 2.5D : 1.0D;
        if (move.lengthSqr() > 0.0D)
        {
            move = move.normalize().scale(value * sprint);
            ViewState.freecamX += move.x;
            ViewState.freecamY += move.y;
            ViewState.freecamZ += move.z;
        }
    }
}
