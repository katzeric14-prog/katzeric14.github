package ru.vexor.visual.util;

import com.mojang.blaze3d.pipeline.BlendFunction;
import com.mojang.blaze3d.pipeline.RenderPipeline;
import com.mojang.blaze3d.platform.DepthTestFunction;
import com.mojang.blaze3d.shaders.UniformType;
import com.mojang.blaze3d.vertex.DefaultVertexFormat;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.blaze3d.vertex.VertexFormat;
import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.renderer.rendertype.RenderSetup;
import net.minecraft.client.renderer.rendertype.RenderType;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import org.joml.Vector3f;
import ru.vexor.visual.mixin.RenderTypeAccessor;

import java.util.ArrayList;
import java.util.List;

public final class RenderUtil
{
    private static final int VERTEX_BUFFER = 2 * 1024 * 1024;

    private static final RenderType ESP_LINES = createLines();
    private static final RenderType WORLD_FACES = createFaces();

    private static final List<Segment> SEGMENTS = new ArrayList<>();
    private static final List<Face> FACES = new ArrayList<>();

    private RenderUtil()
    {
    }

    private static RenderType createLines()
    {
        RenderPipeline pipeline = RenderPipeline.builder()
                .withLocation("vexorvisual_esp_lines")
                .withVertexShader("core/rendertype_lines")
                .withFragmentShader("core/rendertype_lines")
                .withUniform("DynamicTransforms", UniformType.UNIFORM_BUFFER)
                .withUniform("Projection", UniformType.UNIFORM_BUFFER)
                .withUniform("Fog", UniformType.UNIFORM_BUFFER)
                .withBlend(BlendFunction.TRANSLUCENT)
                .withCull(false)
                .withDepthTestFunction(DepthTestFunction.NO_DEPTH_TEST)
                .withDepthWrite(false)
                .withVertexFormat(DefaultVertexFormat.POSITION_COLOR_NORMAL_LINE_WIDTH, VertexFormat.Mode.LINES)
                .build();
        RenderSetup setup = RenderSetup.builder(pipeline).bufferSize(VERTEX_BUFFER).createRenderSetup();
        return RenderTypeAccessor.vexor$create("vexorvisual_esp_lines", setup);
    }

    private static RenderType createFaces()
    {
        RenderPipeline pipeline = RenderPipeline.builder()
                .withLocation("vexorvisual_world_faces")
                .withVertexShader("core/position_color")
                .withFragmentShader("core/position_color")
                .withUniform("DynamicTransforms", UniformType.UNIFORM_BUFFER)
                .withUniform("Projection", UniformType.UNIFORM_BUFFER)
                .withBlend(BlendFunction.TRANSLUCENT)
                .withCull(false)
                .withDepthTestFunction(DepthTestFunction.LEQUAL_DEPTH_TEST)
                .withDepthWrite(false)
                .withVertexFormat(DefaultVertexFormat.POSITION_COLOR, VertexFormat.Mode.QUADS)
                .build();
        RenderSetup setup = RenderSetup.builder(pipeline).bufferSize(VERTEX_BUFFER).createRenderSetup();
        return RenderTypeAccessor.vexor$create("vexorvisual_world_faces", setup);
    }

    public static Vec3 camera(WorldRenderContext context)
    {
        if (context.worldState() != null && context.worldState().cameraRenderState != null
                && context.worldState().cameraRenderState.pos != null)
        {
            return context.worldState().cameraRenderState.pos;
        }
        return Vec3.ZERO;
    }

    public static boolean ready(WorldRenderContext context)
    {
        return context != null && context.matrices() != null;
    }

    public static void beginFrame()
    {
        SEGMENTS.clear();
        FACES.clear();
    }

    public static void line(WorldRenderContext context, Vec3 from, Vec3 to, int color, float width)
    {
        if (!ready(context))
        {
            return;
        }
        SEGMENTS.add(new Segment(from, to, color, width));
    }

