package ru.vexor.visual.mixin;

import net.minecraft.client.gui.Gui;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.DeltaTracker;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.module.impl.visuals.CustomCrosshair;

@Mixin(Gui.class)
public abstract class GuiMixin
{
    @Inject(method = "renderCrosshair", at = @At("HEAD"), cancellable = true)
    private void vexor$renderCustomCrosshair(GuiGraphics graphics, DeltaTracker delta, CallbackInfo ci)
    {
        if (ModuleManager.get().isActive("Custom Crosshair"))
        {
            CustomCrosshair.render(graphics);
            ci.cancel();
        }
    }
}
