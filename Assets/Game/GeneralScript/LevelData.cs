using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public int width;
    public int height;
    public int targetDifficulty;
    public List<string> gridData; 
    public List<LaneData> lanes;  
}

[System.Serializable]
public class LaneData
{
    public List<PigLayoutData> pigs = new List<PigLayoutData>();
}