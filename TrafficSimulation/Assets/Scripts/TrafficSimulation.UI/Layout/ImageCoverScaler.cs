using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI.Layout;

[RequireComponent(typeof(RectTransform)), HideMonoScript]
[TypeInfoBox("This script will scale its child image to always cover the entire parent while maintaining the aspect ratio.\nThis is equivalent to <b>background-size: cover;</b> in CSS.")]
public sealed class ImageCoverScaler : MonoBehaviour {
    [SerializeField, Required] private Image m_TargetImage = null!;

    public Image TargetImage => m_TargetImage;

    private void Awake() {
        var middle = Vector2.one * 0.5f;
        m_TargetImage.rectTransform.anchorMin = middle;
        m_TargetImage.rectTransform.anchorMax = middle;
        m_TargetImage.rectTransform.pivot = middle;
        m_TargetImage.rectTransform.localScale = Vector3.one;
    }

    public void SetSprite(Sprite? sprite) {
        m_TargetImage.sprite = sprite;
        UpdateSize();
    }

    [Button]
    public void UpdateSize() {
        var sprite = m_TargetImage.sprite;
        if (sprite == null) {
            return;
        }

        var parentSize = ((RectTransform)transform).rect.size;
        var imageSize = sprite.rect.size;
        var imageRatio = imageSize.y / imageSize.x;
        var parentRatio = parentSize.y / parentSize.x;

        if (parentRatio > imageRatio) {
            m_TargetImage.rectTransform.sizeDelta = new Vector2(parentSize.y / imageRatio, parentSize.y);
            m_TargetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentSize.y / imageRatio);
            m_TargetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentSize.y);
        } else {
            m_TargetImage.rectTransform.sizeDelta = new Vector2(parentSize.x, parentSize.x * imageRatio);
            m_TargetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentSize.x);
            m_TargetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentSize.x * imageRatio);
        }
    }
}
