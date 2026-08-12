using UnityEngine;

/// <summary>
/// Cartes procédurales : silhouette France (hexagone) + continents monde reconnaissables.
/// Contours en lon/lat (Vector2.x = longitude, Vector2.y = latitude).
/// </summary>
public static class ProceduralMapGenerator
{
    private static Texture2D franceTex;
    private static Texture2D worldTex;

    // Contour détaillé métropole — sens horaire depuis Bretagne (pointe ouest)
    private static readonly Vector2[] FranceOutlineLonLat =
    {
        // Bretagne — pointe ouest (Finistère)
        new Vector2(-4.80f, 48.40f), new Vector2(-4.50f, 48.70f), new Vector2(-4.00f, 48.70f),
        new Vector2(-3.50f, 48.80f), new Vector2(-2.80f, 48.65f), new Vector2(-2.20f, 48.65f),
        // Cotentin / Normandie
        new Vector2(-1.90f, 49.20f), new Vector2(-1.60f, 49.70f), new Vector2(-1.20f, 49.70f),
        new Vector2(-0.80f, 49.50f), new Vector2(0.10f, 49.45f), new Vector2(1.20f, 49.90f),
        // Nord — Pas-de-Calais / Belgique
        new Vector2(1.60f, 50.80f), new Vector2(2.40f, 51.05f), new Vector2(3.20f, 51.05f),
        new Vector2(4.00f, 50.80f), new Vector2(4.60f, 50.10f), new Vector2(5.80f, 49.50f),
        // Est — Alsace / Allemagne / Suisse
        new Vector2(7.50f, 49.00f), new Vector2(8.20f, 48.95f), new Vector2(7.80f, 48.40f),
        new Vector2(7.60f, 47.60f), new Vector2(7.00f, 47.40f), new Vector2(6.90f, 46.20f),
        // Alpes / Savoie
        new Vector2(6.90f, 45.90f), new Vector2(7.10f, 45.20f), new Vector2(6.80f, 44.20f),
        // Côte d'Azur / Nice → Marseille
        new Vector2(7.50f, 43.75f), new Vector2(7.00f, 43.55f), new Vector2(6.20f, 43.20f),
        new Vector2(5.40f, 43.20f), new Vector2(4.80f, 43.30f), new Vector2(3.90f, 43.40f),
        // Languedoc → Pyrénées
        new Vector2(3.10f, 43.20f), new Vector2(2.50f, 42.50f), new Vector2(1.80f, 42.45f),
        new Vector2(0.90f, 42.70f), new Vector2(-0.20f, 42.90f), new Vector2(-1.40f, 43.20f),
        // Pays Basque / Atlantique sud
        new Vector2(-1.70f, 43.40f), new Vector2(-1.50f, 44.40f), new Vector2(-1.20f, 45.50f),
        new Vector2(-1.10f, 46.20f), new Vector2(-1.60f, 46.50f), new Vector2(-2.20f, 47.00f),
        // Retour Bretagne sud
        new Vector2(-2.80f, 47.50f), new Vector2(-3.50f, 47.70f), new Vector2(-4.20f, 48.00f),
        new Vector2(-4.80f, 48.40f),
    };

    // Corse (île)
    private static readonly Vector2[] CorsicaLonLat =
    {
        new Vector2(8.55f, 42.90f), new Vector2(9.45f, 42.70f), new Vector2(9.55f, 41.90f),
        new Vector2(9.20f, 41.40f), new Vector2(8.80f, 41.55f), new Vector2(8.55f, 42.20f),
        new Vector2(8.55f, 42.90f),
    };

