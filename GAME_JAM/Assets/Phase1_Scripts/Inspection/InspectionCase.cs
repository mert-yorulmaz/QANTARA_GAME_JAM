using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Flags]
public enum RuleViolation
{
    None = 0,
    OutdatedReport = 1 << 0,        // 1.1
    NoSealOrSignature = 1 << 1,     // 1.2
    StructuralDamage = 1 << 2,      // 2.1
    MissingWallSupplement = 1 << 3, // 2.2
    NoFireExtinguisher = 1 << 4,    // 3.1
    BlockedExits = 1 << 5,          // 3.2
    IncompleteBag = 1 << 6,         // 3.3
    Bribe = 1 << 7                  // 4.1
}

public enum GroundUse { Residential, Shop, Market, CarDealer, Warehouse }
public enum ExitStatus { OpenAndAccessible, Locked, BlockedByStorage }
public enum Attachment { None, EnvelopeWithCash, GiftBasket, ArchitectLetter }
public enum DamageType { None, LargeCracks, CutColumn, Thinning }

[Flags]
public enum BagItem
{
    None = 0,
    Water = 1 << 0,
    Flashlight = 1 << 1,
    FirstAid = 1 << 2,
    Whistle = 1 << 3
}

[Serializable]
public class InspectionCase
{
    public string building;
    public string address;
    [Tooltip("dd MMM yyyy, örn. 03 Mar 2026")]
    public string filed;
    public GroundUse groundUse;
    [TextArea] public string notes;

    [Header("Declared by owner")]
    public bool signed = true;
    public bool wallReportAttached;
    public BagItem bag = BagItem.Water | BagItem.Flashlight | BagItem.FirstAid | BagItem.Whistle;

    [Header("Check against rules")]
    public bool seal = true;
    public ExitStatus exits;
    public Attachment attached;

    [Header("Photo / X-Ray")]
    public int photoSeed;
    public DamageType damage;
    [Range(0, 3)] public int damageColumn;
    [Range(0, 2)] public int damageFloor;
    [Range(0, 3)] public int fireExtinguishers = 1;
}

public static class InspectionRules
{
    public const BagItem FullBag = BagItem.Water | BagItem.Flashlight | BagItem.FirstAid | BagItem.Whistle;
    public const string DateFormat = "dd MMM yyyy";

    // Ret formundaki kutuların sırası (4x2).
    public static readonly RuleViolation[] All =
    {
        RuleViolation.OutdatedReport,
        RuleViolation.NoSealOrSignature,
        RuleViolation.StructuralDamage,
        RuleViolation.MissingWallSupplement,
        RuleViolation.NoFireExtinguisher,
        RuleViolation.BlockedExits,
        RuleViolation.IncompleteBag,
        RuleViolation.Bribe
    };

    public static string Code(RuleViolation rule)
    {
        switch (rule)
        {
            case RuleViolation.OutdatedReport: return "1.1";
            case RuleViolation.NoSealOrSignature: return "1.2";
            case RuleViolation.StructuralDamage: return "2.1";
            case RuleViolation.MissingWallSupplement: return "2.2";
            case RuleViolation.NoFireExtinguisher: return "3.1";
            case RuleViolation.BlockedExits: return "3.2";
            case RuleViolation.IncompleteBag: return "3.3";
            case RuleViolation.Bribe: return "4.1";
            default: return "";
        }
    }

    public static string ChipLabel(RuleViolation rule)
    {
        switch (rule)
        {
            case RuleViolation.OutdatedReport: return "Outdated report";
            case RuleViolation.NoSealOrSignature: return "No seal or signature";
            case RuleViolation.StructuralDamage: return "Structural damage";
            case RuleViolation.MissingWallSupplement: return "Missing wall supplement";
            case RuleViolation.NoFireExtinguisher: return "No fire extinguisher";
            case RuleViolation.BlockedExits: return "Blocked exits";
            case RuleViolation.IncompleteBag: return "Incomplete emergency bag";
            case RuleViolation.Bribe: return "Bribe attached";
            default: return "";
        }
    }

    public static string CodeList(RuleViolation set, string separator = " · ")
    {
        var codes = new List<string>();
        foreach (var rule in All)
            if ((set & rule) != 0)
                codes.Add(Code(rule));
        return string.Join(separator, codes);
    }

