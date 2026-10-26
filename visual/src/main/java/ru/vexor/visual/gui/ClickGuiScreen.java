package ru.vexor.visual.gui;

import com.mojang.blaze3d.platform.InputConstants;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.client.input.CharacterEvent;
import net.minecraft.client.input.KeyEvent;
import net.minecraft.client.input.MouseButtonEvent;
import net.minecraft.network.chat.Component;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.config.ConfigManager;
import ru.vexor.visual.config.GuiConfig;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.setting.KeySetting;
import ru.vexor.visual.setting.Setting;
import ru.vexor.visual.util.Draw;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

public final class ClickGuiScreen extends Screen
{
    private static final int PANEL_WIDTH = 540;
    private static final int PANEL_HEIGHT = 330;
    private static final int HEADER = 44;
    private static final int SIDEBAR_X = 8;
    private static final int SIDEBAR_WIDTH = 104;
    private static final int CONTENT_X = SIDEBAR_X + SIDEBAR_WIDTH + 8;
    private static final int SETTINGS_WIDTH = 196;
    private static final int SETTINGS_X = PANEL_WIDTH - SETTINGS_WIDTH - 8;
    private static final int LIST_WIDTH = SETTINGS_X - CONTENT_X - 8;
    private static final int SIDEBAR_TOP = HEADER + 8;
    private static final int CATEGORY_TOP = HEADER + 14;
    private static final int CATEGORY_STEP = 29;
    private static final int CATEGORY_HEIGHT = 24;
    private static final int SEARCH_Y = HEADER + 10;
    private static final int SEARCH_HEIGHT = 24;
    private static final int LIST_TOP = HEADER + 42;
    private static final int LIST_BOTTOM = PANEL_HEIGHT - 26;
    private static final int ROW_HEIGHT = 31;

    private static final int TEXT = 0xFFEDEAF8;
    private static final int MUTED = 0xFF9B94B8;
    private static final int ROW = 0xD9151022;
    private static final int ROW_HOVER = 0xFF1E1732;
    private static final int FIELD = 0xFF161125;
    private static final int TRACK = 0xFF2B2442;
    private static final int CARD_FILL = 0xFF110D1D;
    private static final int OFF = 0xFF3A3352;

    private final ModuleManager manager = ModuleManager.get();
    private final GuiConfig config = manager.getGuiConfig();
    private final Map<String, Float> switchAnim = new HashMap<>();
    private final List<Row> rows = new ArrayList<>();

    private Category category = Category.RENDER;
    private Module selectedModule;
    private String search = "";
    private boolean searchFocused;
    private String rebindConfig;
    private Module rebindModule;
    private KeySetting rebindSetting;
    private boolean draggingWindow;
    private double dragWindowOffsetX;
    private double dragWindowOffsetY;
    private Setting<?> expandedColor;
    private float listScroll;
    private float settingsScroll;
    private int interfaceButtonsY;
    private Setting<?> dragSetting;
    private int dragColorIndex = -1;
    private String dragConfig;
    private float dragTrackX;
    private float dragTrackY;
    private float dragTrackWidth;
    private float openAnim;
    private long lastFrame = System.nanoTime();
    private double localMouseX;
    private double localMouseY;
    private float panelScreenX;
    private float panelScreenY;
    private float panelScale = 1.0F;
    private int categoryHover = -1;
    private boolean closeHover;
    private boolean globalHover;

    public ClickGuiScreen()
    {
        super(Component.literal("Vexor Visual"));
    }

    @Override
    protected void init()
    {
        openAnim = config.isAnimations() ? 0.0F : 1.0F;
        lastFrame = System.nanoTime();
    }

    @Override
    public boolean isPauseScreen()
    {
        return false;
    }

    @Override
    public boolean isInGameUi()
    {
        return minecraft != null && minecraft.level != null;
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick)
    {
        long now = System.nanoTime();
        float delta = Math.min(0.05F, (now - lastFrame) / 1_000_000_000.0F);
        lastFrame = now;
        openAnim = config.isAnimations() ? Math.min(1.0F, openAnim + delta * 7.0F) : 1.0F;
        float ease = Draw.easeOut(openAnim);

        float baseScale = baseScale();
        float scale = baseScale * (0.97F + 0.03F * ease);
        float width = PANEL_WIDTH * baseScale;
        float height = PANEL_HEIGHT * baseScale;
        float targetX = config.getPanelX() >= 0 ? config.getPanelX() : (graphics.guiWidth() - width) / 2.0F;
        float targetY = config.getPanelY() >= 0 ? config.getPanelY() : (graphics.guiHeight() - height) / 2.0F;
        targetX = (float) Draw.clamp(targetX, 0.0D, Math.max(0.0D, graphics.guiWidth() - width));
        targetY = (float) Draw.clamp(targetY, 0.0D, Math.max(0.0D, graphics.guiHeight() - height));
        panelScreenX = targetX + (width - PANEL_WIDTH * scale) / 2.0F;
        panelScreenY = targetY + (height - PANEL_HEIGHT * scale) / 2.0F;
        panelScale = scale;

        localMouseX = (mouseX - panelScreenX) / scale;
        localMouseY = (mouseY - panelScreenY) / scale;

        graphics.pose().pushMatrix();
        graphics.pose().translate(panelScreenX, panelScreenY);
        graphics.pose().scale(scale, scale);

        float fade = (float) config.getOpacity() * (0.25F + 0.75F * ease);
        drawPanel(graphics, fade, ease);

        graphics.pose().popMatrix();
        super.render(graphics, mouseX, mouseY, partialTick);
    }

