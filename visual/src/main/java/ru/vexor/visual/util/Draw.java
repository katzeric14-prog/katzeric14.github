package ru.vexor.visual.util;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.network.chat.FontDescription;
import net.minecraft.network.chat.Style;
import net.minecraft.resources.Identifier;
import net.minecraft.util.FormattedCharSequence;

public final class Draw
{
    private static final Style UI_STYLE = Style.EMPTY.withFont(
            new FontDescription.Resource(Identifier.fromNamespaceAndPath("vexorvisual", "ui")));
    private static final Style DISPLAY_STYLE = Style.EMPTY.withFont(
            new FontDescription.Resource(Identifier.fromNamespaceAndPath("vexorvisual", "display")));

    private Draw()
    {
    }

    public static void roundedRect(GuiGraphics graphics, int x, int y, int width, int height, int radius, int color)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }
        int r = Math.max(0, Math.min(radius, Math.min(width, height) / 2));
        graphics.fill(x, y + r, x + width, y + height - r, color);
        for (int row = 0; row < r; row++)
        {
            double dy = r - row - 0.5D;
            int inset = r - (int) Math.round(Math.sqrt(Math.max(0.0D, r * r - dy * dy)));
            graphics.fill(x + inset, y + row, x + width - inset, y + row + 1, color);
            graphics.fill(x + inset, y + height - row - 1, x + width - inset, y + height - row, color);
        }
    }

    public static void roundedOutline(GuiGraphics graphics, int x, int y, int width, int height, int radius, int thickness, int color)
    {
        roundedRect(graphics, x, y, width, height, radius, color);
        roundedRect(graphics, x + thickness, y + thickness, width - thickness * 2, height - thickness * 2,
                Math.max(0, radius - thickness), 0x00000000);
    }

    public static void panel(GuiGraphics graphics, int x, int y, int width, int height, int radius, int fill, int border)
    {
        roundedRect(graphics, x, y, width, height, radius, fill);
        outline(graphics, x, y, width, height, radius, border);
    }

    public static void shadow(GuiGraphics graphics, int x, int y, int width, int height, int radius, int color, int spread)
    {
        for (int i = spread; i >= 1; i--)
        {
            int alpha = Math.max(3, ((color >>> 24) & 0xFF) / (i + 1));
            roundedRect(graphics, x - i, y - i + 2, width + i * 2, height + i * 2, radius + i,
                    withAlpha(color, alpha));
        }
    }

    public static void glow(GuiGraphics graphics, int x, int y, int width, int height, int radius, int color, int spread)
    {
        for (int i = spread; i >= 1; i--)
        {
            int alpha = Math.max(2, ((color >>> 24) & 0xFF) / (i * 2 + 1));
            roundedRect(graphics, x - i, y - i, width + i * 2, height + i * 2, radius + i,
                    withAlpha(color, alpha));
        }
    }

    public static void verticalGradient(GuiGraphics graphics, int x, int y, int width, int height, int top, int bottom)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }
        for (int row = 0; row < height; row++)
        {
            float t = height <= 1 ? 0.0F : row / (float) (height - 1);
            graphics.fill(x, y + row, x + width, y + row + 1, mix(top, bottom, t));
        }
    }

    public static void horizontalGradient(GuiGraphics graphics, int x, int y, int width, int height, int left, int right)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }
        for (int column = 0; column < width; column++)
        {
            float t = width <= 1 ? 0.0F : column / (float) (width - 1);
            graphics.fill(x + column, y, x + column + 1, y + height, mix(left, right, t));
        }
    }

    public static void outline(GuiGraphics graphics, int x, int y, int width, int height, int radius, int color)
    {
        int r = Math.max(0, Math.min(radius, Math.min(width, height) / 2));
        graphics.fill(x + r, y, x + width - r, y + 1, color);
        graphics.fill(x + r, y + height - 1, x + width - r, y + height, color);
        graphics.fill(x, y + r, x + 1, y + height - r, color);
        graphics.fill(x + width - 1, y + r, x + width, y + height - r, color);
        for (int row = 0; row < r; row++)
        {
            double dy = r - row - 0.5D;
            int inset = r - (int) Math.round(Math.sqrt(Math.max(0.0D, r * r - dy * dy)));
            graphics.fill(x + inset, y + row, x + inset + 1, y + row + 1, color);
            graphics.fill(x + width - inset - 1, y + row, x + width - inset, y + row + 1, color);
            graphics.fill(x + inset, y + height - row - 1, x + inset + 1, y + height - row, color);
            graphics.fill(x + width - inset - 1, y + height - row - 1, x + width - inset, y + height - row, color);
        }
    }

    // ------------------------------------------------------------------ text

    private static Font baseFont()
    {
        return Minecraft.getInstance().font;
    }

    private static FormattedCharSequence line(String text, Style style)
    {
        return FormattedCharSequence.forward(text, style);
    }

    public static void text(GuiGraphics graphics, Font font, String text, int x, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, UI_STYLE), x, y, color, false);
    }

    public static void textShadow(GuiGraphics graphics, Font font, String text, int x, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, UI_STYLE), x, y, color, true);
    }

    public static void textRight(GuiGraphics graphics, Font font, String text, int right, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, UI_STYLE), right - width(text), y, color, false);
    }

    public static void textCentered(GuiGraphics graphics, Font font, String text, int centerX, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, UI_STYLE), centerX - width(text) / 2, y, color, false);
    }

    public static void display(GuiGraphics graphics, String text, int x, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, DISPLAY_STYLE), x, y, color, false);
    }

    public static void displayShadow(GuiGraphics graphics, String text, int x, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, DISPLAY_STYLE), x, y, color, true);
    }

    public static void displayRight(GuiGraphics graphics, String text, int right, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, DISPLAY_STYLE), right - displayWidth(text), y, color, false);
    }

    public static void displayCentered(GuiGraphics graphics, String text, int centerX, int y, int color)
    {
        graphics.drawString(baseFont(), line(text, DISPLAY_STYLE), centerX - displayWidth(text) / 2, y, color, false);
    }

    public static void displayGradient(GuiGraphics graphics, String text, int x, int y, int left, int right, boolean shadow)
    {
        int total = displayWidth(text);
        int cursor = x;
        for (int i = 0; i < text.length(); )
        {
            int codePoint = text.codePointAt(i);
            String glyph = new String(Character.toChars(codePoint));
            int glyphWidth = displayWidth(glyph);
            float t = total <= 0 ? 0.0F : (cursor - x) / (float) total;
            graphics.drawString(baseFont(), line(glyph, DISPLAY_STYLE), cursor, y, mix(left, right, t), shadow);
            cursor += glyphWidth;
            i += Character.charCount(codePoint);
        }
    }

    public static int width(String text)
    {
        return text == null || text.isEmpty() ? 0 : baseFont().width(line(text, UI_STYLE));
    }

    public static int displayWidth(String text)
    {
        return text == null || text.isEmpty() ? 0 : baseFont().width(line(text, DISPLAY_STYLE));
    }

    public static String trim(Font font, String text, int maxWidth)
    {
        return trim(text, maxWidth);
    }

    public static String trim(String text, int maxWidth)
    {
        if (text == null) return "";
        if (width(text) <= maxWidth)
        {
            return text;
        }
        String ellipsis = "…";
        int limit = Math.max(0, maxWidth - width(ellipsis));
        int cut = text.length();
        while (cut > 0 && width(text.substring(0, cut)) > limit)
        {
            cut--;
        }
        return text.substring(0, cut) + ellipsis;
    }

    public static String displayTrim(String text, int maxWidth)
    {
        if (text == null) return "";
        if (displayWidth(text) <= maxWidth)
        {
            return text;
        }
        String ellipsis = "…";
        int limit = Math.max(0, maxWidth - displayWidth(ellipsis));
        int cut = text.length();
        while (cut > 0 && displayWidth(text.substring(0, cut)) > limit)
        {
            cut--;
        }
        return text.substring(0, cut) + ellipsis;
    }

    // ----------------------------------------------------------------- colors

    public static int alpha(int color, float alpha)
    {
        int a = Math.max(0, Math.min(255, Math.round(((color >>> 24) & 0xFF) * alpha)));
        return (a << 24) | (color & 0x00FFFFFF);
    }

    public static int withAlpha(int color, int a)
    {
        return (Math.max(0, Math.min(255, a)) << 24) | (color & 0x00FFFFFF);
    }

    public static int mix(int first, int second, float t)
    {
        float f = Math.max(0.0F, Math.min(1.0F, t));
        int a = (int) (((first >>> 24) & 0xFF) + (((second >>> 24) & 0xFF) - ((first >>> 24) & 0xFF)) * f);
        int r = (int) (((first >> 16) & 0xFF) + (((second >> 16) & 0xFF) - ((first >> 16) & 0xFF)) * f);
        int g = (int) (((first >> 8) & 0xFF) + (((second >> 8) & 0xFF) - ((first >> 8) & 0xFF)) * f);
        int b = (int) ((first & 0xFF) + ((second & 0xFF) - (first & 0xFF)) * f);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    public static float easeOut(float t)
    {
        float f = Math.max(0.0F, Math.min(1.0F, t));
        return 1.0F - (1.0F - f) * (1.0F - f) * (1.0F - f);
    }

    public static float lerp(float from, float to, float t)
    {
        return from + (to - from) * Math.max(0.0F, Math.min(1.0F, t));
    }

    public static int clamp(int value, int min, int max)
    {
        return value < min ? min : Math.min(value, max);
    }

    public static double clamp(double value, double min, double max)
    {
        return value < min ? min : Math.min(value, max);
    }

    public static boolean inside(double x, double y, float left, float top, float width, float height)
    {
        return x >= left && x < left + width && y >= top && y < top + height;
    }
}
