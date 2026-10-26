package ru.vexor.visual.util;

import net.fabricmc.fabric.api.resource.v1.ResourceLoader;
import net.fabricmc.fabric.api.resource.v1.pack.PackActivationType;
import net.fabricmc.loader.api.FabricLoader;
import net.fabricmc.loader.api.ModContainer;
import net.minecraft.client.Minecraft;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.Identifier;
import net.minecraft.server.packs.repository.PackRepository;
import ru.vexor.visual.VexorVisual;

import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Registers the resource packs shipped inside the mod jar and keeps the player's
 * selected pack list in sync with the item skin settings.
 */
public final class BuiltinPackManager
{
    private static final Map<String, String> PACKS = new LinkedHashMap<>();

    static
    {
        PACKS.put("vexorvisual:totem_neon", "Vexor: Неоновый тотем");
        PACKS.put("vexorvisual:totem_gold", "Vexor: Золотой тотем");
        PACKS.put("vexorvisual:totem_crimson", "Vexor: Багровый тотем");
        PACKS.put("vexorvisual:totem_frost", "Vexor: Ледяной тотем");
        PACKS.put("vexorvisual:totem_emerald", "Vexor: Изумрудный тотем");
        PACKS.put("vexorvisual:sword_electro", "Vexor: Электро-мечи");
        PACKS.put("vexorvisual:sword_neon", "Vexor: Неоновые мечи");
        PACKS.put("vexorvisual:sword_crimson", "Vexor: Багровые мечи");
        PACKS.put("vexorvisual:sword_gold", "Vexor: Золотые мечи");
        PACKS.put("vexorvisual:sword_emerald", "Vexor: Изумрудные мечи");
    }

    private static Set<String> desired = Collections.emptySet();
    private static boolean registered;
    private static boolean dirty;

    private BuiltinPackManager()
    {
    }

    public static String id(String path)
    {
        return VexorVisual.MOD_ID + ":" + path;
    }

    public static void register()
    {
        if (registered)
        {
            return;
        }
        registered = true;
        ModContainer container = FabricLoader.getInstance().getModContainer(VexorVisual.MOD_ID).orElse(null);
        if (container == null)
        {
            VexorVisual.LOGGER.warn("Не найден контейнер мода, встроенные скины отключены");
            return;
        }
        for (Map.Entry<String, String> entry : PACKS.entrySet())
        {
            Identifier identifier = Identifier.tryParse(entry.getKey());
            if (identifier == null)
            {
                continue;
            }
            if (!ResourceLoader.registerBuiltinPack(identifier, container,
                    Component.literal(entry.getValue()), PackActivationType.NORMAL))
            {
                VexorVisual.LOGGER.warn("Не удалось зарегистрировать встроенный набор " + entry.getKey());
            }
        }
        dirty = true;
    }

    public static void setDesired(Collection<String> ids)
    {
        LinkedHashSet<String> next = new LinkedHashSet<>();
        for (String id : ids)
        {
            if (PACKS.containsKey(id))
            {
                next.add(id);
            }
        }
        if (!next.equals(desired))
        {
            desired = next;
            dirty = true;
        }
    }

    public static void clear()
    {
        setDesired(Collections.emptyList());
    }

    public static void tick()
    {
        if (!dirty || !registered)
        {
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.screen != null || minecraft.level == null)
        {
            return;
        }
        dirty = false;
        apply(minecraft);
    }

    private static void apply(Minecraft minecraft)
    {
        PackRepository repository = minecraft.getResourcePackRepository();
        // The repository caches its pack list, so discover the builtin packs again
        // before validating the identifiers we are about to select.
        repository.reload();

        List<String> wanted = new ArrayList<>();
        for (String id : desired)
        {
            if (repository.isAvailable(id))
            {
                wanted.add(id);
            }
        }

        List<String> current = minecraft.options.resourcePacks;
        List<String> next = new ArrayList<>(current.size() + wanted.size());
        for (String id : current)
        {
            if (!PACKS.containsKey(id))
            {
                next.add(id);
            }
        }
        next.addAll(wanted);
        if (next.equals(current))
        {
            return;
        }

        current.clear();
        current.addAll(next);
        minecraft.options.loadSelectedResourcePacks(repository);
        minecraft.reloadResourcePacks();
    }
}