    private void drawPanel(GuiGraphics graphics, float fade, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        float glow = 0.35F + 0.65F * ease;

        Draw.shadow(graphics, 0, 2, PANEL_WIDTH, PANEL_HEIGHT, 12,
                Draw.withAlpha(0xFF000000, Math.round(190 * fade)), 5);
        Draw.glow(graphics, 0, 0, PANEL_WIDTH, PANEL_HEIGHT, 12,
                Draw.withAlpha(accent, Math.round(48 * glow)), 5);

        Draw.roundedRect(graphics, 0, 0, PANEL_WIDTH, PANEL_HEIGHT, 12, Draw.alpha(0xFF0B0814, fade));
        Draw.roundedRect(graphics, 0, 0, PANEL_WIDTH, 140, 12, Draw.withAlpha(accent, Math.round(13 * ease)));
        Draw.outline(graphics, 0, 0, PANEL_WIDTH, PANEL_HEIGHT, 12, Draw.withAlpha(accent, Math.round(120 * ease)));
        Draw.outline(graphics, 1, 1, PANEL_WIDTH - 2, PANEL_HEIGHT - 2, 11, 0x0DFFFFFF);

        int headerFill = Draw.alpha(0xFF171129, fade);
        Draw.roundedRect(graphics, 0, 0, PANEL_WIDTH, HEADER, 12, headerFill);
        graphics.fill(0, HEADER - 12, PANEL_WIDTH, HEADER, headerFill);
        Draw.roundedRect(graphics, 4, 2, PANEL_WIDTH - 8, HEADER - 6, 10, 0x0AFFFFFF);
        Draw.horizontalGradient(graphics, 10, HEADER - 1, (PANEL_WIDTH - 20) / 2, 1,
                0x00FFFFFF & accent, Draw.withAlpha(accent, Math.round(150 * ease)));
        Draw.horizontalGradient(graphics, 10 + (PANEL_WIDTH - 20) / 2, HEADER - 1, (PANEL_WIDTH - 20) / 2, 1,
                Draw.withAlpha(accent, Math.round(150 * ease)), 0x00FFFFFF & accent);

        Draw.glow(graphics, 13, 10, 24, 24, 8, Draw.withAlpha(accent, Math.round(70 * ease)), 3);
        Draw.roundedRect(graphics, 13, 10, 24, 24, 8,
                Draw.alpha(Draw.mix(accent, 0xFFFFFFFF, 0.12F), fade));
        Draw.roundedRect(graphics, 13, 10, 24, 12, 8, 0x22FFFFFF);
        Draw.displayCentered(graphics, "V", 25, 13, Draw.alpha(0xFFFFFFFF, fade));

        Draw.displayGradient(graphics, "VISUAL", 45, 9, light, 0xFFFFFFFF, true);
        Draw.text(graphics, font, "Vexor Visual · Fabric 1.21.11", 45, 28, Draw.alpha(MUTED, ease));

        boolean visual = manager.isVisualEnabled();
        float global = switchValue("global", visual, ease);
        int pillW = 116;
        int pillX = PANEL_WIDTH - pillW - 36;
        int pillY = 13;
        globalHover = Draw.inside(localMouseX, localMouseY, pillX, pillY, pillW, 20);
        Draw.roundedRect(graphics, pillX, pillY, pillW, 20, 10,
                Draw.alpha(visual ? Draw.withAlpha(accent, 42) : 0xFF1C1630, ease));
        Draw.outline(graphics, pillX, pillY, pillW, 20, 10,
                Draw.withAlpha(visual ? accent : 0xFF352E4C, Math.round((visual ? 90 : 60) * ease)));
        Draw.text(graphics, font, "Визуал", pillX + 10, pillY + 5, visual ? TEXT : Draw.alpha(MUTED, ease));
        Draw.textRight(graphics, font, visual ? "вкл" : "выкл", pillX + pillW - 38, pillY + 5,
                Draw.alpha(visual ? light : MUTED, ease));
        drawSwitch(graphics, pillX + pillW - 34, pillY + 4, 28, 12, global, visual ? accent : OFF);

        closeHover = Draw.inside(localMouseX, localMouseY, PANEL_WIDTH - 30, 11, 20, 22);
        Draw.roundedRect(graphics, PANEL_WIDTH - 29, 12, 18, 18, 9,
                closeHover ? 0xFF3A1D28 : 0);
        Draw.textCentered(graphics, font, "×", PANEL_WIDTH - 20, 16,
                closeHover ? 0xFFFF6B80 : Draw.alpha(MUTED, ease));

        drawSidebar(graphics, fade, ease);

        if (category == Category.SETTINGS)
        {
            drawInterfaceSettings(graphics, fade, ease);
        }
        else
        {
            drawModuleList(graphics, fade, ease);
            drawModuleSettings(graphics, fade, ease);
        }

        graphics.fill(12, PANEL_HEIGHT - 24, PANEL_WIDTH - 12, PANEL_HEIGHT - 23, 0x14FFFFFF);
        Draw.roundedRect(graphics, 12, PANEL_HEIGHT - 16, 3, 3, 1, Draw.withAlpha(accent, Math.round(220 * ease)));
        Draw.text(graphics, font, "Vexor Visual 1.0", 20, PANEL_HEIGHT - 17, Draw.alpha(MUTED, ease));

        if (rebindConfig != null || rebindModule != null || rebindSetting != null)
        {
            Draw.textRight(graphics, font, "Нажмите клавишу · Esc — отмена",
                    PANEL_WIDTH - 12, PANEL_HEIGHT - 17, Draw.alpha(0xFFFFC857, ease));
        }
        else
        {
            String hint = "закрыть меню";
            int hintWidth = Draw.width(hint);
            Draw.textRight(graphics, font, hint, PANEL_WIDTH - 12, PANEL_HEIGHT - 17, Draw.alpha(MUTED, ease));
            int capW = Draw.width("RSHIFT") + 10;
            int capX = PANEL_WIDTH - 20 - hintWidth - capW;
            Draw.roundedRect(graphics, capX, PANEL_HEIGHT - 19, capW, 14, 4, Draw.alpha(FIELD, ease));
            Draw.outline(graphics, capX, PANEL_HEIGHT - 19, capW, 14, 4, 0x1AFFFFFF);
            Draw.textCentered(graphics, font, "RSHIFT", capX + capW / 2, PANEL_HEIGHT - 15, TEXT);
        }
    }

    private void drawSidebar(GuiGraphics graphics, float fade, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        int cardHeight = LIST_BOTTOM - SIDEBAR_TOP;
        Draw.roundedRect(graphics, SIDEBAR_X, SIDEBAR_TOP, SIDEBAR_WIDTH, cardHeight, 10,
                Draw.alpha(CARD_FILL, fade));
        Draw.outline(graphics, SIDEBAR_X, SIDEBAR_TOP, SIDEBAR_WIDTH, cardHeight, 10, 0x14FFFFFF);

        Category[] values = Category.values();
        categoryHover = -1;
        int y = CATEGORY_TOP;
        for (int i = 0; i < values.length; i++)
        {
            Category value = values[i];
            boolean active = value == category;
            boolean hover = Draw.inside(localMouseX, localMouseY, SIDEBAR_X + 4, y, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT);
            if (hover)
            {
                categoryHover = i;
            }
            if (active)
            {
                Draw.roundedRect(graphics, SIDEBAR_X + 4, y, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT, 7,
                        Draw.withAlpha(accent, Math.round(52 * ease)));
                Draw.roundedRect(graphics, SIDEBAR_X + 4, y, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT / 2, 7,
                        Draw.withAlpha(0xFFFFFFFF, 9));
                Draw.outline(graphics, SIDEBAR_X + 4, y, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT, 7,
                        Draw.withAlpha(accent, Math.round(110 * ease)));
                Draw.roundedRect(graphics, SIDEBAR_X + 5, y + 5, 2, CATEGORY_HEIGHT - 10, 1, accent);
            }
            else if (hover)
            {
                Draw.roundedRect(graphics, SIDEBAR_X + 4, y, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT, 7, 0x14FFFFFF);
            }

            Draw.roundedRect(graphics, SIDEBAR_X + 12, y + 10, 4, 4, 2, active ? light : OFF);
            Draw.text(graphics, font, value.getDisplayName(), SIDEBAR_X + 22, y + 7,
                    active ? TEXT : Draw.alpha(MUTED, ease));
            y += CATEGORY_STEP;
        }
    }

