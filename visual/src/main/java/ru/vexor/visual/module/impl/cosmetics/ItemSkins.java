package ru.vexor.visual.module.impl.cosmetics;

import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.setting.EnumSetting;
import ru.vexor.visual.util.BuiltinPackManager;

import java.util.ArrayList;
import java.util.List;

public final class ItemSkins extends Module
{
    public enum TotemSkin
    {
        OFF("Выкл", null),
        NEON("Неоновый", "totem_neon"),
        GOLD("Золотой", "totem_gold"),
        CRIMSON("Багровый", "totem_crimson"),
        FROST("Ледяной", "totem_frost"),
        EMERALD("Изумрудный", "totem_emerald");

        private final String label;
        private final String pack;

        TotemSkin(String label, String pack)
        {
            this.label = label;
            this.pack = pack;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    public enum SwordSkin
    {
        OFF("Выкл", null),
        ELECTRO("Электро", "sword_electro"),
        NEON("Неоновый", "sword_neon"),
        CRIMSON("Багровый", "sword_crimson"),
        GOLD("Золотой", "sword_gold"),
        EMERALD("Изумрудный", "sword_emerald");

        private final String label;
        private final String pack;

        SwordSkin(String label, String pack)
        {
            this.label = label;
            this.pack = pack;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private final EnumSetting<TotemSkin> totem = add(new EnumSetting<>("Тотем", TotemSkin.OFF));
    private final EnumSetting<SwordSkin> sword = add(new EnumSetting<>("Меч", SwordSkin.OFF));

    public ItemSkins()
    {
        super("Item Skins", "Встроенные текстуры тотемов и мечей", Category.COSMETICS);
        setDisplayName("Скины предметов");
    }

    @Override
    public void onEnable()
    {
        sync();
    }

    @Override
    public void onTick()
    {
        sync();
    }

    @Override
    public void onDisable()
    {
        BuiltinPackManager.clear();
    }

    private void sync()
    {
        List<String> ids = new ArrayList<>(2);
        if (totem.getValue().pack != null)
        {
            ids.add(BuiltinPackManager.id(totem.getValue().pack));
        }
        if (sword.getValue().pack != null)
        {
            ids.add(BuiltinPackManager.id(sword.getValue().pack));
        }
        BuiltinPackManager.setDesired(ids);
    }
}
