package ru.vexor.visual.module;

import com.google.gson.JsonObject;
import com.mojang.blaze3d.platform.InputConstants;
import net.fabricmc.fabric.api.client.rendering.v1.world.WorldRenderContext;
import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.renderer.state.BlockOutlineRenderState;
import net.minecraft.network.chat.Component;
import ru.vexor.visual.VexorVisual;
import ru.vexor.visual.config.ConfigManager;
import ru.vexor.visual.config.GuiConfig;
import ru.vexor.visual.gui.ClickGuiScreen;
import ru.vexor.visual.module.impl.cosmetics.CustomCape;
import ru.vexor.visual.module.impl.cosmetics.ItemSkins;
import ru.vexor.visual.module.impl.cosmetics.ParticleEffects;
import ru.vexor.visual.module.impl.cosmetics.SwordSparks;
import ru.vexor.visual.module.impl.cosmetics.Trails;
import ru.vexor.visual.module.impl.cosmetics.Wings;
import ru.vexor.visual.module.impl.hud.ArmorStatus;
import ru.vexor.visual.module.impl.hud.CoordinatesHud;
import ru.vexor.visual.module.impl.hud.CpsHud;
import ru.vexor.visual.module.impl.hud.FpsHud;
import ru.vexor.visual.module.impl.hud.Keystrokes;
import ru.vexor.visual.module.impl.hud.PingHud;
import ru.vexor.visual.module.impl.hud.PotionStatus;
import ru.vexor.visual.module.impl.hud.SpeedHud;
import ru.vexor.visual.module.impl.hud.TargetHud;
import ru.vexor.visual.module.impl.hud.Watermark;
import ru.vexor.visual.module.impl.player.CameraDistance;
import ru.vexor.visual.module.impl.player.Freelook;
import ru.vexor.visual.module.impl.player.Perspective;
import ru.vexor.visual.module.impl.player.SmoothCamera;
import ru.vexor.visual.module.impl.player.Zoom;
import ru.vexor.visual.module.impl.render.BlockEsp;
import ru.vexor.visual.module.impl.render.ChestEsp;
import ru.vexor.visual.module.impl.render.EntityEsp;
import ru.vexor.visual.module.impl.render.Freecam;
import ru.vexor.visual.module.impl.render.Fullbright;
import ru.vexor.visual.module.impl.render.ItemEsp;
import ru.vexor.visual.module.impl.render.NameTags;
import ru.vexor.visual.module.impl.render.PlayerEsp;
import ru.vexor.visual.module.impl.render.StorageEsp;
import ru.vexor.visual.module.impl.render.Tracers;
import ru.vexor.visual.module.impl.visuals.CustomCrosshair;
import ru.vexor.visual.module.impl.visuals.CustomSky;
import ru.vexor.visual.module.impl.visuals.DamageTint;
import ru.vexor.visual.module.impl.visuals.HandAnimations;
import ru.vexor.visual.module.impl.visuals.HitColor;
import ru.vexor.visual.module.impl.visuals.NoHurtCamera;
import ru.vexor.visual.module.impl.visuals.TimeChanger;
import ru.vexor.visual.module.impl.visuals.ViewBobbingModule;
import ru.vexor.visual.module.impl.visuals.WeatherChanger;
import ru.vexor.visual.module.impl.world.BiomeDisplay;
import ru.vexor.visual.module.impl.world.BlockOverlay;
import ru.vexor.visual.module.impl.world.Brightness;
import ru.vexor.visual.module.impl.world.ChunkBorders;
import ru.vexor.visual.module.impl.world.DirectionHud;

import java.util.ArrayList;
import java.util.Collection;
import java.util.IdentityHashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

public final class ModuleManager
{
    private static final ModuleManager INSTANCE = new ModuleManager();

    private final Map<String, Module> modules = new LinkedHashMap<>();
    private final Map<Module, Boolean> keyStates = new IdentityHashMap<>();
    private final GuiConfig guiConfig = new GuiConfig();

    private boolean visualEnabled = true;
    private boolean initialized;
    private boolean configLoaded;
    private boolean openKeyDown;
    private boolean toggleKeyDown;

    private ModuleManager()
    {
    }

    public static ModuleManager get()
    {
        return INSTANCE;
    }

    public void init()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;

        register(new Fullbright());
        register(new EntityEsp());
        register(new PlayerEsp());
        register(new Tracers());
        register(new NameTags());
        register(new ItemEsp());
        register(new ChestEsp());
        register(new BlockEsp());
        register(new StorageEsp());
        register(new Freecam());