    private void drawModuleList(GuiGraphics graphics, float fade, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        Draw.roundedRect(graphics, CONTENT_X, SEARCH_Y, LIST_WIDTH, SEARCH_HEIGHT, 8,
                Draw.alpha(FIELD, fade));
        if (searchFocused)
        {
            Draw.glow(graphics, CONTENT_X, SEARCH_Y, LIST_WIDTH, SEARCH_HEIGHT, 8,
                    Draw.withAlpha(accent, 45), 2);
            Draw.outline(graphics, CONTENT_X, SEARCH_Y, LIST_WIDTH, SEARCH_HEIGHT, 8,
                    Draw.withAlpha(accent, 200));
        }
        else
        {
            Draw.outline(graphics, CONTENT_X, SEARCH_Y, LIST_WIDTH, SEARCH_HEIGHT, 8, 0x14FFFFFF);
        }
        int iconColor = searchFocused ? accent : Draw.alpha(MUTED, ease);
        Draw.roundedRect(graphics, CONTENT_X + 9, SEARCH_Y + 8, 7, 7, 3, iconColor);
        Draw.roundedRect(graphics, CONTENT_X + 10, SEARCH_Y + 9, 5, 5, 2, Draw.alpha(FIELD, fade));
        graphics.fill(CONTENT_X + 15, SEARCH_Y + 14, CONTENT_X + 18, SEARCH_Y + 16, iconColor);
        graphics.fill(CONTENT_X + 16, SEARCH_Y + 13, CONTENT_X + 18, SEARCH_Y + 15, iconColor);

        String shown = search.isEmpty() ? "Поиск модулей..." : search;
        Draw.text(graphics, font, Draw.trim(font, shown, LIST_WIDTH - 48),
                CONTENT_X + 24, SEARCH_Y + 7, search.isEmpty() ? Draw.alpha(MUTED, ease) : TEXT);
        if (searchFocused && (System.currentTimeMillis() / 500L) % 2L == 0L)
        {
            int cursor = CONTENT_X + 24 + Draw.width(Draw.trim(font, shown, LIST_WIDTH - 48));
            graphics.fill(cursor + 1, SEARCH_Y + 6, cursor + 2, SEARCH_Y + 17, accent);
        }
        if (!search.isEmpty())
        {
            Draw.roundedRect(graphics, CONTENT_X + LIST_WIDTH - 17, SEARCH_Y + 7, 10, 10, 5, 0x14FFFFFF);
            Draw.textCentered(graphics, font, "×", CONTENT_X + LIST_WIDTH - 12, SEARCH_Y + 5, MUTED);
        }

        List<Module> modules = visibleModules();
        float contentHeight = modules.size() * ROW_HEIGHT;
        float viewHeight = LIST_BOTTOM - LIST_TOP;
        float maxScroll = Math.max(0.0F, contentHeight - viewHeight);
        listScroll = (float) Draw.clamp(listScroll, 0.0D, maxScroll);

        enableScissor(graphics, CONTENT_X - 3, LIST_TOP - 4, LIST_WIDTH + 6, (int) (viewHeight + 4));
        int y = LIST_TOP;
        for (Module module : modules)
        {
            float rowY = y - listScroll;
            if (rowY + ROW_HEIGHT < LIST_TOP - 4 || rowY > LIST_BOTTOM + 4)
            {
                y += ROW_HEIGHT;
                continue;
            }
            boolean hover = Draw.inside(localMouseX, localMouseY, CONTENT_X, rowY, LIST_WIDTH, ROW_HEIGHT - 2)
                    && localMouseY >= LIST_TOP - 2 && localMouseY <= LIST_BOTTOM;
            boolean selected = module == selectedModule;
            int rowHeight = ROW_HEIGHT - 2;
            Draw.roundedRect(graphics, CONTENT_X, (int) rowY, LIST_WIDTH, rowHeight, 7,
                    Draw.alpha(selected ? Draw.withAlpha(accent, 50) : (hover ? ROW_HOVER : ROW), ease));
            if (selected)
            {
                Draw.outline(graphics, CONTENT_X, (int) rowY, LIST_WIDTH, rowHeight, 7,
                        Draw.withAlpha(accent, 110));
                Draw.roundedRect(graphics, CONTENT_X + 1, (int) rowY + 6, 2, rowHeight - 12, 1, light);
            }
            else if (hover)
            {
                Draw.outline(graphics, CONTENT_X, (int) rowY, LIST_WIDTH, rowHeight, 7,
                        Draw.withAlpha(accent, 55));
            }

            boolean on = module.isEnabled();
            Draw.roundedRect(graphics, CONTENT_X + 9, (int) rowY + 9, 4, 4, 2, on ? light : OFF);
            String name = Draw.trim(font, module.getDisplayName(), LIST_WIDTH - 78);
            Draw.text(graphics, font, name, CONTENT_X + 18, (int) rowY + 6, on ? TEXT : 0xFFD8D3EA);
            String sub = module.getKeyBind().isBound()
                    ? module.getKeyBind().displayValue()
                    : Draw.trim(font, module.getDescription(), LIST_WIDTH - 78);
            Draw.text(graphics, font, Draw.trim(font, sub, LIST_WIDTH - 78), CONTENT_X + 18, (int) rowY + 18,
                    Draw.alpha(MUTED, ease));

            float toggle = switchValue(module.getName(), on, ease);
            drawSwitch(graphics, CONTENT_X + LIST_WIDTH - 38, (int) rowY + 8, 28, 13, toggle,
                    on ? accent : OFF);

            y += ROW_HEIGHT;
        }
        disableScissor(graphics);

        if (maxScroll > 0.0F)
        {
            float barHeight = viewHeight * viewHeight / contentHeight;
            float barY = LIST_TOP + (viewHeight - barHeight) * (listScroll / maxScroll);
            Draw.roundedRect(graphics, CONTENT_X + LIST_WIDTH + 3, LIST_TOP, 2, (int) viewHeight, 1, 0x0DFFFFFF);
            Draw.roundedRect(graphics, CONTENT_X + LIST_WIDTH + 3, (int) barY, 2, Math.max(10, (int) barHeight), 1,
                    Draw.withAlpha(accent, 140));
        }

        if (modules.isEmpty())
        {
            Draw.roundedRect(graphics, CONTENT_X + LIST_WIDTH / 2 - 9, LIST_TOP + 10, 18, 18, 9, 0x14FFFFFF);
            Draw.textCentered(graphics, font, "?", CONTENT_X + LIST_WIDTH / 2, LIST_TOP + 14, MUTED);
            Draw.textCentered(graphics, font, "Ничего не найдено", CONTENT_X + LIST_WIDTH / 2,
                    LIST_TOP + 36, Draw.alpha(MUTED, ease));
        }
    }

