package ru.vexor.visual.mixin;

import net.minecraft.client.MouseHandler;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.util.ViewState;

@Mixin(MouseHandler.class)
public abstract class MouseHandlerMixin
{
    @Shadow
    private double accumulatedDX;

    @Shadow
    private double accumulatedDY;

    @Inject(method = "turnPlayer", at = @At("HEAD"), cancellable = true)
    private void vexor$customViewTurn(double sensitivityScale, CallbackInfo ci)
    {
        if (ViewState.applyMouse(accumulatedDX, accumulatedDY))
        {
            ci.cancel();
        }
    }
}
