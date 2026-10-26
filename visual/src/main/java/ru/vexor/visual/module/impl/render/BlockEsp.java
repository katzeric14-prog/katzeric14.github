package ru.vexor.visual.module.impl.render;

import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import ru.vexor.visual.module.WorldModule;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.RenderUtil;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public final class BlockEsp extends WorldModule
{
    public enum Target
    {
        DIAMOND("Алмазы"),
        IRON("Железо"),
        GOLD("Золото"),
        EMERALD("Изумруды"),
        DEBRIS("Древние обломки"),
        ALL("Все руды");

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

    private static final Map<Block, Integer> ORE_COLORS = new HashMap<>();

    static
    {
        ORE_COLORS.put(Blocks.DIAMOND_ORE, 0xFF4FE3FF);
        ORE_COLORS.put(Blocks.DEEPSLATE_DIAMOND_ORE, 0xFF4FE3FF);
        ORE_COLORS.put(Blocks.IRON_ORE, 0xFFFFC9A8);
        ORE_COLORS.put(Blocks.DEEPSLATE_IRON_ORE, 0xFFFFC9A8);
        ORE_COLORS.put(Blocks.GOLD_ORE, 0xFFFFD54A);
        ORE_COLORS.put(Blocks.DEEPSLATE_GOLD_ORE, 0xFFFFD54A);
        ORE_COLORS.put(Blocks.EMERALD_ORE, 0xFF4CFF8A);
        ORE_COLORS.put(Blocks.DEEPSLATE_EMERALD_ORE, 0xFF4CFF8A);
        ORE_COLORS.put(Blocks.ANCIENT_DEBRIS, 0xFFC08B5C);
        ORE_COLORS.put(Blocks.COAL_ORE, 0xFFB9B9B9);
        ORE_COLORS.put(Blocks.DEEPSLATE_COAL_ORE, 0xFFB9B9B9);
        ORE_COLORS.put(Blocks.COPPER_ORE, 0xFFFF9A62);
        ORE_COLORS.put(Blocks.DEEPSLATE_COPPER_ORE, 0xFFFF9A62);
        ORE_COLORS.put(Blocks.LAPIS_ORE, 0xFF3B6BFF);
        ORE_COLORS.put(Blocks.DEEPSLATE_LAPIS_ORE, 0xFF3B6BFF);
        ORE_COLORS.put(Blocks.REDSTONE_ORE, 0xFFFF3B3B);
        ORE_COLORS.put(Blocks.DEEPSLATE_REDSTONE_ORE, 0xFFFF3B3B);
        ORE_COLORS.put(Blocks.NETHER_QUARTZ_ORE, 0xFFF2E8E1);
        ORE_COLORS.put(Blocks.NETHER_GOLD_ORE, 0xFFFFD54A);
    }

    private final EnumSetting<Target> target = add(new EnumSetting<>("Блок", Target.DIAMOND));
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFF4FE3FF));
    private final IntSetting radius = add(new IntSetting("Радиус", 24, 8, 48, 4));

    private final List<BlockPos> found = new ArrayList<>();
    private int cooldown;

    public BlockEsp()
    {
        super("Block Esp", "Поиск руды вокруг игрока", -1);
        setDisplayName("ESP блоков");
    }

    @Override
    public void onTick()
    {
        if (--cooldown > 0)
        {
            return;
        }
        cooldown = 40;
        scan();
    }

    @Override
    public void onEnable()
    {
        cooldown = 0;
        found.clear();
    }

    @Override
    public void onDisable()
    {
        found.clear();
    }

    private void scan()
    {
        found.clear();
        Minecraft minecraft = Minecraft.getInstance();
        LocalPlayer player = minecraft.player;
        Level level = minecraft.level;
        if (player == null || level == null)
        {
            return;
        }
        Target mode = target.getValue();
        int r = radius.getValue();
        BlockPos center = player.blockPosition();
        int minY = Math.max(level.getMinY(), center.getY() - 20);
        int maxY = Math.min(level.getMaxY(), center.getY() + 20);
        BlockPos from = new BlockPos(center.getX() - r, minY, center.getZ() - r);
        BlockPos to = new BlockPos(center.getX() + r, maxY, center.getZ() + r);
        for (BlockPos pos : BlockPos.betweenClosed(from, to))
        {
            if (found.size() >= 512)
            {
                break;
            }
            if (!level.isLoaded(pos))
            {
                continue;
            }
            BlockState state = level.getBlockState(pos);
            if (matches(state, mode))
            {
                found.add(pos.immutable());
            }
        }
    }

    private static boolean matches(BlockState state, Target mode)
    {
        switch (mode)
        {
            case DIAMOND:
                return state.is(Blocks.DIAMOND_ORE) || state.is(Blocks.DEEPSLATE_DIAMOND_ORE);
            case IRON:
                return state.is(Blocks.IRON_ORE) || state.is(Blocks.DEEPSLATE_IRON_ORE);
            case GOLD:
                return state.is(Blocks.GOLD_ORE) || state.is(Blocks.DEEPSLATE_GOLD_ORE) || state.is(Blocks.NETHER_GOLD_ORE);
            case EMERALD:
                return state.is(Blocks.EMERALD_ORE) || state.is(Blocks.DEEPSLATE_EMERALD_ORE);
            case DEBRIS:
                return state.is(Blocks.ANCIENT_DEBRIS);
            default:
                return ORE_COLORS.containsKey(state.getBlock());
        }
    }

    @Override
    public void renderWorld(WorldRenderContext context)
    {
        if (!RenderUtil.ready(context) || found.isEmpty())
        {
            return;
        }
        Vec3 camera = RenderUtil.camera(context);
        int fallback = EspUtil.color(color);
        for (BlockPos pos : found)
        {
            if (Vec3.atCenterOf(pos).distanceToSqr(camera) > 4096.0D)
            {
                continue;
            }
            int rgb = fallback;
            if (target.getValue() == Target.ALL)
            {
                Minecraft minecraft = Minecraft.getInstance();
                if (minecraft.level != null)
                {
                    Integer mapped = ORE_COLORS.get(minecraft.level.getBlockState(pos).getBlock());
                    if (mapped != null)
                    {
                        rgb = mapped;
                    }
                }
            }
            RenderUtil.box(context, new AABB(pos).inflate(0.002D), rgb, 2.0F);
        }
    }
}
