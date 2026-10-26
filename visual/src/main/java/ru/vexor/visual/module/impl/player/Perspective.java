package ru.vexor.visual.module.impl.player;

import net.minecraft.client.CameraType;
import net.minecraft.client.Minecraft;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.EnumSetting;

public final class Perspective extends Module
{
    public enum Mode
    {
        FIRST("Первое лицо", CameraType.FIRST_PERSON),
        THIRD_BACK("Третье лицо", CameraType.THIRD_PERSON_BACK),
        THIRD_FRONT("Спереди", CameraType.THIRD_PERSON_FRONT);

        private final String label;
        private final CameraType type;

        Mode(String label, CameraType type)
        {
            this.label = label;
            this.type = type;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private final EnumSetting<Mode> mode = add(new EnumSetting<>("Вид", Mode.THIRD_BACK));
    private CameraType previous;

    public Perspective()
    {
        super("Perspective", "Фиксированный вид камеры", Category.PLAYER);
        setDisplayName("Перспектива");
    }

    @Override
    public void onEnable()
    {
        previous = Minecraft.getInstance().options.getCameraType();
        apply();
    }

    @Override
    public void onTick()
    {
        apply();
    }

    @Override
    public void onDisable()
    {
        if (previous != null)
        {
            Minecraft.getInstance().options.setCameraType(previous);
            previous = null;
        }
    }

    private void apply()
    {
        Minecraft.getInstance().options.setCameraType(mode.getValue().type);
    }
}
