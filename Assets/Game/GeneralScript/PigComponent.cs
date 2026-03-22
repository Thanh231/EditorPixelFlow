using TMPro;
using UnityEngine;

public class PigComponent : MonoBehaviour
{

    public int bulletCount;
    public TextMeshProUGUI text;
    public string color;
    public bool isHidden;
    public PigMarker pigLeft = null;
    public PigMarker pigRight = null;
    public void SetBulletCount(int count,string color)
    {
        bulletCount = count;
        text.text = bulletCount.ToString();
        this.color = color;
    }

    public void ReduceBulletCount()
    {
        bulletCount--;
        text.text = bulletCount.ToString();
    }

}