using LitMotion;
using TrafficSimulation.Core.Async;
using TrafficSimulation.Core.Tweening;
using UnityEngine;

namespace TrafficSimulation.UI.Colors;

internal sealed class ColorAnimationController : IColorProperties, IDisposable {
    private static Shader? s_ColorShader;
    private static Shader? s_ColorMorphShader;

    public bool IsAnimating => m_AnimationHandler.IsActive;

    public Material? Material { get; private set; }
    public ColorPresetMode FromMode { get; private set; }
    public ColorPresetMode ToMode { get; private set; }
    public Color Color1 { get; private set; }
    public Color Color2 { get; private set; }
    public float Angle { get; private set; }
    public Vector2 Stops { get; private set; }
    public Vector2 Center { get; private set; }
    public Vector2 Radius { get; private set; }
    public float MorphProgress { get; private set; }

    private readonly AsyncHandler m_AnimationHandler = new();
    private Material? m_ColorMaterial;
    private Material? m_ColorMorphMaterial;

    public ColorPresetMode GetMode() => ToMode;
    public Color GetColor1() => Color1;
    public Color GetColor2() => Color2;
    public float GetAngle() => Angle;
    public Vector2 GetStops() => Stops;
    public Vector2 GetCenter() => Center;
    public Vector2 GetRadius() => Radius;

    public async UniTask Animate(IColorProperties currentProperties, IColorProperties targetProperties, float duration, Action onUpdate, CancellationToken cancellationToken) {
        IColorProperties fromProperties = new ColorComponentProperties(currentProperties);
        IColorProperties toProperties = new ColorComponentProperties(targetProperties);

        using var scope = m_AnimationHandler.Create(cancellationToken);
        FromMode = fromProperties.GetMode();
        ToMode = toProperties.GetMode();

        SetupMaterial(fromProperties, toProperties);
        var updateMaterialFunction = GetUpdateMaterialFunction(fromProperties, toProperties);
        var animationFunction = GetAnimationFunction(fromProperties, toProperties);

        await LSequence.Create()
            .Join(LMotion.Create(0.0f, 1.0f, duration).Bind(this, animationFunction, static (t, controller, action) => action(controller, t)))
            .Join(LMotion.Create(0.0f, 0.0f, duration).Bind(updateMaterialFunction + onUpdate, static (_, action) => action()))
            .Run().ToUniTask(scope.Token);
    }

    public void Dispose() {
        m_ColorMaterial.DestroyObject();
        m_ColorMorphMaterial.DestroyObject();
    }

    private void SetupMaterial(IColorProperties fromProperties, IColorProperties toProperties) {
        var fromMode = fromProperties.GetMode();
        var toMode = toProperties.GetMode();

        if (fromMode == toMode && toMode is ColorPresetMode.SolidColor) {
            Material = null;
            return;
        }

        if (fromMode == toMode || fromMode is ColorPresetMode.SolidColor || toMode is ColorPresetMode.SolidColor) {
            CreateColorMaterial();
            return;
        }

        SetupColorMorphMaterial();
    }

    private void CreateColorMaterial() {
        OrNull(ref s_ColorShader) ??= Shader.Find("TeodorVecerdi/Color");
        OrNull(ref m_ColorMaterial) ??= new Material(s_ColorShader) {
            name = $"Color_{Guid.NewGuid()}",
            hideFlags = HideFlags.HideAndDontSave,
        };
        Material = m_ColorMaterial;
    }

    private void SetupColorMorphMaterial() {
        OrNull(ref s_ColorMorphShader) ??= Shader.Find("TeodorVecerdi/ColorMorph");
        OrNull(ref m_ColorMorphMaterial) ??= new Material(s_ColorMorphShader) {
            name = $"ColorMorph_{Guid.NewGuid()}",
            hideFlags = HideFlags.HideAndDontSave,
        };
        Material = m_ColorMorphMaterial;
    }

    private Action GetUpdateMaterialFunction(IColorProperties fromProperties, IColorProperties toProperties) {
        var fromMode = fromProperties.GetMode();
        var toMode = toProperties.GetMode();

        if (fromMode == toMode && toMode is ColorPresetMode.SolidColor) {
            return () => { };
        }

        if (fromMode == toMode || fromMode is ColorPresetMode.SolidColor || toMode is ColorPresetMode.SolidColor) {
            return UpdateColorMaterial;
        }

        return UpdateMorphingMaterial;
    }

