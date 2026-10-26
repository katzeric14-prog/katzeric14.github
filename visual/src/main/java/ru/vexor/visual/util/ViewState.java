package ru.vexor.visual.util;

import net.minecraft.client.Camera;
import net.minecraft.world.phys.Vec3;

public final class ViewState
{
    public static boolean freecamActive;
    public static double freecamX;
    public static double freecamY;
    public static double freecamZ;
    public static float freecamYaw;
    public static float freecamPitch;

    public static boolean freelookActive;
    public static float freelookYaw;
    public static float freelookPitch;

    public static boolean smoothActive;
    public static float smoothSpeed = 0.35F;

    private static boolean smoothInitialized;
    private static float smoothYaw;
    private static float smoothPitch;
    private static long smoothLastNanos;

    private ViewState()
    {
    }

    public static void reset()
    {
        freecamActive = false;
        freelookActive = false;
        smoothActive = false;
        smoothInitialized = false;
    }

    public static void stopFreecam()
    {
        freecamActive = false;
    }

    public static void stopFreelook()
    {
        freelookActive = false;
    }

    public static void stopSmooth()
    {
        smoothActive = false;
        smoothInitialized = false;
    }

    public static void startFreecam(Camera camera)
    {
        freecamActive = true;
        Vec3 position = camera.position();
        freecamX = position.x;
        freecamY = position.y;
        freecamZ = position.z;
        freecamYaw = camera.yRot();
        freecamPitch = camera.xRot();
    }

    public static void startFreelook(Camera camera)
    {
        freelookActive = true;
        freelookYaw = camera.yRot();
        freelookPitch = camera.xRot();
    }

    public static boolean applyMouse(double deltaX, double deltaY)
    {
        if (freecamActive)
        {
            freecamYaw += (float) (deltaX * 0.15D);
            freecamPitch = (float) Math.max(-89.9D, Math.min(89.9D, freecamPitch + deltaY * 0.15D));
            return true;
        }
        if (freelookActive)
        {
            freelookYaw += (float) (deltaX * 0.15D);
            freelookPitch = (float) Math.max(-89.9D, Math.min(89.9D, freelookPitch + deltaY * 0.15D));
            return true;
        }
        return false;
    }

    public static Vec3 overridePosition(Vec3 vanilla)
    {
        if (freecamActive)
        {
            return new Vec3(freecamX, freecamY, freecamZ);
        }
        return null;
    }

    public static float[] overrideRotation(float vanillaYaw, float vanillaPitch)
    {
        if (freecamActive)
        {
            return new float[] {freecamPitch, freecamYaw};
        }
        if (freelookActive)
        {
            return new float[] {freelookPitch, freelookYaw};
        }
        if (smoothActive)
        {
            return smooth(vanillaYaw, vanillaPitch);
        }
        return null;
    }

    private static float[] smooth(float targetYaw, float targetPitch)
    {
        long now = System.nanoTime();
        if (!smoothInitialized)
        {
            smoothInitialized = true;
            smoothYaw = targetYaw;
            smoothPitch = targetPitch;
            smoothLastNanos = now;
            return null;
        }
        float deltaSeconds = Math.min(0.1F, (now - smoothLastNanos) / 1_000_000_000.0F);
        smoothLastNanos = now;
        float tau = Math.max(0.02F, 0.22F - smoothSpeed * 0.18F);
        float alpha = 1.0F - (float) Math.exp(-deltaSeconds / tau);
        smoothYaw += wrap(targetYaw - smoothYaw) * alpha;
        smoothPitch += (targetPitch - smoothPitch) * alpha;
        return new float[] {smoothPitch, smoothYaw};
    }

    private static float wrap(float angle)
    {
        float value = angle % 360.0F;
        if (value >= 180.0F)
        {
            value -= 360.0F;
        }
        if (value < -180.0F)
        {
            value += 360.0F;
        }
        return value;
    }
}