    private void drawModuleSettings(GuiGraphics graphics, float fade, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        int x = SETTINGS_X;
        int y = HEADER + 10;
        int height = LIST_BOTTOM - y;
        Draw.roundedRect(graphics, x, y, SETTINGS_WIDTH, height, 10, Draw.alpha(CARD_FILL, fade));
        Draw.outline(graphics, x, y, SETTINGS_WIDTH, height, 10, 0x14FFFFFF);
        rows.clear();

        if (selectedModule == null)
        {
            Draw.roundedRect(graphics, x + SETTINGS_WIDTH / 2 - 11, y + 28, 22, 22, 7,
                    Draw.withAlpha(accent, Math.round(34 * ease)));
            Draw.outline(graphics, x + SETTINGS_WIDTH / 2 - 11, y + 28, 22, 22, 7,
                    Draw.withAlpha(accent, Math.round(90 * ease)));
            Draw.roundedRect(graphics, x + SETTINGS_WIDTH / 2 - 3, y + 37, 6, 2, 1, Draw.withAlpha(light, 220));
            Draw.roundedRect(graphics, x + SETTINGS_WIDTH / 2 - 3, y + 40, 6, 2, 1, Draw.withAlpha(light, 140));
            Draw.textCentered(graphics, font, "Выберите модуль", x + SETTINGS_WIDTH / 2, y + 62,
                    Draw.alpha(TEXT, ease));
            Draw.textCentered(graphics, font, "справа откроются", x + SETTINGS_WIDTH / 2, y + 77,
                    Draw.alpha(MUTED, ease));
            Draw.textCentered(graphics, font, "его настройки", x + SETTINGS_WIDTH / 2, y + 90,
                    Draw.alpha(MUTED, ease));
            return;
        }

        int innerX = x + 10;
        int innerWidth = SETTINGS_WIDTH - 20;
        Draw.display(graphics, Draw.displayTrim(selectedModule.getDisplayName(), innerWidth - 20),
                innerX, y + 7, TEXT);
        Draw.textRight(graphics, font, "×", x + SETTINGS_WIDTH - 12, y + 10, MUTED);
        Draw.text(graphics, font, Draw.trim(font, selectedModule.getDescription(), innerWidth),
                innerX, y + 29, Draw.alpha(MUTED, ease));
        Draw.horizontalGradient(graphics, innerX, y + 45, innerWidth / 2, 1,
                0x00FFFFFF & accent, Draw.withAlpha(accent, 90));
        Draw.horizontalGradient(graphics, innerX + innerWidth / 2, y + 45, innerWidth / 2, 1,
                Draw.withAlpha(accent, 90), 0x00FFFFFF & accent);

        int keyY = y + 54;
        Draw.text(graphics, font, "Клавиша", innerX, keyY + 4, MUTED);
        String keyText = rebindModule == selectedModule ? "Нажмите..." : selectedModule.getKeyBind().displayValue();
        Draw.roundedRect(graphics, innerX + innerWidth - 82, keyY, 82, 16, 5, Draw.alpha(FIELD, ease));
        Draw.outline(graphics, innerX + innerWidth - 82, keyY, 82, 16, 5, 0x14FFFFFF);
        Draw.textCentered(graphics, font, Draw.trim(font, keyText, 74),
                innerX + innerWidth - 41, keyY + 3, TEXT);

        int clipTop = y + 76;
        int clipBottom = y + height - 42;
        int total = 0;
        for (Setting<?> setting : selectedModule.getSettings())
        {
            if (setting.isVisible())
            {
                total += rowHeight(setting) + 3;
            }
        }
        float maxScroll = Math.max(0.0F, total - (clipBottom - clipTop));
        settingsScroll = (float) Draw.clamp(settingsScroll, 0.0D, maxScroll);

        enableScissor(graphics, x + 1, clipTop, SETTINGS_WIDTH - 2, clipBottom - clipTop);
        int rowY = (int) (clipTop - settingsScroll);
        for (Setting<?> setting : selectedModule.getSettings())
        {
            if (!setting.isVisible())
            {
                continue;
            }
            int rowHeight = rowHeight(setting);
            if (rowY + rowHeight >= clipTop - 4 && rowY <= clipBottom + 4)
            {
                drawSetting(graphics, setting, innerX, rowY, innerWidth, ease);
            }
            rowY += rowHeight + 3;
        }
        disableScissor(graphics);

        if (total == 0)
        {
            Draw.textCentered(graphics, font, "У модуля нет настроек",
                    x + SETTINGS_WIDTH / 2, clipTop + 14, Draw.alpha(MUTED, ease));
        }

        if (maxScroll > 0.0F)
        {
            Draw.roundedRect(graphics, x + SETTINGS_WIDTH - 4, clipTop, 2, clipBottom - clipTop, 1, 0x0DFFFFFF);
            float thumb = (clipBottom - clipTop) * (clipBottom - clipTop) / Math.max(1.0F, total);
            float thumbY = clipTop + (clipBottom - clipTop - thumb) * (settingsScroll / maxScroll);
            Draw.roundedRect(graphics, x + SETTINGS_WIDTH - 4, (int) thumbY, 2, Math.max(8, (int) thumb), 1,
                    Draw.withAlpha(accent, 160));
        }

        int buttonY = y + height - 32;
        boolean enabled = selectedModule.isEnabled();
        Draw.roundedRect(graphics, innerX, buttonY, innerWidth / 2 - 3, 18, 6,
                Draw.alpha(enabled ? Draw.withAlpha(accent, 46) : Draw.withAlpha(accent, 76), ease));
        Draw.outline(graphics, innerX, buttonY, innerWidth / 2 - 3, 18, 6,
                Draw.withAlpha(accent, 120));
        Draw.textCentered(graphics, font, enabled ? "Выключить" : "Включить",
                innerX + innerWidth / 4 - 2, buttonY + 4, enabled ? 0xFFFF8C9A : light);
        Draw.roundedRect(graphics, innerX + innerWidth / 2 + 3, buttonY, innerWidth / 2 - 3, 18, 6,
                Draw.alpha(FIELD, ease));
        Draw.outline(graphics, innerX + innerWidth / 2 + 3, buttonY, innerWidth / 2 - 3, 18, 6, 0x14FFFFFF);
        Draw.textCentered(graphics, font, "Сброс", innerX + innerWidth * 3 / 4, buttonY + 4, MUTED);
    }

