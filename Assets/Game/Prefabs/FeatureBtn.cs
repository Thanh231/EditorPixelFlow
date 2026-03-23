using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FeatureBtn : MonoBehaviour, IPointerDownHandler
{
    public TextMeshProUGUI featureText;
    private string featureName;
    private LevelEditor levelEditor;
    private Image _bgImage;

    private static readonly Color ColorSelected = new Color(0.20f, 0.80f, 0.20f); // xanh lục sáng
    private static readonly Color ColorDeselected = new Color(0.25f, 0.25f, 0.25f); // tối xám
    private static readonly Color ColorNormal = Color.white;

    private void Awake()
    {
        _bgImage = GetComponent<Image>();
    }

    public void SetFeatureName(string name, LevelEditor editor)
    {
        featureName = name;
        levelEditor = editor;
        featureText.text = name;
    }

    public void SetSelected(bool selected)
    {
        _bgImage = GetComponent<Image>();

        _bgImage.color = selected ? ColorSelected : ColorDeselected;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        int featureIndex = System.Array.IndexOf(levelEditor.features, featureName);
        if (featureIndex >= 0)
            levelEditor.OnClickFeatureButton(featureIndex);

    }
}
