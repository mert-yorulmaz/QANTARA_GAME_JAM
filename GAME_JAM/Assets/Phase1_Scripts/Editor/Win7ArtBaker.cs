using System.IO;
using UnityEditor;
using UnityEngine;

// BIA OS'un Windows 7 temalı görsellerini koddan üretip PNG olarak kaydeder.
// Dosyalar Assets/Dispatch/Win7/ altına yazılır; istenirse elle çizilmiş görsellerle değiştirilebilir.
public static class Win7ArtBaker
{
    public const string Folder = "Assets/Dispatch/Win7/";

    public class Art
    {
        public Sprite wallpaper, taskbar, startOrb, showDesktop;
        public Sprite paperPanel, paperMinimize;
        public Sprite folder, ruleBookFolder, iconHighlight;
        public Sprite taskButton, taskButtonActive;
        public Sprite balloon;
        public Sprite hudPanel, checkEmpty, checkDone, glowFrame;
        public Sprite paperTickEmpty, paperTickDone;
        public Sprite[] periods;
    }

    public static Art Bake()
    {
        Directory.CreateDirectory(Folder);

        var slice8 = new Vector4(8, 8, 8, 8);
        var art = new Art
        {
            wallpaper = Save("wallpaper", 640, 360, Wallpaper(640, 360)),
            taskbar = Save("taskbar", 4, 36, Taskbar()),
            startOrb = Save("start_orb", 44, 44, StartOrb()),
            showDesktop = Save("show_desktop", 12, 36, ShowDesktop()),
            paperPanel = Save("paper_panel", 48, 48, PaperPanel(), new Vector4(12, 12, 12, 12)),
            paperMinimize = Save("paper_minimize", 80, 80, PaperMinimize()),
            folder = Save("folder", 64, 64, Folder64(false)),
            ruleBookFolder = Save("folder_rule_book", 64, 64, Folder64(true)),
            iconHighlight = Save("icon_highlight", 16, 16, Rounded(16, 16, 3f, new Color32(160, 196, 236, 90), new Color32(125, 162, 206, 230)), new Vector4(4, 4, 4, 4)),
            taskButton = Save("taskbar_button", 24, 30, Rounded(24, 30, 3f, new Color32(255, 255, 255, 26), new Color32(255, 255, 255, 70)), new Vector4(6, 6, 6, 6)),
            taskButtonActive = Save("taskbar_button_active", 24, 30, Rounded(24, 30, 3f, new Color32(255, 255, 255, 70), new Color32(255, 255, 255, 150)), new Vector4(6, 6, 6, 6)),
            balloon = Save("notification_balloon", 24, 24, Balloon(), slice8),
            hudPanel = Save("hud_panel", 32, 32, Rounded(32, 32, 8f, new Color32(0x22, 0x24, 0x28, 225), new Color32(0x14, 0x14, 0x13, 255)), new Vector4(10, 10, 10, 10)),
            checkEmpty = Save("check_empty", 32, 32, Checkbox(false)),
            checkDone = Save("check_done", 32, 32, Checkbox(true)),
            glowFrame = Save("glow_frame", 40, 40, GlowFrame(), new Vector4(14, 14, 14, 14)),
            paperTickEmpty = Save("paper_tick_empty", 28, 28, PaperTick(false)),
            paperTickDone = Save("paper_tick_done", 28, 28, PaperTick(true)),
            periods = new[]
            {
                Save("period_morning", 16, 16, Sun(new Color32(255, 213, 74, 255), 4.2f, true)),
                Save("period_noon", 16, 16, Sun(new Color32(255, 236, 110, 255), 4.8f, false)),
                Save("period_afternoon", 16, 16, Sun(new Color32(255, 167, 38, 255), 4.2f, true)),
                Save("period_evening", 16, 16, Moon())
            }
        };
        return art;
    }

    // ---------- Görseller (koordinatlar üstten, piksel merkezleri) ----------

