using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;


public class LevelEditor : MonoBehaviour
{
    public class PigDataPool
    {
        public string color;
        public int bullets;
        public bool isUsed;
    }

    public string[] test1 = {
        "Paint",
        "Re-Color",
        "HardPixel",
        "EndHardPixel"
    };

    // public string[] featurePigName = {
    //     "Swap",
    //     "Hidden",
    //     "Linked",
    //     "EndLink",
    //     "HardPixel"
    // };

    public string[] test12 = {
        "Swap",
        "Hidden",
        "Linked",
        "EndLink",
    };

    [Header("UI References")]
    public GameObject pigFeature;
    public GameObject pigPrefab;
    public Sprite emptySprite;
    public GameObject cellPrefab;
    public GameObject featurePrefab;
    public GameObject colorPrefab;
    public RawImage textureDisplay;
    public Transform gridCellContainer; // Đối tượng Content của ScrollView
    public Transform featureContainer; // Đối tượng Content của ScrollView
    public Transform colorContainer; // Đối tượng Content của ScrollView
    public Transform pigContainer; // Đối tượng Content của ScrollView
    public Transform pigFeatureContainer;
    public TMP_InputField levelInput;
    public TMP_InputField widthInput;
    public TMP_InputField stepsInput;
    public TMP_InputField columnsInput;
    public TMP_Text reportTextDisplay;

    [Header("Settings")]
    [SerializeField] private int _queueColumns = 3;
    private int _targetWidth = 20;
    private int _targetStepsInput = 12;
    private const int MaxQueue1 = 5;

    [Header("Internal Data")]
    private Texture2D _textureInput;
    private string[,] _tempGrid;      // 35×35 full painting canvas
    private string[,] _finalGridMap;  // trimmed bounding box for simulation/export
    private int _finalWidth, _finalHeight;
    private int _finalOffsetX, _finalOffsetY;
    private const int TempGridSize = 35;
    private Dictionary<string, int> _finalColorCounts = new Dictionary<string, int>();
    private List<PigLayoutData>[] _multiColumnPigs;
    private string _activeColorBrush = "red";
    private List<string[,]> _undoHistory = new List<string[,]>();
    private (List<string> finalDeck, List<PigDataPool> finalPool, int actualSteps) _lastPigResult;
    private int _selectedPigCol = -1;
    private int _selectedPigRow = -1;
    private int _pigsBeforeAdjust = 0;
    private int levelIndex = 1;
    private int MaxUndoSteps = 10;

    private struct UndoSnapshot
    {
        public string[,] tempGrid;
        public List<GridCell.CellData> hardPixelEntries;
        public List<PigLayoutData>[] multiColumnPigs;
        public int queueColumns;
    }
    private List<UndoSnapshot> _undoSnapshots = new List<UndoSnapshot>();

    // --- Feature state ---
    public enum FeatureMode { None, Paint, RecolorPicking, RecolorWaitBrush, HardPixelColorPick, HardPixelCellPick, HardPixelActive }
    private FeatureMode _currentMode = FeatureMode.Paint;
    private string _recolorSourceColor = null;
    private List<FeatureBtn> _featureBtns = new List<FeatureBtn>();
    private List<FeaturePig> _pigFeatureBtns = new List<FeaturePig>();
    private string _activePigFeature = "Swap";
    private int _nextLinkId = 0;
    private List<(int col, int row)> _linkingPigs = new List<(int col, int row)>();

    // HardPixel state
    private string _hardPixelColor = null;
    private List<(int x, int y)> _hardPixelSelectedCells = new List<(int x, int y)>();
    private List<GridCell.CellData> _hardPixelEntries = new List<GridCell.CellData>();

    private void Start()
    {
        if (levelInput  != null && string.IsNullOrEmpty(levelInput.text))  levelInput.text  = levelIndex.ToString();
        if (widthInput  != null && string.IsNullOrEmpty(widthInput.text))  widthInput.text  = _targetWidth.ToString();
        if (stepsInput  != null && string.IsNullOrEmpty(stepsInput.text))  stepsInput.text  = _targetStepsInput.ToString();
        if (columnsInput != null && string.IsNullOrEmpty(columnsInput.text)) columnsInput.text = _queueColumns.ToString();

        GenerateColorUI();
        GenerateFeatureUI();
        GeneratePigFeatureUI();
    }

    // --- SPAWN PIG FEATURE PALETTE ---
    private void GeneratePigFeatureUI()
    {
        if (pigFeatureContainer == null || pigFeature == null) return;
        var toDelete = new List<GameObject>();
        foreach (Transform child in pigFeatureContainer) toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Destroy(go);

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = pigFeatureContainer as RectTransform;
        if (containerRect == null) return;

        const float spacing = 2f;
        int count = test12.Length;
        float containerW = containerRect.rect.width;
        float containerH = containerRect.rect.height;

        RectTransform pigFeatureRect = pigFeature.GetComponent<RectTransform>();
        float prefabW = (pigFeatureRect != null && pigFeatureRect.rect.width > 0f) ? pigFeatureRect.rect.width : 100f;
        float prefabH = (pigFeatureRect != null && pigFeatureRect.rect.height > 0f) ? pigFeatureRect.rect.height : 40f;

        float cellW = containerW;
        float cellH = Mathf.Max(1f, (containerH - spacing * (count - 1)) / count);
        float scale = Mathf.Min(cellW / prefabW, cellH / prefabH);

        GridLayoutGroup layout = pigFeatureContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) layout = pigFeatureContainer.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 1;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Vertical;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = new Vector2(0f, spacing);
        layout.cellSize = new Vector2(prefabW * scale, prefabH * scale);

