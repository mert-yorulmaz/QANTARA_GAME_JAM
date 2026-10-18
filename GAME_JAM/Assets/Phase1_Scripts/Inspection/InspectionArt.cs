using System.Collections.Generic;
using UnityEngine;

// Dosya fotoğrafı ve X-RAY işaretlerini koddan çizer.
public static class InspectionArt
{
    // Fotoğraf: 230x102 tasarım birimi, 2 birim = 1 piksel.
    public const int PhotoWidth = 115;
    public const int PhotoHeight = 51;

    // X-RAY katmanı xray_overlay_grid ile aynı alan: 236x108, 1 birim = 1 piksel.
    public const int PlanWidth = 236;
    public const int PlanHeight = 108;

    // Fotoğraf kartının eğikliği (derece, saat yönü tersine).
    public const float CardTilt = 1.48f;

    // xray_overlay_grid.png üzerindeki kolon ve kiriş çizgileri (eğiklik öncesi, üstten).
    static readonly float[] ColumnX = { 43f, 96.75f, 150.75f, 204.5f };
    static readonly float[] BeamY = { 13.25f, 40.25f, 69.25f, 98.25f };

    static readonly Color32 DamageColor = new Color32(0xFF, 0x5E, 0x6C, 0xFF);
    static readonly Color32 ExtinguisherColor = new Color32(0xFF, 0xC4, 0x4D, 0xFF);
    static readonly Color32 PlanBackground = new Color32(0x12, 0x2C, 0x4E, 0xFF);

    struct Palette
    {
        public Color32 wall;
        public Color32 shade;

        public Palette(Color32 wall, Color32 shade)
        {
            this.wall = wall;
            this.shade = shade;
        }
    }

    static readonly Palette[] Palettes =
    {
        new Palette(new Color32(176, 92, 64, 255), new Color32(140, 70, 50, 255)),   // tuğla
        new Palette(new Color32(218, 200, 160, 255), new Color32(182, 162, 124, 255)), // bej
        new Palette(new Color32(170, 178, 192, 255), new Color32(134, 142, 158, 255)), // gri
        new Palette(new Color32(228, 224, 212, 255), new Color32(190, 186, 174, 255)), // krem
        new Palette(new Color32(120, 168, 170, 255), new Color32(92, 134, 138, 255))   // yeşilimsi
    };

    public static Texture2D BuildingPhoto(int seed)
    {
        var rng = new System.Random(seed);
        int w = PhotoWidth, h = PhotoHeight;
        var px = new Color32[w * h];

        var skyBottom = new Color32(186, 218, 236, 255);
        var skyTop = new Color32(126, 182, 222, 255);
        for (int y = 0; y < h; y++)
        {
            var c = Color32.Lerp(skyBottom, skyTop, y / (float)(h - 1));
            for (int x = 0; x < w; x++)
                px[y * w + x] = c;
        }

        int cursor = -rng.Next(0, 6);
        while (cursor < w)
        {
            int width = rng.Next(30, 46);
            int top = rng.Next(h - 14, h + 6);
            DrawBuilding(px, w, h, cursor, width, top, Palettes[rng.Next(Palettes.Length)], rng);
            cursor += width + (rng.Next(3) == 0 ? rng.Next(2, 6) : 0);
        }

        Fill(px, w, h, 0, 0, w, 2, new Color32(92, 96, 104, 255));
        return ToTexture(px, w, h);
    }

    static void DrawBuilding(Color32[] px, int w, int h, int x, int width, int top, Palette p, System.Random rng)
    {
        Fill(px, w, h, x, 2, width, top - 2, p.wall);
        Fill(px, w, h, x + width - 3, 2, 3, top - 2, p.shade);
        Fill(px, w, h, x, top - 1, width, 1, p.shade);

        // Çatıda su deposu.
        if (rng.Next(2) == 0)
            Fill(px, w, h, x + rng.Next(4, Mathf.Max(5, width - 10)), top, 6, 3, new Color32(80, 84, 92, 255));

        var glass = new Color32(44, 84, 128, 255);
        var shine = new Color32(140, 190, 226, 255);
        for (int wy = 5; wy + 3 < top - 2; wy += 6)
        {
            for (int wx = x + 3; wx + 3 < x + width - 3; wx += 6)
            {
                Fill(px, w, h, wx, wy, 3, 3, glass);
                if (rng.Next(3) == 0)
                    Fill(px, w, h, wx, wy + 2, 1, 1, shine);
            }
        }
    }

    public static Texture2D XRayMarks(InspectionCase c)
    {
        var px = new Color32[PlanWidth * PlanHeight];
        int column = Mathf.Clamp(c.damageColumn, 0, ColumnX.Length - 1);
        int floor = Mathf.Clamp(c.damageFloor, 0, BeamY.Length - 2);

        if (c.damage != DamageType.None)
            DrawDamage(px, c.damage, ColumnX[column], BeamY[floor], BeamY[floor + 1], new System.Random(c.photoSeed));

        // Yangın tüpleri kolonlar arasındaki hücrelere yerleşir.
        var cells = new List<Vector2Int>();
        for (int gap = 0; gap <= ColumnX.Length; gap++)
        {
            for (int f = 0; f < BeamY.Length - 1; f++)
            {
                bool underClip = gap == 0 && f == 0;
                bool damaged = c.damage != DamageType.None && f == floor && (gap == column || gap == column + 1);
                if (!underClip && !damaged)
                    cells.Add(new Vector2Int(gap, f));
            }
        }

        var rng = new System.Random(c.photoSeed * 31 + 7);
        for (int i = 0; i < c.fireExtinguishers && cells.Count > 0; i++)
        {
            int pick = rng.Next(cells.Count);
            var cell = cells[pick];
            cells.RemoveAt(pick);

            float left = cell.x == 0 ? 0f : ColumnX[cell.x - 1];
            float right = cell.x == ColumnX.Length ? PlanWidth : ColumnX[cell.x];
            DrawExtinguisher(px, (left + right) * 0.5f, (BeamY[cell.y] + BeamY[cell.y + 1]) * 0.5f);
        }

        return ToTexture(px, PlanWidth, PlanHeight);
    }

