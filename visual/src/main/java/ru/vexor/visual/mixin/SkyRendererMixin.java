package ru.vexor.visual.mixin;

import net.minecraft.client.Camera;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.SkyRenderer;
import net.minecraft.client.renderer.state.SkyRenderState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.module.impl.visuals.CustomSky;

@Mixin(SkyRenderer.class)
public abstract class SkyRendererMixin
{
    @Inject(method = "extractRenderState", at = @At("TAIL"))
    private void vexor$customSky(ClientLevel level, float partialTick, Camera camera, SkyRenderState state,
                                 CallbackInfo ci)
    {
        if (ModuleManager.get().isActive("Custom Sky"))
        {
            state.skyColor = CustomSky.getSkyColor();
        }
    }
}