        _pigFeatureBtns.Clear();
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(pigFeature, pigFeatureContainer);
            FeaturePig featurePig = go.GetComponent<FeaturePig>();
            featurePig.SetFeatureName(test12[i], this);
            _pigFeatureBtns.Add(featurePig);
        }
        NotifyPigFeatureSelected(0);
    }

    private void NotifyPigFeatureSelected(int selectedIndex)
    {
        for (int i = 0; i < _pigFeatureBtns.Count; i++)
            _pigFeatureBtns[i].SetSelected(i == selectedIndex);
    }

    // --- SPAWN COLOR PALETTE ---
    private void GenerateColorUI()
    {
        if (colorContainer == null || colorPrefab == null) return;

        // Thu thập trước rồi mới Destroy tránh lỗi khi iterate
        var toDelete = new List<GameObject>();
        foreach (Transform child in colorContainer) toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Destroy(go);

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = colorContainer as RectTransform;
        if (containerRect == null) return;

        const float baseSize = 60f;   // kích thước ô vuông (gấp đôi 30f)
        const float spacing  = 8f;    // khoảng cách đều nhau giữa các ô
        var colorKeys = new List<string>(Helper.ColorMap.Keys);
        int count = colorKeys.Count;

        float containerW = containerRect.rect.width;

        // Số cột vừa khít chiều ngang container
        int cols = Mathf.Max(1, Mathf.FloorToInt((containerW + spacing) / (baseSize + spacing)));
        // cellSize tính lại để lấp đầy đúng chiều ngang (không có khoảng trắng thừa)
        float cellSize = Mathf.Max(1f, (containerW - spacing * (cols - 1)) / cols);

        // Tính tổng chiều cao content để ContentSizeFitter scroll đúng
        int rows = Mathf.CeilToInt((float)count / cols);
        float totalH = rows * cellSize + (rows - 1) * spacing;
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalH);

        GridLayoutGroup layout = colorContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) layout = colorContainer.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount  = cols;
        layout.startCorner      = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis        = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment   = TextAnchor.UpperLeft;
        layout.spacing          = new Vector2(spacing, spacing);
        layout.cellSize         = new Vector2(cellSize, cellSize);

        for (int i = 0; i < count; i++)
        {
            string colorName = colorKeys[i];
            GameObject go = Instantiate(colorPrefab, colorContainer);

            Image img = go.GetComponent<Image>();
            if (img != null) img.color = Helper.GetColorFromName(colorName);

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                string captured = colorName;
                btn.onClick.AddListener(() => SetActiveBrush(captured));
            }

            ColorBtn label = go.GetComponentInChildren<ColorBtn>();
            if (label != null) label.SetColor(colorName, this);
        }
    }

    // --- SPAWN FEATURE PALETTE ---
    private void GenerateFeatureUI()
    {
        if (featureContainer == null || featurePrefab == null) return;
        var toDeleteF = new List<GameObject>();
        foreach (Transform child in featureContainer) toDeleteF.Add(child.gameObject);
        foreach (var go in toDeleteF) Destroy(go);

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = featureContainer as RectTransform;
        if (containerRect == null) return;

        const float spacing = 2f;
        int count = test1.Length;
        float containerW = containerRect.rect.width;
        float containerH = containerRect.rect.height;

        RectTransform featurePrefabRect = featurePrefab.GetComponent<RectTransform>();
        float prefabW = (featurePrefabRect != null && featurePrefabRect.rect.width > 0f) ? featurePrefabRect.rect.width : 100f;
        float prefabH = (featurePrefabRect != null && featurePrefabRect.rect.height > 0f) ? featurePrefabRect.rect.height : 40f;

        float cellW = containerW;
        float cellH = Mathf.Max(1f, (containerH - spacing * (count - 1)) / count);
        float scaleX = cellW / prefabW;
        float scaleY = cellH / prefabH;
        float scale = Mathf.Min(scaleX, scaleY);

        GridLayoutGroup layout = featureContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) layout = featureContainer.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 1;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Vertical;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = new Vector2(0f, spacing);
        layout.cellSize = new Vector2(prefabW * scale, prefabH * scale);

        _featureBtns.Clear();
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(featurePrefab, featureContainer);

            FeatureBtn featureBtn = go.GetComponent<FeatureBtn>();
            featureBtn.SetFeatureName(test1[i], this);
            _featureBtns.Add(featureBtn);

        }
        NotifyFeatureSelected(-1);
    }

    public void OnClickFeatureButton(int index)
    {
        if (index < 0 || index >= test1.Length) return;
        NotifyFeatureSelected(index);

        switch (test1[index])
        {
            case "Paint":
                Paint();
                break;
            case "Re-Color":
                ReColorSpecificColor();
                break;
            case "HardPixel":
                ProcessHardPixelFeature();
                break;
            case "EndHardPixel":
                CommitHardPixelGroup();
                break;
            default:
                break;
        }
    }

    public void ProcessHardPixelFeature()
    {
        _hardPixelColor = null;
        _hardPixelSelectedCells.Clear();
        _currentMode = FeatureMode.HardPixelColorPick;
        UpdateReport("HardPixel: select a color from the palette first.");
    }
    public void OnClickFeaturePigButton(int index)
    {
        if (index < 0 || index >= test12.Length) return;

        string feature = test12[index];

        // EndLink: finalize current chain and stay in Linked mode to start a new one
        if (feature == "EndLink")
        {
            if (_linkingPigs.Count == 1)
            {
                ClearPigLinkData(_multiColumnPigs[_linkingPigs[0].col][_linkingPigs[0].row]);
            }
            _linkingPigs.Clear();
            _activePigFeature = "Linked";
            int linkedIndex = System.Array.IndexOf(test12, "Linked");
            NotifyPigFeatureSelected(linkedIndex);
            _selectedPigCol = -1;
            _selectedPigRow = -1;
            TryAutoFitLinkedPigs();
            return;
        }

        // Finalize any pending 1-pig chain (lone link is meaningless)
        if (_linkingPigs.Count == 1)
        {
            _multiColumnPigs[_linkingPigs[0].col][_linkingPigs[0].row].linkId = -1;
        }
        _linkingPigs.Clear();

        _activePigFeature = feature;
        NotifyPigFeatureSelected(index);
        _selectedPigCol = -1;
        _selectedPigRow = -1;
        SpawnPigUI();

        switch (feature)
        {
            case "Swap":
                UpdateReport("Mode: Swap — click a pig to select, click another to swap.");
                break;
            case "Hidden":
                UpdateReport("Mode: Hidden — click a pig to toggle hidden (alpha 0.5).");
                break;
            case "Linked":
                UpdateReport("Mode: Linked — click adjacent pigs to chain them. Press EndLink to finish.");
                break;
        }
    }

    private void NotifyFeatureSelected(int selectedIndex)
    {
        for (int i = 0; i < _featureBtns.Count; i++)
            _featureBtns[i].SetSelected(i == selectedIndex);
    }

    public void Paint()
    {
        _currentMode = FeatureMode.Paint;
        _recolorSourceColor = null;
        UpdateReport("Mode: Paint — click cell to paint");
    }

    // Bước 1: kích hoạt mode chờ click cell để lấy màu nguồn
    public void ReColorSpecificColor()
    {
        _currentMode = FeatureMode.RecolorPicking;
        _recolorSourceColor = null;
        UpdateReport("ReColor: click a cell to pick source color");
    }

    // Bước 2: GridCell gọi hàm này khi người dùng click ô trong mode RecolorPicking
    public void SelectRecolorSource(string sourceColor)
    {
        _recolorSourceColor = sourceColor;
        _currentMode = FeatureMode.RecolorWaitBrush;
        UpdateReport($"ReColor: source = [{sourceColor}] — now pick a color from palette");
    }

    public void DisableColor() { }

    public void EnableColor() { }

    // ─── HARDPIXEL HELPERS ───────────────────────────────────────────────────

    // Toggle cell selection in HardPixelCellPick mode; tints selected cells yellow.
    // cellImage is passed directly from PaintCell to avoid any index-based child lookup.
    private void ToggleHardPixelCell(int x, int y, Image cellImage)
    {
        if (_tempGrid == null) return;
        int idx = _hardPixelSelectedCells.FindIndex(c => c.x == x && c.y == y);
        if (idx >= 0)
        {
            _hardPixelSelectedCells.RemoveAt(idx);
            if (cellImage != null) RestoreGridCellColor(x, y, cellImage);
        }
        else
        {
            _hardPixelSelectedCells.Add((x, y));
            if (cellImage != null) cellImage.color = Color.Lerp(cellImage.color, Color.yellow, 0.55f);
        }
        UpdateReport($"HardPixel: {_hardPixelSelectedCells.Count} cell(s) selected — click EndHardPixel to commit.");
    }

    // Validate selection, create CellData, hide source cells, spawn overlay.
    private void CommitHardPixelGroup()
    {
        if (_currentMode != FeatureMode.HardPixelCellPick)
        {
            UpdateReport("HardPixel: not in cell-selection mode.");
            return;
        }
        if (_hardPixelSelectedCells.Count < 2)
        {
            UpdateReport("HardPixel: select at least 2 cells first.");
            return;
        }

        int minX = _hardPixelSelectedCells.Min(c => c.x);
        int maxX = _hardPixelSelectedCells.Max(c => c.x);
        int minY = _hardPixelSelectedCells.Min(c => c.y);
        int maxY = _hardPixelSelectedCells.Max(c => c.y);
        int sizeX = maxX - minX + 1;
        int sizeY = maxY - minY + 1;

        if (_hardPixelSelectedCells.Count != sizeX * sizeY)
        {
            UpdateReport("HardPixel: selected cells must form a perfect rectangle!");
            return;
        }

        var entry = new GridCell.CellData
        {
            xPos = minX,
            yPos = minY,
            sizeX = sizeX,
            sizeY = sizeY,
            colorName = _hardPixelColor,
            bulletCount = Mathf.Max(2, _hardPixelSelectedCells.Count)
        };

        _hardPixelEntries.Add(entry);
        _hardPixelSelectedCells.Clear();
        _currentMode = FeatureMode.HardPixelActive;

        // Rebuild grid to hide source cells and add overlay
        GenerateGridUI();
        ActionShuffleAndSimulate();
        UpdateReport($"HardPixel committed: {sizeX}×{sizeY} at ({minX},{minY}), bullets={entry.bulletCount}. Click cell to increment bullet count.");
    }

    // Spawn absolutely-positioned overlay GameObjects for each committed HardPixel entry.
    private void SpawnHardPixelOverlay(GridLayoutGroup layout)
    {
        if (_hardPixelEntries.Count == 0) return;

        const int uiRows = 35;
        const float spacing = 1f;

        float cellW = layout.cellSize.x;
        float cellH = layout.cellSize.y;
        int padLeft = layout.padding.left;
        int padTop = layout.padding.top;

        foreach (var entry in _hardPixelEntries)
        {
            GameObject overlayGO = Instantiate(cellPrefab, gridCellContainer);

            // Disable GridCell to prevent NullRef (Setup() was never called on overlays)
            GridCell gcComp = overlayGO.GetComponent<GridCell>();
            if (gcComp != null) gcComp.enabled = false;

            // Exclude from GridLayoutGroup layout
            LayoutElement le = overlayGO.GetComponent<LayoutElement>();
            if (le == null) le = overlayGO.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // Compute position: top-left of bounding box in UI coords (top-left origin)
            int row_ui_top = entry.yPos;
            float posX = padLeft + entry.xPos * (cellW + spacing);
            float posY = padTop + row_ui_top * (cellH + spacing);
            float width = entry.sizeX * cellW + (entry.sizeX - 1) * spacing;
            float height = entry.sizeY * cellH + (entry.sizeY - 1) * spacing;

            RectTransform rt = overlayGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(posX, -posY);
            rt.sizeDelta = new Vector2(width, height);

            Image img = overlayGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = Helper.GetColorFromName(entry.colorName);
                img.raycastTarget = true;
            }

            // Add a dedicated TextMeshProUGUI to overlay (avoids relying on prefab child index)
            GameObject textGO = new GameObject("BulletCountText", typeof(RectTransform));
            textGO.transform.SetParent(overlayGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            TMPro.TextMeshProUGUI overlayText = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            overlayText.text = entry.bulletCount.ToString();
            overlayText.alignment = TMPro.TextAlignmentOptions.Center;
            overlayText.fontSize = Mathf.Clamp(Mathf.Min(width, height) * 0.4f, 8f, 36f);
            overlayText.color = Color.white;
            overlayText.raycastTarget = false;

            // Click → increment bullet count while in HardPixelActive mode
            Button btn = overlayGO.GetComponent<Button>();
            if (btn == null) btn = overlayGO.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var capturedEntry = entry;
            var capturedText = overlayText;
            btn.onClick.AddListener(() =>
            {
                if (_currentMode == FeatureMode.HardPixelActive)
                {
                    capturedEntry.bulletCount++;
                    capturedText.text = capturedEntry.bulletCount.ToString();
                    ActionShuffleAndSimulate();
                    UpdateReport($"HardPixel bullet count: {capturedEntry.bulletCount} — re-simulated.");
                }
            });
        }
    }

    // Look up the Image component of the regular grid cell at (x, y) by child index.
    private Image GetGridCellImage(int x, int y)
    {
        const int uiCols = 35;
        int row_ui = y;
        int index = row_ui * uiCols + x;
        if (index < 0 || index >= gridCellContainer.childCount) return null;
        return gridCellContainer.GetChild(index).GetComponent<Image>();
    }

    // Restore a regular grid cell's color from _tempGrid (used when deselecting in HardPixel mode).
    private void RestoreGridCellColor(int x, int y, Image img)
    {
        if (_tempGrid == null) return;
        string colorName = _tempGrid[x, y];
        GridCell cell = img.GetComponent<GridCell>();
        if (colorName == "scan_empty")
        {
            if (emptySprite != null) img.sprite = emptySprite;
            img.color = Color.white;
        }
        else if (colorName == "empty")
        {
            img.sprite = null;
            img.color = new Color(0, 0, 0, 0.4f);
        }
        else
        {
            if (cell != null && cell.defaultSprite != null) img.sprite = cell.defaultSprite;
            img.color = Helper.GetColorFromName(colorName);
        }
    }


    public void OnClickOpenImage()
    {
        // Cách viết chuẩn cho phiên bản mới nhất của Simple File Browser
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".png", ".jpg", ".jpeg"));
        FileBrowser.SetDefaultFilter(".png");

        FileBrowser.ShowLoadDialog((paths) =>
        {
            StartCoroutine(LoadImageRoutine(paths[0]));
        },
            null,
            FileBrowser.PickMode.Files,
            false,
            null,
            "Select Map Image",
            "Load");
    }

    public void OnClickOpenJson()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");

        FileBrowser.ShowLoadDialog((paths) =>
        {
            StartCoroutine(LoadJsonRoutine(paths[0]));
        },
            null,
            FileBrowser.PickMode.Files,
            false,
            null,
            "Select Grid JSON",
            "Load");
    }

    private IEnumerator LoadJsonRoutine(string path)
    {
        string json;
        try { json = File.ReadAllText(path); }
        catch (System.Exception e)
        {
            UpdateReport($"<color=red>Error reading JSON:</color> {e.Message}");
            yield break;
        }

        var rawGrid = ParseJsonGrid(json);
        if (rawGrid == null || rawGrid.Count == 0)
        {
            UpdateReport("<color=red>Error:</color> Invalid or empty JSON grid.");
            yield break;
        }

        int jsonH = rawGrid.Count;
        int jsonW = rawGrid[0].Count;

        if (jsonW > TempGridSize || jsonH > TempGridSize)
        {
            UpdateReport($"<color=red>Error:</color> Grid too large ({jsonW}×{jsonH}), max is {TempGridSize}×{TempGridSize}.");
            yield break;
        }

        int offsetX = (TempGridSize - jsonW) / 2;
        int offsetY = (TempGridSize - jsonH) / 2;

        _tempGrid = new string[TempGridSize, TempGridSize];
        for (int cy = 0; cy < TempGridSize; cy++)
            for (int cx = 0; cx < TempGridSize; cx++)
                _tempGrid[cx, cy] = "empty";

        // JSON row 0 = top of image → low y in _tempGrid (rendered at top of UI)
        for (int row = 0; row < jsonH; row++)
        {
            var rowData = rawGrid[row];
            int count = Mathf.Min(rowData.Count, jsonW);
            for (int col = 0; col < count; col++)
            {
                string colorName = rowData[col].ToLowerInvariant();
                _tempGrid[col + offsetX, row + offsetY] = colorName;
            }
        }

        _hardPixelEntries.Clear();
        _hardPixelSelectedCells.Clear();
        _currentMode = FeatureMode.Paint;

        ComputeFinalGrid();
        GenerateGridUI();
        ActionShuffleAndSimulate();
        UpdateReport($"JSON loaded: {jsonW}×{jsonH}  →  Final: {_finalWidth}×{_finalHeight}");
        yield return null;
    }

    private List<List<string>> ParseJsonGrid(string json)
    {
        var result = new List<List<string>>();
        int depth = 0;
        int start = -1;
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[')
            {
                depth++;
                if (depth == 2) start = i;
            }
            else if (c == ']')
            {
                if (depth == 2)
                    result.Add(ParseJsonStringArray(json.Substring(start, i - start + 1)));
                depth--;
            }
        }
        return result;
    }

    private List<string> ParseJsonStringArray(string rowStr)
    {
        var items = new List<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(rowStr, "\"([^\"]*)\"");
        foreach (System.Text.RegularExpressions.Match m in matches)
            items.Add(m.Groups[1].Value);
        return items;
    }

    private IEnumerator LoadImageRoutine(string path)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                _textureInput = DownloadHandlerTexture.GetContent(www);
                textureDisplay.texture = _textureInput;
                ProcessScan();
            }
        }
    }

    // --- 2. XỬ LÝ SCAN VÀ TẠO GRID ---
    public void ProcessScan()
    {
        if (_textureInput == null)
        {
            UpdateReport("<color=red>Error:</color> No image loaded!");
            return;
        }

        if (widthInput != null && !string.IsNullOrEmpty(widthInput.text))
            if (int.TryParse(widthInput.text, out int w))
                _targetWidth = Mathf.Clamp(w, 5, 100);
        if (stepsInput != null && !string.IsNullOrEmpty(stepsInput.text))
            if (int.TryParse(stepsInput.text, out int s))
                _targetStepsInput = s;

        if (columnsInput != null && !string.IsNullOrEmpty(columnsInput.text))
            if (int.TryParse(columnsInput.text, out int col))
                _queueColumns = Mathf.Clamp(col, 2, 5);

        if (levelInput != null && !string.IsNullOrEmpty(levelInput.text))
            if (int.TryParse(levelInput.text, out int lvl))
                levelIndex = Mathf.Max(1, lvl);

        int scanW = _targetWidth;
        float aspect = (float)_textureInput.width / _textureInput.height;
        int scanH = Mathf.RoundToInt(scanW / aspect);
        float stepX = (float)_textureInput.width / scanW;
        float stepY = (float)_textureInput.height / scanH;
        int offsetX = (TempGridSize - scanW) / 2;
        int offsetY = (TempGridSize - scanH) / 2;

        _tempGrid = new string[TempGridSize, TempGridSize];
        for (int cy = 0; cy < TempGridSize; cy++)
            for (int cx = 0; cx < TempGridSize; cx++)
                _tempGrid[cx, cy] = "empty";

        for (int sy = 0; sy < scanH; sy++)
        {
            for (int sx = 0; sx < scanW; sx++)
            {
                int px = Mathf.FloorToInt(sx * stepX + stepX * 0.5f);
                int py = Mathf.FloorToInt(sy * stepY + stepY * 0.5f);
                Color col = _textureInput.GetPixel(px, py);
                _tempGrid[sx + offsetX, scanH - 1 - sy + offsetY] = (col.a < 0.1f) ? "scan_empty" : Helper.GetClosestColor(col);
            }
        }

        ComputeFinalGrid();
        GenerateGridUI();
        ActionShuffleAndSimulate();
        UpdateReport($"Scan OK: {scanW}x{scanH}  →  Final: {_finalWidth}x{_finalHeight}");
    }

    // Tính final grid = bounding box của tất cả cell không phải "empty" trong tempGrid
    private void ComputeFinalGrid()
    {
        if (_tempGrid == null) { _finalGridMap = null; _finalWidth = 0; _finalHeight = 0; return; }
        int minX = TempGridSize, maxX = -1, minY = TempGridSize, maxY = -1;
        for (int y = 0; y < TempGridSize; y++)
        {
            for (int x = 0; x < TempGridSize; x++)
            {
                string c = _tempGrid[x, y];
                if (!string.IsNullOrEmpty(c) && c != "empty")
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }
        }
        if (maxX < 0) { _finalGridMap = null; _finalWidth = 0; _finalHeight = 0; return; }
        _finalWidth = maxX - minX + 1;
        _finalHeight = maxY - minY + 1;
        _finalOffsetX = minX;
        _finalOffsetY = minY;
        _finalGridMap = new string[_finalWidth, _finalHeight];
        for (int y = 0; y < _finalHeight; y++)
            for (int x = 0; x < _finalWidth; x++)
                _finalGridMap[x, y] = _tempGrid[x + minX, y + minY];
    }

    private void GenerateGridUI()
    {
        var toDeleteG = new List<GameObject>();
        foreach (Transform child in gridCellContainer) toDeleteG.Add(child.gameObject);
        foreach (var go in toDeleteG) Destroy(go);

        if (gridCellContainer == null || cellPrefab == null) return;

        const int uiColumns = 35;
        const int uiRows = 35;
        const float cellSpacing = 1f;

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = gridCellContainer as RectTransform;
        if (containerRect == null) return;

        GridLayoutGroup layout = gridCellContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) layout = gridCellContainer.gameObject.AddComponent<GridLayoutGroup>();

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = uiColumns;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = new Vector2(cellSpacing, cellSpacing);

        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        float availableWidth = containerWidth - layout.padding.left - layout.padding.right - cellSpacing * (uiColumns - 1);
        float availableHeight = containerHeight - layout.padding.top - layout.padding.bottom - cellSpacing * (uiRows - 1);

        RectTransform prefabRect = cellPrefab.GetComponent<RectTransform>();
        float prefabWidth = (prefabRect != null && prefabRect.rect.width > 0f) ? prefabRect.rect.width : 20f;
        float prefabHeight = (prefabRect != null && prefabRect.rect.height > 0f) ? prefabRect.rect.height : 20f;

        float fitCellWidth = Mathf.Max(1f, availableWidth / uiColumns);
        float fitCellHeight = Mathf.Max(1f, availableHeight / uiRows);
        float scale = Mathf.Min(fitCellWidth / prefabWidth, fitCellHeight / prefabHeight);

        layout.cellSize = new Vector2(prefabWidth * scale, prefabHeight * scale);

        for (int row = 0; row < uiRows; row++)
        {
            for (int col = 0; col < uiColumns; col++)
            {
                int x = col;
                int y = row;

                // Luôn đọc từ _tempGrid (canvas 35x35)
                string colorName = (_tempGrid != null) ? _tempGrid[x, y] : "empty";

                GameObject cellGO = Instantiate(cellPrefab, gridCellContainer);

                Image cellImage = cellGO.GetComponent<Image>();
                GridCell cell = cellGO.GetComponent<GridCell>();
                if (cell != null)
                {
                    if (_tempGrid != null) cell.Setup(x, y, this, 0);
                    else cell.enabled = false;
                }

                if (cellImage != null)
                {
                    // Tất cả cell đều có thể vẽ để mở rộng vùng final grid
                    cellImage.raycastTarget = _tempGrid != null;

                    // Ẩn cells nằm trong vùng HardPixel đã commit
                    bool coveredByHardPixel = _hardPixelEntries.Any(e =>
                        x >= e.xPos && x < e.xPos + e.sizeX &&
                        y >= e.yPos && y < e.yPos + e.sizeY);
                    if (coveredByHardPixel)
                    {
                        cellImage.color = new Color(0, 0, 0, 0);
                        cellImage.raycastTarget = false;
                    }
                    else if (colorName == "scan_empty")
                    {
                        if (emptySprite != null) cellImage.sprite = emptySprite;
                        cellImage.color = Color.white;
                    }
                    else if (colorName == "empty")
                    {
                        cellImage.sprite = null;
                        cellImage.color = new Color(0, 0, 0, 0.4f);
                    }
                    else
                    {
                        if (cell != null && cell.defaultSprite != null) cellImage.sprite = cell.defaultSprite;
                        cellImage.color = Helper.GetColorFromName(colorName);
                    }
                }
            }
        }

        // Spawn overlay cells for committed HardPixel entries
        SpawnHardPixelOverlay(layout);
    }

    // --- 3. TÔ MÀU & SIMULATION ---
    public void SetActiveBrush(string colorName)
    {
        // HardPixel: first step is picking a color
        if (_currentMode == FeatureMode.HardPixelColorPick)
        {
            _hardPixelColor = colorName;
            _currentMode = FeatureMode.HardPixelCellPick;
            UpdateReport($"HardPixel: color=[{colorName}] — click cells to select them, EndHardPixel to commit.");
            return;
        }

        _activeColorBrush = colorName;
        if (_currentMode == FeatureMode.Paint)
            UpdateReport($"[Paint] Active brush with color [{colorName}]");
        // Nếu đang chờ chọn màu đích cho ReColor → áp dụng ngay
        if (_currentMode == FeatureMode.RecolorWaitBrush && _recolorSourceColor != null)
        {
            RecordUndo();
            for (int y = 0; y < TempGridSize; y++)
                for (int x = 0; x < TempGridSize; x++)
                    if (_tempGrid[x, y] == _recolorSourceColor)
                        _tempGrid[x, y] = colorName;
            _recolorSourceColor = null;
            _currentMode = FeatureMode.Paint;
            NotifyFeatureSelected(-1);
            ComputeFinalGrid();
            GenerateGridUI();
            ActionShuffleAndSimulate();
            UpdateReport($"ReColor: all cells recolored to [{colorName}]");
        }
    }

    public void PaintCell(int x, int y, Image cellImage)
    {
        if (_tempGrid == null) return;

        if (_currentMode == FeatureMode.RecolorPicking)
        {
            SelectRecolorSource(_tempGrid[x, y]);
            return;
        }

        if (_currentMode == FeatureMode.HardPixelCellPick)
        {
            ToggleHardPixelCell(x, y, cellImage);
            return;
        }

        if (_currentMode != FeatureMode.Paint) return;
        if (_tempGrid[x, y] == _activeColorBrush) return;
        RecordUndo();
        if (_activeColorBrush == "empty" || _activeColorBrush == "scan_empty")
        {
            _tempGrid[x, y] = "scan_empty";
            if (emptySprite != null) cellImage.sprite = emptySprite;
            cellImage.color = Color.white;
        }
        else
        {
            _tempGrid[x, y] = _activeColorBrush;
            GridCell cell = cellImage.GetComponent<GridCell>();
            if (cell != null && cell.defaultSprite != null) cellImage.sprite = cell.defaultSprite;
            cellImage.color = Helper.GetColorFromName(_activeColorBrush);
        }
        ComputeFinalGrid();
        ActionShuffleAndSimulate();
    }

    // Drag-paint: same as PaintCell but skips HardPixel toggle (only click should toggle).
    public void PaintCellDrag(int x, int y, Image cellImage)
    {
        if (_currentMode == FeatureMode.HardPixelCellPick ||
            _currentMode == FeatureMode.HardPixelColorPick ||
            _currentMode == FeatureMode.HardPixelActive) return;
        PaintCell(x, y, cellImage);
    }

    // Gọi hàm này từ UI khi thay đổi Target Steps hoặc Columns
    public void OnSettingsChanged()
    {
        if (stepsInput != null && !string.IsNullOrEmpty(stepsInput.text))
            if (int.TryParse(stepsInput.text, out int s))
                _targetStepsInput = s;
        if (columnsInput != null && !string.IsNullOrEmpty(columnsInput.text))
            if (int.TryParse(columnsInput.text, out int col))
                _queueColumns = Mathf.Clamp(col, 2, 5);
        ActionShuffleAndSimulate();
    }

    public void ActionShuffleAndSimulate()
    {
        if (_finalGridMap == null) return;

        // Luôn đọc lại settings từ InputField dù được gọi từ đâu
        if (stepsInput != null && !string.IsNullOrEmpty(stepsInput.text))
            if (int.TryParse(stepsInput.text, out int s))
                _targetStepsInput = s;
        if (columnsInput != null && !string.IsNullOrEmpty(columnsInput.text))
            if (int.TryParse(columnsInput.text, out int col))
                _queueColumns = Mathf.Clamp(col, 2, 5);

        _finalColorCounts.Clear();
        for (int y = 0; y < _finalHeight; y++)
        {
            for (int x = 0; x < _finalWidth; x++)
            {
                string c = _finalGridMap[x, y];
                if (c != "empty" && c != "scan_empty" && !string.IsNullOrEmpty(c))
                {
                    if (!_finalColorCounts.ContainsKey(c)) _finalColorCounts[c] = 0;
                    _finalColorCounts[c]++;
                }
            }
        }
        // HardPixel: remove underlying cells' original color contributions, add HardPixel color with bulletCount
        if (_tempGrid != null)
        {
            foreach (var hp in _hardPixelEntries)
            {
                // Subtract each covered cell's original color from the count
                for (int hy = hp.yPos; hy < hp.yPos + hp.sizeY; hy++)
                    for (int hx = hp.xPos; hx < hp.xPos + hp.sizeX; hx++)
                    {
                        string orig = _tempGrid[hx, hy];
                        if (!string.IsNullOrEmpty(orig) && orig != "empty" && orig != "scan_empty"
                            && _finalColorCounts.ContainsKey(orig))
                        {
                            _finalColorCounts[orig]--;
                            if (_finalColorCounts[orig] <= 0) _finalColorCounts.Remove(orig);
                        }
                    }
                // Add the HardPixel color with its bulletCount
                if (!_finalColorCounts.ContainsKey(hp.colorName)) _finalColorCounts[hp.colorName] = 0;
                _finalColorCounts[hp.colorName] += hp.bulletCount;
            }
        }
        _lastPigResult = RunAdaptiveSimulation();
        _multiColumnPigs = new List<PigLayoutData>[_queueColumns];
        for (int i = 0; i < _queueColumns; i++) _multiColumnPigs[i] = new List<PigLayoutData>();

        if (_lastPigResult.finalDeck != null)
        {
            var pool = _lastPigResult.finalPool;
            foreach (var p in pool) p.isUsed = false;
            for (int i = 0; i < _lastPigResult.finalDeck.Count; i++)
            {
                string color = _lastPigResult.finalDeck[i];
                var data = pool.FirstOrDefault(x => x.color == color && !x.isUsed);
                if (data != null)
                {
                    data.isUsed = true;
                    _multiColumnPigs[i % _queueColumns].Add(new PigLayoutData { colorName = data.color, bullets = data.bullets });
                }
            }
            UpdateReport($"Target: {_targetStepsInput} | Actual: {_lastPigResult.actualSteps} steps | Pigs: {_lastPigResult.finalPool.Count}");
        }
        else
        {
            UpdateReport($"Pigs: {_pigsBeforeAdjust} | <color=red>Unwinnable!</color>");
        }
        _selectedPigCol = -1;
        _selectedPigRow = -1;
        _nextLinkId = 0;
        _linkingPigs.Clear();
        SpawnPigUI();
    }

    private void SpawnPigUI()
    {
        if (pigContainer == null || pigPrefab == null) return;
        var toDelete = new List<GameObject>();
        foreach (Transform child in pigContainer) toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Destroy(go);
        if (_multiColumnPigs == null) return;

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = pigContainer as RectTransform;
        if (containerRect == null) return;

        float containerW = containerRect.rect.width;
        float containerH = containerRect.rect.height;
        float colSpacing = 4f;
        float rowSpacing = 2f;

        float colWidth = Mathf.Max(1f, (containerW - colSpacing * (_queueColumns - 1)) / _queueColumns);
        int maxRows = _multiColumnPigs.Max(c => c.Count);
        float pigHeight = maxRows > 0
            ? Mathf.Max(20f, (containerH - rowSpacing * (maxRows - 1)) / maxRows)
            : containerH;

        HorizontalLayoutGroup hLayout = pigContainer.GetComponent<HorizontalLayoutGroup>();
        if (hLayout == null) hLayout = pigContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = colSpacing;
        hLayout.childAlignment = TextAnchor.UpperLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;

        for (int col = 0; col < _queueColumns; col++)
        {
            GameObject colGO = new GameObject($"PigCol_{col}", typeof(RectTransform));
            colGO.transform.SetParent(pigContainer, false);

            RectTransform colRect = colGO.GetComponent<RectTransform>();
            colRect.sizeDelta = new Vector2(colWidth, containerH);

            VerticalLayoutGroup vLayout = colGO.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = rowSpacing;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandHeight = false;

            if (col >= _multiColumnPigs.Length) continue;

            for (int row = 0; row < _multiColumnPigs[col].Count; row++)
            {
                var pigData = _multiColumnPigs[col][row];
                GameObject pigGO = Instantiate(pigPrefab, colGO.transform);

                LayoutElement le = pigGO.GetComponent<LayoutElement>();
                if (le == null) le = pigGO.AddComponent<LayoutElement>();
                le.preferredHeight = pigHeight;
                le.flexibleWidth = 1f;

                bool isSelected = (col == _selectedPigCol && row == _selectedPigRow);
                Image pigImage = pigGO.GetComponent<Image>();
                if (pigImage != null)
                {
                    pigImage.raycastTarget = true;
                    pigImage.alphaHitTestMinimumThreshold = 0f; // luôn clickable dù alpha thấp
                    Color baseColor = Helper.GetColorFromName(pigData.colorName);
                    if (pigData.isHidden) baseColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
                    pigImage.color = isSelected ? Color.Lerp(baseColor, Color.white, 0.5f) : baseColor;
                }

                PigComponent pigComp = pigGO.GetComponent<PigComponent>();
                if (pigComp != null)
                {
                    pigComp.SetBulletCount(pigData.bullets, pigData.colorName);
                    if (pigData.isHidden) pigImage.color = new Color(pigImage.color.r, pigImage.color.g, pigImage.color.b, 0.2f);
                    pigComp.pigLeft = pigData.pigLeft;
                    pigComp.pigRight = pigData.pigRight;
                }

                // Link visual: golden tint; brighter if pig is in the chain being built
                if (pigData.linkId >= 0 && pigImage != null)
                {
                    bool inChain = _linkingPigs.Any(p => p.col == col && p.row == row);
                    pigImage.color = Color.Lerp(pigImage.color,
                        inChain ? Color.yellow : new Color(1f, 0.75f, 0f, pigImage.color.a),
                        inChain ? 0.55f : 0.35f);
                    if (pigComp != null && pigComp.text != null)
                        pigComp.text.text = $"L{pigData.linkId}\n{pigData.bullets}";
                }

                Button btn = pigGO.GetComponent<Button>();
                if (btn == null) btn = pigGO.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                int capturedCol = col;
                int capturedRow = row;
                btn.onClick.AddListener(() => OnPigClicked(capturedCol, capturedRow));
            }

            // Ghost pig ở cuối mỗi cột — màu xám alpha 0.5, không ảnh hưởng maxRows
            GameObject ghostPigGO = Instantiate(pigPrefab, colGO.transform);

            LayoutElement ghostLE = ghostPigGO.GetComponent<LayoutElement>();
            if (ghostLE == null) ghostLE = ghostPigGO.AddComponent<LayoutElement>();
            ghostLE.preferredHeight = pigHeight;
            ghostLE.flexibleWidth = 1f;

            Image ghostImage = ghostPigGO.GetComponent<Image>();
            if (ghostImage != null)
                ghostImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            TextMeshProUGUI ghostText = ghostPigGO.GetComponentInChildren<TextMeshProUGUI>();
            if (ghostText != null)
            {
                ghostText.text = "+";
                ghostText.fontSize = 36;
                ghostText.alignment = TextAlignmentOptions.Center;
            }

            Button ghostBtn = ghostPigGO.GetComponent<Button>();
            if (ghostBtn == null) ghostBtn = ghostPigGO.AddComponent<Button>();
            ghostBtn.onClick.RemoveAllListeners();
            int capturedGhostCol = col;
            ghostBtn.onClick.AddListener(() => OnEmptySlotClicked(capturedGhostCol));
        }
    }

    // Khi click vào ghost pig cuối cột: di chuyển heo đang chọn đến cuối cột đó,
    // các heo phía sau vị trí cũ tự dồn lên (List.RemoveAt)
    private void OnEmptySlotClicked(int col)
    {
        if (_activePigFeature != "Swap" || _selectedPigCol == -1) return;

        var pig = _multiColumnPigs[_selectedPigCol][_selectedPigRow];
        _multiColumnPigs[_selectedPigCol].RemoveAt(_selectedPigRow);
        _multiColumnPigs[col].Add(pig);

        _selectedPigCol = -1;
        _selectedPigRow = -1;
        UpdateSimulateFromLanes();
        SpawnPigUI();
    }

    // Adjacency rules:
    //  - Cột khác nhau (|dc|==1): bất kỳ hàng nào đều được
    //  - Cùng cột (dc==0): chỉ được nối hàng cạnh nhau (|dr|==1)
    private bool ArePigsAdjacent(int col1, int row1, int col2, int row2)
    {
        int dc = Mathf.Abs(col1 - col2);
        int dr = Mathf.Abs(row1 - row2);
        return dc == 1 || (dc == 0 && dr == 1);
    }

    private void ClearPigLinkData(PigLayoutData p)
    {
        p.linkId = -1;
        p.pigLeft = null; p.pigRight = null;
    }

    private int GetPigConnectionCount(PigLayoutData p)
    {
        int n = 0;
        if (p.pigLeft != null) n++;
        if (p.pigRight != null) n++;
        return n;
    }

    // Xóa slot link trỏ đến (otherCol, otherRow)
    private void RemoveLinkBetween(PigLayoutData p, int otherCol, int otherRow)
    {
        if (p.pigLeft != null && p.pigLeft.LaneIndex == otherCol && p.pigLeft.index == otherRow) p.pigLeft = null;
        else if (p.pigRight != null && p.pigRight.LaneIndex == otherCol && p.pigRight.index == otherRow) p.pigRight = null;
    }

    // Ghi vào slot rỗng đầu tiên
    private void AddLink(PigLayoutData p, int toCol, int toRow)
    {
        if (p.pigLeft == null) p.pigLeft = new PigMarker { LaneIndex = toCol, index = toRow };
        else p.pigRight = new PigMarker { LaneIndex = toCol, index = toRow };
    }

    // Kiểm tra pig p (ở myCol,myRow) đã có kết nối về phía (toCol,toRow) chưa
    private bool HasConnectionInDirection(PigLayoutData p, int myCol, int myRow, int toCol, int toRow)
    {
        int dc = toCol - myCol, dr = toRow - myRow;
        if (p.pigLeft != null && SameDirection(dc, dr, p.pigLeft.LaneIndex - myCol, p.pigLeft.index - myRow)) return true;
        if (p.pigRight != null && SameDirection(dc, dr, p.pigRight.LaneIndex - myCol, p.pigRight.index - myRow)) return true;
        return false;
    }

    private bool SameDirection(int dc1, int dr1, int dc2, int dr2)
    {
        if (dc1 != 0 && dc2 != 0) return System.Math.Sign(dc1) == System.Math.Sign(dc2);
        if (dc1 == 0 && dc2 == 0) return System.Math.Sign(dr1) == System.Math.Sign(dr2);
        return false;
    }

    private void HandleLinkPigClick(int col, int row)
    {
        if (_multiColumnPigs == null) return;
        var pig = _multiColumnPigs[col][row];

        // 1. Clicking a pig already in the current building chain → remove it
        int existingIdx = _linkingPigs.FindIndex(p => p.col == col && p.row == row);
        if (existingIdx >= 0)
        {
            if (existingIdx > 0)
            {
                var prev = _linkingPigs[existingIdx - 1];
                RemoveLinkBetween(_multiColumnPigs[prev.col][prev.row], col, row);
            }
            if (existingIdx < _linkingPigs.Count - 1)
            {
                var next = _linkingPigs[existingIdx + 1];
                RemoveLinkBetween(_multiColumnPigs[next.col][next.row], col, row);
            }
            ClearPigLinkData(pig);
            _linkingPigs.RemoveAt(existingIdx);
            if (_linkingPigs.Count == 1)
            {
                ClearPigLinkData(_multiColumnPigs[_linkingPigs[0].col][_linkingPigs[0].row]);
                _linkingPigs.Clear();
            }
            SpawnPigUI();
            UpdateReport(_linkingPigs.Count == 0 ? "Link cleared." : $"Linking: {_linkingPigs.Count} pig(s).");
            return;
        }

        // 2. Chain rỗng + click pig đã linked → unlink toàn bộ group
        if (_linkingPigs.Count == 0 && pig.linkId >= 0)
        {
            int oldId = pig.linkId;
            foreach (var lane in _multiColumnPigs)
                foreach (var p in lane)
                    if (p.linkId == oldId) ClearPigLinkData(p);
            SpawnPigUI();
            UpdateSimulateFromLanes();
            return;
        }

        // 3. Giới hạn tối đa 5
        if (_linkingPigs.Count >= 5)
        {
            UpdateReport("Max 5 pigs per link group!");
            return;
        }

        // 4. Kiểm tra kề nhau
        if (_linkingPigs.Count > 0)
        {
            var last = _linkingPigs[_linkingPigs.Count - 1];
            if (!ArePigsAdjacent(last.col, last.row, col, row))
            {
                UpdateReport($"Không thể nối: pig ({col},{row}) không kề với pig cuối ({last.col},{last.row}).");
                return;
            }

            var lastPig = _multiColumnPigs[last.col][last.row];

            // 5a. Mỗi con heo chỉ được nối tối đa 2 con
            if (GetPigConnectionCount(lastPig) >= 2)
            {
                UpdateReport($"Pig ({last.col},{last.row}) đã có đủ 2 kết nối!");
                return;
            }
            if (GetPigConnectionCount(pig) >= 2)
            {
                UpdateReport($"Pig ({col},{row}) đã có đủ 2 kết nối!");
                return;
            }

            // 5b. Kiểm tra hướng cụ thể còn trống
            if (HasConnectionInDirection(lastPig, last.col, last.row, col, row))
            {
                UpdateReport($"Pig ({last.col},{last.row}) đã có kết nối theo hướng đó!");
                return;
            }
            if (HasConnectionInDirection(pig, col, row, last.col, last.row))
            {
                UpdateReport($"Pig ({col},{row}) đã có kết nối theo hướng đó!");
                return;
            }
        }

        // 6. Xác định linkId — nếu pig mới thuộc group khác, merge chain hiện tại vào group đó
        int assignLinkId;
        if (_linkingPigs.Count == 0)
        {
            assignLinkId = _nextLinkId++;
        }
        else
        {
            assignLinkId = _multiColumnPigs[_linkingPigs[0].col][_linkingPigs[0].row].linkId;
            if (pig.linkId >= 0 && pig.linkId != assignLinkId)
            {
                // Merge: tất cả pig trong chain hiện tại nhận linkId của pig đích
                int targetId = pig.linkId;
                foreach (var lp in _linkingPigs)
                    _multiColumnPigs[lp.col][lp.row].linkId = targetId;
                assignLinkId = targetId;
            }
        }
        pig.linkId = assignLinkId;

        // 7. Ghi link vào slot rỗng của cả 2
        if (_linkingPigs.Count > 0)
        {
            var last = _linkingPigs[_linkingPigs.Count - 1];
            AddLink(_multiColumnPigs[last.col][last.row], col, row);
            AddLink(pig, last.col, last.row);
        }

        _linkingPigs.Add((col, row));
        SpawnPigUI();
        if (_linkingPigs.Count >= 2)
            UpdateSimulateFromLanes();
        else
            UpdateReport($"Linking: 1 pig selected (col {col} row {row}) — click pig kế tiếp để nối.");
    }

    private void OnPigClicked(int col, int row)
    {
        if (_activePigFeature == "Linked")
        {
            HandleLinkPigClick(col, row);
            return;
        }

        if (_activePigFeature == "Hidden")
        {
            _multiColumnPigs[col][row].isHidden = !_multiColumnPigs[col][row].isHidden;
            SpawnPigUI();
            return;
        }

        // Swap mode
        if (_selectedPigCol == -1)
        {
            _selectedPigCol = col;
            _selectedPigRow = row;
            SpawnPigUI();
        }
        else if (_selectedPigCol == col && _selectedPigRow == row)
        {
            _selectedPigCol = -1;
            _selectedPigRow = -1;
            SpawnPigUI();
        }
        else
        {
            var temp = _multiColumnPigs[_selectedPigCol][_selectedPigRow];
            _multiColumnPigs[_selectedPigCol][_selectedPigRow] = _multiColumnPigs[col][row];
            _multiColumnPigs[col][row] = temp;
            _selectedPigCol = -1;
            _selectedPigRow = -1;
            UpdateSimulateFromLanes();
            SpawnPigUI();
        }
    }

    private void UpdateSimulateFromLanes()
    {
        if (_multiColumnPigs == null) return;

        var hpSim = BuildHpSimStates();
        int newSteps = RunSimulationWithLinks(_multiColumnPigs, PrepareSimGrid(hpSim), hpSim);

        // Rebuild flat deck for export compatibility
        List<string> flatColors = new List<string>();
        int maxRows = _multiColumnPigs.Max(c => c.Count);
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < _queueColumns; c++)
                if (r < _multiColumnPigs[c].Count)
                    flatColors.Add(_multiColumnPigs[c][r].colorName);
        _lastPigResult.finalDeck = flatColors;
        _lastPigResult.actualSteps = newSteps;

        int linkGroups = _multiColumnPigs.SelectMany(c => c)
            .Where(p => p.linkId >= 0).Select(p => p.linkId).Distinct().Count();
        string msg = newSteps == -1
            ? "<color=red>Unwinnable!</color>"
            : $"Steps: {newSteps} (target: {_targetStepsInput}){(newSteps < _targetStepsInput ? " <color=yellow>↓ under</color>" : newSteps > _targetStepsInput ? " <color=orange>↑ over</color>" : " <color=green>✓</color>")}";
        if (linkGroups > 0) msg += $" | {linkGroups} link group(s)";
        UpdateReport(msg);
    }

    // --- CLEAR / RESET ---
    public void OnClickClear()
    {
        _tempGrid = new string[TempGridSize, TempGridSize];
        for (int cy = 0; cy < TempGridSize; cy++)
            for (int cx = 0; cx < TempGridSize; cx++)
                _tempGrid[cx, cy] = "empty";

        _finalGridMap = null;
        _finalWidth = 0;
        _finalHeight = 0;
        _finalOffsetX = 0;
        _finalOffsetY = 0;
        _finalColorCounts.Clear();

        _hardPixelEntries.Clear();
        _hardPixelSelectedCells.Clear();
        _hardPixelColor = null;

        _multiColumnPigs = new List<PigLayoutData>[_queueColumns];
        for (int i = 0; i < _queueColumns; i++) _multiColumnPigs[i] = new List<PigLayoutData>();

        _undoHistory.Clear();
        _undoSnapshots.Clear();
        _linkingPigs.Clear();
        _nextLinkId = 0;
        _selectedPigCol = -1;
        _selectedPigRow = -1;
        _lastPigResult = default;

        _currentMode = FeatureMode.Paint;
        _recolorSourceColor = null;
        NotifyFeatureSelected(-1);

        GenerateGridUI();
        SpawnPigUI();
        UpdateReport("Cleared — ready for new level.");
    }

    // --- 4. EXPORT JSON ---
    public void OnClickSaveJSON()
    {
        SimpleFileBrowser.FileBrowser.ShowSaveDialog((paths) =>
        {
            LevelData data = new LevelData
            {
                width = _finalWidth,
                height = _finalHeight,
                targetDifficulty = _targetStepsInput
            };
            for (int y = 0; y < _finalHeight; y++)
                for (int x = 0; x < _finalWidth; x++)
                    data.gridData.Add(_finalGridMap[x, y]);

            foreach (var hp in _hardPixelEntries)
                data.hardPixels.Add(new HardPixelData
                {
                    xPos = hp.xPos - _finalOffsetX,
                    yPos = hp.yPos - _finalOffsetY,
                    sizeX = hp.sizeX,
                    sizeY = hp.sizeY,
                    colorName = hp.colorName,
                    bulletCount = hp.bulletCount
                });

            if (_multiColumnPigs != null)
                foreach (var col in _multiColumnPigs) data.lanes.Add(new LaneConfig { pigs = new List<PigLayoutData>(col) });

            File.WriteAllText(paths[0], JsonUtility.ToJson(data, true));
            UpdateReport("Saved Success!");
        }, null, SimpleFileBrowser.FileBrowser.PickMode.Files, false, null, $"Save Level {levelIndex}", "Save");
    }

    // --- MATH & HELPERS ---
    private (List<string> finalDeck, List<PigDataPool> finalPool, int actualSteps) RunAdaptiveSimulation()
    {
        List<PigDataPool> pool = new List<PigDataPool>();
        foreach (var item in _finalColorCounts)
        {
            int total = item.Value;
            int perPig = (total >= 100) ? 40 : 20;
            int count = Mathf.Max(1, total / perPig);
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                int bullets = (i == count - 1) ? total - sum : perPig;
                if (bullets > 0) { pool.Add(new PigDataPool { color = item.Key, bullets = bullets }); sum += bullets; }
            }
        }
        _pigsBeforeAdjust = pool.Count;
        int target = Mathf.Max(_targetStepsInput, pool.Select(p => p.color).Distinct().Count());
        List<string> finalDeck = null;
        int finalSteps = -1;
        List<string> bestDeck = null;
        List<PigDataPool> bestPool = null;
        int bestSteps = -1;
        int limit = 50;
        while (limit-- > 0)
        {
            if (pool.Count > target) { MergeTwoPigs(pool); continue; }
            bool found = false;
            int best = -1, worst = 1000;
            for (int i = 0; i < 300; i++)
            {
                var sim = ExecuteSimulation(pool.Select(p => p.color).ToList(), pool);
                if (sim.steps == target) { finalSteps = sim.steps; finalDeck = sim.deck; found = true; break; }
                if (sim.steps != -1)
                {
                    best = Mathf.Max(best, sim.steps); worst = Mathf.Min(worst, sim.steps);
                    if (bestDeck == null || Mathf.Abs(sim.steps - target) < Mathf.Abs(bestSteps - target))
                    {
                        bestDeck = sim.deck; bestSteps = sim.steps;
                        // Snapshot pool state matching this deck
                        bestPool = pool.Select(p => new PigDataPool { color = p.color, bullets = p.bullets }).ToList();
                    }
                }
            }
            if (found) break;
            if (best < target) SplitOnePig(pool); else if (worst > target) MergeTwoPigs(pool);
        }
        if (finalDeck == null && bestDeck != null) { finalDeck = bestDeck; finalSteps = bestSteps; pool = bestPool; }
        return (finalDeck, pool, finalSteps);
    }

    private (List<string> deck, int steps) ExecuteSimulation(List<string> poolNames, List<PigDataPool> pool)
    {
        var deck = poolNames.OrderBy(x => Random.value).ToList();
        var tempB = pool.GroupBy(p => p.color).ToDictionary(g => g.Key, g => g.Select(p => p.bullets).OrderByDescending(b => b).ToList());
        List<int> bullets = deck.Select(c => { int b = tempB[c][0]; tempB[c].RemoveAt(0); return b; }).ToList();
        var hpStates = BuildHpSimStates();
        return (deck, RunFullSimulationEnhanced(deck, bullets, PrepareSimGrid(hpStates), hpStates));
    }

    private void MergeTwoPigs(List<PigDataPool> pool)
    {
        var group = pool.GroupBy(p => p.color).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (group != null) { var items = group.OrderBy(p => p.bullets).ToList(); items[1].bullets += items[0].bullets; pool.Remove(items[0]); }
    }

    // Sau EndLink: simulate lại, nếu steps != target thì tự động split/merge
    // các pig KHÔNG linked để cố chỉnh về đúng target steps.
    private void TryAutoFitLinkedPigs()
    {
        if (_multiColumnPigs == null || _finalGridMap == null) { SpawnPigUI(); return; }

        var hpSim = BuildHpSimStates();
        int currentSteps = RunSimulationWithLinks(_multiColumnPigs, PrepareSimGrid(hpSim), hpSim);
        int maxIter = 40;

        while (currentSteps != _targetStepsInput && maxIter-- > 0)
        {
            if (currentSteps == -1 || currentSteps < _targetStepsInput)
            {
                // Cần THÊM step → SPLIT pig không linked có bullets lớn nhất
                PigLayoutData best = null; int bestCol = -1, bestRow = -1;
                for (int c = 0; c < _queueColumns; c++)
                    for (int r = 0; r < _multiColumnPigs[c].Count; r++)
                    {
                        var p = _multiColumnPigs[c][r];
                        if (p.linkId >= 0 || p.bullets < 2) continue;
                        if (best == null || p.bullets > best.bullets) { best = p; bestCol = c; bestRow = r; }
                    }
                if (best == null) break;
                int b1 = best.bullets / 2, b2 = best.bullets - b1;
                best.bullets = b1;
                _multiColumnPigs[bestCol].Insert(bestRow + 1,
                    new PigLayoutData { colorName = best.colorName, bullets = b2, linkId = -1 });
            }
            else
            {
                // Cần ÍT step hơn → MERGE 2 pig không linked cùng màu
                bool merged = false;
                for (int c = 0; c < _queueColumns && !merged; c++)
                    for (int r = 0; r < _multiColumnPigs[c].Count && !merged; r++)
                    {
                        var p = _multiColumnPigs[c][r];
                        if (p.linkId >= 0) continue;
                        for (int c2 = c; c2 < _queueColumns && !merged; c2++)
                        {
                            int startR = (c2 == c) ? r + 1 : 0;
                            for (int r2 = startR; r2 < _multiColumnPigs[c2].Count && !merged; r2++)
                            {
                                var p2 = _multiColumnPigs[c2][r2];
                                if (p2.linkId >= 0 || p2.colorName != p.colorName) continue;
                                p.bullets += p2.bullets;
                                _multiColumnPigs[c2].RemoveAt(r2);
                                merged = true;
                            }
                        }
                    }
                if (!merged) break;
            }
            hpSim = BuildHpSimStates();
            currentSteps = RunSimulationWithLinks(_multiColumnPigs, PrepareSimGrid(hpSim), hpSim);
        }

        // Cập nhật _lastPigResult để export vẫn đúng
        var flatColors = new List<string>();
        int maxRows = _multiColumnPigs.Max(c => c.Count);
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < _queueColumns; c++)
                if (r < _multiColumnPigs[c].Count) flatColors.Add(_multiColumnPigs[c][r].colorName);
        _lastPigResult.finalDeck = flatColors;
        _lastPigResult.actualSteps = currentSteps;

        int linkGroups = _multiColumnPigs.SelectMany(col => col)
            .Where(p => p.linkId >= 0).Select(p => p.linkId).Distinct().Count();
        string stepStr = currentSteps == -1
            ? "<color=red>Unwinnable sau khi auto-adjust!</color>"
            : $"Steps: {currentSteps} (target: {_targetStepsInput})"
              + (currentSteps == _targetStepsInput ? " <color=green>✓</color>"
                : currentSteps < _targetStepsInput ? " <color=yellow>↓ under</color>"
                : " <color=orange>↑ over</color>");
        UpdateReport($"Link ended. {stepStr}{(linkGroups > 0 ? $" | {linkGroups} link group(s)" : "")}");
        SpawnPigUI();
    }

    private void SplitOnePig(List<PigDataPool> pool)
    {
        var pigTarget = pool.Where(p => p.bullets > 1).OrderByDescending(p => p.bullets).FirstOrDefault();
        if (pigTarget != null)
        {
            int b1 = pigTarget.bullets / 2;
            int b2 = pigTarget.bullets - b1;
            pigTarget.bullets = b1;
            pool.Add(new PigDataPool { color = pigTarget.color, bullets = b2 });
        }
    }

    // Link-aware simulation: linked group deploys as 1 step when ALL members are at
    // column-front or in queue; group fires combined bullets; group only consumed together.
    private int RunSimulationWithLinks(List<PigLayoutData>[] lanes, string[,] grid, List<HpSimState> hpStates = null)
    {
        var cols = new List<(string color, int bullets, int linkId)>[_queueColumns];
        for (int i = 0; i < _queueColumns; i++)
        {
            cols[i] = new List<(string, int, int)>();
            if (i < lanes.Length)
                foreach (var p in lanes[i])
                    cols[i].Add((p.colorName, p.bullets, p.linkId));
        }
        var queue = new List<(string color, int bullets, int linkId)>();
        int steps = 0;
        int failsafe = 0;

        while ((cols.Any(c => c.Count > 0) || queue.Count > 0) && ++failsafe < 2000)
        {
            bool moved = false;

            // 1. Queue: non-linked exposed pig
            for (int i = 0; i < queue.Count && !moved; i++)
            {
                if (queue[i].linkId < 0 && IsExposed(queue[i].color, grid))
                { steps++; ClearGridSim(queue[i].color, queue[i].bullets, grid, hpStates); queue.RemoveAt(i); moved = true; }
            }

            // 2. Column front: non-linked exposed pig
            for (int i = 0; i < _queueColumns && !moved; i++)
            {
                if (cols[i].Count > 0 && cols[i][0].linkId < 0 && IsExposed(cols[i][0].color, grid))
                { steps++; ClearGridSim(cols[i][0].color, cols[i][0].bullets, grid, hpStates); cols[i].RemoveAt(0); moved = true; }
            }

            if (moved) continue;

            // 3. Linked groups: deploy if ALL members are at column-front OR in queue
            var allLinkIds = cols.SelectMany(c => c).Concat(queue)
                .Where(p => p.linkId >= 0).Select(p => p.linkId).Distinct().ToList();

            foreach (int lid in allLinkIds)
            {
                // Every column that contains a pig with this lid must have it at index 0
                bool ready = true;
                for (int i = 0; i < _queueColumns && ready; i++)
                    if (cols[i].Any(p => p.linkId == lid) && cols[i][0].linkId != lid)
                        ready = false;
                if (!ready) continue;

                var fromCols = new List<int>();
                for (int i = 0; i < _queueColumns; i++)
                    if (cols[i].Count > 0 && cols[i][0].linkId == lid) fromCols.Add(i);
                var fromQ = queue.Where(p => p.linkId == lid).ToList();
                var allMembers = fromCols.Select(i => cols[i][0]).Concat(fromQ).ToList();

                if (!allMembers.Any(p => IsExposed(p.color, grid))) continue;

                steps++;
                foreach (var mp in allMembers) ClearGridSim(mp.color, mp.bullets, grid, hpStates);
                foreach (int ci in fromCols) cols[ci].RemoveAt(0);
                foreach (var qp in fromQ) queue.Remove(qp);
                moved = true;
                break;
            }
            if (moved) continue;

            // 4. Force a pig to queue
            for (int i = 0; i < _queueColumns && !moved; i++)
            {
                if (cols[i].Count > 0 && queue.Count < MaxQueue1)
                { steps++; queue.Add(cols[i][0]); cols[i].RemoveAt(0); moved = true; }
            }
            if (!moved) break;
        }

        return (cols.Any(c => c.Count > 0) || queue.Count > 0) ? -1 : steps;
    }

    private int RunFullSimulationEnhanced(List<string> playDeck, List<int> pigBullets, string[,] grid, List<HpSimState> hpStates = null)
    {
        int steps = 0; List<string> q1C = new List<string>(); List<int> q1B = new List<int>();
        List<string>[] colsC = new List<string>[_queueColumns]; List<int>[] colsB = new List<int>[_queueColumns];
        for (int i = 0; i < _queueColumns; i++) { colsC[i] = new List<string>(); colsB[i] = new List<int>(); }
        for (int i = 0; i < playDeck.Count; i++) { colsC[i % _queueColumns].Add(playDeck[i]); colsB[i % _queueColumns].Add(pigBullets[i]); }
        int failsafe = 0;
        while ((colsC.Any(c => c.Count > 0) || q1C.Count > 0) && failsafe++ < 1000)
        {
            bool moved = false;
            for (int i = 0; i < q1C.Count; i++) if (IsExposed(q1C[i], grid)) { steps++; ClearGridSim(q1C[i], q1B[i], grid, hpStates); q1C.RemoveAt(i); q1B.RemoveAt(i); moved = true; break; }
            if (moved) continue;
            for (int i = 0; i < _queueColumns; i++) if (colsC[i].Count > 0 && IsExposed(colsC[i][0], grid)) { steps++; ClearGridSim(colsC[i][0], colsB[i][0], grid, hpStates); colsC[i].RemoveAt(0); colsB[i].RemoveAt(0); moved = true; break; }
            if (!moved)
            {
                for (int i = 0; i < _queueColumns; i++) if (colsC[i].Count > 0 && q1C.Count < MaxQueue1) { steps++; q1C.Add(colsC[i][0]); q1B.Add(colsB[i][0]); colsC[i].RemoveAt(0); colsB[i].RemoveAt(0); moved = true; break; }
            }
            if (!moved) break;
        }
        return (colsC.Any(c => c.Count > 0) || q1C.Count > 0) ? -1 : steps;
    }

    private bool IsExposed(string color, string[,] grid)
    {
        for (int i = 0; i < _finalHeight; i++)
        {
            for (int j = 0; j < _finalWidth; j++) { if (string.IsNullOrEmpty(grid[j, i]) || grid[j, i] == "empty" || grid[j, i] == "scan_empty") continue; if (grid[j, i] == color) return true; break; }
            for (int j = _finalWidth - 1; j >= 0; j--) { if (string.IsNullOrEmpty(grid[j, i]) || grid[j, i] == "empty" || grid[j, i] == "scan_empty") continue; if (grid[j, i] == color) return true; break; }
        }
        return false;
    }

    private void ClearGridSim(string color, int amount, string[,] grid, List<HpSimState> hpStates = null)
    {
        if (hpStates != null)
        {
            foreach (var hp in hpStates)
            {
                if (hp.colorName != color || hp.cleared) continue;
                bool present = false;
                for (int hy = hp.y0; hy < hp.y0 + hp.sizeY && !present; hy++)
                    for (int hx = hp.x0; hx < hp.x0 + hp.sizeX && !present; hx++)
                        if (hx >= 0 && hx < _finalWidth && hy >= 0 && hy < _finalHeight && grid[hx, hy] == color)
                            present = true;
                if (!present) continue;
                hp.health -= amount;
                if (hp.health <= 0)
                {
                    hp.cleared = true;
                    for (int hy = hp.y0; hy < hp.y0 + hp.sizeY; hy++)
                        for (int hx = hp.x0; hx < hp.x0 + hp.sizeX; hx++)
                            if (hx >= 0 && hx < _finalWidth && hy >= 0 && hy < _finalHeight)
                                grid[hx, hy] = "empty";
                }
                return; // one pig fires at one HP block per shot
            }
        }
        int cleared = 0;
        for (int i = 0; i < _finalHeight && cleared < amount; i++)
            for (int j = 0; j < _finalWidth && cleared < amount; j++)
                if (grid[j, i] == color) { grid[j, i] = "empty"; cleared++; }
    }

    // ─── HARDPIXEL SIMULATION HELPERS ──────────────────────────────────────────
    private class HpSimState
    {
        public string colorName;
        public int x0, y0, sizeX, sizeY; // in _finalGridMap coordinates
        public int health;
        public bool cleared;
    }

    private List<HpSimState> BuildHpSimStates()
    {
        var list = new List<HpSimState>();
        foreach (var hp in _hardPixelEntries)
            list.Add(new HpSimState
            {
                colorName = hp.colorName,
                x0 = hp.xPos - _finalOffsetX,
                y0 = hp.yPos - _finalOffsetY,
                sizeX = hp.sizeX,
                sizeY = hp.sizeY,
                health = hp.bulletCount
            });
        return list;
    }

    // Returns a cloned finalGridMap with HP-covered cells tagged with their HP colorName.
    private string[,] PrepareSimGrid(List<HpSimState> hpStates)
    {
        var grid = (string[,])_finalGridMap.Clone();
        if (hpStates == null) return grid;
        foreach (var hp in hpStates)
            for (int hy = hp.y0; hy < hp.y0 + hp.sizeY; hy++)
                for (int hx = hp.x0; hx < hp.x0 + hp.sizeX; hx++)
                    if (hx >= 0 && hx < _finalWidth && hy >= 0 && hy < _finalHeight)
                        grid[hx, hy] = hp.colorName;
        return grid;
    }



    private void RecordUndo()
    {
        if (_tempGrid == null) return;

        // Deep-copy hardPixelEntries
        var hpCopy = new List<GridCell.CellData>();
        foreach (var hp in _hardPixelEntries)
            hpCopy.Add(new GridCell.CellData { xPos = hp.xPos, yPos = hp.yPos, sizeX = hp.sizeX, sizeY = hp.sizeY, colorName = hp.colorName, bulletCount = hp.bulletCount });

        // Deep-copy pig columns
        List<PigLayoutData>[] pigCopy = null;
        if (_multiColumnPigs != null)
        {
            pigCopy = new List<PigLayoutData>[_multiColumnPigs.Length];
            for (int i = 0; i < _multiColumnPigs.Length; i++)
            {
                pigCopy[i] = new List<PigLayoutData>();
                foreach (var p in _multiColumnPigs[i])
                    pigCopy[i].Add(new PigLayoutData
                    {
                        colorName = p.colorName, bullets = p.bullets, isHidden = p.isHidden,
                        linkId = p.linkId,
                        pigLeft  = p.pigLeft  != null ? new PigMarker { LaneIndex = p.pigLeft.LaneIndex,  index = p.pigLeft.index  } : null,
                        pigRight = p.pigRight != null ? new PigMarker { LaneIndex = p.pigRight.LaneIndex, index = p.pigRight.index } : null
                    });
            }
        }

        _undoSnapshots.Add(new UndoSnapshot
        {
            tempGrid        = (string[,])_tempGrid.Clone(),
            hardPixelEntries = hpCopy,
            multiColumnPigs = pigCopy,
            queueColumns    = _queueColumns
        });
        if (_undoSnapshots.Count > MaxUndoSteps) _undoSnapshots.RemoveAt(0);
    }

    public void PerformUndo()
    {
        if (_undoSnapshots.Count == 0)
        {
            UpdateReport("Nothing to undo.");
            return;
        }
        var snap = _undoSnapshots[_undoSnapshots.Count - 1];
        _undoSnapshots.RemoveAt(_undoSnapshots.Count - 1);

        _tempGrid = snap.tempGrid;
        _hardPixelEntries = snap.hardPixelEntries;
        _hardPixelSelectedCells.Clear();

        if (snap.multiColumnPigs != null)
        {
            _queueColumns = snap.queueColumns;
            _multiColumnPigs = snap.multiColumnPigs;
        }

        _currentMode = FeatureMode.Paint;
        _linkingPigs.Clear();
        _selectedPigCol = -1;
        _selectedPigRow = -1;

        ComputeFinalGrid();
        GenerateGridUI();
        ActionShuffleAndSimulate();
        UpdateReport($"Undo — {_undoSnapshots.Count} step(s) remaining.");
    }

    private void UpdateReport(string msg) { if (reportTextDisplay != null) reportTextDisplay.text = msg; }
}