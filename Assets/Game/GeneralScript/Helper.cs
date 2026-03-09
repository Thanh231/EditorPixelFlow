using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Helper 
{
    public static string GetClosestColor(Color c)
    {
        if (c.a < 0.1f) return "empty";
        // var targetRed = new Color(0.82f, 0.14f, 0.13f);
        // var targetGreen = new Color(0.36f, 0.96f, 0.23f);
        // var targetDarkGreen = new Color(0.12f, 0.67f, 0.09f);
        // var targetYellow = new Color(0.98f, 0.87f, 0.24f);
        // var targetOrange = new Color(0.95f, 0.57f, 0.14f);
        // var targetBlack = new Color(0.309f, 0.322f, 0.357f);
        // var targetWhite = new Color(0.96f, 0.99f, 0.97f);
        // var targetPink = new Color(1f, 0.77f, 1f);
        // var targetDarkPink = new Color(0.945f, 0.34f, 0.71f);
        // var targetBlue = new Color(0.26f, 0.95f, 0.95f);
        // var targetDarkBlue = new Color(0.027f, 0.635f, 0.98f);

        Dictionary<string, Color> colorMap = new Dictionary<string, Color>()
        {
            { "red", Color.red },
            { "green", Color.green },
            { "blue", Color.blue },
            { "yellow", Color.yellow },
            { "black", Color.black },
            { "white", Color.white },
            { "pink", new Color(1f, 0.6f, 0.7f) },
            { "dark pink", new Color(1f, 0.2f, 0.7f) },
            { "orange", new Color(1f, 0.5f, 0f) },
            { "dark green", new Color(0f, 0.5f, 0f) },
            { "light green", new Color(0.56f, 0.93f, 0.56f) }, // LightGreen chuẩn
            { "dark blue", new Color(0f, 0f, 0.5f) },
            { "light blue", new Color(0.68f, 0.85f, 0.9f) },  // LightBlue chuẩn
            { "purple", new Color(0.5f, 0f, 0.5f) },          // Purple thật (tối hơn Magenta)
            { "brown", new Color(0.59f, 0.29f, 0f) },
            { "light brown", new Color(0.76f, 0.6f, 0.42f) },
            { "gray", Color.gray },
            { "cream", new Color(1f, 0.99f, 0.82f) }          // LemonChiffon/Cream
        };

        string closestColor = "empty";
        float minDist = float.MaxValue;

        foreach (var kvp in colorMap)
        {
            // Tính khoảng cách RGB (bỏ qua Alpha để chính xác hơn)
            float dist = Mathf.Sqrt(
                Mathf.Pow(c.r - kvp.Value.r, 2) +
                Mathf.Pow(c.g - kvp.Value.g, 2) +
                Mathf.Pow(c.b - kvp.Value.b, 2)
            );

            if (dist < minDist)
            {
                minDist = dist;
                closestColor = kvp.Key;
            }
        }

        // Nếu khoảng cách quá lớn (màu lạ), coi như ô trống
        if (minDist > 0.75f) return "empty";

        return closestColor;
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
