using LitMotion;
using TrafficSimulation.Core.Async;
using TrafficSimulation.Core.Tweening;
using TrafficSimulation.UI.Colors;
using UnityEngine;

namespace TrafficSimulation.UI;

public sealed partial class UILabel {
    private sealed class AnimationController : IColorProperties {
        public bool IsAnimating => m_AnimationHandler.IsActive;

        private readonly AsyncHandler m_AnimationHandler = new();
        private ColorPresetMode m_FromMode;
        private ColorPresetMode m_ToMode;
        private Color m_Color1;
        private Color m_Color2;
        private float m_Angle;
        private Vector2 m_Stops;
        private Vector2 m_Center;
        private Vector2 m_Radius;
        private float m_AnimationProgress;
        private ColorFunction? m_AnimatedColorFunction;

        public ColorPresetMode GetMode() => m_ToMode;
        public Color GetColor1() => m_Color1;
        public Color GetColor2() => m_Color2;
        public float GetAngle() => m_Angle;
        public Vector2 GetStops() => m_Stops;
        public Vector2 GetCenter() => m_Center;
        public Vector2 GetRadius() => m_Radius;

        public async UniTask Animate(IColorProperties currentProperties, IColorProperties targetProperties, float duration, Action onUpdate, CancellationToken cancellationToken) {
            IColorProperties fromProperties = new ColorComponentProperties(currentProperties);
            IColorProperties toProperties = new ColorComponentProperties(targetProperties);

            using var scope = m_AnimationHandler.Create(cancellationToken);
            var (animationFunction, colorFunction) = GetAnimationFunction(fromProperties, toProperties, this);
            m_AnimatedColorFunction = colorFunction;

            await LMotion.Create(0.0f, 1.0f, duration).WithDefaults()
                .Bind(this, animationFunction, onUpdate, static (t, target, animationFunction, onUpdate) => {
                    target.m_AnimationProgress = t;
                    animationFunction(target, t);
                    onUpdate();
                })
                .ToUniTask(cancellationToken);
        }

        public Color GetAnimatedColor(Vector2 uv, Color color1, Color color2, float angle, Vector2 stops, Vector2 center, Vector2 radius) {
            return m_AnimatedColorFunction!(uv, color1, color2, angle, stops, center, radius);
        }

        private static (Action<AnimationController, float> AnimateFunction, ColorFunction ColorFunction) GetAnimationFunction(IColorProperties fromProperties, IColorProperties toProperties, AnimationController controller) {
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
                var colorFunction = GetColorFunction(toMode);
                return toMode switch {
                    ColorPresetMode.SolidColor => (AnimateSolidColor(fromProperties, toProperties), colorFunction),
                    ColorPresetMode.LinearGradient => (AnimateLinearGradient(fromProperties, toProperties), colorFunction),
                    ColorPresetMode.RadialGradient => (AnimateRadialGradient(fromProperties, toProperties), colorFunction),
                    ColorPresetMode.ConicGradient => (AnimateConicGradient(fromProperties, toProperties), colorFunction),
                    _ => throw new ArgumentOutOfRangeException(nameof(toMode), toMode, null),
                };
            }

            return (AnimateAllProperties(fromProperties, toProperties), GetMorphColorFunction());

            ColorFunction GetMorphColorFunction() {
                var fromFunction = GetColorFunction(fromMode);
                var toFunction = GetColorFunction(toMode);
                return (uv, color1, color2, angle, stops, center, radius) => {
                    var fromColor = fromFunction(uv, color1, color2, angle, stops, center, radius);
                    var toColor = toFunction(uv, color1, color2, angle, stops, center, radius);
                    return UnityEngine.Color.Lerp(fromColor, toColor, controller.m_AnimationProgress);
                };
            }
        }

        private static Action<AnimationController, float> AnimateSolidColor(IColorProperties fromProperties, IColorProperties toProperties) =>
            (controller, time) => {
                controller.m_Color1 = UnityEngine.Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
            };

        private static Action<AnimationController, float> AnimateLinearGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
            (controller, time) => {
                controller.m_Color1 = UnityEngine.Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
                controller.m_Color2 = UnityEngine.Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
                controller.m_Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
                controller.m_Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
            };

        private static Action<AnimationController, float> AnimateRadialGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
            (controller, time) => {
                controller.m_Color1 = UnityEngine.Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
                controller.m_Color2 = UnityEngine.Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
                controller.m_Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
                controller.m_Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
                controller.m_Radius = Vector2.Lerp(fromProperties.GetRadius(), toProperties.GetRadius(), time);
            };

        private static Action<AnimationController, float> AnimateConicGradient(IColorProperties fromProperties, IColorProperties toProperties) =>
            (controller, time) => {
                controller.m_Color1 = UnityEngine.Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
                controller.m_Color2 = UnityEngine.Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
                controller.m_Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
                controller.m_Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
                controller.m_Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
            };

        private static Action<AnimationController, float> AnimateAllProperties(IColorProperties fromProperties, IColorProperties toProperties) =>
            (controller, time) => {
                controller.m_Color1 = UnityEngine.Color.Lerp(fromProperties.GetColor1(), toProperties.GetColor1(), time);
                controller.m_Color2 = UnityEngine.Color.Lerp(fromProperties.GetColor2(), toProperties.GetColor2(), time);
                controller.m_Angle = Mathf.LerpAngle(fromProperties.GetAngle(), toProperties.GetAngle(), time);
                controller.m_Stops = Vector2.Lerp(fromProperties.GetStops(), toProperties.GetStops(), time);
                controller.m_Center = Vector2.Lerp(fromProperties.GetCenter(), toProperties.GetCenter(), time);
                controller.m_Radius = Vector2.Lerp(fromProperties.GetRadius(), toProperties.GetRadius(), time);
            };
    }
}
