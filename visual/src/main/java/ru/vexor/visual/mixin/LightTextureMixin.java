package ru.vexor.visual.mixin;

import net.minecraft.client.OptionInstance;
import net.minecraft.client.renderer.LightTexture;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Redirect;
import ru.vexor.visual.util.GammaState;

@Mixin(LightTexture.class)
public abstract class LightTextureMixin
{
    @Redirect(method = "updateLightTexture",
            at = @At(value = "INVOKE", target = "Lnet/minecraft/client/OptionInstance;get()Ljava/lang/Object;", ordinal = 2))
    private Object vexor$gamma(OptionInstance<?> instance)
    {
        Object original = instance.get();
        if (GammaState.active())
        {
            return GammaState.value();
        }
        return original;
    }
}
