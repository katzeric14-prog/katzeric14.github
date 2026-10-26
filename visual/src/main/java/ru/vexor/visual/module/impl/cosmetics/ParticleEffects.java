package ru.vexor.visual.module.impl.cosmetics;

import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.particles.ParticleOptions;
import net.minecraft.core.particles.ParticleTypes;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.setting.IntSetting;

import java.util.Random;

public final class ParticleEffects extends Module
{
    public enum Effect
    {
        END_ROD("Эндер-род", ParticleTypes.END_ROD),
        FLAME("Пламя", ParticleTypes.FLAME),
        HEART("Сердечки", ParticleTypes.HEART),
        CRIT("Критический", ParticleTypes.CRIT),
        SOUL("Души", ParticleTypes.SOUL_FIRE_FLAME);

        private final String label;
        private final ParticleOptions particle;

        Effect(String label, ParticleOptions particle)
        {
            this.label = label;
            this.particle = particle;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private final EnumSetting<Effect> effect = add(new EnumSetting<>("Эффект", Effect.END_ROD));
    private final IntSetting rate = add(new IntSetting("Частиц в секунду", 8, 1, 40, 1));
    private final DoubleSetting radius = add(new DoubleSetting("Радиус", 0.8D, 0.3D, 2.0D, 0.1D, 1));

    private final Random random = new Random();
    private double pending;

    public ParticleEffects()
    {
        super("Particle Effects", "Частицы вокруг персонажа", Category.COSMETICS);
        setDisplayName("Частицы");
    }

    @Override
    public void onDisable()
    {
        pending = 0.0D;
    }

    @Override
    public void onTick()
    {
        Minecraft minecraft = Minecraft.getInstance();
        LocalPlayer player = minecraft.player;
        if (player == null || minecraft.level == null)
        {
            return;
        }
        pending += rate.getValue() / 20.0D;
        while (pending >= 1.0D)
        {
            pending -= 1.0D;
            double r = radius.getValue();
            double ox = (random.nextDouble() - 0.5D) * 2.0D * r;
            double oz = (random.nextDouble() - 0.5D) * 2.0D * r;
            double oy = random.nextDouble() * 1.8D;
            minecraft.level.addParticle(effect.getValue().particle,
                    player.getX() + ox, player.getY() + oy, player.getZ() + oz,
                    0.0D, 0.01D, 0.0D);
        }
    }
}