    private void drawSetting(GuiGraphics graphics, Setting<?> setting, int x, int y, int width, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        int height = rowHeight(setting);
        boolean hover = Draw.inside(localMouseX, localMouseY, x, y, width, height);
        Draw.roundedRect(graphics, x, y, width, height, 6, Draw.alpha(hover ? ROW_HOVER : 0xB3130F20, ease));
        if (hover)
        {
            Draw.outline(graphics, x, y, width, height, 6, Draw.withAlpha(accent, 50));
        }

        if (setting instanceof BoolSetting)
        {
            Draw.text(graphics, font, Draw.trim(font, setting.getName(), width - 44), x + 7, y + 6, TEXT);
            boolean on = (Boolean) setting.getValue();
            float value = switchValue(setting.getName(), on, ease);
            drawSwitch(graphics, x + width - 33, y + 5, 26, 13, value, on ? accent : OFF);
            rows.add(new Row(setting, x, y, width, height));
            return;
        }

        Draw.text(graphics, font, Draw.trim(font, setting.getName(), width - 64), x + 7, y + 4, TEXT);
        Draw.textRight(graphics, font, setting.displayValue(), x + width - 7, y + 4, accent);

        if (setting instanceof IntSetting || setting instanceof DoubleSetting)
        {
            float trackY = y + height - 9;
            float t = valueFraction(setting);
            int trackX = x + 7;
            int trackWidth = width - 14;
            Draw.roundedRect(graphics, trackX, (int) trackY, trackWidth, 3, 1, TRACK);
            int fillWidth = Math.max(3, Math.round(trackWidth * t));
            Draw.roundedRect(graphics, trackX, (int) trackY, fillWidth, 3, 1, accent);
            Draw.roundedRect(graphics, trackX, (int) trackY, fillWidth, 1, 0, Draw.withAlpha(light, 140));
            int knobX = trackX + Math.round(trackWidth * t) - 3;
            Draw.glow(graphics, knobX, (int) trackY - 2, 6, 6, 3, Draw.withAlpha(accent, 90), 2);
            Draw.roundedRect(graphics, knobX, (int) trackY - 2, 6, 6, 3, 0xFFFFFFFF);
            Row row = new Row(setting, x, y, width, height);
            row.trackX = trackX;
            row.trackY = trackY - 4;
            row.trackWidth = trackWidth;
            rows.add(row);
            return;
        }

        if (setting instanceof ColorSetting)
        {
            ColorSetting color = (ColorSetting) setting;
            int swatch = color.getValue() == null ? 0xFF000000 : color.getValue();
            Draw.roundedRect(graphics, x + width - 31, y + 5, 25, 12, 3, 0xFF0D0A17);
            Draw.roundedRect(graphics, x + width - 30, y + 6, 23, 10, 2, swatch);
            Draw.outline(graphics, x + width - 30, y + 6, 23, 10, 2, 0x33FFFFFF);
            Row row = new Row(setting, x, y, width, expandedColor == setting ? height : rowHeight(setting));
            row.trackX = x + width - 31;
            row.trackY = y + 5;
            rows.add(row);
            if (expandedColor == setting)
            {
                int[] componentColors = {0xFFE0606C, 0xFF6FD07A, 0xFF6E8FF0, 0xFFB0A8C8};
                int componentY = y + 24;
                for (int i = 0; i < 4; i++)
                {
                    String[] letters = {"R", "G", "B", "A"};
                    int value = i == 0 ? color.getRed() : i == 1 ? color.getGreen() : i == 2 ? color.getBlue() : color.getAlpha();
                    Draw.text(graphics, font, letters[i], x + 7, componentY + 1, componentColors[i]);
                    float trackT = value / 255.0F;
                    int trackX = x + 20;
                    int trackWidth = width - 52;
                    Draw.roundedRect(graphics, trackX, componentY + 6, trackWidth, 3, 1, TRACK);
                    Draw.roundedRect(graphics, trackX, componentY + 6, Math.max(3, Math.round(trackWidth * trackT)), 3, 1, accent);
                    int knobX = trackX + Math.round(trackWidth * trackT) - 2;
                    Draw.roundedRect(graphics, knobX, componentY + 4, 5, 6, 2, 0xFFFFFFFF);
                    Draw.textRight(graphics, font, String.valueOf(value), x + width - 7, componentY + 1, TEXT);
                    Row sub = new Row(setting, x, componentY, width, 14);
                    sub.trackX = trackX;
                    sub.trackY = componentY + 3;
                    sub.trackWidth = trackWidth;
                    sub.colorIndex = i;
                    rows.add(sub);
                    componentY += 14;
                }
            }
            return;
        }

        if (setting instanceof EnumSetting)
        {
            EnumSetting<?> value = (EnumSetting<?>) setting;
            Draw.roundedRect(graphics, x + width - 88, y + 3, 84, 16, 5, Draw.alpha(FIELD, ease));
            Draw.outline(graphics, x + width - 88, y + 3, 84, 16, 5, 0x14FFFFFF);
            Draw.textCentered(graphics, font, Draw.trim(font, value.displayValue(), 72),
                    x + width - 46, y + 6, TEXT);
            Draw.text(graphics, font, "›", x + width - 10, y + 6, MUTED);
            Row row = new Row(setting, x, y, width, height);
            row.trackX = x + width - 88;
            row.trackY = y + 3;
            rows.add(row);
            return;
        }

        if (setting instanceof KeySetting)
        {
            KeySetting value = (KeySetting) setting;
            String text = rebindSetting == setting ? "Нажмите..." : value.displayValue();
            Draw.roundedRect(graphics, x + width - 88, y + 3, 84, 16, 5, Draw.alpha(FIELD, ease));
            Draw.outline(graphics, x + width - 88, y + 3, 84, 16, 5, 0x14FFFFFF);
            Draw.textCentered(graphics, font, Draw.trim(font, text, 74), x + width - 46, y + 6, TEXT);
            rows.add(new Row(setting, x, y, width, height));
        }
    }

    private void drawInterfaceSettings(GuiGraphics graphics, float fade, float ease)
    {
        int accent = config.getAccent();
        int light = Draw.mix(accent, 0xFFFFFFFF, 0.5F);
        int x = CONTENT_X;
        int y = HEADER + 10;
        int width = PANEL_WIDTH - x - 8;
        int height = LIST_BOTTOM - y;
        Draw.roundedRect(graphics, x, y, width, height, 10, Draw.alpha(CARD_FILL, fade));
        Draw.outline(graphics, x, y, width, height, 10, 0x14FFFFFF);
        rows.clear();
        Draw.displayGradient(graphics, "Интерфейс", x + 12, y + 8, light, 0xFFFFFFFF, true);
        Draw.roundedRect(graphics, x + width - 68, y + 8, 56, 16, 5, 0x14FFFFFF);
        Draw.textCentered(graphics, font, "Сброс", x + width - 40, y + 11, MUTED);

        int innerX = x + 12;
        int innerWidth = width - 24;
        int rowY = y + 34;

        Draw.text(graphics, font, "Акцент", innerX, rowY + 1, TEXT);
        Draw.textRight(graphics, font, String.format(Locale.ROOT, "#%06X", accent & 0xFFFFFF),
                innerX + innerWidth - 36, rowY + 1, MUTED);
        Draw.roundedRect(graphics, innerX + innerWidth - 30, rowY - 1, 30, 12, 3, accent);
        Draw.outline(graphics, innerX + innerWidth - 30, rowY - 1, 30, 12, 3, 0x40FFFFFF);
        rowY += 18;

        int[] components = {(accent >> 16) & 0xFF, (accent >> 8) & 0xFF, accent & 0xFF};
        int[] componentColors = {0xFFE0606C, 0xFF6FD07A, 0xFF6E8FF0};
        String[] letters = {"R", "G", "B"};
        for (int i = 0; i < 3; i++)
        {
            Draw.text(graphics, font, letters[i], innerX, rowY + 2, componentColors[i]);
            float t = components[i] / 255.0F;
            int trackX = innerX + 18;
            int trackWidth = innerWidth - 58;
            Draw.roundedRect(graphics, trackX, rowY + 6, trackWidth, 3, 1, TRACK);
            Draw.roundedRect(graphics, trackX, rowY + 6, Math.max(3, Math.round(trackWidth * t)), 3, 1, accent);
            int knobX = trackX + Math.round(trackWidth * t) - 2;
            Draw.roundedRect(graphics, knobX, rowY + 4, 5, 6, 2, 0xFFFFFFFF);
            Draw.textRight(graphics, font, String.valueOf(components[i]), innerX + innerWidth, rowY + 2, TEXT);
            rows.add(new Row(null, innerX, rowY, innerWidth, 16, "accent" + i));
            Row row = rows.get(rows.size() - 1);
            row.trackX = trackX;
            row.trackY = rowY + 3;
            row.trackWidth = trackWidth;
            rowY += 17;
        }

        Draw.horizontalGradient(graphics, innerX, rowY + 3, innerWidth / 2, 1,
                0x00FFFFFF & accent, Draw.withAlpha(accent, 70));
        Draw.horizontalGradient(graphics, innerX + innerWidth / 2, rowY + 3, innerWidth / 2, 1,
                Draw.withAlpha(accent, 70), 0x00FFFFFF & accent);
        rowY += 8;

        rowY = drawInterfaceSlider(graphics, "Масштаб", String.format(Locale.ROOT, "%.2f", config.getScale()),
                innerX, rowY, innerWidth, fraction(config.getScale(), 0.7D, 1.6D), "scale", ease);
        rowY = drawInterfaceSlider(graphics, "Прозрачность", String.format(Locale.ROOT, "%.2f", config.getOpacity()),
                innerX, rowY, innerWidth, fraction(config.getOpacity(), 0.5D, 1.0D), "opacity", ease);

        Draw.text(graphics, font, "Анимации", innerX, rowY + 4, TEXT);
        float animations = switchValue("config.animations", config.isAnimations(), ease);
        drawSwitch(graphics, innerX + innerWidth - 30, rowY + 3, 28, 13, animations,
                config.isAnimations() ? accent : OFF);
        rows.add(new Row(null, innerX, rowY, innerWidth, 20, "animations"));
        rowY += 22;

        rowY = drawKeyRow(graphics, "Открыть меню", keyName(config.getOpenKey()),
                innerX, rowY, innerWidth, "openKey", ease);
        rowY = drawKeyRow(graphics, "Выключить визуал", keyName(config.getToggleKey()),
                innerX, rowY, innerWidth, "toggleKey", ease);

        interfaceButtonsY = rowY + 4;
        Draw.roundedRect(graphics, innerX, interfaceButtonsY, innerWidth / 2 - 3, 18, 6,
                Draw.withAlpha(accent, 66));
        Draw.outline(graphics, innerX, interfaceButtonsY, innerWidth / 2 - 3, 18, 6, Draw.withAlpha(accent, 120));
        Draw.textCentered(graphics, font, "Сброс интерфейса", innerX + innerWidth / 4 - 2,
                interfaceButtonsY + 4, light);
        Draw.roundedRect(graphics, innerX + innerWidth / 2 + 3, interfaceButtonsY, innerWidth / 2 - 3, 18, 6,
                Draw.alpha(FIELD, ease));
        Draw.outline(graphics, innerX + innerWidth / 2 + 3, interfaceButtonsY, innerWidth / 2 - 3, 18, 6, 0x14FFFFFF);
        Draw.textCentered(graphics, font, "Сброс модулей", innerX + innerWidth * 3 / 4,
                interfaceButtonsY + 4, 0xFFFF8C9A);
    }