    static Color32[] Wallpaper(int w, int h)
    {
        var px = new Color32[w * h];
        var deep = new Color(0.024f, 0.153f, 0.341f);
        var mid = new Color(0.043f, 0.333f, 0.643f);
        var glow = new Color(0.30f, 0.70f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                var c = Color.Lerp(mid, deep, Mathf.Clamp01(v * 0.75f + u * 0.25f));

                float dx = (u - 0.36f) * 1.6f, dy = v - 0.52f;
                float g = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / 0.6f);
                c += glow * g * g * 0.55f;

                // Işık şeritleri.
                for (int s = 0; s < 3; s++)
                {
                    float center = 0.50f + s * 0.09f + 0.16f * Mathf.Sin(u * 3.1f + s * 0.9f) - (u - 0.5f) * 0.25f;
                    float d = Mathf.Abs(v - center);
                    c += Color.white * (Mathf.Exp(-Mathf.Pow(d / 0.012f, 2f)) * 0.18f + Mathf.Exp(-Mathf.Pow(d / 0.07f, 2f)) * 0.07f);
                }

                float vignette = 1f - 0.35f * Mathf.Pow(Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2f, 3f);
                c *= vignette;
                c.a = 1f;
                Set(px, w, h, x, y, c);
            }
        }
        return Pack(px, w, h);
    }

    static Color32[] Taskbar()
    {
        int w = 4, h = 36;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            Color c = y == 0
                ? new Color(1f, 1f, 1f, 0.35f)
                : Color.Lerp(new Color(0.20f, 0.31f, 0.43f, 0.90f), new Color(0.07f, 0.11f, 0.16f, 0.95f), y / (float)h);
            for (int x = 0; x < w; x++)
                Set(px, w, h, x, y, c);
        }
        return Pack(px, w, h);
    }

    static Color32[] ShowDesktop()
    {
        int w = 12, h = 36;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                Set(px, w, h, x, y, x == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.08f));
        return Pack(px, w, h);
    }

    static Color32[] StartOrb()
    {
        int s = 44;
        var px = new Color32[s * s];
        float c = (s - 1) * 0.5f, r = 20f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float cover = Mathf.Clamp01(r + 0.5f - d);
                if (cover <= 0f)
                    continue;

                float t = d / r;
                var col = Color.Lerp(new Color(0.24f, 0.62f, 0.96f), new Color(0.04f, 0.22f, 0.48f), t * t);
                if (d > r - 1.5f)
                    col = new Color(0.03f, 0.13f, 0.28f);

                // Üst yarıda cam parlaması.
                float gy = (y - (c - 7f)) / 9f, gx = (x - c) / 15f;
                if (gx * gx + gy * gy < 1f && y < c)
                    col = Color.Lerp(col, Color.white, 0.38f * (1f - gy * gy));

                col.a = cover;
                Set(px, s, s, x, y, col);
            }
        }
        return Pack(px, s, s);
    }

    // Kâğıt pencere zemini (9-slice): #ECDCB0 kâğıt, #B89B5E kenar, hafif gölge.
    static Color32[] PaperPanel()
    {
        int s = 48;
        var px = new Color32[s * s];
        var paper = new Color(0.925f, 0.863f, 0.690f);
        var border = new Color(0.722f, 0.608f, 0.369f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float shadow = Mathf.Clamp01(0.5f - RoundSdf(x + 0.5f - 1f, y + 0.5f - 3f, s - 2f, s - 3f, 8f) * 0.35f);
                if (shadow > 0f)
                    Set(px, s, s, x, y, new Color(0f, 0f, 0f, 0.25f * shadow));

                float d = RoundSdf(x + 0.5f, y + 0.5f, s - 2f, s - 4f, 8f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -2f ? border : paper;
                col.a = cover;
                Blend(px, s, s, x, y, col);
            }
        }
        return Pack(px, s, s);
    }

    // Kural kitabının btn_close düğmesiyle aynı stilde "_" düğmesi (80x80, 2x).
    static Color32[] PaperMinimize()
    {
        int s = 80;
        var px = new Color32[s * s];
        var paper = new Color(0.925f, 0.863f, 0.690f);
        var ink = new Color(0.165f, 0.133f, 0.086f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = RoundSdf(x + 0.5f, y + 0.5f, s, s, 6f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -4f ? ink : paper;
                col.a = cover;
                Set(px, s, s, x, y, col);
            }
        }
        for (int y = 50; y < 58; y++)
            for (int x = 22; x < 58; x++)
                Set(px, s, s, x, y, ink);
        return Pack(px, s, s);
    }

    static Color32[] Folder64(bool ruleBook)
    {
        int s = 64;
        var px = new Color32[s * s];
        var outline = new Color(0.62f, 0.45f, 0.13f);

        // Arka kapak ve sekme.
        RoundRect(px, s, 6, 8, 22, 10, 2f, new Color(0.87f, 0.66f, 0.25f), outline);
        RoundRect(px, s, 6, 13, 52, 40, 3f, new Color(0.86f, 0.64f, 0.22f), outline);

        // İçindeki kâğıtlar.
        RoundRect(px, s, 11, 16, 42, 28, 1f, new Color(0.97f, 0.97f, 0.95f), new Color(0.78f, 0.78f, 0.74f));
        RoundRect(px, s, 14, 19, 40, 26, 1f, Color.white, new Color(0.80f, 0.80f, 0.76f));

        // Ön kapak (degrade).
        for (int y = 24; y < 57; y++)
        {
            for (int x = 3; x < 61; x++)
            {
                float d = RoundSdf(x - 3 + 0.5f, y - 24 + 0.5f, 58, 33, 3f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = Color.Lerp(new Color(0.99f, 0.89f, 0.62f), new Color(0.94f, 0.76f, 0.36f), (y - 24) / 33f);
                if (d > -1.1f)
                    col = outline;
                else if (y < 27)
                    col = Color.Lerp(col, Color.white, 0.5f);

                col.a = cover;
                Blend(px, s, s, x, y, col);
            }
        }

        if (ruleBook)
        {
            // Lacivert kitapçık ve altın şerit.
            RoundRect(px, s, 33, 30, 22, 25, 1.5f, new Color(0.12f, 0.23f, 0.37f), new Color(0.05f, 0.11f, 0.19f));
            RoundRect(px, s, 33, 30, 4, 25, 1f, new Color(0.20f, 0.34f, 0.52f), new Color(0.05f, 0.11f, 0.19f));
            for (int x = 38; x < 54; x++)
                for (int y = 46; y < 49; y++)
                    Set(px, s, s, x, y, new Color(0.85f, 0.71f, 0.29f));
            for (int x = 40; x < 52; x++)
                Set(px, s, s, x, 36, new Color(0.85f, 0.71f, 0.29f));
            for (int x = 40; x < 49; x++)
                Set(px, s, s, x, 39, new Color(0.85f, 0.71f, 0.29f));
        }
        return Pack(px, s, s);
    }

    static Color32[] Rounded(int w, int h, float radius, Color32 fill, Color32 border)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = RoundSdf(x + 0.5f, y + 0.5f, w, h, radius);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -1.2f ? (Color)border : fill;
                col.a *= cover;
                Set(px, w, h, x, y, col);
            }
        }
        return Pack(px, w, h);
    }

    // Beyaz kenarlık + dışa doğru sönen ışıltı, ortası boş (renk Image'dan verilir).
    static Color32[] GlowFrame()
    {
        int s = 40;
        var px = new Color32[s * s];
        const float inset = 7f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = RoundSdf(x + 0.5f - inset, y + 0.5f - inset, s - inset * 2f, s - inset * 2f, 7f);
                float a;
                if (d > 0f)
                    a = Mathf.Pow(Mathf.Clamp01(1f - d / inset), 1.5f) * 0.7f; // dış ışıltı
                else if (d > -4f)
                    a = 1f;                                              // kenarlık
                else
                    a = Mathf.Clamp01(1f + (d + 4f) / 2f) * 0.4f;        // iç yumuşak kenar
                if (a > 0f)
                    Set(px, s, s, x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return Pack(px, s, s);
    }

    // Dosya üzerindeki kural kontrol kutucuğu: mürekkep çerçeve, işaretliyse lacivert tik.
    static Color32[] PaperTick(bool done)
    {
        int s = 28;
        var px = new Color32[s * s];
        var ink = new Color(0.42f, 0.35f, 0.21f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = RoundSdf(x + 0.5f, y + 0.5f, s, s, 4f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -3f ? ink : new Color(1f, 0.98f, 0.92f, 0.85f);
                col.a *= cover;
                Set(px, s, s, x, y, col);
            }
        }

        if (done)
        {
            var navy = new Color(0.12f, 0.23f, 0.37f);
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                Disk(px, s, s, Mathf.Lerp(6f, 12f, t), Mathf.Lerp(14f, 21f, t), 1.9f, navy);
                Disk(px, s, s, Mathf.Lerp(12f, 23f, t), Mathf.Lerp(21f, 6f, t), 1.9f, navy);
            }
        }
        return Pack(px, s, s);
    }

    static Color32[] Checkbox(bool done)
    {
        int s = 32;
        var px = new Color32[s * s];
        var green = new Color(0.50f, 0.88f, 0.71f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = RoundSdf(x + 0.5f, y + 0.5f, s, s, 6f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col;
                if (done)
                    col = green;
                else if (d > -3f)
                    col = new Color(1f, 0.88f, 0.54f);
                else
                    col = new Color(1f, 1f, 1f, 0.06f);
                col.a *= cover;
                Set(px, s, s, x, y, col);
            }
        }

        if (done)
        {
            var ink = new Color(0.08f, 0.16f, 0.12f);
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                Disk(px, s, s, Mathf.Lerp(8f, 14f, t), Mathf.Lerp(16f, 22f, t), 1.8f, ink);
                Disk(px, s, s, Mathf.Lerp(14f, 24f, t), Mathf.Lerp(22f, 10f, t), 1.8f, ink);
            }
        }
        return Pack(px, s, s);
    }

    static Color32[] Balloon()
    {
        int s = 24;
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = RoundSdf(x + 0.5f, y + 0.5f, s, s, 5f);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -1.2f
                    ? new Color(0.46f, 0.46f, 0.46f)
                    : Color.Lerp(Color.white, new Color(0.89f, 0.91f, 0.95f), y / (float)s);
                col.a = cover;
                Set(px, s, s, x, y, col);
            }
        }
        return Pack(px, s, s);
    }

    static Color32[] Sun(Color color, float radius, bool low)
    {
        int s = 16;
        var px = new Color32[s * s];
        float cx = 7.5f, cy = low ? 9f : 7.5f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            for (float r = radius + 1.5f; r < radius + 3.2f; r += 0.3f)
                Disk(px, s, s, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, 0.6f, color);
        }
        Disk(px, s, s, cx, cy, radius, color);
        if (low)
            for (int x = 1; x < 15; x++)
                Set(px, s, s, x, 14, new Color(1f, 0.6f, 0.2f));
        return Pack(px, s, s);
    }

    static Color32[] Moon()
    {
        int s = 16;
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float a = Mathf.Clamp01(5.6f - Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)));
                float b = Mathf.Clamp01(5.2f - Vector2.Distance(new Vector2(x, y), new Vector2(10f, 5.5f)));
                float cover = a * (1f - b);
                if (cover > 0f)
                    Set(px, s, s, x, y, new Color(0.92f, 0.94f, 0.98f, cover));
            }
        }
        return Pack(px, s, s);
    }

    // ---------- Çizim yardımcıları ----------

    static float RoundSdf(float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
        float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    static void RoundRect(Color32[] px, int size, int left, int top, int w, int h, float r, Color fill, Color border)
    {
        for (int y = top; y < top + h; y++)
        {
            for (int x = left; x < left + w; x++)
            {
                float d = RoundSdf(x - left + 0.5f, y - top + 0.5f, w, h, r);
                float cover = Mathf.Clamp01(0.5f - d);
                if (cover <= 0f)
                    continue;

                Color col = d > -1.1f ? border : fill;
                col.a = cover;
                Blend(px, size, size, x, y, col);
            }
        }
    }

    static void Disk(Color32[] px, int w, int h, float cx, float cy, float r, Color color)
    {
        for (int y = Mathf.FloorToInt(cy - r - 1); y <= Mathf.CeilToInt(cy + r + 1); y++)
        {
            for (int x = Mathf.FloorToInt(cx - r - 1); x <= Mathf.CeilToInt(cx + r + 1); x++)
            {
                float cover = Mathf.Clamp01(r + 0.5f - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)));
                if (cover <= 0f)
                    continue;
                var c = color;
                c.a *= cover;
                Blend(px, w, h, x, y, c);
            }
        }
    }

    static void Set(Color32[] px, int w, int h, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
            return;
        px[y * w + x] = color;
    }

    static void Blend(Color32[] px, int w, int h, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
            return;

        Color dst = px[y * w + x];
        float a = color.a + dst.a * (1f - color.a);
        if (a <= 0f)
            return;

        Color result = (color * color.a + dst * dst.a * (1f - color.a)) / a;
        result.a = a;
        px[y * w + x] = result;
    }

    // Üstten satır sırasını Texture2D'nin alttan sırasına çevirir.
    static Color32[] Pack(Color32[] px, int w, int h)
    {
        var flipped = new Color32[px.Length];
        for (int y = 0; y < h; y++)
            System.Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
        return flipped;
    }

    static Sprite Save(string name, int w, int h, Color32[] pixels, Vector4 border = default)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        string path = Folder + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        if (border != default)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