    public static void box(WorldRenderContext context, AABB box, int color, float width)
    {
        if (!ready(context))
        {
            return;
        }
        Vec3 a = new Vec3(box.minX, box.minY, box.minZ);
        Vec3 b = new Vec3(box.maxX, box.minY, box.minZ);
        Vec3 c = new Vec3(box.maxX, box.minY, box.maxZ);
        Vec3 d = new Vec3(box.minX, box.minY, box.maxZ);
        Vec3 e = new Vec3(box.minX, box.maxY, box.minZ);
        Vec3 f = new Vec3(box.maxX, box.maxY, box.minZ);
        Vec3 g = new Vec3(box.maxX, box.maxY, box.maxZ);
        Vec3 h = new Vec3(box.minX, box.maxY, box.maxZ);
        line(context, a, b, color, width);
        line(context, b, c, color, width);
        line(context, c, d, color, width);
        line(context, d, a, color, width);
        line(context, e, f, color, width);
        line(context, f, g, color, width);
        line(context, g, h, color, width);
        line(context, h, e, color, width);
        line(context, a, e, color, width);
        line(context, b, f, color, width);
        line(context, c, g, color, width);
        line(context, d, h, color, width);
    }

    public static void quad(WorldRenderContext context, Vec3 a, Vec3 b, Vec3 c, Vec3 d, int color)
    {
        if (!ready(context))
        {
            return;
        }
        FACES.add(new Face(a, b, c, d, color));
    }

    public static void flush(WorldRenderContext context)
    {
        if (context == null || context.matrices() == null)
        {
            SEGMENTS.clear();
            FACES.clear();
            return;
        }
        if (SEGMENTS.isEmpty() && FACES.isEmpty())
        {
            return;
        }
        Vec3 camera = camera(context);
        if (!SEGMENTS.isEmpty())
        {
            List<Segment> segments = new ArrayList<>(SEGMENTS);
            SEGMENTS.clear();
            context.commandQueue().submitCustomGeometry(context.matrices(), ESP_LINES, (pose, buffer) ->
            {
                for (Segment segment : segments)
                {
                    addLine(buffer, pose,
                            segment.from.x - camera.x, segment.from.y - camera.y, segment.from.z - camera.z,
                            segment.to.x - camera.x, segment.to.y - camera.y, segment.to.z - camera.z,
                            segment.color, segment.width);
                }
            });
        }
        if (!FACES.isEmpty())
        {
            List<Face> faces = new ArrayList<>(FACES);
            FACES.clear();
            context.commandQueue().submitCustomGeometry(context.matrices(), WORLD_FACES, (pose, buffer) ->
            {
                for (Face face : faces)
                {
                    addFaceVertex(buffer, pose, face.a, camera, face.color);
                    addFaceVertex(buffer, pose, face.b, camera, face.color);
                    addFaceVertex(buffer, pose, face.c, camera, face.color);
                    addFaceVertex(buffer, pose, face.d, camera, face.color);
                }
            });
        }
    }

    private static void addFaceVertex(VertexConsumer consumer, PoseStack.Pose pose, Vec3 point, Vec3 camera, int color)
    {
        consumer.addVertex(pose,
                (float) (point.x - camera.x), (float) (point.y - camera.y), (float) (point.z - camera.z))
                .setColor(color);
    }

    public static void addLine(VertexConsumer consumer, PoseStack.Pose pose,
                               double x1, double y1, double z1, double x2, double y2, double z2,
                               int color, float width)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double dz = z2 - z1;
        double length = Math.sqrt(dx * dx + dy * dy + dz * dz);
        if (length < 1.0E-6D)
        {
            return;
        }
        Vector3f normal = new Vector3f((float) (dx / length), (float) (dy / length), (float) (dz / length));
        consumer.addVertex(pose, (float) x1, (float) y1, (float) z1)
                .setColor(color)
                .setNormal(pose, normal)
                .setLineWidth(width);
        consumer.addVertex(pose, (float) x2, (float) y2, (float) z2)
                .setColor(color)
                .setNormal(pose, normal)
                .setLineWidth(width);
    }

    public static int distanceColor(double distance, int nearColor, int farColor, double maxDistance)
    {
        double t = Math.max(0.0D, Math.min(1.0D, distance / Math.max(1.0D, maxDistance)));
        return Draw.mix(nearColor, farColor, (float) t);
    }

    private record Segment(Vec3 from, Vec3 to, int color, float width)
    {
    }

    private record Face(Vec3 a, Vec3 b, Vec3 c, Vec3 d, int color)
    {
    }
}
