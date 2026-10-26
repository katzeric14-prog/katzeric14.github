package ru.vexor.visual.mixin;

import net.minecraft.client.player.LocalPlayer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.util.ViewState;

@Mixin(LocalPlayer.class)
public abstract class LocalPlayerMixin
{
    @Inject(method = "aiStep", at = @At("HEAD"), cancellable = true)
    private void vexor$freezeForFreecam(CallbackInfo ci)
    {
        if (ViewState.freecamActive)
        {
            ci.cancel();
        }
    }
}
