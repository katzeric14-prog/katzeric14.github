package ru.vexor.visual.module.impl.player;

import com.mojang.blaze3d.platform.InputConstants;
import net.minecraft.client.Minecraft;
import org.lwjgl.glfw.GLFW;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.KeySetting;

public final class Zoom extends Module
{
    private static Zoom instance;

    private final DoubleSetting factor = add(new DoubleSetting("Кратность", 4.0D, 2.0D, 10.0D, 0.5D, 1));
    private final KeySetting holdKey = add(new KeySetting("Клавиша зума", GLFW.GLFW_KEY_C));

    private boolean held;

    public Zoom()
    {
        super("Zoom", "Приближение по удержанию клавиши", Category.PLAYER);
        setDisplayName("Зум");
        instance = this;
    }

    @Override
    public void onEnable()
    {
        held = false;
    }

    @Override
    public void onDisable()
    {
        held = false;
    }

    @Override
    public void onTick()
    {
        Minecraft minecraft = Minecraft.getInstance();
        held = holdKey.isBound() && InputConstants.isKeyDown(minecraft.getWindow(), holdKey.getValue());
    }

    public static float getZoomFactor()
    {
        if (instance == null || !instance.isEnabled() || !instance.held || Minecraft.getInstance().screen != null)
        {
            return 1.0F;
        }
        if (!ModuleManager.get().isVisualEnabled())
        {
            return 1.0F;
        }
        return (float) (double) instance.factor.getValue();
    }
}
