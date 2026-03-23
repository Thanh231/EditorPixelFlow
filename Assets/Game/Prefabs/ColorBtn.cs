using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Bắt buộc phải có để bắt sự kiện chuột

public class ColorBtn : MonoBehaviour, IPointerDownHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private string color;
    public Image sprite;
    
    private LevelEditor levelEditor;
    public void SetColor(string colorName, LevelEditor editor)
    {
        color = colorName;
        levelEditor = editor;
        if (sprite != null)
        {
            if (colorName == "empty")
            {
                sprite.sprite = editor.emptySprite;
                sprite.color = Color.white;
            }
            else
            {
                sprite.sprite = null;
                sprite.color = Helper.GetColorFromName(colorName); // Hiển thị màu trên nút
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // Xử lý sự kiện khi nút được nhấn
        if (levelEditor != null)
        {
            levelEditor.SetActiveBrush(color);
        }
    }
}
