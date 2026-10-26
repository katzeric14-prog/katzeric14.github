package ru.vexor.visual.config;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import net.fabricmc.loader.api.FabricLoader;
import ru.vexor.visual.VexorVisual;
import ru.vexor.visual.module.ModuleManager;

import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;

public final class ConfigManager
{
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final long SAVE_DELAY_MS = 1500L;

    private static boolean dirty;
    private static long lastSave;
    private static boolean loading;

    private ConfigManager()
    {
    }

    public static Path getPath()
    {
        return FabricLoader.getInstance().getConfigDir().resolve("vexorvisual.json");
    }

    public static void markDirty()
    {
        if (loading)
        {
            return;
        }
        dirty = true;
    }

    public static void load()
    {
        Path path = getPath();
        if (!Files.isRegularFile(path))
        {
            return;
        }

        loading = true;
        try (Reader reader = Files.newBufferedReader(path, StandardCharsets.UTF_8))
        {
            JsonObject root = JsonParser.parseReader(reader).getAsJsonObject();
            ModuleManager manager = ModuleManager.get();
            if (root.has("visual"))
            {
                manager.setVisualEnabled(root.get("visual").getAsBoolean(), false);
            }
            if (root.has("gui") && root.get("gui").isJsonObject())
            {
                manager.getGuiConfig().fromJson(root.getAsJsonObject("gui"));
            }
            if (root.has("modules") && root.get("modules").isJsonObject())
            {
                JsonObject modules = root.getAsJsonObject("modules");
                for (var entry : modules.entrySet())
                {
                    var module = manager.getModule(entry.getKey());
                    if (module != null && entry.getValue().isJsonObject())
                    {
                        module.fromJson(entry.getValue().getAsJsonObject());
                    }
                }
            }
        }
        catch (Exception e)
        {
            VexorVisual.LOGGER.error("Не удалось прочитать конфигурацию " + path, e);
        }
        finally
        {
            loading = false;
            dirty = false;
        }
    }

    public static void tick()
    {
        if (dirty && System.currentTimeMillis() - lastSave >= SAVE_DELAY_MS)
        {
            save();
        }
    }

    public static void save()
    {
        Path path = getPath();
        try
        {
            Files.createDirectories(path.getParent());
            JsonObject root = new JsonObject();
            ModuleManager manager = ModuleManager.get();
            root.addProperty("visual", manager.isVisualEnabled());
            root.add("gui", manager.getGuiConfig().toJson());
            root.add("modules", manager.modulesToJson());

            Path temp = path.resolveSibling(path.getFileName() + ".tmp");
            try (Writer writer = Files.newBufferedWriter(temp, StandardCharsets.UTF_8))
            {
                GSON.toJson(root, writer);
            }
            try
            {
                Files.move(temp, path, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE);
            }
            catch (IOException e)
            {
                Files.move(temp, path, StandardCopyOption.REPLACE_EXISTING);
            }
            dirty = false;
            lastSave = System.currentTimeMillis();
        }
        catch (Exception e)
        {
            VexorVisual.LOGGER.error("Не удалось сохранить конфигурацию " + path, e);
        }
    }
}
