package ru.vexor.visual.module;

public enum Category
{
    RENDER("Render", "Отрисовка"),
    VISUALS("Visuals", "Визуал"),
    HUD("HUD", "Интерфейс"),
    PLAYER("Player", "Игрок"),
    WORLD("World", "Мир"),
    COSMETICS("Cosmetics", "Косметика"),
    SETTINGS("Settings", "Настройки");

    private final String englishName;
    private final String displayName;

    Category(String englishName, String displayName)
    {
        this.englishName = englishName;
        this.displayName = displayName;
    }

    public String getEnglishName()
    {
        return englishName;
    }

    public String getDisplayName()
    {
        return displayName;
    }
}