    private int drawInterfaceSlider(GuiGraphics graphics, String label, String value, int x, int y, int width,
                                    float t, String id, float ease)
    {
        int accent = config.getAccent();
        Draw.text(graphics, font, label, x, y, TEXT);
        Draw.textRight(graphics, font, value, x + width, y, accent);
        int fillWidth = Math.max(3, Math.round(width * t));
        Draw.roundedRect(graphics, x, y + 14, width, 3, 1, TRACK);
        Draw.roundedRect(graphics, x, y + 14, fillWidth, 3, 1, accent);
        Draw.roundedRect(graphics, x, y + 14, fillWidth, 1, 0, Draw.withAlpha(Draw.mix(accent, 0xFFFFFFFF, 0.5F), 140));
        int knobX = x + Math.round(width * t) - 3;
        Draw.glow(graphics, knobX, y + 12, 6, 6, 3, Draw.withAlpha(accent, 90), 2);
        Draw.roundedRect(graphics, knobX, y + 12, 6, 6, 3, 0xFFFFFFFF);
        Row row = new Row(null, x, y, width, 21, id);
        row.trackX = x;
        row.trackY = y + 10;
        row.trackWidth = width;
        rows.add(row);
        return y + 23;
    }

    private int drawKeyRow(GuiGraphics graphics, String label, String value, int x, int y, int width,
                           String id, float ease)
    {
        Draw.text(graphics, font, label, x, y + 4, TEXT);
        String shown = rebindConfig != null && rebindConfig.equals(id) ? "Нажмите..." : value;
        Draw.roundedRect(graphics, x + width - 96, y, 96, 17, 5, Draw.alpha(FIELD, ease));
        Draw.outline(graphics, x + width - 96, y, 96, 17, 5, 0x14FFFFFF);
        Draw.textCentered(graphics, font, Draw.trim(font, shown, 86), x + width - 48, y + 3, TEXT);
        rows.add(new Row(null, x, y, width, 18, "key:" + id));
        Row row = rows.get(rows.size() - 1);
        row.trackX = x + width - 96;
        row.trackY = y;
        return y + 21;
    }

    private void drawSwitch(GuiGraphics graphics, int x, int y, int width, int height, float t, int onColor)
    {
        int color = Draw.mix(TRACK, onColor, t);
        Draw.roundedRect(graphics, x, y, width, height, height / 2, color);
        if (t > 0.05F)
        {
            Draw.roundedRect(graphics, x + 1, y + 1, width - 2, height / 2, height / 2,
                    Draw.withAlpha(0xFFFFFFFF, Math.round(34 * t)));
        }
        int knob = height - 4;
        int knobX = x + 2 + Math.round((width - 4 - knob) * t);
        int knobY = y + 2;
        Draw.glow(graphics, knobX, knobY, knob, knob, knob / 2, Draw.withAlpha(onColor, Math.round(80 * t)), 2);
        Draw.roundedRect(graphics, knobX, knobY, knob, knob, knob / 2, 0xFFFFFFFF);
    }

    private float switchValue(String key, boolean state, float ease)
    {
        float target = state ? 1.0F : 0.0F;
        if (!config.isAnimations())
        {
            switchAnim.put(key, target);
            return target;
        }
        Float current = switchAnim.get(key);
        float value = current == null ? target : current;
        value += (target - value) * 0.35F;
        switchAnim.put(key, value);
        return value;
    }

    private List<Module> visibleModules()
    {
        if (!search.trim().isEmpty())
        {
            return manager.search(search);
        }
        return manager.getModules(category);
    }

    private int rowHeight(Setting<?> setting)
    {
        if (setting instanceof IntSetting || setting instanceof DoubleSetting)
        {
            return 29;
        }
        if (setting instanceof ColorSetting && expandedColor == setting)
        {
            return 22 + 4 * 14;
        }
        return 22;
    }

    private float valueFraction(Setting<?> setting)
    {
        if (setting instanceof IntSetting)
        {
            IntSetting value = (IntSetting) setting;
            int range = value.getMax() - value.getMin();
            return range == 0 ? 0.0F : (value.getValue() - value.getMin()) / (float) range;
        }
        DoubleSetting value = (DoubleSetting) setting;
        double range = value.getMax() - value.getMin();
        return range == 0.0D ? 0.0F : (float) ((value.getValue() - value.getMin()) / range);
    }

    private static float fraction(double value, double min, double max)
    {
        double range = max - min;
        return range == 0.0D ? 0.0F : (float) ((value - min) / range);
    }

    private static String keyName(int key)
    {
        if (key <= 0)
        {
            return "Не задана";
        }
        return InputConstants.Type.KEYSYM.getOrCreate(key).getDisplayName().getString();
    }

    private float baseScale()
    {
        float fit = Math.min((width - 8) / (float) PANEL_WIDTH, (height - 8) / (float) PANEL_HEIGHT);
        return (float) config.getScale() * (float) Draw.clamp(fit, 0.0D, 1.0D);
    }

    private void enableScissor(GuiGraphics graphics, int x, int y, int width, int height)
    {
        graphics.enableScissor(x, y, x + width, y + height);
    }

