package ru.vexor.visual.module.impl.player;

import com.mojang.blaze3d.platform.InputConstants;
import net.minecraft.client.Minecraft;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.KeySetting;
import ru.vexor.visual.util.ViewState;

public final class Freelook extends Module
{
    private final KeySetting holdKey = add(new KeySetting("Клавиша обзора", GLFW.GLFW_KEY_LEFT_ALT));

    public Freelook()
    {
        super("Freelook", "Осмотр вокруг без поворота тела", Category.PLAYER);
        setDisplayName("Свободный обзор");
    }

    @Override
    public void onDisable()
    {
        ViewState.stopFreelook();
    }

    @Override
    public void onTick()
    {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null)
        {
            return;
        }
        boolean down = holdKey.isBound() && InputConstants.isKeyDown(minecraft.getWindow(), holdKey.getValue());
        if (down && !ViewState.freelookActive)
        {
            ViewState.startFreelook(minecraft.gameRenderer.getMainCamera());
        }
        else if (!down && ViewState.freelookActive)
        {
            ViewState.stopFreelook();
        }
    }
}
