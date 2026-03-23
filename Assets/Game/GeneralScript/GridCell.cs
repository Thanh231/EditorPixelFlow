using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    private int _x;
    private int _y;
    private LevelEditor _manager;
    private Image _myImage;
    public Sprite defaultSprite { get; private set; }

    public void Setup(int x, int y, LevelEditor manager, int bulletCount)
    {
        _x = x;
        _y = y;
        _manager = manager;
        _myImage = GetComponent<Image>();
        if (_myImage != null) defaultSprite = _myImage.sprite;
    }
    
    // Khi nhấn chuột xuống ô này
    public void OnPointerDown(PointerEventData eventData)
    {
        _manager.PaintCell(_x, _y, _myImage);
    }
    
    // Khi giữ chuột và di ngang qua (Tô màu như Paint)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Nếu chuột trái đang được nhấn giữ
        if (Input.GetMouseButton(0)) 
        {
            _manager.PaintCellDrag(_x, _y, _myImage);
        }
    }
}