using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;


public class LevelEditor : MonoBehaviour
{
    // public class PigDataPool {
    //     public string color;
    //     public int bullets;
    //     public bool isUsed;
    // }
    //
    // [Header("UI References")]
    // public RawImage textureDisplay; 
    // public Transform gridContainer; // Đối tượng Content của ScrollView
    // public GameObject cellPrefab;   // Prefab có gắn script GridCell
    // public TMP_InputField widthInput;
    // public TMP_InputField stepsInput;
    // public TMP_Text reportTextDisplay;
    //
    // [Header("Settings")]
    // [SerializeField] private int _queueColumns = 3;
    // private int _targetWidth = 20;
    // private int _targetStepsInput = 12;
    // private const int MaxQueue1 = 5;
    //
    // [Header("Internal Data")]
    // private Texture2D _textureInput;
    // private string[,] _finalGridMap;
    // private int _finalWidth, _finalHeight;
    // private Dictionary<string, int> _finalColorCounts = new Dictionary<string, int>();
    // private List<PigLayoutData>[] _multiColumnPigs;
    // private string _activeColorBrush = "red"; 
    // private List<string[,]> _undoHistory = new List<string[,]>();
    // private (List<string> finalDeck, List<PigDataPool> finalPool, int actualSteps) _lastPigResult;
    //
    // private int MaxUndoSteps = 10;
    //
    // private readonly string[] ALL_COLORS = { 
    //     "red", "green", "blue", "yellow", "black", 
    //     "white", "pink", "dark pink", "orange", "dark green", "dark blue", "empty" 
    // };
    //
    // // --- 1. CHỌN VÀ LOAD ẢNH ---
    // public void OnClickOpenImage() {
    //     // Cách viết chuẩn cho phiên bản mới nhất của Simple File Browser
    //     FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".png", ".jpg", ".jpeg"));
    //     FileBrowser.SetDefaultFilter(".png");
    //
    //     FileBrowser.ShowLoadDialog((paths) => {
    //             StartCoroutine(LoadImageRoutine(paths[0]));
    //         }, 
    //         null, 
    //         FileBrowser.PickMode.Files, 
    //         false, 
    //         null, 
    //         "Select Map Image", 
    //         "Load");
    // }
    //
    // private IEnumerator LoadImageRoutine(string path) {
    //     using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + path)) {
    //         yield return www.SendWebRequest();
    //         if (www.result == UnityWebRequest.Result.Success) {
    //             _textureInput = DownloadHandlerTexture.GetContent(www);
    //             textureDisplay.texture = _textureInput;
    //             ProcessScan();
    //         }
    //     }
    // }
    //
    // // --- 2. XỬ LÝ SCAN VÀ TẠO GRID ---
    // public void ProcessScan() {
    //     if (_textureInput == null) {
    //         UpdateReport("<color=red>Error:</color> No image loaded!");
    //         return;
    //     }
    //
    //     // CẬP NHẬT GIÁ TRỊ TỪ UI NGAY LẬP TỨC
    //     if (widthInput != null && !string.IsNullOrEmpty(widthInput.text)) {
    //         if (int.TryParse(widthInput.text, out int w)) {
    //             _targetWidth = Mathf.Clamp(w, 5, 100); // Giới hạn từ 5 đến 100 ô để tránh treo máy
    //         }
    //     }
    //
    //     if (stepsInput != null && !string.IsNullOrEmpty(stepsInput.text)) {
    //         if (int.TryParse(stepsInput.text, out int s)) {
    //             _targetStepsInput = s;
    //         }
    //     }
    //
    //     // TÍNH TOÁN LẠI TỈ LỆ CHUẨN
    //     _finalWidth = _targetWidth;
    //     float aspect = (float)_textureInput.width / _textureInput.height;
    //     _finalHeight = Mathf.RoundToInt(_finalWidth / aspect);
    //
    //     // Khởi tạo lại mảng dữ liệu với kích thước mới
    //     _finalGridMap = new string[_finalWidth, _finalHeight];
    //
    //     // Tính toán bước nhảy pixel
    //     float stepX = (float)_textureInput.width / _finalWidth;
    //     float stepY = (float)_textureInput.height / _finalHeight;
    //
    //     for (int y = 0; y < _finalHeight; y++) {
    //         for (int x = 0; x < _finalWidth; x++) {
    //             // Lấy màu tại tâm của ô (offset +0.5f) để chính xác hơn
    //             int px = Mathf.FloorToInt(x * stepX + stepX * 0.5f);
    //             int py = Mathf.FloorToInt(y * stepY + stepY * 0.5f);
    //         
    //             Color col = _textureInput.GetPixel(px, py);
    //             _finalGridMap[x, y] = (col.a < 0.1f) ? "empty" : GetClosestColorName(col);
    //         }
    //     }
    //
    //     GenerateGridUI();
    //     ActionShuffleAndSimulate();
    //     UpdateReport("Scan Success: " + _finalWidth + "x" + _finalHeight);
    // }
    //
    // private void GenerateGridUI() {
    //     foreach (Transform child in gridContainer) Destroy(child.gameObject);
    //     GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
    //     layout.constraintCount = _finalWidth; 
    //
    //     for (int y = _finalHeight - 1; y >= 0; y--) {
    //         for (int x = 0; x < _finalWidth; x++) {
    //             GameObject cellGO = Instantiate(cellPrefab, gridContainer);
    //             cellGO.GetComponent<Image>().color = GetColorFromName(_finalGridMap[x, y]);
    //             cellGO.GetComponent<GridCell>().Setup(x, y, this);
    //         }
    //     }
    // }
    //
    // // --- 3. TÔ MÀU & SIMULATION ---
    // public void SetActiveBrush(string colorName) => _activeColorBrush = colorName.ToLower();
    //
    // public void PaintCell(int x, int y, Image cellImage) {
    //     if (_finalGridMap == null || _finalGridMap[x, y] == _activeColorBrush) return;
    //     RecordUndo();
    //     _finalGridMap[x, y] = _activeColorBrush;
    //     cellImage.color = GetColorFromName(_activeColorBrush);
    //     ActionShuffleAndSimulate();
    // }
    //
    // private void ActionShuffleAndSimulate() {
    //     if (_finalGridMap == null) return;
    //     _finalColorCounts.Clear();
    //     for (int y = 0; y < _finalHeight; y++) {
    //         for (int x = 0; x < _finalWidth; x++) {
    //             string c = _finalGridMap[x, y];
    //             if (c != "empty" && !string.IsNullOrEmpty(c)) {
    //                 if (!_finalColorCounts.ContainsKey(c)) _finalColorCounts[c] = 0;
    //                 _finalColorCounts[c]++;
    //             }
    //         }
    //     }
    //     _lastPigResult = RunAdaptiveSimulation();
    //     _multiColumnPigs = new List<PigLayoutData>[_queueColumns];
    //     for (int i = 0; i < _queueColumns; i++) _multiColumnPigs[i] = new List<PigLayoutData>();
    //
    //     if (_lastPigResult.finalDeck != null) {
    //         var pool = _lastPigResult.finalPool;
    //         foreach (var p in pool) p.isUsed = false;
    //         for (int i = 0; i < _lastPigResult.finalDeck.Count; i++) {
    //             string color = _lastPigResult.finalDeck[i];
    //             var data = pool.FirstOrDefault(x => x.color == color && !x.isUsed);
    //             if (data != null) {
    //                 data.isUsed = true;
    //                 _multiColumnPigs[i % _queueColumns].Add(new PigLayoutData { colorName = data.color, bullets = data.bullets });
    //             }
    //         }
    //         UpdateReport($"Solvable: {_lastPigResult.actualSteps} steps");
    //     } else {
    //         UpdateReport("<color=red>Unwinnable!</color>");
    //     }
    // }
    //
    // // --- 4. EXPORT JSON ---
    // public void OnClickSaveJSON() {
    //     SimpleFileBrowser.FileBrowser.ShowSaveDialog((paths) => {
    //         LevelData data = new LevelData {
    //             width = _finalWidth, height = _finalHeight, targetDifficulty = _targetStepsInput
    //         };
    //         for (int y = 0; y < _finalHeight; y++)
    //             for (int x = 0; x < _finalWidth; x++) data.gridData.Add(_finalGridMap[x, y]);
    //
    //         if (_multiColumnPigs != null)
    //             foreach (var col in _multiColumnPigs) data.lanes.Add(new LaneData { pigs = new List<PigLayoutData>(col) });
    //
    //         File.WriteAllText(paths[0], JsonUtility.ToJson(data, true));
    //         UpdateReport("Saved Success!");
    //     }, null, SimpleFileBrowser.FileBrowser.PickMode.Files, false, null, "Save Level", "Save");
    // }
    //
    // // --- MATH & HELPERS (Logic Simulation giữ nguyên) ---
    // private (List<string> finalDeck, List<PigDataPool> finalPool, int actualSteps) RunAdaptiveSimulation() {
    //     List<PigDataPool> pool = new List<PigDataPool>();
    //     foreach (var item in _finalColorCounts) {
    //         int total = item.Value;
    //         int perPig = (total >= 100) ? 40 : 20;
    //         int count = Mathf.Max(1, total / perPig);
    //         int sum = 0;
    //         for (int i = 0; i < count; i++) {
    //             int bullets = (i == count - 1) ? total - sum : perPig;
    //             if (bullets > 0) { pool.Add(new PigDataPool { color = item.Key, bullets = bullets }); sum += bullets; }
    //         }
    //     }
    //     int target = Mathf.Max(_targetStepsInput, pool.Select(p => p.color).Distinct().Count());
    //     List<string> finalDeck = null; int finalSteps = -1; int limit = 50;
    //     while (limit-- > 0) {
    //         var deck = pool.Select(p => p.color).OrderBy(x => Random.value).ToList();
    //         var tempB = pool.GroupBy(p => p.color).ToDictionary(g => g.Key, g => g.Select(p => p.bullets).OrderByDescending(b => b).ToList());
    //         List<int> bullets = deck.Select(c => { int b = tempB[c][0]; tempB[c].RemoveAt(0); return b; }).ToList();
    //         int steps = RunFullSimulationEnhanced(deck, bullets, (string[,])_finalGridMap.Clone());
    //         if (steps != -1) { finalDeck = deck; finalSteps = steps; if (steps >= target) break; }
    //     }
    //     return (finalDeck, pool, finalSteps);
    // }
    //
    // private int RunFullSimulationEnhanced(List<string> playDeck, List<int> pigBullets, string[,] grid) {
    //     int steps = 0; List<string> q1C = new List<string>(); List<int> q1B = new List<int>();
    //     List<string>[] colsC = new List<string>[_queueColumns]; List<int>[] colsB = new List<int>[_queueColumns];
    //     for (int i = 0; i < _queueColumns; i++) { colsC[i] = new List<string>(); colsB[i] = new List<int>(); }
    //     for (int i = 0; i < playDeck.Count; i++) { colsC[i % _queueColumns].Add(playDeck[i]); colsB[i % _queueColumns].Add(pigBullets[i]); }
    //     int failsafe = 0;
    //     while ((colsC.Any(c => c.Count > 0) || q1C.Count > 0) && failsafe++ < 1000) {
    //         bool moved = false;
    //         for (int i = 0; i < q1C.Count; i++) if (IsExposed(q1C[i], grid)) { steps++; ClearGridSim(q1C[i], q1B[i], grid); q1C.RemoveAt(i); q1B.RemoveAt(i); moved = true; break; }
    //         if (moved) continue;
    //         for (int i = 0; i < _queueColumns; i++) if (colsC[i].Count > 0 && IsExposed(colsC[i][0], grid)) { steps++; ClearGridSim(colsC[i][0], colsB[i][0], grid); colsC[i].RemoveAt(0); colsB[i].RemoveAt(0); moved = true; break; }
    //         if (!moved) {
    //             for (int i = 0; i < _queueColumns; i++) if (colsC[i].Count > 0 && q1C.Count < MaxQueue1) { steps++; q1C.Add(colsC[i][0]); q1B.Add(colsB[i][0]); colsC[i].RemoveAt(0); colsB[i].RemoveAt(0); moved = true; break; }
    //         }
    //         if (!moved) break;
    //     }
    //     return (colsC.Any(c => c.Count > 0) || q1C.Count > 0) ? -1 : steps;
    // }
    //
    // private bool IsExposed(string color, string[,] grid) {
    //     for (int i = 0; i < _finalHeight; i++) {
    //         for (int j = 0; j < _finalWidth; j++) { if (string.IsNullOrEmpty(grid[j, i]) || grid[j, i] == "empty") continue; if (grid[j, i] == color) return true; break; }
    //         for (int j = _finalWidth - 1; j >= 0; j--) { if (string.IsNullOrEmpty(grid[j, i]) || grid[j, i] == "empty") continue; if (grid[j, i] == color) return true; break; }
    //     }
    //     return false;
    // }
    //
    // private void ClearGridSim(string color, int amount, string[,] grid) {
    //     int cleared = 0;
    //     for (int i = 0; i < _finalHeight && cleared < amount; i++) 
    //         for (int j = 0; j < _finalWidth && cleared < amount; j++) 
    //             if (grid[j, i] == color) { grid[j, i] = "empty"; cleared++; }
    // }
    //
    // private string GetClosestColorName(Color c) {
    //     float minDst = float.MaxValue; string best = "white";
    //     foreach (var name in ALL_COLORS) {
    //         if (name == "empty") continue;
    //         Color target = GetColorFromName(name);
    //         float dst = Mathf.Sqrt(Mathf.Pow(c.r - target.r, 2) + Mathf.Pow(c.g - target.g, 2) + Mathf.Pow(c.b - target.b, 2));
    //         if (dst < minDst) { minDst = dst; best = name; }
    //     }
    //     return best;
    // }
    //
    // private Color GetColorFromName(string name) {
    //     switch (name.ToLower()) {
    //         case "red": return Color.red; case "green": return Color.green; case "blue": return Color.blue;
    //         case "yellow": return Color.yellow; case "black": return Color.black; case "white": return Color.white;
    //         case "orange": return new Color(1, 0.5f, 0); case "pink": return new Color(1, 0.6f, 0.7f);
    //         case "dark pink": return new Color(1, 0.2f, 0.7f); case "dark green": return new Color(0, 0.5f, 0);
    //         case "dark blue": return new Color(0, 0, 0.5f);
    //         case "empty": return new Color(0.2f, 0.2f, 0.2f, 0.5f); default: return Color.gray;
    //     }
    // }
    //
    // private void RecordUndo() {
    //     if (_finalGridMap == null) return;
    //     _undoHistory.Add((string[,])_finalGridMap.Clone());
    //     if (_undoHistory.Count > MaxUndoSteps) _undoHistory.RemoveAt(0);
    // }
    //
    // public void PerformUndo() {
    //     if (_undoHistory.Count == 0) return;
    //     _finalGridMap = _undoHistory.Last();
    //     _undoHistory.RemoveAt(_undoHistory.Count - 1);
    //     GenerateGridUI(); ActionShuffleAndSimulate();
    // }
    //
    // private void UpdateReport(string msg) { if (reportTextDisplay != null) reportTextDisplay.text = msg; }
}