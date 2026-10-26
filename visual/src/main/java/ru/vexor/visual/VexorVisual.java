package ru.vexor.visual;

import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.api.EnvType;
import net.fabricmc.api.Environment;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientLifecycleEvents;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.rendering.v1.hud.HudElementRegistry;
import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderEvents;
import net.minecraft.resources.Identifier;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.util.BuiltinPackManager;
import ru.vexor.visual.util.Projection;
import ru.vexor.visual.util.RenderUtil;

@Environment(EnvType.CLIENT)
public final class VexorVisual implements ClientModInitializer
{
    public static final String MOD_ID = "vexorvisual";
    public static final Logger LOGGER = LoggerFactory.getLogger("VexorVisual");

    @Override
    public void onInitializeClient()
    {
        BuiltinPackManager.register();
        ModuleManager manager = ModuleManager.get();
        manager.init();

        HudElementRegistry.addLast(Identifier.fromNamespaceAndPath(MOD_ID, "hud"),
                (graphics, delta) -> manager.renderHud(graphics, delta));

        WorldRenderEvents.AFTER_ENTITIES.register(context ->
        {
            Projection.capture(context);
            RenderUtil.beginFrame();
            manager.renderWorld(context);
            RenderUtil.flush(context);
        });

        WorldRenderEvents.BEFORE_BLOCK_OUTLINE.register((context, state) -> manager.renderBlockOutline(context, state));

        ClientTickEvents.END_CLIENT_TICK.register(manager::tick);
        ClientTickEvents.END_CLIENT_TICK.register(minecraft -> BuiltinPackManager.tick());

        ClientLifecycleEvents.CLIENT_STOPPING.register(minecraft -> manager.saveNow());

        LOGGER.info("Vexor Visual загружен");
    }
}