    static void DrawDamage(Color32[] px, DamageType damage, float x, float yTop, float yBottom, System.Random rng)
    {
        float mid = (yTop + yBottom) * 0.5f;

        switch (damage)
        {
            case DamageType.LargeCracks:
            {
                var points = new List<Vector2>();
                int steps = 6;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float jitter = i == 0 || i == steps ? 0f : (float)(rng.NextDouble() * 8.0 - 4.0);
                    points.Add(new Vector2(x + jitter, Mathf.Lerp(yTop + 2f, yBottom - 2f, t)));
                }
                for (int i = 0; i < points.Count - 1; i++)
                    PlanLine(px, points[i], points[i + 1], DamageColor);

                // Yan çatlaklar.
                PlanLine(px, points[2], points[2] + new Vector2(9f, -4f), DamageColor);
                PlanLine(px, points[4], points[4] + new Vector2(-10f, 3f), DamageColor);
                PlanLine(px, new Vector2(x - 8f, yBottom), new Vector2(x + 10f, yBottom - 2f), DamageColor);
                break;
            }
            case DamageType.CutColumn:
            {
                // Kolonun bir bölümü yok: çizgiyi sil, iki kesik izi çiz.
                for (float y = mid - 3f; y <= mid + 3f; y += 0.5f)
                    PlanLine(px, new Vector2(x - 3f, y), new Vector2(x + 3f, y), PlanBackground);
                PlanLine(px, new Vector2(x - 8f, mid - 6f), new Vector2(x + 8f, mid - 2f), DamageColor);
                PlanLine(px, new Vector2(x - 8f, mid + 2f), new Vector2(x + 8f, mid + 6f), DamageColor);
                break;
            }
            case DamageType.Thinning:
            {
                PlanLine(px, new Vector2(x - 6f, yTop + 2f), new Vector2(x - 1.5f, mid), DamageColor);
                PlanLine(px, new Vector2(x - 1.5f, mid), new Vector2(x - 6f, yBottom - 2f), DamageColor);
                PlanLine(px, new Vector2(x + 6f, yTop + 2f), new Vector2(x + 1.5f, mid), DamageColor);
                PlanLine(px, new Vector2(x + 1.5f, mid), new Vector2(x + 6f, yBottom - 2f), DamageColor);
                break;
            }
        }
    }

    static readonly string[] LetterF = { "111", "100", "110", "100", "100" };
    static readonly string[] LetterE = { "111", "100", "110", "100", "111" };

    static void DrawExtinguisher(Color32[] px, float cx, float cy)
    {
        var center = Tilt(new Vector2(cx, cy));
        int x = Mathf.RoundToInt(center.x) - 14;
        int top = Mathf.RoundToInt(center.y) - 8;

        // Tüp: başlık + gövde, yanında "FE". Her piksel 2x2 çizilir.
        BlitRows(px, x + 1, top, new[] { "0110", "1111", "1111", "1001", "1111", "1111", "1111", "1111" }, ExtinguisherColor, 2);
        BlitRows(px, x + 13, top + 3, LetterF, ExtinguisherColor, 2);
        BlitRows(px, x + 21, top + 3, LetterE, ExtinguisherColor, 2);
    }

    // Satırları üstten aşağı yazar (y üstten ölçülür).
    static void BlitRows(Color32[] px, int x, int yTop, string[] rows, Color32 color, int scale)
    {
        for (int r = 0; r < rows.Length; r++)
            for (int i = 0; i < rows[r].Length; i++)
                if (rows[r][i] == '1')
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                            SetPlan(px, x + i * scale + sx, yTop + r * scale + sy, color);
    }

    // Plan koordinatını (üstten) ızgara görselinin eğikliğine göre döndürür.
    static Vector2 Tilt(Vector2 p)
    {
        float a = CardTilt * Mathf.Deg2Rad;
        float cx = PlanWidth * 0.5f, cy = PlanHeight * 0.5f;
        float dx = p.x - cx, dy = p.y - cy;
        return new Vector2(
            cx + dx * Mathf.Cos(a) + dy * Mathf.Sin(a),
            cy - dx * Mathf.Sin(a) + dy * Mathf.Cos(a));
    }

    static void PlanLine(Color32[] px, Vector2 from, Vector2 to, Color32 color)
    {
        Vector2 a = Tilt(from), b = Tilt(to);
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f) + 1;
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                    SetPlan(px, x + ox, y + oy, color);
        }
    }

    static void SetPlan(Color32[] px, int x, int yFromTop, Color32 color)
    {
        int y = PlanHeight - 1 - yFromTop;
        if (x < 0 || x >= PlanWidth || y < 0 || y >= PlanHeight)
            return;
        px[y * PlanWidth + x] = color;
    }

    static void Fill(Color32[] px, int w, int h, int x, int y, int width, int height, Color32 color)
    {
        for (int yy = Mathf.Max(0, y); yy < Mathf.Min(h, y + height); yy++)
            for (int xx = Mathf.Max(0, x); xx < Mathf.Min(w, x + width); xx++)
                px[yy * w + xx] = color;
    }

    static Texture2D ToTexture(Color32[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
