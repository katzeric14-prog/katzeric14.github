package ru.vexor.visual.module;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import ru.vexor.visual.util.Draw;

public abstract class HudModule extends Module
{
    public enum Anchor
    {
        TOP_LEFT,
        CENTER,
        OVERLAY
    }

    private final Anchor anchor;

    protected HudModule(String name, String description, int defaultKey)
    {
        this(name, description, defaultKey, Anchor.TOP_LEFT);
    }

    protected HudModule(String name, String description, int defaultKey, Anchor anchor)
    {
        this(name, description, defaultKey, Category.HUD, anchor);
    }

    protected HudModule(String name, String description, int defaultKey, Category category, Anchor anchor)
    {
        super(name, description, category, defaultKey);
        this.anchor = anchor;
    }

    public Anchor getAnchor()
    {
        return anchor;
    }

    public abstract int render(GuiGraphics graphics, DeltaTracker delta, int x, int y);

    protected static Font font()
    {
        return Minecraft.getInstance().font;
    }

    protected static int accent()
    {
        return ModuleManager.get().getGuiConfig().getAccent();
    }

    protected static int textColor()
    {
        return 0xFFE9E6F5;
    }

    protected static int mutedColor()
    {
        return 0xFF9A93B3;
    }

    protected static void card(GuiGraphics graphics, int x, int y, int width, int height)
    {
        int alpha = (int) Math.round(255 * ModuleManager.get().getGuiConfig().getOpacity() * 0.82D);
        int accent = accent();
        Draw.shadow(graphics, x, y, width, height, 5, Draw.withAlpha(0xFF000000, Math.round(alpha * 0.5F)), 1);
        Draw.roundedRect(graphics, x, y, width, height, 5, Draw.withAlpha(0xFF0F0B1A, alpha));
        Draw.verticalGradient(graphics, x + 1, y + 1, width - 2, Math.max(2, height / 3),
                Draw.withAlpha(0xFFFFFFFF, 7), 0x00FFFFFF);
        Draw.roundedRect(graphics, x, y + 2, 2, height - 4, 1, Draw.withAlpha(accent, 200));
        Draw.outline(graphics, x, y, width, height, 5, Draw.withAlpha(accent, 55));
    }

    protected static void label(GuiGraphics graphics, String text, int x, int y)
    {
        Draw.text(graphics, font(), text, x, y, textColor());
    }
}