    private static void disableScissor(GuiGraphics graphics)
    {
        graphics.disableScissor();
    }

    @Override
    public boolean mouseClicked(MouseButtonEvent event, boolean doubled)
    {
        double x = (event.x() - panelScreenX) / panelScale;
        double y = (event.y() - panelScreenY) / panelScale;
        int button = event.button();
        if (button != 0)
        {
            return super.mouseClicked(event, doubled);
        }

        if (x < 0 || y < 0 || x > PANEL_WIDTH || y > PANEL_HEIGHT)
        {
            if (rebindConfig == null && rebindModule == null && rebindSetting == null)
            {
                onClose();
            }
            return true;
        }

        if (y <= HEADER)
        {
            if (x >= PANEL_WIDTH - 32)
            {
                onClose();
                return true;
            }
            if (x >= PANEL_WIDTH - 154 && x <= PANEL_WIDTH - 34)
            {
                manager.setVisualEnabled(!manager.isVisualEnabled(), true);
                return true;
            }
            draggingWindow = true;
            float baseWidth = PANEL_WIDTH * baseScale();
            float currentX = config.getPanelX() >= 0 ? config.getPanelX() : (width - baseWidth) / 2.0F;
            float currentY = config.getPanelY() >= 0 ? config.getPanelY() : (height - PANEL_HEIGHT * baseScale()) / 2.0F;
            dragWindowOffsetX = event.x() - currentX;
            dragWindowOffsetY = event.y() - currentY;
            searchFocused = false;
            return true;
        }

        int catY = CATEGORY_TOP;
        for (Category value : Category.values())
        {
            if (Draw.inside(x, y, SIDEBAR_X + 4, catY, SIDEBAR_WIDTH - 8, CATEGORY_HEIGHT))
            {
                category = value;
                selectedModule = null;
                search = "";
                searchFocused = false;
                listScroll = 0.0F;
                settingsScroll = 0.0F;
                expandedColor = null;
                return true;
            }
            catY += CATEGORY_STEP;
        }

        if (category == Category.SETTINGS)
        {
            return clickInterfaceSettings(x, y);
        }

        if (Draw.inside(x, y, CONTENT_X, SEARCH_Y, LIST_WIDTH, SEARCH_HEIGHT))
        {
            searchFocused = true;
            if (!search.isEmpty() && x >= CONTENT_X + LIST_WIDTH - 22)
            {
                search = "";
            }
            return true;
        }
        searchFocused = false;

        if (x >= CONTENT_X && x <= CONTENT_X + LIST_WIDTH && y >= LIST_TOP && y <= LIST_BOTTOM)
        {
            List<Module> modules = visibleModules();
            int index = (int) ((y - LIST_TOP + listScroll) / ROW_HEIGHT);
            if (index >= 0 && index < modules.size())
            {
                float rowY = LIST_TOP + index * ROW_HEIGHT - listScroll;
                if (y - rowY <= ROW_HEIGHT - 2)
                {
                    Module module = modules.get(index);
                    if (x >= CONTENT_X + LIST_WIDTH - 44)
                    {
                        module.toggle();
                    }
                    else
                    {
                        selectedModule = module;
                        settingsScroll = 0.0F;
                        expandedColor = null;
                    }
                    return true;
                }
            }
        }

        if (selectedModule != null && x >= SETTINGS_X && x <= SETTINGS_X + SETTINGS_WIDTH)
        {
            return clickModuleSettings(x, y);
        }
        return true;
    }

    private boolean clickModuleSettings(double x, double y)
    {
        int panelY = HEADER + 10;
        int panelHeight = LIST_BOTTOM - panelY;
        if (y <= panelY + 30 && x >= SETTINGS_X + SETTINGS_WIDTH - 28)
        {
            selectedModule = null;
            return true;
        }
        if (y >= panelY + 54 && y <= panelY + 72)
        {
            rebindModule = selectedModule;
            rebindSetting = null;
            rebindConfig = null;
            return true;
        }
        int buttonY = panelY + panelHeight - 32;
        if (y >= buttonY && y <= buttonY + 18 && x >= SETTINGS_X + 10 && x <= SETTINGS_X + SETTINGS_WIDTH - 10)
        {
            if (x < SETTINGS_X + SETTINGS_WIDTH / 2)
            {
                selectedModule.toggle();
            }
            else
            {
                selectedModule.resetSettings();
            }
            return true;
        }
        for (Row row : rows)
        {
            if (row.setting == null || !Draw.inside(x, y, row.x, row.y, row.width, row.height))
            {
                continue;
            }
            Setting<?> setting = row.setting;
            if (setting instanceof BoolSetting)
            {
                ((BoolSetting) setting).toggle();
                ConfigManager.markDirty();
                return true;
            }
            if (setting instanceof ColorSetting)
            {
                if (row.colorIndex >= 0)
                {
                    dragSetting = setting;
                    dragColorIndex = row.colorIndex;
                    dragTrackX = row.trackX;
                    dragTrackWidth = row.trackWidth;
                    applyColorDrag(x);
                    return true;
                }
                if (x >= row.trackX)
                {
                    expandedColor = expandedColor == setting ? null : setting;
                }
                return true;
            }
            if (setting instanceof IntSetting || setting instanceof DoubleSetting)
            {
                dragSetting = setting;
                dragColorIndex = -1;
                dragTrackX = row.trackX;
                dragTrackWidth = row.trackWidth;
                applyValueDrag(x);
                return true;
            }
            if (setting instanceof EnumSetting)
            {
                EnumSetting<?> value = (EnumSetting<?>) setting;
                if (x >= row.trackX)
                {
                    value.cycle(1);
                    ConfigManager.markDirty();
                }
                return true;
            }
            if (setting instanceof KeySetting)
            {
                rebindSetting = (KeySetting) setting;
                rebindModule = null;
                rebindConfig = null;
                return true;
            }
        }
        return true;
    }

    private boolean clickInterfaceSettings(double x, double y)
    {
        int panelTop = HEADER + 10;
        int width = PANEL_WIDTH - CONTENT_X - 8;
        if (y <= panelTop + 26 && x >= CONTENT_X + width - 72)
        {
            config.reset();
            manager.saveNow();
            return true;
        }
        for (Row row : rows)
        {
            if (row.id == null || !Draw.inside(x, y, row.x, row.y, row.width, row.height))
            {
                continue;
            }
            if (row.id.equals("animations"))
            {
                config.setAnimations(!config.isAnimations());
                ConfigManager.markDirty();
                return true;
            }
            if (row.id.startsWith("key:"))
            {
                rebindConfig = row.id.substring(4);
                rebindModule = null;
                rebindSetting = null;
                return true;
            }
            dragConfig = row.id;
            dragTrackX = row.trackX;
            dragTrackWidth = row.trackWidth;
            applyConfigDrag(x);
            return true;
        }
        int buttonRowY = interfaceButtonsY;
        if (y >= buttonRowY && y <= buttonRowY + 18)
        {
            if (x < CONTENT_X + 12 + (PANEL_WIDTH - CONTENT_X - 32) / 2)
            {
                config.reset();
                manager.saveNow();
            }
            else
            {
                for (Module module : manager.getModules())
                {
                    module.resetSettings();
                    module.getKeyBind().reset();
                }
                manager.saveNow();
            }
            return true;
        }
        return true;
    }

