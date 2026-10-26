package ru.vexor.visual.mixin;

import net.minecraft.client.multiplayer.MultiPlayerGameMode;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.player.Player;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import ru.vexor.visual.module.impl.visuals.HitColor;

@Mixin(MultiPlayerGameMode.class)
public abstract class MultiPlayerGameModeMixin
{
    @Inject(method = "attack", at = @At("HEAD"))
    private void vexor$rememberHit(Player player, Entity target, CallbackInfo ci)
    {
        HitColor.onAttack(target);
    }
}
