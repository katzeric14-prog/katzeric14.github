package ru.vexor.visual.module.impl.visuals;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

public final class HitColor extends WorldModule
{
    private static HitColor instance;
    private static int targetId = -1;
    private static long until;

    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFF2D55));
    private final IntSetting duration = add(new IntSetting("Длительность", 450, 100, 2000, 50));
    private final BoolSetting redOverlay = add(new BoolSetting("Красный оверлей", true));

    public HitColor()
    {
        super("Hit Color", "Подсветка цели при ударе", -1, Category.VISUALS);
        setDisplayName("Цвет удара");
        instance = this;
    }

    public static void onAttack(Entity target)
    {
        if (instance == null || target == null || !ModuleManager.get().isActive("Hit Color"))
        {
            return;
        }
        targetId = target.getId();
        until = System.currentTimeMillis() + instance.duration.getValue();
    }

    public static boolean isRecentlyHit(Entity entity)
    {
        return instance != null
                && entity != null
                && entity.getId() == targetId
                && System.currentTimeMillis() < until
                && instance.redOverlay.getValue()
                && ModuleManager.get().isActive("Hit Color");
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        if (System.currentTimeMillis() >= until || !RenderUtil.ready(context))
        {
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.level == null)
        {
            return;
        }
        float partialTick = Minecraft.getInstance().getDeltaTracker().getGameTimeDeltaPartialTick(true);
        for (Entity entity : minecraft.level.entitiesForRendering())
        {
            if (entity.getId() != targetId)
            {
                continue;
            }
            Vec3 current = entity.getPosition(partialTick);
            Vec3 delta = current.subtract(entity.position());
            RenderUtil.box(context, entity.getBoundingBox().move(delta),
                    color.getValue() == null ? 0xFFFF2D55 : color.getValue(), 3.0F);
            return;
        }
    }
}
