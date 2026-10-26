package ru.vexor.visual.module.impl.visuals;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.entity.HumanoidArm;
import ru.vexor.visual.module.Category;
import ru.vexor.visual.module.Module;
import ru.vexor.visual.module.ModuleManager;
import ru.vexor.visual.setting.BoolSetting;
import ru.vexor.visual.setting.DoubleSetting;
import ru.vexor.visual.setting.EnumSetting;

public final class HandAnimations extends Module
{
    public enum Preset
    {
        SMOOTH("Плавная"),
        SWAY("Волна"),
        TILT("Разворот"),
        FIGURE("Восьмёрка"),
        COMBAT("Боевая"),
        STATIC("Прижатая");

        private final String label;

        Preset(String label)
        {
            this.label = label;
        }

        @Override
        public String toString()
        {
            return label;
        }
    }

    private static HandAnimations instance;

    private final EnumSetting<Preset> preset = add(new EnumSetting<>("Анимация", Preset.SMOOTH));
    private final DoubleSetting strength = add(new DoubleSetting("Сила", 1.0D, 0.2D, 2.0D, 0.1D, 1));
    private final BoolSetting idleOnly = add(new BoolSetting("Только в покое", false));

    public HandAnimations()
    {
        super("Hand Animations", "Анимации предмета в руке от первого лица", Category.VISUALS);
        setDisplayName("Анимации рук");
        instance = this;
    }

    public static void apply(PoseStack pose, InteractionHand hand, float partialTick)
    {
        if (instance == null || !ModuleManager.get().isActive("Hand Animations"))
        {
            return;
        }
        LocalPlayer player = Minecraft.getInstance().player;
        if (player == null)
        {
            return;
        }
        float walkPosition = player.walkAnimation.position(partialTick);
        float walkSpeed = player.walkAnimation.speed(partialTick);
        boolean moving = walkSpeed > 0.05F || player.getDeltaMovement().horizontalDistanceSqr() > 0.0015D;
        float swing = player.getAttackAnim(partialTick);
        if (instance.idleOnly.getValue() && (moving || swing > 0.05F))
        {
            return;
        }

        HumanoidArm arm = hand == InteractionHand.MAIN_HAND
                ? player.getMainArm()
                : player.getMainArm().getOpposite();
        float side = arm == HumanoidArm.RIGHT ? 1.0F : -1.0F;
        float power = (float) (double) instance.strength.getValue();
        float time = player.tickCount + partialTick;
        float seconds = time / 20.0F;

        switch (instance.preset.getValue())
        {
            case SMOOTH ->
            {
                pose.translate(0.0F, (float) Math.sin(seconds * 1.6D) * 0.006F * power,
                        (float) Math.cos(seconds * 1.1D) * 0.004F * power);
                pose.mulPose(Axis.ZP.rotationDegrees((float) Math.sin(seconds * 1.1D) * 1.4F * side * power));
                pose.mulPose(Axis.XP.rotationDegrees((float) Math.cos(seconds * 1.3D) * 1.2F * power));
            }
            case SWAY ->
            {
                float walk = (float) Math.sin(walkPosition * Math.PI);
                pose.translate(side * walk * 0.012F * power, (float) Math.cos(walkPosition * Math.PI * 2.0D) * 0.008F * power,
                        0.0F);
                pose.mulPose(Axis.ZP.rotationDegrees(walk * 3.2F * side * power));
                pose.mulPose(Axis.YP.rotationDegrees(walk * 2.0F * side * power));
            }
            case TILT ->
            {
                pose.translate(-0.035F * side * power, -0.01F * power, 0.0F);
                pose.mulPose(Axis.YP.rotationDegrees(-14.0F * side * power));
                pose.mulPose(Axis.ZP.rotationDegrees(7.0F * side * power));
            }
            case FIGURE ->
            {
                pose.translate((float) Math.sin(seconds * 1.4D) * 0.008F * power,
                        (float) Math.sin(seconds * 2.8D) * 0.006F * power, 0.0F);
                pose.mulPose(Axis.XP.rotationDegrees((float) Math.sin(seconds * 2.8D) * 3.0F * power));
                pose.mulPose(Axis.YP.rotationDegrees((float) Math.sin(seconds * 1.4D) * 4.0F * side * power));
                pose.mulPose(Axis.ZP.rotationDegrees((float) Math.cos(seconds * 2.8D) * 2.0F * side * power));
            }
            case COMBAT ->
            {
                pose.translate(0.0F, -0.02F * swing * power, -0.03F * swing * power);
                pose.mulPose(Axis.XP.rotationDegrees(-26.0F * swing * power));
                pose.mulPose(Axis.YP.rotationDegrees(10.0F * swing * side * power));
            }
            case STATIC ->
            {
                pose.translate(0.0F, -0.015F * power, -0.02F * power);
                pose.mulPose(Axis.ZP.rotationDegrees(3.0F * side * power));
                pose.mulPose(Axis.XP.rotationDegrees(2.0F * power));
            }
        }
    }
}
