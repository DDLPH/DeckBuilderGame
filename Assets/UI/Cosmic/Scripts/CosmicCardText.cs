using System.Collections.Generic;

// Shared presentation only. All numbers come from actual effects, not duplicated card descriptions.
public static class CosmicCardText
{
    public static string Name(CardData card)
    {
        if (card == null) return "";
        if (!CosmicLanguage.IsThai) return card.CardName;
        string suffix = card.name.Contains("+") ? "+" : "";
        if (card.name.StartsWith("Strike")) return "ฟันโจมตี" + suffix;
        if (card.name.StartsWith("Defend")) return "ตั้งรับ" + suffix;
        switch (card.name)
        {
            case "AstralCut": return "คมดาบดารา";
            case "MoonWard": return "ม่านจันทรา";
            case "Starfall": return "ดาวถล่ม";
            case "Eclipse": return "คมคราส";
            case "Insight": return "หยั่งรู้";
            case "Focus": return "รวมสมาธิ";
        }
        return card.CardName;
    }

    public static string Description(CardData card)
    {
        if (card == null) return "";
        bool thai = CosmicLanguage.IsThai;
        var lines = new List<string>();
        if (card.Effects != null) foreach (CardEffect effect in card.Effects)
        {
            if (effect is DamageEffect damage) lines.Add(thai ? "สร้างความเสียหาย " + damage.DamageAmount : "Deal " + damage.DamageAmount + " damage.");
            else if (effect is BlockEffect block) lines.Add(thai ? "ได้รับเกราะ " + block.BlockAmount : "Gain " + block.BlockAmount + " Block.");
            else if (effect is DrawCardsEffect draw) lines.Add(thai ? "จั่ว " + draw.Amount + " ใบ" : "Draw " + draw.Amount + " cards.");
            else if (effect is GainEnergyEffect energy) lines.Add(thai ? "ได้รับพลังงาน " + energy.Amount : "Gain " + energy.Amount + " Energy.");
            else if (effect != null) return card.Description;
        }
        if (card.Exhausts) lines.Add(thai ? "เผาทิ้งหลังใช้" : "Exhaust.");
        if (BattleManager.TargetsEnemySide(card) && !BattleManager.NeedsEnemy(card)) lines.Add(thai ? "ศัตรูทั้งหมด" : "All enemies.");
        return lines.Count == 0 ? card.Description : string.Join("\n", lines);
    }

    public static string Type(CardData card)
    {
        if (!CosmicLanguage.IsThai) return card.CardType.ToString().ToUpperInvariant();
        return card.CardType == CardType.Attack ? "โจมตี" : card.CardType == CardType.Power ? "พลัง" : "ทักษะ";
    }
    public static string Rarity(CardData card)
    {
        if (!CosmicLanguage.IsThai) return card.Rarity.ToString().ToUpperInvariant();
        return card.Rarity == CardRarity.Rare ? "หายาก" : card.Rarity == CardRarity.Uncommon ? "ไม่ธรรมดา" : "ทั่วไป";
    }
    public static AstralMapGraphic.Motif Icon(CardData card)
    {
        if (card.Effects != null) foreach (CardEffect effect in card.Effects)
        {
            if (effect is DrawCardsEffect) return AstralMapGraphic.Motif.Crescent;
            if (effect is GainEnergyEffect) return AstralMapGraphic.Motif.Eclipse;
        }
        return BattleManager.TargetsEnemySide(card) ? AstralMapGraphic.Motif.Swords : AstralMapGraphic.Motif.Shield;
    }
}
