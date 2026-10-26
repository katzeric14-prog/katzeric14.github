package ru.vexor.visual.module.impl.world;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.Holder;
import net.minecraft.resources.ResourceKey;
import net.minecraft.world.level.biome.Biome;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.util.Draw;

import java.util.HashMap;
import java.util.Map;

public final class BiomeDisplay extends HudModule
{
    private static final Map<String, String> NAMES = new HashMap<>();

    static
    {
        NAMES.put("plains", "Равнины");
        NAMES.put("forest", "Лес");
        NAMES.put("birch_forest", "Берёзовый лес");
        NAMES.put("old_growth_birch_forest", "Старый берёзовый лес");
        NAMES.put("dark_forest", "Тёмный лес");
        NAMES.put("flower_forest", "Цветочный лес");
        NAMES.put("taiga", "Тайга");
        NAMES.put("snowy_taiga", "Снежная тайга");
        NAMES.put("old_growth_pine_taiga", "Старая сосновая тайга");
        NAMES.put("old_growth_spruce_taiga", "Старая еловая тайга");
        NAMES.put("jungle", "Джунгли");
        NAMES.put("sparse_jungle", "Редкие джунгли");
        NAMES.put("bamboo_jungle", "Бамбуковые джунгли");
        NAMES.put("desert", "Пустыня");
        NAMES.put("savanna", "Саванна");
        NAMES.put("savanna_plateau", "Саванное плато");
        NAMES.put("badlands", "Бэдлендс");
        NAMES.put("swamp", "Болото");
        NAMES.put("mangrove_swamp", "Мангровое болото");
        NAMES.put("ocean", "Океан");
        NAMES.put("deep_ocean", "Глубокий океан");
        NAMES.put("warm_ocean", "Тёплый океан");
        NAMES.put("lukewarm_ocean", "Умеренный океан");
        NAMES.put("cold_ocean", "Холодный океан");
        NAMES.put("frozen_ocean", "Ледяной океан");
        NAMES.put("river", "Река");
        NAMES.put("beach", "Пляж");
        NAMES.put("snowy_plains", "Снежные равнины");
        NAMES.put("snowy_beach", "Снежный пляж");
        NAMES.put("ice_spikes", "Ледяные шипы");
        NAMES.put("mountains", "Горы");
        NAMES.put("stony_peaks", "Каменные пики");
        NAMES.put("windswept_hills", "Холмы");
        NAMES.put("meadow", "Луг");
        NAMES.put("cherry_grove", "Вишнёвая роща");
        NAMES.put("grove", "Роща");
        NAMES.put("sunflower_plains", "Подсолнуховые равнины");
        NAMES.put("stony_shore", "Каменистый берег");
        NAMES.put("mushroom_fields", "Грибные поля");
        NAMES.put("lush_caves", "Пышные пещеры");
        NAMES.put("dripstone_caves", "Капельные пещеры");
        NAMES.put("deep_dark", "Глубинная тьма");
        NAMES.put("nether_wastes", "Нижний мир");
        NAMES.put("soul_sand_valley", "Долина песка душ");
        NAMES.put("crimson_forest", "Багровый лес");
        NAMES.put("warped_forest", "Искажённый лес");
        NAMES.put("basalt_deltas", "Базальтовые дельты");
        NAMES.put("the_end", "Энд");
        NAMES.put("end_highlands", "Эндовые нагорья");
        NAMES.put("end_midlands", "Эндовые равнины");
        NAMES.put("small_end_islands", "Малые острова Энда");
        NAMES.put("end_barrens", "Пустоши Энда");
    }

    public BiomeDisplay()
    {
        super("Biome Display", "Название биома", -1, Category.WORLD, Anchor.TOP_LEFT);
        setDisplayName("Биом");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null || Minecraft.getInstance().level == null)
        {
            return 0;
        }
        Holder<Biome> holder = Minecraft.getInstance().level.getBiome(player.blockPosition());
        String path = holder.unwrapKey().map(ResourceKey::identifier).map(key -> key.getPath()).orElse("");
        String name = NAMES.get(path);
        if (name == null)
        {
            name = path.isEmpty() ? "Неизвестно" : pretty(path);
        }
        String label = "Биом";
        int width = Draw.width(label) + Draw.width(name) + 14;
        card(graphics, x, y, width, 15);
        Draw.text(graphics, font(), label, x + 4, y + 4, mutedColor());
        Draw.textRight(graphics, font(), name, x + width - 4, y + 4, accent());
        return 15;
    }

    private static String pretty(String path)
    {
        String text = path.replace('_', ' ');
        if (text.isEmpty())
        {
            return text;
        }
        return Character.toUpperCase(text.charAt(0)) + text.substring(1);
    }
}
