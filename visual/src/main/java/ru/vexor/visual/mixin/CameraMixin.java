package ru.vexor.visual.mixin;

import net.minecraft.client.Camera;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;
import ru.vexor.visual.module.impl.player.CameraDistance;
import ru.vexor.visual.util.ViewState;

@Mixin(Camera.class)
public abstract class CameraMixin
{
    @Shadow
    protected abstract void setRotation(float yaw, float pitch);

    @Shadow
    protected abstract void setPosition(double x, double y, double z);

    @Inject(method = "setup", at = @At("TAIL"))
    private void vexor$applyCustomView(Level level, Entity entity, boolean detached, boolean thirdPersonReverse,
                                       float partialTick, CallbackInfo ci)
    {
        Camera camera = (Camera) (Object) this;
        float[] rotation = ViewState.overrideRotation(camera.yRot(), camera.xRot());
        if (rotation != null)
        {
            setRotation(rotation[1], rotation[0]);
        }
        Vec3 position = ViewState.overridePosition(camera.position());
        if (position != null)
        {
            setPosition(position.x, position.y, position.z);
        }
    }

    @Inject(method = "getMaxZoom", at = @At("RETURN"), cancellable = true)
    private void vexor$cameraDistance(float requestedDistance, CallbackInfoReturnable<Float> cir)
    {
        float multiplier = CameraDistance.getMultiplier();
        if (multiplier != 1.0F)
        {
            cir.setReturnValue(cir.getReturnValue() * multiplier);
        }
    }
}