    // Continents — polygones simplifiés mais reconnaissables (lon, lat)
    private static readonly Vector2[][] Continents =
    {
        // Amérique du Nord
        new[]
        {
            new Vector2(-168f, 65f), new Vector2(-140f, 70f), new Vector2(-100f, 73f),
            new Vector2(-80f, 72f), new Vector2(-55f, 60f), new Vector2(-55f, 47f),
            new Vector2(-70f, 45f), new Vector2(-80f, 30f), new Vector2(-97f, 26f),
            new Vector2(-110f, 23f), new Vector2(-125f, 38f), new Vector2(-130f, 50f),
            new Vector2(-140f, 60f), new Vector2(-168f, 65f),
        },
        // Amérique centrale / Mexique sud
        new[]
        {
            new Vector2(-110f, 23f), new Vector2(-97f, 26f), new Vector2(-87f, 21f),
            new Vector2(-83f, 9f), new Vector2(-78f, 8f), new Vector2(-85f, 13f),
            new Vector2(-105f, 18f), new Vector2(-110f, 23f),
        },
        // Amérique du Sud
        new[]
        {
            new Vector2(-80f, 12f), new Vector2(-70f, 12f), new Vector2(-50f, 5f),
            new Vector2(-35f, -5f), new Vector2(-35f, -20f), new Vector2(-40f, -35f),
            new Vector2(-55f, -50f), new Vector2(-70f, -55f), new Vector2(-75f, -40f),
            new Vector2(-80f, -20f), new Vector2(-80f, 0f), new Vector2(-80f, 12f),
        },
        // Europe (occidentale + Scandinavie simplifiée)
        new[]
        {
            new Vector2(-10f, 36f), new Vector2(-9f, 42f), new Vector2(-6f, 48f),
            new Vector2(-5f, 52f), new Vector2(0f, 54f), new Vector2(5f, 58f),
            new Vector2(10f, 60f), new Vector2(15f, 68f), new Vector2(25f, 70f),
            new Vector2(30f, 60f), new Vector2(30f, 50f), new Vector2(28f, 42f),
            new Vector2(20f, 40f), new Vector2(15f, 38f), new Vector2(10f, 36f),
            new Vector2(3f, 36f), new Vector2(-5f, 36f), new Vector2(-10f, 36f),
        },
        // Îles UK
        new[]
        {
            new Vector2(-8f, 50f), new Vector2(-6f, 55f), new Vector2(-2f, 58f),
            new Vector2(2f, 53f), new Vector2(1f, 51f), new Vector2(-2f, 50f),
            new Vector2(-5f, 50f), new Vector2(-8f, 50f),
        },
        // Afrique
        new[]
        {
            new Vector2(-17f, 15f), new Vector2(-10f, 32f), new Vector2(0f, 36f),
            new Vector2(10f, 37f), new Vector2(25f, 32f), new Vector2(35f, 30f),
            new Vector2(42f, 12f), new Vector2(45f, -5f), new Vector2(40f, -15f),
            new Vector2(35f, -30f), new Vector2(25f, -35f), new Vector2(18f, -35f),
            new Vector2(12f, -18f), new Vector2(5f, 5f), new Vector2(-10f, 5f),
            new Vector2(-17f, 15f),
        },
        // Moyen-Orient / Arabie
        new[]
        {
            new Vector2(35f, 30f), new Vector2(42f, 37f), new Vector2(48f, 30f),
            new Vector2(55f, 25f), new Vector2(55f, 17f), new Vector2(45f, 12f),
            new Vector2(42f, 15f), new Vector2(35f, 30f),
        },
        // Asie
        new[]
        {
            new Vector2(30f, 50f), new Vector2(40f, 55f), new Vector2(60f, 60f),
            new Vector2(90f, 70f), new Vector2(130f, 70f), new Vector2(160f, 65f),
            new Vector2(175f, 60f), new Vector2(145f, 45f), new Vector2(140f, 35f),
            new Vector2(130f, 30f), new Vector2(120f, 20f), new Vector2(105f, 10f),
            new Vector2(100f, 5f), new Vector2(95f, 15f), new Vector2(80f, 20f),
            new Vector2(70f, 25f), new Vector2(60f, 25f), new Vector2(50f, 35f),
            new Vector2(40f, 40f), new Vector2(30f, 42f), new Vector2(30f, 50f),
        },
        // Inde
        new[]
        {
            new Vector2(68f, 24f), new Vector2(75f, 30f), new Vector2(88f, 27f),
            new Vector2(92f, 22f), new Vector2(85f, 12f), new Vector2(78f, 8f),
            new Vector2(72f, 15f), new Vector2(68f, 24f),
        },
        // Australie
        new[]
        {
            new Vector2(113f, -22f), new Vector2(125f, -12f), new Vector2(140f, -12f),
            new Vector2(150f, -20f), new Vector2(153f, -30f), new Vector2(145f, -38f),
            new Vector2(130f, -35f), new Vector2(115f, -35f), new Vector2(113f, -22f),
        },
        // Japon (île)
        new[]
        {
            new Vector2(130f, 32f), new Vector2(135f, 35f), new Vector2(141f, 43f),
            new Vector2(145f, 43f), new Vector2(141f, 35f), new Vector2(132f, 31f),
            new Vector2(130f, 32f),
        },
        // Groenland
        new[]
        {
            new Vector2(-55f, 60f), new Vector2(-45f, 70f), new Vector2(-30f, 75f),
            new Vector2(-20f, 70f), new Vector2(-30f, 60f), new Vector2(-45f, 58f),
            new Vector2(-55f, 60f),
        },
    };

    public static void InvalidateCache()
    {
        // Ne détruit pas les textures Resources — juste oublie le cache
        franceTex = null;
        worldTex = null;
    }

    public static Texture2D GetFranceMap()
    {
        if (franceTex != null) return franceTex;
        franceTex = LoadResourceMap("Maps/map_france") ?? BuildFranceTexture(512, 640);
        return franceTex;
    }

    public static Texture2D GetWorldMap()
    {
        if (worldTex != null) return worldTex;
        worldTex = LoadResourceMap("Maps/map_world") ?? BuildWorldTexture(720, 400);
        return worldTex;
    }

    private static Texture2D LoadResourceMap(string resourcePath)
    {
        return Resources.Load<Texture2D>(resourcePath);
    }

