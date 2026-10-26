package ru.vexor.visual.module.impl.hud;

import net.minecraft.client.DeltaTracker;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;
import ru.vexor.visual.module.HudModule;
import ru.vexor.visual.setting.BoolSetting;

public final class ArmorStatus extends HudModule
{
    private static final EquipmentSlot[] SLOTS = {
            EquipmentSlot.HEAD, EquipmentSlot.CHEST, EquipmentSlot.LEGS, EquipmentSlot.FEET
    };

    private final BoolSetting durability = add(new BoolSetting("Прочность", true));

    public ArmorStatus()
    {
        super("Armor Status", "Броня и её прочность", -1);
        setDisplayName("Броня");
    }

    @Override
    public int render(GuiGraphics graphics, DeltaTracker delta, int x, int y)
    {
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null)
        {
            return 0;
        }
        boolean any = false;
        for (EquipmentSlot slot : SLOTS)
        {
            if (!player.getItemBySlot(slot).isEmpty())
            {
                any = true;
                break;
            }
        }
        if (!any)
        {
            return 0;
        }

        int width = 4 + SLOTS.length * 20;
        int height = 26;
        card(graphics, x, y, width, height);
        for (int i = 0; i < SLOTS.length; i++)
        {
            ItemStack stack = player.getItemBySlot(SLOTS[i]);
            int itemX = x + 4 + i * 20;
            int itemY = y + 4;
            if (stack.isEmpty())
            {
                continue;
            }
            graphics.renderItem(stack, itemX, itemY);
            if (durability.getValue() && stack.isDamageableItem() && stack.getMaxDamage() > 0)
            {
                float ratio = 1.0F - stack.getDamageValue() / (float) stack.getMaxDamage();
                int barColor = ratio > 0.5F ? 0xFF4CFF8A : (ratio > 0.2F ? 0xFFFFC857 : 0xFFFF5D73);
                graphics.fill(itemX, itemY + 17, itemX + 16, itemY + 19, 0xAA000000);
                graphics.fill(itemX, itemY + 17, itemX + Math.max(1, Math.round(16 * ratio)), itemY + 19, barColor);
            }
        }
        return height;
    }
}
