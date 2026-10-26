package ru.vexor.visual.module.impl.hud;

import com.mojang.blaze3d.platform.InputConstants;
import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.util.Draw;

public final class Keystrokes extends HudModule
{
    private final BoolSetting showSpace = add(new BoolSetting("Пробел", true));
    private final BoolSetting showShift = add(new BoolSetting("Shift", true));

    public Keystrokes()
    {
        super("Keystrokes", "Нажатия клавиш", -1);
        setDisplayName("Клавиши");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        int height = 4 + 14 + 3 + 14 + 3;
        if (showSpace.getValue())
        {
            height += 12 + 3;
        }
        if (showShift.getValue())
        {
            height += 12 + 3;
        }
        height += 1;

        int width = 68;
        card(graphics, x, y, width, height);

        com.mojang.blaze3d.platform.Window window = Minecraft.getInstance().getWindow();
        int keyWidth = 20;
        int keyHeight = 14;
        int top = y + 4;
        key(graphics, x + (width - keyWidth) / 2, top, keyWidth, keyHeight, "W", InputConstants.isKeyDown(window, GLFW.GLFW_KEY_W));
        int second = top + keyHeight + 3;
        key(graphics, x + 4, second, keyWidth, keyHeight, "A", InputConstants.isKeyDown(window, GLFW.GLFW_KEY_A));
        key(graphics, x + 4 + keyWidth + 2, second, keyWidth, keyHeight, "S", InputConstants.isKeyDown(window, GLFW.GLFW_KEY_S));
        key(graphics, x + 4 + (keyWidth + 2) * 2, second, keyWidth, keyHeight, "D", InputConstants.isKeyDown(window, GLFW.GLFW_KEY_D));

        int row = second + keyHeight + 3;
        if (showSpace.getValue())
        {
            key(graphics, x + 4, row, width - 8, 12, "SPACE", InputConstants.isKeyDown(window, GLFW.GLFW_KEY_SPACE));
            row += 15;
        }
        if (showShift.getValue())
        {
            key(graphics, x + 4, row, width - 8, 12, "SHIFT",
                    InputConstants.isKeyDown(window, GLFW.GLFW_KEY_LEFT_SHIFT)
                            || InputConstants.isKeyDown(window, GLFW.GLFW_KEY_RIGHT_SHIFT));
        }
        return height;
    }

    private void key(GuiGraphics graphics, int x, int y, int width, int height, String label, boolean pressed)
    {
        int accent = accent();
        if (pressed)
        {
            Draw.glow(graphics, x, y, width, height, 4, Draw.withAlpha(accent, 60), 2);
        }
        int fill = pressed ? Draw.withAlpha(accent, 170) : 0x99141022;
        Draw.roundedRect(graphics, x, y, width, height, 4, fill);
        Draw.roundedRect(graphics, x, y, width, Math.max(5, height / 2), 4, 0x0DFFFFFF);
        Draw.outline(graphics, x, y, width, height, 4, pressed ? accent : 0x1FFFFFFF);
        Draw.textCentered(graphics, font(), label, x + width / 2, y + (height - 11) / 2,
                pressed ? 0xFFFFFFFF : 0xFFCFC8E6);
    }
}
