package ru.vexor.visual.module.impl.cosmetics;

import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.particles.DustParticleOptions;
import net.minecraft.core.particles.ParticleOptions;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.tags.ItemTags;
import net.minecraft.world.entity.HumanoidArm;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.setting.IntSetting;

import java.util.Random;

public final class SwordSparks extends Module
{
    public enum Target
    {
        AUTO("Авто"),
        SWORD("Меч"),
        TOTEM("Тотем");

        private final String label;

        Target(String label)
        {
            this.label = label;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private final EnumSetting<Target> target = add(new EnumSetting<>("Цель", Target.AUTO));
    private final IntSetting rate = add(new IntSetting("Частиц в секунду", 16, 4, 80, 2));
    private final DoubleSetting spread = add(new DoubleSetting("Разброс", 0.08D, 0.02D, 0.4D, 0.02D, 2));
    private final ColorSetting color = add(new ColorSetting("Цвет искр", 0xFF6FE7FF));

    private final Random random = new Random();
    private double pending;

    public SwordSparks()
    {
        super("Sword Sparks", "Электро-искры вокруг меча и тотема", Category.COSMETICS);
        setDisplayName("Искры предметов");
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
        ItemStack stack = player.getMainHandItem();
        boolean sword = stack.is(ItemTags.SWORDS);
        boolean totem = stack.is(Items.TOTEM_OF_UNDYING);
        boolean active = switch (target.getValue())
        {
            case SWORD -> sword;
            case TOTEM -> totem;
            case AUTO -> sword || totem;
        };
        if (!active)
        {
            pending = 0.0D;
            return;
        }

        pending += rate.getValue() / 20.0D;
        while (pending >= 1.0D)
        {
            pending -= 1.0D;
            spawn(minecraft, player, sword, totem);
        }
    }

    private void spawn(Minecraft minecraft, LocalPlayer player, boolean sword, boolean totem)
    {
        float partialTick = minecraft.getDeltaTracker().getGameTimeDeltaPartialTick(true);
        Vec3 look = player.getViewVector(partialTick);
        Vec3 right = new Vec3(-look.z, 0.0D, look.x).normalize();
        double side = player.getMainArm() == HumanoidArm.RIGHT ? 1.0D : -1.0D;
        Vec3 hand = player.getEyePosition(partialTick)
                .add(look.scale(0.7D))
                .add(right.scale(0.48D * side))
                .add(0.0D, -0.45D, 0.0D);

        double s = spread.getValue();
        double x = hand.x + (random.nextDouble() - 0.5D) * 2.0D * s;
        double y = hand.y + (random.nextDouble() - 0.5D) * 2.0D * s;
        double z = hand.z + (random.nextDouble() - 0.5D) * 2.0D * s;

        if (totem && !sword)
        {
            minecraft.level.addParticle(ParticleTypes.TOTEM_OF_UNDYING, x, y, z,
                    (random.nextDouble() - 0.5D) * 0.02D, 0.015D, (random.nextDouble() - 0.5D) * 0.02D);
            return;
        }
        Integer rgb = color.getValue();
        ParticleOptions spark = new DustParticleOptions((rgb == null ? 0xFF6FE7FF : rgb) & 0xFFFFFF, 0.55F);
        minecraft.level.addParticle(spark, x, y, z,
                (random.nextDouble() - 0.5D) * 0.03D, (random.nextDouble() - 0.2D) * 0.03D,
                (random.nextDouble() - 0.5D) * 0.03D);
        if (random.nextFloat() < 0.35F)
        {
            minecraft.level.addParticle(ParticleTypes.ELECTRIC_SPARK, x, y, z, 0.0D, 0.01D, 0.0D);
        }
    }
}