    private void applyValueDrag(double x)
    {
        if (dragSetting == null || dragTrackWidth <= 0.0F)
        {
            return;
        }
        float t = (float) Draw.clamp((x - dragTrackX) / dragTrackWidth, 0.0D, 1.0D);
        if (dragSetting instanceof IntSetting)
        {
            IntSetting value = (IntSetting) dragSetting;
            value.setValue(Math.round(value.getMin() + t * (value.getMax() - value.getMin())));
        }
        else if (dragSetting instanceof DoubleSetting)
        {
            DoubleSetting value = (DoubleSetting) dragSetting;
            value.setValue(value.getMin() + t * (value.getMax() - value.getMin()));
        }
        ConfigManager.markDirty();
    }

    private void applyColorDrag(double x)
    {
        if (!(dragSetting instanceof ColorSetting) || dragTrackWidth <= 0.0F || dragColorIndex < 0)
        {
            return;
        }
        float t = (float) Draw.clamp((x - dragTrackX) / dragTrackWidth, 0.0D, 1.0D);
        ((ColorSetting) dragSetting).setComponent(dragColorIndex, Math.round(t * 255.0F));
        ConfigManager.markDirty();
    }

    private void applyConfigDrag(double x)
    {
        if (dragConfig == null || dragTrackWidth <= 0.0F)
        {
            return;
        }
        float t = (float) Draw.clamp((x - dragTrackX) / dragTrackWidth, 0.0D, 1.0D);
        switch (dragConfig)
        {
            case "scale":
                config.setScale(0.7D + t * 0.9D);
                break;
            case "opacity":
                config.setOpacity(0.5D + t * 0.5D);
                break;
            case "accent0":
            case "accent1":
            case "accent2":
                int component = dragConfig.charAt(6) - '0';
                int accent = config.getAccent();
                int red = (accent >> 16) & 0xFF;
                int green = (accent >> 8) & 0xFF;
                int blue = accent & 0xFF;
                int value = Math.round(t * 255.0F);
                if (component == 0)
                {
                    red = value;
                }
                else if (component == 1)
                {
                    green = value;
                }
                else
                {
                    blue = value;
                }
                config.setAccent((red << 16) | (green << 8) | blue);
                break;
            default:
                return;
        }
        ConfigManager.markDirty();
    }

    @Override
    public boolean mouseDragged(MouseButtonEvent event, double dragX, double dragY)
    {
        double x = (event.x() - panelScreenX) / panelScale;
        double y = (event.y() - panelScreenY) / panelScale;
        if (draggingWindow)
        {
            float baseWidth = PANEL_WIDTH * baseScale();
            float baseHeight = PANEL_HEIGHT * baseScale();
            float targetX = (float) (event.x() - dragWindowOffsetX);
            float targetY = (float) (event.y() - dragWindowOffsetY);
            targetX = (float) Draw.clamp(targetX, 0.0D, Math.max(0.0D, width - baseWidth));
            targetY = (float) Draw.clamp(targetY, 0.0D, Math.max(0.0D, height - baseHeight));
            config.setPanelX(Math.round(targetX));
            config.setPanelY(Math.round(targetY));
            ConfigManager.markDirty();
            return true;
        }
        if (dragSetting != null)
        {
            if (dragColorIndex >= 0)
            {
                applyColorDrag(x);
            }
            else
            {
                applyValueDrag(x);
            }
            return true;
        }
        if (dragConfig != null)
        {
            applyConfigDrag(x);
            return true;
        }
        return super.mouseDragged(event, dragX, dragY);
    }

    @Override
    public boolean mouseReleased(MouseButtonEvent event)
    {
        draggingWindow = false;
        dragSetting = null;
        dragColorIndex = -1;
        dragConfig = null;
        return super.mouseReleased(event);
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double horizontal, double vertical)
    {
        double x = (mouseX - panelScreenX) / panelScale;
        double y = (mouseY - panelScreenY) / panelScale;
        if (category != Category.SETTINGS && x >= CONTENT_X && x <= CONTENT_X + LIST_WIDTH
                && y >= LIST_TOP && y <= LIST_BOTTOM)
        {
            listScroll -= (float) vertical * 20.0F;
            return true;
        }
        if (category == Category.SETTINGS)
        {
            return true;
        }
        if (selectedModule != null && x >= SETTINGS_X && x <= SETTINGS_X + SETTINGS_WIDTH)
        {
            settingsScroll -= (float) vertical * 16.0F;
            return true;
        }
        return super.mouseScrolled(mouseX, mouseY, horizontal, vertical);
    }

    @Override
    public boolean keyPressed(KeyEvent event)
    {
        int key = event.key();

        if (rebindConfig != null || rebindModule != null || rebindSetting != null)
        {
            if (key != GLFW.GLFW_KEY_ESCAPE)
            {
                int value = key == GLFW.GLFW_KEY_DELETE || key == GLFW.GLFW_KEY_BACKSPACE ? -1 : key;
                if (rebindConfig != null)
                {
                    if (rebindConfig.equals("openKey"))
                    {
                        config.setOpenKey(value);
                    }
                    else if (rebindConfig.equals("toggleKey"))
                    {
                        config.setToggleKey(value);
                    }
                }
                else if (rebindSetting != null)
                {
                    rebindSetting.setKey(value);
                }
                else if (rebindModule != null)
                {
                    rebindModule.getKeyBind().setKey(value);
                }
                ConfigManager.markDirty();
            }
            rebindConfig = null;
            rebindModule = null;
            rebindSetting = null;
            return true;
        }

        if (key == GLFW.GLFW_KEY_ESCAPE)
        {
            onClose();
            return true;
        }
        if (searchFocused)
        {
            if (key == GLFW.GLFW_KEY_BACKSPACE)
            {
                if (!search.isEmpty())
                {
                    search = search.substring(0, search.length() - 1);
                }
                return true;
            }
            if (key == GLFW.GLFW_KEY_DELETE)
            {
                search = "";
                return true;
            }
            if (key == GLFW.GLFW_KEY_ENTER || key == GLFW.GLFW_KEY_KP_ENTER)
            {
                searchFocused = false;
                return true;
            }
        }
        if (key == config.getOpenKey())
        {
            onClose();
            return true;
        }
        return super.keyPressed(event);
    }

    @Override
    public boolean charTyped(CharacterEvent event)
    {
        if (!searchFocused || rebindConfig != null || rebindModule != null || rebindSetting != null)
        {
            return super.charTyped(event);
        }
        int codePoint = event.codepoint();
        if (codePoint >= 32 && codePoint != 127 && search.length() < 32)
        {
            search += new String(Character.toChars(codePoint));
            listScroll = 0.0F;
            return true;
        }
        return super.charTyped(event);
    }

    @Override
    public void onClose()
    {
        ConfigManager.markDirty();
        Minecraft.getInstance().setScreen(null);
    }

    private static final class Row
    {
        private final Setting<?> setting;
        private final float x;
        private final float y;
        private final float width;
        private final float height;
        private final String id;
        private float trackX;
        private float trackY;
        private float trackWidth;
        private int colorIndex = -1;

        private Row(Setting<?> setting, float x, float y, float width, float height)
        {
            this(setting, x, y, width, height, null);
        }

        private Row(Setting<?> setting, float x, float y, float width, float height, String id)
        {
            this.setting = setting;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.id = id;
        }
    }
}
