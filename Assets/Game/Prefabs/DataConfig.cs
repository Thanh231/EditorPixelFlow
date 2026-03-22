using UnityEngine;
using System.Collections.Generic;

public class DataConfig {
    public TextAsset sourceJson;

    [Header("Parsed Data")]
    public int levelIndex;
    public int width;
    public int height;
    public List<string> gridData;
    public List<LaneConfig> lanes;

}