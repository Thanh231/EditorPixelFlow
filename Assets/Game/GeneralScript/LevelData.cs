using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public int width;
    public int height;
    public int targetDifficulty;
    public List<string> gridData = new List<string>();
    public List<LaneConfig> lanes = new List<LaneConfig>();
}

[System.Serializable]
public class LaneConfig
{
    public List<PigLayoutData> pigs = new List<PigLayoutData>();
}

[System.Serializable]
public class PigMarker
{
    public int LaneIndex = -1;
    public int index = -1;
    public bool IsValid()
    {
        return LaneIndex != -1 && index != -1;
    }
}