        register(new CustomCrosshair());
        register(new HitColor());
        register(new DamageTint());
        register(new HandAnimations());
        register(new ViewBobbingModule());
        register(new NoHurtCamera());
        register(new WeatherChanger());
        register(new TimeChanger());
        register(new CustomSky());

        register(new Watermark());
        register(new FpsHud());
        register(new CpsHud());
        register(new PingHud());
        register(new CoordinatesHud());
        register(new SpeedHud());
        register(new Keystrokes());
        register(new ArmorStatus());
        register(new PotionStatus());
        register(new TargetHud());

        register(new Perspective());
        register(new Zoom());
        register(new Freelook());
        register(new CameraDistance());
        register(new SmoothCamera());

        register(new Brightness());
        register(new ChunkBorders());
        register(new BlockOverlay());
        register(new DirectionHud());
        register(new BiomeDisplay());

        register(new CustomCape());
        register(new Wings());
        register(new ParticleEffects());
        register(new Trails());
        register(new ItemSkins());
        register(new SwordSparks());
    }

    private void loadConfig()
    {
        if (configLoaded)
        {
            return;
        }
        configLoaded = true;
        ConfigManager.load();
        if (!visualEnabled)
        {
            suspendEnabledModules();
        }
    }

    private void register(Module module)
    {
        modules.put(module.getName(), module);
        keyStates.put(module, Boolean.FALSE);
    }

    public Collection<Module> getModules()
    {
        return modules.values();
    }

    public Module getModule(String name)
    {
        return modules.get(name);
    }

    public List<Module> getModules(Category category)
    {
        List<Module> result = new ArrayList<>();
        for (Module module : modules.values())
        {
            if (module.getCategory() == category)
            {
                result.add(module);
            }
        }
        return result;
    }

    public List<Module> search(String query)
    {
        String needle = query.trim().toLowerCase(Locale.ROOT);
        List<Module> result = new ArrayList<>();
        if (needle.isEmpty())
        {
            return result;
        }
        for (Module module : modules.values())
        {
            String haystack = (module.getName() + " " + module.getDisplayName() + " "
                    + module.getDescription()).toLowerCase(Locale.ROOT);
            if (haystack.contains(needle))
            {
                result.add(module);
            }
        }
        return result;
    }

    public GuiConfig getGuiConfig()
    {
        return guiConfig;
    }

    public boolean isVisualEnabled()
    {
        return visualEnabled;
    }

    public void setVisualEnabled(boolean value, boolean notify)
    {
        if (visualEnabled == value)
        {
            return;
        }
        visualEnabled = value;
        if (value)
        {
            resumeEnabledModules();
        }
        else
        {
            suspendEnabledModules();
        }
        if (notify)
        {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.player != null)
            {
                minecraft.player.displayClientMessage(Component.literal(
                        value ? "Vexor Visual включён" : "Vexor Visual выключен"), true);
            }
        }
        ConfigManager.markDirty();
    }

    public boolean isActive(String moduleName)
    {
        if (!visualEnabled)
        {
            return false;
        }
        Module module = modules.get(moduleName);
        return module != null && module.isEnabled();
    }

    private void suspendEnabledModules()
    {
        for (Module module : modules.values())
        {
            if (!module.isEnabled())
            {
                continue;
            }
            try
            {
                module.onDisable();
            }
            catch (Throwable t)
            {
                VexorVisual.LOGGER.error("Ошибка отключения модуля " + module.getName(), t);
            }
        }
    }

    private void resumeEnabledModules()
    {
        for (Module module : modules.values())
        {
            if (!module.isEnabled())
            {
                continue;
            }
            try
            {
                module.onEnable();
            }
            catch (Throwable t)
            {
                VexorVisual.LOGGER.error("Ошибка включения модуля " + module.getName(), t);
            }
        }
    }

    public void tick(Minecraft minecraft)
    {
        loadConfig();
        ConfigManager.tick();

        handleMenuKeys(minecraft);

        if (!visualEnabled || minecraft.player == null || minecraft.screen != null)
        {
            resetKeyStates(minecraft);
            return;
        }

        for (Module module : modules.values())
        {
            int key = module.getKeyBind().getValue() == null ? -1 : module.getKeyBind().getValue();
            boolean down = key > 0 && InputConstants.isKeyDown(minecraft.getWindow(), key);
            boolean wasDown = keyStates.get(module);
            keyStates.put(module, down);
            if (down && !wasDown)
            {
                module.toggle();
                minecraft.player.displayClientMessage(Component.literal(
                        module.getDisplayName() + (module.isEnabled() ? ": вкл" : ": выкл")), true);
            }
            if (module.isEnabled())
            {
                try
                {
                    module.onTick();
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка тика модуля " + module.getName(), t);
                    module.setEnabled(false);
                }
            }
        }
    }

    private void handleMenuKeys(Minecraft minecraft)
    {
        int openKey = guiConfig.getOpenKey();
        boolean openDown = openKey > 0 && InputConstants.isKeyDown(minecraft.getWindow(), openKey);
        if (openDown && !openKeyDown && minecraft.player != null && minecraft.screen == null)
        {
            minecraft.setScreen(new ClickGuiScreen());
        }
        openKeyDown = openDown;

        int toggleKey = guiConfig.getToggleKey();
        boolean toggleDown = toggleKey > 0 && InputConstants.isKeyDown(minecraft.getWindow(), toggleKey);
        if (toggleDown && !toggleKeyDown)
        {
            setVisualEnabled(!visualEnabled, true);
        }
        toggleKeyDown = toggleDown;
    }

    private void resetKeyStates(Minecraft minecraft)
    {
        for (Module module : modules.values())
        {
            int key = module.getKeyBind().getValue() == null ? -1 : module.getKeyBind().getValue();
            keyStates.put(module, key > 0 && InputConstants.isKeyDown(minecraft.getWindow(), key));
        }
        int openKey = guiConfig.getOpenKey();
        int toggleKey = guiConfig.getToggleKey();
        openKeyDown = openKey > 0 && InputConstants.isKeyDown(minecraft.getWindow(), openKey);
        toggleKeyDown = toggleKey > 0 && InputConstants.isKeyDown(minecraft.getWindow(), toggleKey);
    }

    public void renderHud(GuiGraphics graphics, DeltaTracker delta)
    {
        if (!visualEnabled)
        {
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.options.hideGui || minecraft.player == null)
        {
            return;
        }

        for (Module module : modules.values())
        {
            if (module instanceof HudModule && module.isEnabled() && ((HudModule) module).getAnchor() == HudModule.Anchor.OVERLAY)
            {
                try
                {
                    ((HudModule) module).render(graphics, delta, 0, 0);
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка отрисовки HUD " + module.getName(), t);
                }
            }
        }

        for (Module module : modules.values())
        {
            if (module instanceof HudModule && module.isEnabled() && ((HudModule) module).getAnchor() == HudModule.Anchor.CENTER)
            {
                try
                {
                    ((HudModule) module).render(graphics, delta, graphics.guiWidth() / 2, graphics.guiHeight() / 2 + 30);
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка отрисовки HUD " + module.getName(), t);
                }
            }
        }

        int y = 6;
        for (Module module : modules.values())
        {
            if (module instanceof HudModule && module.isEnabled() && ((HudModule) module).getAnchor() == HudModule.Anchor.TOP_LEFT)
            {
                try
                {
                    y += ((HudModule) module).render(graphics, delta, 6, y) + 3;
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка отрисовки HUD " + module.getName(), t);
                }
            }
        }
    }

    public void renderWorld(WorldRenderContext context)
    {
        if (!visualEnabled)
        {
            return;
        }
        for (Module module : modules.values())
        {
            if (module instanceof WorldModule && module.isEnabled())
            {
                try
                {
                    ((WorldModule) module).renderWorld(context);
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка отрисовки в мире " + module.getName(), t);
                }
            }
        }
    }

    public boolean renderBlockOutline(WorldRenderContext context, BlockOutlineRenderState state)
    {
        if (!visualEnabled)
        {
            return true;
        }
        boolean drawDefault = true;
        for (Module module : modules.values())
        {
            if (module instanceof WorldModule && module.isEnabled())
            {
                try
                {
                    if (!((WorldModule) module).onBlockOutline(context, state))
                    {
                        drawDefault = false;
                    }
                }
                catch (Throwable t)
                {
                    VexorVisual.LOGGER.error("Ошибка контура блока " + module.getName(), t);
                }
            }
        }
        return drawDefault;
    }

    public JsonObject modulesToJson()
    {
        JsonObject object = new JsonObject();
        for (Module module : modules.values())
        {
            object.add(module.getName(), module.toJson());
        }
        return object;
    }

    public void saveNow()
    {
        ConfigManager.save();
    }
}
