package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.util.Draw;

import java.util.ArrayDeque;
import java.util.Deque;

public final class CpsHud extends HudModule
{
    private final BoolSetting rightButton = add(new BoolSetting("Правая кнопка", true));

    private final Deque<Long> leftClicks = new ArrayDeque<>();
    private final Deque<Long> rightClicks = new ArrayDeque<>();
    private boolean leftDown;
    private boolean rightDown;

    public CpsHud()
    {
        super("CPS", "Кликов в секунду", -1);
        setDisplayName("CPS");
    }

    @Override
    public void onTick()
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.screen != null)
        {
            leftDown = false;
            rightDown = false;
            return;
        }
        long window = minecraft.getWindow().handle();
        boolean left = GLFW.glfwGetMouseButton(window, GLFW.GLFW_MOUSE_BUTTON_LEFT) == GLFW.GLFW_PRESS;
        boolean right = GLFW.glfwGetMouseButton(window, GLFW.GLFW_MOUSE_BUTTON_RIGHT) == GLFW.GLFW_PRESS;
        long now = System.currentTimeMillis();
        if (left && !leftDown)
        {
            leftClicks.addLast(now);
        }
        if (right && !rightDown)
        {
            rightClicks.addLast(now);
        }
        leftDown = left;
        rightDown = right;
        trim(leftClicks, now);
        trim(rightClicks, now);
    }

    private static void trim(Deque<Long> clicks, long now)
    {
        while (!clicks.isEmpty() && now - clicks.peekFirst() > 1000L)
        {
            clicks.pollFirst();
        }
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        long now = System.currentTimeMillis();
        trim(leftClicks, now);
        trim(rightClicks, now);
        String left = "L " + leftClicks.size();
        String right = rightButton.getValue() ? "  R " + rightClicks.size() : "";
        String text = left + right;
        int width = Draw.width(text) + 10;
        card(graphics, x, y, width, 18);
        Draw.text(graphics, font(), left, x + 4, y + 5, accent());
        if (rightButton.getValue())
        {
            Draw.text(graphics, font(), "R " + rightClicks.size(), x + 4 + Draw.width(left) + 6, y + 5, 0xFFFFC857);
        }
        return 18;
    }
}
