using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Helper 
{
    public static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>()
    {
            // Unity built-in
            { "red",              Color.red },                          // (1, 0, 0)
            // { "green",            Color.green },                       // (0, 1, 0)
            { "blue",             Color.blue },                        // (0, 0, 1)
            { "yellow",           Color.yellow },                      // (1, 0.92, 0.016)
            { "black",            Color.black },                       // (0, 0, 0)
            { "white",            Color.white },                       // (1, 1, 1)
            { "cyan",             Color.cyan },                        // (0, 1, 1)
            // { "magenta",          Color.magenta },                     // (1, 0, 1)
            { "gray",             Color.gray },                        // (0.5, 0.5, 0.5)

            // Reds / Pinks
            { "dark red",         new Color(0.55f, 0f, 0f) },         // DarkRed
            { "maroon",           new Color(0.5f, 0f, 0f) },          // Maroon
            { "crimson",          new Color(0.86f, 0.08f, 0.24f) },   // Crimson
            // { "indian red",       new Color(0.80f, 0.36f, 0.36f) },   // IndianRed
            // { "salmon",           new Color(0.98f, 0.50f, 0.45f) },   // Salmon
            // { "light salmon",     new Color(1f, 0.63f, 0.48f) },      // LightSalmon
            // { "coral",            new Color(1f, 0.50f, 0.31f) },      // Coral
            { "tomato",           new Color(1f, 0.39f, 0.28f) },      // Tomato
            { "pink",             new Color(1f, 0.75f, 0.80f) },      // Pink
            { "hot pink",         new Color(1f, 0.41f, 0.71f) },      // HotPink
            { "deep pink",        new Color(1f, 0.08f, 0.58f) },      // DeepPink
            { "light pink",       new Color(1f, 0.71f, 0.76f) },      // LightPink
            // { "rose",             new Color(1f, 0f, 0.50f) },         // Rose

            // Oranges
            { "orange",           new Color(1f, 0.65f, 0f) },         // Orange
            { "dark orange",      new Color(1f, 0.55f, 0f) },         // DarkOrange
            // { "orange red",       new Color(1f, 0.27f, 0f) },         // OrangeRed
            // { "peach",            new Color(1f, 0.85f, 0.73f) },      // PeachPuff

            // Yellows
            // { "gold",             new Color(1f, 0.84f, 0f) },         // Gold
            // { "light yellow",     new Color(1f, 1f, 0.88f) },         // LightYellow
            // { "lemon",            new Color(1f, 0.97f, 0f) },         // Lemon
            { "khaki",            new Color(0.94f, 0.90f, 0.55f) },   // Khaki
            // { "dark khaki",       new Color(0.74f, 0.72f, 0.42f) },   // DarkKhaki

            // Greens
            { "lime",             new Color(0.75f, 1f, 0f) },         // Lime/Chartreuse
            { "lime green",       new Color(0.20f, 0.80f, 0.20f) },   // LimeGreen
            { "light green",      new Color(0.56f, 0.93f, 0.56f) },   // LightGreen
            { "dark green",       new Color(0f, 0.39f, 0f) },         // DarkGreen
            // { "forest green",     new Color(0.13f, 0.55f, 0.13f) },   // ForestGreen
            // { "sea green",        new Color(0.18f, 0.55f, 0.34f) },   // SeaGreen
            // { "spring green",     new Color(0f, 1f, 0.50f) },         // SpringGreen
            { "olive",            new Color(0.50f, 0.50f, 0f) },      // Olive
            { "dark olive",       new Color(0.33f, 0.42f, 0.18f) },   // DarkOliveGreen
            // { "mint",             new Color(0.60f, 1f, 0.60f) },      // Mint
            { "teal",             new Color(0f, 0.50f, 0.50f) },      // Teal

            // Blues
            // { "light blue",       new Color(0.68f, 0.85f, 0.90f) },   // LightBlue
            // { "sky blue",         new Color(0.53f, 0.81f, 0.92f) },   // SkyBlue
            { "deep sky blue",    new Color(0f, 0.75f, 1f) },         // DeepSkyBlue
            // { "dodger blue",      new Color(0.12f, 0.56f, 1f) },      // DodgerBlue
            // { "royal blue",       new Color(0.25f, 0.41f, 0.88f) },   // RoyalBlue
            { "dark blue",        new Color(0f, 0f, 0.55f) },         // DarkBlue
            // { "navy",             new Color(0f, 0f, 0.50f) },         // Navy
            // { "midnight blue",    new Color(0.10f, 0.10f, 0.44f) },   // MidnightBlue
            // { "steel blue",       new Color(0.27f, 0.51f, 0.71f) },   // SteelBlue
            // { "cornflower blue",  new Color(0.39f, 0.58f, 0.93f) },   // CornflowerBlue
            { "turquoise",        new Color(0.25f, 0.88f, 0.82f) },   // Turquoise
            // { "aquamarine",       new Color(0.50f, 1f, 0.83f) },      // Aquamarine

            // Purples / Violets
            // { "purple",           new Color(0.50f, 0f, 0.50f) },      // Purple
            { "dark purple",      new Color(0.30f, 0f, 0.30f) },      // DarkPurple
            { "violet",           new Color(0.93f, 0.51f, 0.93f) },   // Violet
            { "dark violet",      new Color(0.58f, 0f, 0.83f) },      // DarkViolet
            // { "indigo",           new Color(0.29f, 0f, 0.51f) },      // Indigo
            // { "lavender",         new Color(0.90f, 0.90f, 0.98f) },   // Lavender
            // { "orchid",           new Color(0.85f, 0.44f, 0.84f) },   // Orchid
            // { "plum",             new Color(0.87f, 0.63f, 0.87f) },   // Plum
            // { "medium purple",    new Color(0.58f, 0.44f, 0.86f) },   // MediumPurple
            // { "blue violet",      new Color(0.54f, 0.17f, 0.89f) },   // BlueViolet
            // { "slate blue",       new Color(0.42f, 0.35f, 0.80f) },   // SlateBlue

            // Browns / Beiges
            // { "brown",            new Color(0.65f, 0.16f, 0.16f) },   // SaddleBrown-ish
            { "dark brown",       new Color(0.40f, 0.26f, 0.13f) },   // DarkBrown
            { "light brown",      new Color(0.76f, 0.60f, 0.42f) },   // BurlyWood
            // { "chocolate",        new Color(0.82f, 0.41f, 0.12f) },   // Chocolate
            { "sienna",           new Color(0.63f, 0.32f, 0.18f) },   // Sienna
            // { "tan",              new Color(0.82f, 0.71f, 0.55f) },   // Tan
            // { "wheat",            new Color(0.96f, 0.87f, 0.70f) },   // Wheat
            // { "beige",            new Color(0.96f, 0.96f, 0.86f) },   // Beige
            // { "sandy brown",      new Color(0.96f, 0.64f, 0.38f) },   // SandyBrown
            // { "peru",             new Color(0.80f, 0.52f, 0.25f) },   // Peru

            // Grays / Neutrals
            { "dark gray",        new Color(0.25f, 0.25f, 0.25f) },   // DarkGray
            { "light gray",       new Color(0.75f, 0.75f, 0.75f) },   // LightGray
            // { "silver",           new Color(0.75f, 0.75f, 0.75f) },   // Silver
            { "dim gray",         new Color(0.41f, 0.41f, 0.41f) },   // DimGray
            // { "slate gray",       new Color(0.44f, 0.50f, 0.56f) },   // SlateGray
            // { "ivory",            new Color(1f, 1f, 0.94f) },         // Ivory
            // { "snow",             new Color(1f, 0.98f, 0.98f) },      // Snow
            { "cream",            new Color(1f, 0.99f, 0.82f) },      // Cream
    };

    public static string GetClosestColor(Color c)
    {
        if (c.a < 0.1f) return "empty";

        string closestColor = "empty";
        float minDist = float.MaxValue;

        foreach (var kvp in ColorMap)
        {
            float dist = (c.r - kvp.Value.r) * (c.r - kvp.Value.r)
                       + (c.g - kvp.Value.g) * (c.g - kvp.Value.g)
                       + (c.b - kvp.Value.b) * (c.b - kvp.Value.b);

            if (dist < minDist)
            {
                minDist = dist;
                closestColor = kvp.Key;
            }
        }

        if (minDist > 0.75f) return "empty";

        return closestColor;
    }

    public static Color GetColorFromName(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "empty")
            return new Color(0.2f, 0.2f, 0.2f, 0.5f);

        string key = name.ToLower();
        if (ColorMap.TryGetValue(key, out Color c)) return c;
        return Color.gray;
    }

    public static string MostColoredAtEdge(Dictionary<string, int> dict)
    {
        var maxCount = 0;
        var maxColor = "";
        foreach (var pair in dict.Where(pair => pair.Value > maxCount))
        {
            maxCount = pair.Value;
            maxColor = pair.Key;
        }
        return maxColor;
    }

    public static void ShuffleList(List<GameObject> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var temp = list[i];
            var randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    
}
