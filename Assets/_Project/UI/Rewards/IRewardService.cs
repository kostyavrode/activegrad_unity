using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Одна строка награды для экранов наград (иконка + количество).
/// Id: "coins", "xp", "metal", "wood", "blueprints" (или любой другой).
/// </summary>
[Serializable]
public struct RewardEntry
{
    public string Id;
    public string Label;
    public int Amount;
    public Sprite Icon;
    public Color Tint;

    public RewardEntry(string id, string label, int amount, Color tint, Sprite icon = null)
    {
        Id = id;
        Label = label;
        Amount = amount;
        Tint = tint;
        Icon = icon;
    }

    /// <summary>Tint с учётом значения по умолчанию (если Tint не задан — alpha == 0).</summary>
    public Color EffectiveTint => Tint.a > 0.01f ? Tint : DefaultTint(Id);

    public static RewardEntry Coins(int amount)      => new RewardEntry("coins", "Монеты", amount, DefaultTint("coins"));
    public static RewardEntry Xp(int amount)         => new RewardEntry("xp", "Опыт", amount, DefaultTint("xp"));
    public static RewardEntry Metal(int amount)      => new RewardEntry("metal", "Металл", amount, DefaultTint("metal"));
    public static RewardEntry Wood(int amount)       => new RewardEntry("wood", "Дерево", amount, DefaultTint("wood"));
    public static RewardEntry Blueprints(int amount) => new RewardEntry("blueprints", "Чертежи", amount, DefaultTint("blueprints"));

    /// <summary>Список ненулевых ресурсов (металл/дерево/чертежи).</summary>
    public static List<RewardEntry> FromResources(int metal, int wood, int blueprints)
    {
        var list = new List<RewardEntry>(3);
        if (metal > 0)      list.Add(Metal(metal));
        if (wood > 0)       list.Add(Wood(wood));
        if (blueprints > 0) list.Add(Blueprints(blueprints));
        return list;
    }

    public static Color DefaultTint(string id)
    {
        switch ((id ?? "").ToLowerInvariant())
        {
            case "coins":      return new Color(1.00f, 0.78f, 0.18f); // золото
            case "xp":         return new Color(0.62f, 0.42f, 1.00f); // фиолетовый
            case "metal":      return new Color(0.45f, 0.62f, 0.80f); // стальной синий
            case "wood":       return new Color(0.62f, 0.42f, 0.24f); // коричневый
            case "blueprints": return new Color(0.25f, 0.85f, 0.95f); // циан
            default:           return new Color(0.35f, 0.85f, 0.45f); // зелёный
        }
    }
}

/// <summary>
/// Экраны наград: церемония квеста, новый уровень, компактная плашка ресурсов, полёт валюты в HUD.
/// Все полноэкранные показы ставятся в очередь и не перекрывают друг друга.
/// </summary>
public interface IRewardService
{
    void ShowQuestComplete(string title, IReadOnlyList<RewardEntry> rewards);
    void ShowLevelUp(int oldLevel, int newLevel, int statPointsGained);
    void ShowResources(string header, IReadOnlyList<RewardEntry> rewards);
    void FlyCurrency(RewardEntry entry, Vector2 fromScreenPos, RectTransform target = null);
}