    public static bool TryParseDate(string text, out DateTime date)
    {
        return DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static RuleViolation Evaluate(InspectionCase c, DateTime outdatedBefore)
    {
        var result = RuleViolation.None;

        if (!TryParseDate(c.filed, out DateTime filed) || filed < outdatedBefore)
            result |= RuleViolation.OutdatedReport;
        if (!c.signed || !c.seal)
            result |= RuleViolation.NoSealOrSignature;
        if (c.damage != DamageType.None)
            result |= RuleViolation.StructuralDamage;
        if (c.groundUse != GroundUse.Residential && !c.wallReportAttached)
            result |= RuleViolation.MissingWallSupplement;
        if (c.fireExtinguishers <= 0)
            result |= RuleViolation.NoFireExtinguisher;
        if (c.exits != ExitStatus.OpenAndAccessible)
            result |= RuleViolation.BlockedExits;
        if ((c.bag & FullBag) != FullBag)
            result |= RuleViolation.IncompleteBag;
        if (c.attached == Attachment.EnvelopeWithCash || c.attached == Attachment.GiftBasket)
            result |= RuleViolation.Bribe;

        return result;
    }

    public static string Display(GroundUse use)
    {
        switch (use)
        {
            case GroundUse.Shop: return "Commercial · Shop";
            case GroundUse.Market: return "Commercial · Market";
            case GroundUse.CarDealer: return "Commercial · Car dealer";
            case GroundUse.Warehouse: return "Commercial · Warehouse";
            default: return "Residential";
        }
    }

    public static string Display(BagItem bag)
    {
        var items = new List<string>();
        if ((bag & BagItem.Water) != 0) items.Add("Water");
        if ((bag & BagItem.Flashlight) != 0) items.Add("Flashlight");
        if ((bag & BagItem.FirstAid) != 0) items.Add("First aid");
        if ((bag & BagItem.Whistle) != 0) items.Add("Whistle");
        return items.Count == 0 ? "Empty" : string.Join(" · ", items);
    }

    public static string Display(ExitStatus exits)
    {
        switch (exits)
        {
            case ExitStatus.Locked: return "Locked";
            case ExitStatus.BlockedByStorage: return "Blocked by storage";
            default: return "Open and accessible";
        }
    }

    public static string Display(Attachment attached)
    {
        switch (attached)
        {
            case Attachment.EnvelopeWithCash: return "Envelope with cash";
            case Attachment.GiftBasket: return "Gift basket";
            case Attachment.ArchitectLetter: return "Architect's letter";
            default: return "None";
        }
    }

    public static string WallReport(InspectionCase c)
    {
        if (c.wallReportAttached)
            return "Attached";
        return c.groundUse == GroundUse.Residential ? "Not required" : "Missing";
    }

    // Mesai 3 dosya: temiz (onay), rüşvetli çoklu ihlal, X-RAY'de gizli çatlak.
    // (Yıldız Market -> 2.2 ve Çınar Towers -> 1.1 · 1.2 · 3.1 dosyaları kısalık için çıkarıldı.)
    public static List<InspectionCase> DefaultCases()
    {
        return new List<InspectionCase>
        {
            // Temiz dosya -> APPROVE
            new InspectionCase
            {
                building = "Gül Apartments", address = "14 Gül Street", filed = "03 Mar 2026",
                groundUse = GroundUse.Residential, notes = "Routine inspection. Tenants cooperative.",
                photoSeed = 11, fireExtinguishers = 2
            },
            // Kesik kolon, kilitli çıkış, eksik çanta, rüşvet -> 2.1 · 3.2 · 3.3 · 4.1
            new InspectionCase
            {
                building = "Demir Residence", address = "27 Plane Tree Street", filed = "19 Feb 2026",
                groundUse = GroundUse.Residential, notes = "Owner is elderly and asks for leniency.",
                bag = BagItem.Water | BagItem.Flashlight | BagItem.Whistle,
                exits = ExitStatus.Locked, attached = Attachment.EnvelopeWithCash,
                photoSeed = 37, damage = DamageType.CutColumn, damageColumn = 1, damageFloor = 1,
                fireExtinguishers = 1
            },
            // Belgeler kusursuz ama X-RAY'de büyük çatlak -> 2.1
            new InspectionCase
            {
                building = "Lale Residence", address = "52 Tulip Lane", filed = "27 Feb 2026",
                groundUse = GroundUse.Shop, notes = "Facade freshly painted. Owner says the building is like new.",
                wallReportAttached = true,
                photoSeed = 58, damage = DamageType.LargeCracks, damageColumn = 2, damageFloor = 0,
                fireExtinguishers = 1
            }
        };
    }
}
