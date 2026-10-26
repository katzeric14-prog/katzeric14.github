package ru.vexor.visual.mixin;

import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.world.level.Level;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.module.impl.visuals.TimeChanger;

@Mixin(Level.class)
public abstract class LevelMixin
{
    @Inject(method = "getDayTime", at = @At("HEAD"), cancellable = true)
    private void vexor$customTime(CallbackInfoReturnable<Long> cir)
    {
        if (!((Object) this instanceof ClientLevel))
        {
            return;
        }
        if (ModuleManager.get().isActive("Time Changer"))
        {
            cir.setReturnValue(TimeChanger.getTime());
        }
    }

    @Inject(method = "getRainLevel", at = @At("HEAD"), cancellable = true)
    private void vexor$noRain(float partialTick, CallbackInfoReturnable<Float> cir)
    {
        if ((Object) this instanceof ClientLevel && ModuleManager.get().isActive("Weather"))
        {
            cir.setReturnValue(0.0F);
        }
    }

    @Inject(method = "getThunderLevel", at = @At("HEAD"), cancellable = true)
    private void vexor$noThunder(float partialTick, CallbackInfoReturnable<Float> cir)
    {
        if ((Object) this instanceof ClientLevel && ModuleManager.get().isActive("Weather"))
        {
            cir.setReturnValue(0.0F);
        }
    }
}
