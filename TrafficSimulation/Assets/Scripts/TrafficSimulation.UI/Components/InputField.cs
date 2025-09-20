using System.Reflection;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

public sealed class InputField : TMP_InputField {
    private static readonly ValueGetter<TMP_InputField, bool> s_WasCanceled;
    private static readonly ValueSetter<TMP_InputField, bool> s_IsTextComponentUpdateRequired;
    private static readonly ValueGetter<TMP_InputField, bool> s_HasSelection;
    private static readonly ValueSetter<TMP_InputField, float> s_SetScrollPosition;
    private static readonly ValueGetter<TMP_InputField, RectTransform> s_CaretRectTrans;
    private static readonly Action<TMP_InputField, bool, bool> s_MoveLeft;
    private static readonly Action<TMP_InputField, bool, bool> s_MoveRight;
    private static readonly Action<TMP_InputField> s_DeleteKey;
    private static readonly Action<TMP_InputField> s_Backspace;
    private static readonly Func<TMP_InputField, float> s_GetScrollPositionRelativeToViewport;
    private static readonly Action<TMP_InputField, float> s_AdjustTextPositionRelativeToViewport;

    static InputField() {
        s_WasCanceled = EmitUtilities.CreateInstanceFieldGetter<TMP_InputField, bool>(GetField("m_WasCanceled"));
        s_IsTextComponentUpdateRequired = EmitUtilities.CreateInstanceFieldSetter<TMP_InputField, bool>(GetField("m_IsTextComponentUpdateRequired"));
        s_HasSelection = EmitUtilities.CreateInstancePropertyGetter<TMP_InputField, bool>(GetProperty("hasSelection"));
        s_SetScrollPosition = EmitUtilities.CreateInstanceFieldSetter<TMP_InputField, float>(GetField("m_ScrollPosition"));
        s_CaretRectTrans = EmitUtilities.CreateInstanceFieldGetter<TMP_InputField, RectTransform>(GetField("caretRectTrans"));
        s_MoveLeft = CreateInstanceMethodCaller<TMP_InputField, bool, bool>(GetMethod("MoveLeft"));
        s_MoveRight = CreateInstanceMethodCaller<TMP_InputField, bool, bool>(GetMethod("MoveRight"));
        s_DeleteKey = EmitUtilities.CreateInstanceMethodCaller<TMP_InputField>(GetMethod("DeleteKey"));
        s_Backspace = EmitUtilities.CreateInstanceMethodCaller<TMP_InputField>(GetMethod("Backspace"));
        s_GetScrollPositionRelativeToViewport = EmitUtilities.CreateMethodReturner<TMP_InputField, float>(GetMethod("GetScrollPositionRelativeToViewport"));
        s_AdjustTextPositionRelativeToViewport = EmitUtilities.CreateInstanceMethodCaller<TMP_InputField, float>(GetMethod("AdjustTextPositionRelativeToViewport"));
        return;

        static FieldInfo GetField(string name) => typeof(TMP_InputField).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(TMP_InputField).FullName, name);
        static PropertyInfo GetProperty(string name) => typeof(TMP_InputField).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMemberException(typeof(TMP_InputField).FullName, name);
        static MethodInfo GetMethod(string name) => typeof(TMP_InputField).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(TMP_InputField).FullName, name);

        static Action<TInstanceType, TArg1, TArg2> CreateInstanceMethodCaller<TInstanceType, TArg1, TArg2>(MethodInfo methodInfo) {
            return (Action<TInstanceType, TArg1, TArg2>)Delegate.CreateDelegate(typeof(Action<TInstanceType, TArg1, TArg2>), methodInfo.DeAliasMethod());
        }
    }

    private InputAction m_ShiftAction = null!;

    protected override void Awake() {
        base.Awake();
        m_ShiftAction = InputSystem.actions.FindAction("UI/Shift", true);
    }

    protected override void OnEnable() {
        base.OnEnable();
        onValueChanged.AddListener(OnValueChanged);
    }

    protected override void OnDisable() {
        base.OnDisable();
        onValueChanged.RemoveListener(OnValueChanged);
    }

    public override void OnUpdateSelected(BaseEventData eventData) {
        if (!isFocused)
            return;

        TMP_InputField @this = this;
        var consumedEvent = false;

        var evt = new Event();
        while (Event.PopEvent(evt)) {
            var eventType = evt.rawType;
            if (eventType is EventType.KeyUp)
                continue;

            if (eventType is EventType.KeyDown) {
                consumedEvent = true;

                var editState = HandleKeyPressed(evt);
                if (editState is EditState.Finish) {
                    if (!s_WasCanceled(ref @this)) {
                        SendOnSubmit();
                    }

                    break;
                }

                s_IsTextComponentUpdateRequired(ref @this, true);
                UpdateLabel();
                continue;
            }

            if (eventType is EventType.ValidateCommand or EventType.ExecuteCommand && evt.commandName is "SelectAll") {
                SelectAll();
                consumedEvent = true;
            }
        }

        if (consumedEvent) {
            UpdateLabel();
            eventData.Use();
        }
    }

    private void OnValueChanged(string _) {
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);

        // Fix text position being offset after going multiline and back to single line
        if (textComponent.textInfo.lineCount <= 1) {
            UniTask.Post(() => {
                TMP_InputField instance = this;
                textComponent.rectTransform.anchoredPosition = new Vector2(textComponent.rectTransform.anchoredPosition.x, 0.0f);
                s_CaretRectTrans(ref instance).anchoredPosition = textComponent.rectTransform.anchoredPosition;
            });
        }

        // Update scroll position
        var scrollPosition = Mathf.Clamp01(s_GetScrollPositionRelativeToViewport(this));
        TMP_InputField instance = this;
        s_SetScrollPosition(ref instance, scrollPosition);
        s_AdjustTextPositionRelativeToViewport(this, scrollPosition);
    }

    private EditState HandleKeyPressed(Event evt) {
        if (lineType is LineType.MultiLineSubmit && evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter && m_ShiftAction.IsPressed()) {
            Append('\n');
            return EditState.Continue;
        }

        // Bypass deactivating the input field when pressing escape since it's pretty broken
        if (evt.keyCode is KeyCode.Escape) {
            return EditState.Continue;
        }

        TMP_InputField @this = this;
        if (!s_HasSelection(ref @this)) {
            var ctrlOnly = evt is { control: true, alt: false, shift: false };
            if (ctrlOnly && evt.keyCode is KeyCode.Backspace) {
                s_MoveLeft(this, true, true);
                s_Backspace(this);
                return EditState.Continue;
            }

            if (ctrlOnly && evt.keyCode is KeyCode.Delete) {
                s_MoveRight(this, true, true);
                s_DeleteKey(this);
                return EditState.Continue;
            }
        }

        return KeyPressed(evt);
    }
}
