package ru.vexor.visual.mixin;

import com.mojang.blaze3d.vertex.PoseStack;
import net.minecraft.client.Camera;
import net.minecraft.client.renderer.GameRenderer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.module.impl.player.Zoom;

@Mixin(GameRenderer.class)
public abstract class GameRendererMixin
{
    @Inject(method = "getFov", at = @At("RETURN"), cancellable = true)
    private void vexor$zoomFov(Camera camera, float partialTick, boolean useFovSetting,
                               CallbackInfoReturnable<Float> cir)
    {
        float factor = Zoom.getZoomFactor();
        if (factor > 1.0F)
        {
            cir.setReturnValue(Math.max(1.0F, cir.getReturnValue() / factor));
        }
    }

    @Inject(method = "bobHurt", at = @At("HEAD"), cancellable = true)
    private void vexor$noHurtCamera(PoseStack poseStack, float partialTick, CallbackInfo ci)
    {
        if (ModuleManager.get().isActive("No Hurt Camera"))
        {
            ci.cancel();
        }
    }
}
