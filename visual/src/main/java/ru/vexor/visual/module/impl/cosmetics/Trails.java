package ru.vexor.visual.module.impl.cosmetics;

import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.particles.DustParticleOptions;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.IntSetting;

import java.util.Random;

public final class Trails extends Module
{
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF8B5CF6));
    private final DoubleSetting size = add(new DoubleSetting("Размер", 0.8D, 0.2D, 2.0D, 0.1D, 1));
    private final IntSetting density = add(new IntSetting("Плотность", 2, 1, 8, 1));
    private final BoolSetting movingOnly = add(new BoolSetting("Только в движении", true));

    private final Random random = new Random();
    private Vec3 lastPosition;

    public Trails()
    {
        super("Trails", "След из частиц за персонажем", Category.COSMETICS);
        setDisplayName("След");
    }

    @Override
    public void onDisable()
    {
        lastPosition = null;
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
        Vec3 current = player.position();
        if (lastPosition == null)
        {
            lastPosition = current;
            return;
        }
        double distance = current.distanceTo(lastPosition);
        boolean moving = distance > 0.03D;
        if (movingOnly.getValue() && !moving)
        {
            lastPosition = current;
            return;
        }

        Integer value = color.getValue();
        int rgb = value == null ? 0x8B5CF6 : (value & 0xFFFFFF);
        DustParticleOptions particle = new DustParticleOptions(rgb, (float) (double) size.getValue());
        for (int i = 0; i < density.getValue(); i++)
        {
            double t = random.nextDouble();
            double x = lastPosition.x + (current.x - lastPosition.x) * t + (random.nextDouble() - 0.5D) * 0.25D;
            double y = lastPosition.y + (current.y - lastPosition.y) * t + 0.08D + random.nextDouble() * 0.15D;
            double z = lastPosition.z + (current.z - lastPosition.z) * t + (random.nextDouble() - 0.5D) * 0.25D;
            minecraft.level.addParticle(particle, x, y, z, 0.0D, 0.0D, 0.0D);
        }
        lastPosition = current;
    }
}
