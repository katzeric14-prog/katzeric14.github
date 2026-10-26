package ru.vexor.visual.module.impl.visuals;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.ColorSetting;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.setting.IntSetting;
import ru.vexor.visual.util.Draw;

public final class CustomCrosshair extends Module
{
    public enum Shape
    {
        CROSS("Крест"),
        DOT("Точка"),
        CIRCLE("Круг"),
        SQUARE("Квадрат");

        private final String label;

        Shape(String label)
        {
            this.label = label;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private static CustomCrosshair instance;

    private final EnumSetting<Shape> shape = add(new EnumSetting<>("Форма", Shape.CROSS));
    private final ColorSetting color = add(new ColorSetting("Цвет", 0xFFFFFFFF));
    private final ColorSetting outline = add(new ColorSetting("Обводка", 0xCC000000));
    private final IntSetting size = add(new IntSetting("Размер", 5, 1, 14, 1));
    private final IntSetting thickness = add(new IntSetting("Толщина", 1, 1, 4, 1));
    private final IntSetting gap = add(new IntSetting("Зазор", 3, 0, 10, 1));
    private final BoolSetting centerDot = add(new BoolSetting("Точка в центре", true));

    public CustomCrosshair()
    {
        super("Custom Crosshair", "Свой прицел вместо стандартного", Category.VISUALS);
        setDisplayName("Свой прицел");
        instance = this;
    }

    public static void render(GuiGraphics graphics)
    {
        if (instance == null)
        {
            return;
        }
        instance.draw(graphics);
    }

    private void draw(GuiGraphics graphics)
    {
        int centerX = graphics.guiWidth() / 2;
        int centerY = graphics.guiHeight() / 2;
        int rgb = color.getValue() == null ? 0xFFFFFFFF : color.getValue();
        int border = outline.getValue() == null ? 0 : outline.getValue();
        int borderAlpha = (border >>> 24) & 0xFF;
        int arm = size.getValue();
        int width = thickness.getValue();
        int spacing = gap.getValue();

        switch (shape.getValue())
        {
            case DOT:
                box(graphics, centerX - width / 2, centerY - width / 2, width, width, rgb, borderAlpha, border);
                break;
            case CIRCLE:
                int radius = Math.max(2, arm);
                Draw.roundedOutline(graphics, centerX - radius, centerY - radius, radius * 2, radius * 2,
                        radius, width, rgb);
                if (centerDot.getValue())
                {
                    box(graphics, centerX - width / 2, centerY - width / 2, width, width, rgb, borderAlpha, border);
                }
                break;
            case SQUARE:
                int half = Math.max(2, arm);
                Draw.roundedOutline(graphics, centerX - half, centerY - half, half * 2, half * 2,
                        2, width, rgb);
                if (centerDot.getValue())
                {
                    box(graphics, centerX - width / 2, centerY - width / 2, width, width, rgb, borderAlpha, border);
                }
                break;
            default:
                horizontal(graphics, centerX - spacing - arm, centerX - spacing, centerY - width / 2, width, arm, rgb, borderAlpha, border);
                horizontal(graphics, centerX + spacing, centerX + spacing + arm, centerY - width / 2, width, arm, rgb, borderAlpha, border);
                vertical(graphics, centerY - spacing - arm, centerY - spacing, centerX - width / 2, width, rgb, borderAlpha, border);
                vertical(graphics, centerY + spacing, centerY + spacing + arm, centerX - width / 2, width, rgb, borderAlpha, border);
                if (centerDot.getValue())
                {
                    box(graphics, centerX - width / 2, centerY - width / 2, width, width, rgb, borderAlpha, border);
                }
                break;
        }
    }

    private static void horizontal(GuiGraphics graphics, int x1, int x2, int y, int thickness, int length,
                                   int color, int borderAlpha, int border)
    {
        if (borderAlpha > 0)
        {
            graphics.fill(x1 - 1, y - 1, x2 + 1, y + thickness + 1, border);
        }
        graphics.fill(x1, y, x2, y + thickness, color);
    }

    private static void vertical(GuiGraphics graphics, int y1, int y2, int x, int thickness,
                                 int color, int borderAlpha, int border)
    {
        if (borderAlpha > 0)
        {
            graphics.fill(x - 1, y1 - 1, x + thickness + 1, y2 + 1, border);
        }
        graphics.fill(x, y1, x + thickness, y2, color);
    }

    private static void box(GuiGraphics graphics, int x, int y, int width, int height, int color, int borderAlpha, int border)
    {
        if (borderAlpha > 0)
        {
            graphics.fill(x - 1, y - 1, x + width + 1, y + height + 1, border);
        }
        graphics.fill(x, y, x + width, y + height, color);
    }
}
