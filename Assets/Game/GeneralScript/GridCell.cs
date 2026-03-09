using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bắt buộc phải có để bắt sự kiện chuột

// public class GridCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
public class GridCell : MonoBehaviour
{
    // private int _x;
    // private int _y;
    // private LevelEditor _manager; // Đổi tên này cho khớp với Class chính của bạn
    // private Image _myImage;
    //
    // // Hàm này được gọi lúc ProcessScan tạo ra ô này
    // public void Setup(int x, int y, LevelEditor manager)
    // {
    //     _x = x;
    //     _y = y;
    //     _manager = manager;
    //     _myImage = GetComponent<Image>();
    // }
    //
    // // Khi nhấn chuột xuống ô này
    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     _manager.PaintCell(_x, _y, _myImage);
    // }
    //
    // // Khi giữ chuột và di ngang qua (Tô màu như Paint)
    // public void OnPointerEnter(PointerEventData eventData)
    // {
    //     // Nếu chuột trái đang được nhấn giữ
    //     if (Input.GetMouseButton(0)) 
    //     {
    //         _manager.PaintCell(_x, _y, _myImage);
    //     }
    // }
}