    private static Texture2D BuildFranceTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color sea = new Color(0.10f, 0.32f, 0.55f);
        Color land = new Color(0.55f, 0.68f, 0.38f);
        Color landDark = new Color(0.42f, 0.55f, 0.30f);
        Color coast = new Color(0.95f, 0.90f, 0.70f);
        Color neighbor = new Color(0.55f, 0.55f, 0.50f, 0.35f);

        var outline = LonLatToUi(FranceOutlineLonLat, false);
        var corsica = LonLatToUi(CorsicaLonLat, false);

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float v = y / (float)(h - 1);
                var p = new Vector2(u, v);
                bool inside = PointInPolygon(p, outline) || PointInPolygon(p, corsica);
                if (inside)
                {
                    // Léger relief (sud = un peu plus foncé = Pyrénées/Alpes)
                    float shade = Mathf.Lerp(0.92f, 1.05f, v);
                    pixels[y * w + x] = Color.Lerp(landDark, land, shade - 0.9f);
                }
                else
                {
                    // Voisins (Espagne / Italie / Belgique) en gris léger pour contexte
                    bool spain = v < 0.18f && u > 0.15f && u < 0.55f;
                    bool italy = u > 0.78f && v < 0.45f && v > 0.15f;
                    bool belgium = v > 0.88f && u > 0.35f && u < 0.65f;
                    pixels[y * w + x] = (spain || italy || belgium)
                        ? Color.Lerp(sea, neighbor, 0.55f)
                        : sea;
                }
            }
        }

        DrawPolyLine(pixels, w, h, outline, coast, 2);
        DrawPolyLine(pixels, w, h, corsica, coast, 1);
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BuildWorldTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color sea = new Color(0.08f, 0.22f, 0.42f);
        Color land = new Color(0.40f, 0.58f, 0.30f);
        Color coast = new Color(0.85f, 0.80f, 0.55f);
        Color grid = new Color(1f, 1f, 1f, 0.08f);

        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = sea;

        // Grille lat/lon discrète
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (x % (w / 18) == 0 || y % (h / 9) == 0)
                pixels[y * w + x] = Color.Lerp(pixels[y * w + x], Color.white, 0.07f);
        }

        // Équateur
        int eqY = Mathf.RoundToInt(0.5f * (h - 1));
        for (int x = 0; x < w; x++)
            pixels[eqY * w + x] = Color.Lerp(pixels[eqY * w + x], new Color(1f, 0.85f, 0.3f), 0.25f);

        foreach (var continent in Continents)
        {
            var ui = LonLatToUi(continent, true);
            FillPolygon(pixels, w, h, ui, land);
            DrawPolyLine(pixels, w, h, ui, coast, 1);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Vector2[] LonLatToUi(Vector2[] lonLat, bool world)
    {
        var ui = new Vector2[lonLat.Length];
        for (int i = 0; i < lonLat.Length; i++)
            ui[i] = world
                ? MapProjection.WorldToUI(lonLat[i].y, lonLat[i].x)
                : MapProjection.FranceToUI(lonLat[i].y, lonLat[i].x);
        return ui;
    }

    private static void FillPolygon(Color[] px, int w, int h, Vector2[] poly, Color c)
    {
        float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
        for (int i = 0; i < poly.Length; i++)
        {
            minX = Mathf.Min(minX, poly[i].x);
            maxX = Mathf.Max(maxX, poly[i].x);
            minY = Mathf.Min(minY, poly[i].y);
            maxY = Mathf.Max(maxY, poly[i].y);
        }

        int x0 = Mathf.Clamp(Mathf.FloorToInt(minX * (w - 1)) - 1, 0, w - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX * (w - 1)) + 1, 0, w - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(minY * (h - 1)) - 1, 0, h - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY * (h - 1)) + 1, 0, h - 1);

        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);
            if (PointInPolygon(new Vector2(u, v), poly))
                px[y * w + x] = c;
        }
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y + 0.00001f) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    private static void DrawPolyLine(Color[] px, int w, int h, Vector2[] poly, Color c, int thickness)
    {
        for (int i = 0; i < poly.Length - 1; i++)
            DrawLine(px, w, h, poly[i], poly[i + 1], c, thickness);
    }

    private static void DrawLine(Color[] px, int w, int h, Vector2 a, Vector2 b, Color c, int thickness)
    {
        int x0 = Mathf.RoundToInt(a.x * (w - 1));
        int y0 = Mathf.RoundToInt(a.y * (h - 1));
        int x1 = Mathf.RoundToInt(b.x * (w - 1));
        int y1 = Mathf.RoundToInt(b.y * (h - 1));
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            for (int t = -thickness; t <= thickness; t++)
            for (int u = -thickness; u <= thickness; u++)
            {
                int pxX = x0 + u, pxY = y0 + t;
                if (pxX >= 0 && pxY >= 0 && pxX < w && pxY < h)
                    px[pxY * w + pxX] = c;
            }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
}