    private void UpdateColorMaterial() {
        ColorMaterialHelper.SetMaterialProperties(Material!, this);
    }

    private void UpdateMorphingMaterial() {
        ColorMaterialHelper.SetMorphingMaterialProperties(Material!, FromMode, this, MorphProgress);
    }

    private static Action<ColorAnimationController, float> GetAnimationFunction(IColorProperties fromProperties, IColorProperties toProperties) {
        var fromMode = fromProperties.GetMode();
        var toMode = toProperties.GetMode();

        // If we're animating to or from a solid color, we can create an IColorProperties
        // with the same mode, but with both colors set to the target color.
        if (fromMode != toMode) {
            if (fromMode is ColorPresetMode.SolidColor) {
                var color = fromProperties.GetColor1();
                fromProperties = toMode switch {
                    ColorPresetMode.LinearGradient => ColorProperties.CreateLinearGradient(color, color, toProperties.GetAngle(), toProperties.GetStops()),
                    ColorPresetMode.RadialGradient => ColorProperties.CreateRadialGradient(color, color, toProperties.GetStops(), toProperties.GetCenter(), toProperties.GetRadius()),
                    ColorPresetMode.ConicGradient => ColorProperties.CreateConicGradient(color, color, toProperties.GetAngle(), toProperties.GetStops(), toProperties.GetCenter()),
                    _ => throw new ArgumentOutOfRangeException(nameof(fromMode), fromMode, null),
                };

                fromMode = toMode;
            } else if (toMode is ColorPresetMode.SolidColor) {
                var color = toProperties.GetColor1();
                toProperties = fromMode switch {
                    ColorPresetMode.LinearGradient => ColorProperties.CreateLinearGradient(color, color, fromProperties.GetAngle(), fromProperties.GetStops()),
                    ColorPresetMode.RadialGradient => ColorProperties.CreateRadialGradient(color, color, fromProperties.GetStops(), fromProperties.GetCenter(), fromProperties.GetRadius()),
                    ColorPresetMode.ConicGradient => ColorProperties.CreateConicGradient(color, color, fromProperties.GetAngle(), fromProperties.GetStops(), fromProperties.GetCenter()),
                    _ => throw new ArgumentOutOfRangeException(nameof(fromMode), fromMode, null),
                };

                toMode = fromMode;
            }
        }

        if (fromMode == toMode) {
            return toMode switch {
                ColorPresetMode.SolidColor => AnimateSolidColor(fromProperties, toProperties),
                ColorPresetMode.LinearGradient => AnimateLinearGradient(fromProperties, toProperties),
                ColorPresetMode.RadialGradient => AnimateRadialGradient(fromProperties, toProperties),
                ColorPresetMode.ConicGradient => AnimateConicGradient(fromProperties, toProperties),
                _ => throw new ArgumentOutOfRangeException(nameof(toMode), toMode, null),
            };
        }

        return AnimateMorphing(fromProperties, toProperties);
    }

    private static Action<ColorAnimationController, float> AnimateSolidColor(IColorProperties fromProperties, IColorProperties toProperties) =>
        (controller, time) => {
            controller.Color1 = Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
        };

    private static Action<ColorAnimationController, float> AnimateLinearGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
        (controller, time) => {
            controller.Color1 = Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
            controller.Color2 = Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
            controller.Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
            controller.Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
        };

    private static Action<ColorAnimationController, float> AnimateRadialGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
        (controller, time) => {
            controller.Color1 = Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
            controller.Color2 = Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
            controller.Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
            controller.Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
            controller.Radius = Vector2.Lerp(fromProperties.GetRadius(), toProperties.GetRadius(), time);
        };

    private static Action<ColorAnimationController, float> AnimateConicGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
        (controller, time) => {
            controller.Color1 = Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
            controller.Color2 = Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
            controller.Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
            controller.Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
            controller.Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
        };

    private static Action<ColorAnimationController, float> AnimateMorphing(IColorProperties fromProperties, IColorProperties toProperties) =>
        (controller, time) => {
            controller.Color1 = Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
            controller.Color2 = Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
            controller.Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
            controller.Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
            controller.Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
            controller.Radius = Vector2.Lerp(fromProperties.GetRadius(), toProperties.GetRadius(), time);
            controller.MorphProgress = time;
        };
}
