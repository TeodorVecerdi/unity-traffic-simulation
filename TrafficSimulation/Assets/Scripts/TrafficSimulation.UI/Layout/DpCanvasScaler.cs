using UnityEngine;
using UnityEngine.EventSystems;

namespace TrafficSimulation.UI.Layout;

[RequireComponent(typeof(Canvas))]
[ExecuteInEditMode]
[AddComponentMenu("Layout/DP Canvas Scaler")]
public sealed class DpCanvasScaler : UIBehaviour {
    [Tooltip("If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.")]
    [SerializeField]
    private float m_ReferencePixelsPerUnit = 100;
    public float ReferencePixelsPerUnit { get { return m_ReferencePixelsPerUnit; } set { m_ReferencePixelsPerUnit = value; } }

    // The log base doesn't have any influence on the results whatsoever, as long as the same base is used everywhere.
    private const float KLogBase = 2;

    [Tooltip("The DPI to assume if the screen DPI is not known.")]
    [SerializeField]
    private float m_FallbackScreenDPI = 96;

    public float FallbackScreenDPI {
        get => m_FallbackScreenDPI;
        set => m_FallbackScreenDPI = value;
    }

    [Tooltip("The pixels per inch to use for sprites that have a 'Pixels Per Unit' setting that matches the 'Reference Pixels Per Unit' setting.")]
    [SerializeField]
    private float m_DefaultSpriteDPI = 96;

    public float DefaultSpriteDPI {
        get => m_DefaultSpriteDPI;
        set => m_DefaultSpriteDPI = value;
    }

    // World Canvas settings
    [Tooltip("The amount of pixels per unit to use for dynamically created bitmaps in the UI, such as Text.")]
    [SerializeField]
    private float m_DynamicPixelsPerUnit = 1;
    [SerializeField]
    private float m_Scale = 1;

    public float DynamicPixelsPerUnit {
        get => m_DynamicPixelsPerUnit;
        set => m_DynamicPixelsPerUnit = value;
    }

    // General variables
    private Canvas? m_Canvas;
    [NonSerialized]
    private float m_PrevScaleFactor = 1;
    [NonSerialized]
    private float m_PrevReferencePixelsPerUnit = 100;

    protected override void OnEnable() {
        base.OnEnable();
        m_Canvas = GetComponent<Canvas>();
        Handle();
    }

    protected override void OnDisable() {
        SetScaleFactor(1);
        SetReferencePixelsPerUnit(100);
        base.OnDisable();
    }

    private void Update() {
        Handle();
    }

    private void Handle() {
        if (m_Canvas == null || !m_Canvas.isRootCanvas)
            return;

        if (m_Canvas.renderMode == RenderMode.WorldSpace) {
            HandleWorldCanvas();
            return;
        }

        HandleConstantPhysicalSize();
    }

    private void HandleWorldCanvas() {
        SetScaleFactor(m_DynamicPixelsPerUnit);
        SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
    }

    private void HandleConstantPhysicalSize() {
        var currentDpi = Screen.dpi;
        var dpi = currentDpi == 0 ? m_FallbackScreenDPI : currentDpi;
        float targetDPI = 160;

#if (UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
        targetDPI = 96;
#endif

        SetScaleFactor(m_Scale * dpi / targetDPI);
        SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
    }

    private void SetScaleFactor(float scaleFactor) {
        if (Mathf.Approximately(scaleFactor, m_PrevScaleFactor))
            return;

        m_Canvas!.scaleFactor = scaleFactor;
        m_PrevScaleFactor = scaleFactor;
    }

    private void SetReferencePixelsPerUnit(float referencePixelsPerUnit) {
        if (Mathf.Approximately(referencePixelsPerUnit, m_PrevReferencePixelsPerUnit))
            return;

        m_Canvas!.referencePixelsPerUnit = referencePixelsPerUnit;
        m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
    }
}
