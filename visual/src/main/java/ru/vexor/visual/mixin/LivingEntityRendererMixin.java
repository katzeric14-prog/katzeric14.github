package ru.vexor.visual.mixin;

import net.minecraft.client.renderer.entity.LivingEntityRenderer;
import net.minecraft.client.renderer.entity.state.LivingEntityRenderState;
import net.minecraft.world.entity.LivingEntity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.module.impl.visuals.HitColor;

@Mixin(LivingEntityRenderer.class)
public abstract class LivingEntityRendererMixin
{
    @Inject(method = "extractRenderState", at = @At("TAIL"))
    private void vexor$hitTint(LivingEntity entity, LivingEntityRenderState state, float partialTick, CallbackInfo ci)
    {
        if (HitColor.isRecentlyHit(entity))
        {
            state.hasRedOverlay = true;
        }
    }
}
