package ru.vexor.visual.util;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.world.phys.Vec3;
import org.joml.Matrix4f;
import org.joml.Vector4f;

public final class Projection
{
    private static final Matrix4f view = new Matrix4f();
    private static final Matrix4f projection = new Matrix4f();
    private static final Vector4f vector = new Vector4f();
    private static Vec3 camera = Vec3.ZERO;
    private static boolean valid;

    private Projection()
    {
    }

    public static void capture(WorldRenderContext context)
    {
        if (context == null || context.matrices() == null)
        {
            valid = false;
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        float partialTick = minecraft.getDeltaTracker().getGameTimeDeltaPartialTick(true);
        view.set(context.matrices().last().pose());
        projection.set(minecraft.gameRenderer.getProjectionMatrix(partialTick));
        camera = RenderUtil.camera(context);
        valid = true;
    }

    public static boolean isAvailable()
    {
        return valid;
    }

    public static boolean project(Vec3 point, float[] out)
    {
        if (!valid)
        {
            return false;
        }
        vector.set((float) (point.x - camera.x), (float) (point.y - camera.y), (float) (point.z - camera.z), 1.0F);
        view.transform(vector);
        projection.transform(vector);
        if (vector.w <= 0.001F)
        {
            return false;
        }
        float ndcX = vector.x / vector.w;
        float ndcY = vector.y / vector.w;
        float ndcZ = vector.z / vector.w;
        if (ndcZ < -1.0F || ndcZ > 1.0F)
        {
            return false;
        }
        out[0] = ndcX;
        out[1] = ndcY;
        return true;
    }
}
