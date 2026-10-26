package ru.vexor.visual.config;

import com.google.gson.JsonObject;

public final class GuiConfig
{
    private double scale = 1.0D;
    private double opacity = 0.88D;
    private int accent = 0xFF8B5CF6;
    private boolean animations = true;
    private int openKey = 344;
    private int toggleKey = -1;
    private int panelX = -1;
    private int panelY = -1;

    public double getScale()
    {
        return scale;
    }

    public void setScale(double scale)
    {
        this.scale = Math.max(0.7D, Math.min(1.6D, scale));
    }

    public double getOpacity()
    {
        return opacity;
    }

    public void setOpacity(double opacity)
    {
        this.opacity = Math.max(0.5D, Math.min(1.0D, opacity));
    }

    public int getAccent()
    {
        return accent;
    }

    public void setAccent(int accent)
    {
        this.accent = accent | 0xFF000000;
    }

    public boolean isAnimations()
    {
        return animations;
    }

    public void setAnimations(boolean animations)
    {
        this.animations = animations;
    }

    public int getOpenKey()
    {
        return openKey;
    }

    public void setOpenKey(int openKey)
    {
        this.openKey = openKey;
    }

    public int getToggleKey()
    {
        return toggleKey;
    }

    public void setToggleKey(int toggleKey)
    {
        this.toggleKey = toggleKey;
    }

    public int getPanelX()
    {
        return panelX;
    }

    public void setPanelX(int panelX)
    {
        this.panelX = panelX;
    }

    public int getPanelY()
    {
        return panelY;
    }

    public void setPanelY(int panelY)
    {
        this.panelY = panelY;
    }

    public JsonObject toJson()
    {
        JsonObject object = new JsonObject();
        object.addProperty("scale", scale);
        object.addProperty("opacity", opacity);
        object.addProperty("accent", accent);
        object.addProperty("animations", animations);
        object.addProperty("openKey", openKey);
        object.addProperty("toggleKey", toggleKey);
        object.addProperty("panelX", panelX);
        object.addProperty("panelY", panelY);
        return object;
    }

    public void fromJson(JsonObject object)
    {
        if (object.has("scale")) setScale(object.get("scale").getAsDouble());
        if (object.has("opacity")) setOpacity(object.get("opacity").getAsDouble());
        if (object.has("accent")) setAccent(object.get("accent").getAsInt());
        if (object.has("animations")) setAnimations(object.get("animations").getAsBoolean());
        if (object.has("openKey")) setOpenKey(object.get("openKey").getAsInt());
        if (object.has("toggleKey")) setToggleKey(object.get("toggleKey").getAsInt());
        if (object.has("panelX")) setPanelX(object.get("panelX").getAsInt());
        if (object.has("panelY")) setPanelY(object.get("panelY").getAsInt());
    }

    public void reset()
    {
        scale = 1.0D;
        opacity = 0.88D;
        accent = 0xFF8B5CF6;
        animations = true;
        openKey = 344;
        toggleKey = -1;
        panelX = -1;
        panelY = -1;
    }
}
