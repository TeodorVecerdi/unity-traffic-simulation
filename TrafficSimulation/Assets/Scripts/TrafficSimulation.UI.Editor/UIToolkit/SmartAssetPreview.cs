using TrafficSimulation.Core.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace TrafficSimulation.UI.Editor.UIToolkit;

[UxmlElement]
public sealed partial class SmartAssetPreview : VisualElement {
    private static readonly EditorResource<StyleSheet> s_StyleSheet = new("StyleSheets/SmartAssetPreview.uss");
    private Object? m_Asset;
    private bool m_IsLoadingPreview;
    private int m_RetryCount;
    private const int MaxRetries = 20;

    public Object? Asset {
        get => m_Asset;
        set {
            if (m_Asset != value) {
                m_Asset = value;
                m_RetryCount = 0;
                RefreshPreview();
            }
        }
    }

    public SmartAssetPreview() {
        AddToClassList("smart-asset-preview");
        if (s_StyleSheet.Value != null) {
            styleSheets.Add(s_StyleSheet.Value);
        }

        style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
        style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
        style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
    }

    public SmartAssetPreview(Object asset) : this() {
        Asset = asset;
    }

    private void RefreshPreview() {
        if (m_Asset == null) {
            style.backgroundImage = null;
            RemoveFromClassList("loading");
            RemoveFromClassList("fallback");
            return;
        }

        // Step 1: Try to get full preview
        var preview = AssetPreview.GetAssetPreview(m_Asset);
        if (preview != null) {
            style.backgroundImage = new StyleBackground(preview);
            RemoveFromClassList("loading");
            RemoveFromClassList("fallback");
            return;
        }

        // Step 2: Try mini thumbnail
        var miniThumbnail = AssetPreview.GetMiniThumbnail(m_Asset);
        if (miniThumbnail != null) {
            style.backgroundImage = new StyleBackground(miniThumbnail);
            AddToClassList("fallback");
        } else {
            // Step 3: Use type thumbnail as last resort
            var typeThumbnail = AssetPreview.GetMiniTypeThumbnail(m_Asset.GetType());
            if (typeThumbnail != null) {
                style.backgroundImage = new StyleBackground(typeThumbnail);
                AddToClassList("fallback");
            }
        }

        // Schedule retry for full preview
        if (!m_IsLoadingPreview && m_RetryCount < MaxRetries) {
            m_IsLoadingPreview = true;
            AddToClassList("loading");
            schedule.Execute(TryLoadPreview).Every(100);
        }
    }

    private void TryLoadPreview() {
        if (m_Asset == null || m_RetryCount >= MaxRetries) {
            m_IsLoadingPreview = false;
            RemoveFromClassList("loading");
            return;
        }

        m_RetryCount++;

        var preview = AssetPreview.GetAssetPreview(m_Asset);
        if (preview != null) {
            style.backgroundImage = new StyleBackground(preview);
            RemoveFromClassList("loading");
            RemoveFromClassList("fallback");
            m_IsLoadingPreview = false;
        }
    }
}
