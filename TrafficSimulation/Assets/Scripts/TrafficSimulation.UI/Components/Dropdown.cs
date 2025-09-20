using System.Collections;
using TrafficSimulation.Core.UI;
using TrafficSimulation.Core.UI.Interfaces;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrafficSimulation.UI {
    [RequireComponent(typeof(RectTransform))]
    public class Dropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler {
        public class DropdownItem : MonoBehaviour, IPointerEnterHandler, ICancelHandler {
            [SerializeField]
            private TMP_Text? m_Text;
            [SerializeField]
            private Image? m_Image;
            [SerializeField, Required]
            private RectTransform m_RectTransform = null!;
            [SerializeField, Required]
            private Toggle m_Toggle = null!;

            public TMP_Text? Text { get { return m_Text; } set { m_Text = value; } }
            public Image? Image { get { return m_Image; } set { m_Image = value; } }
            public RectTransform RectTransform { get { return m_RectTransform; } set { m_RectTransform = value; } }
            public Toggle Toggle { get { return m_Toggle; } set { m_Toggle = value; } }

            public virtual void OnPointerEnter(PointerEventData eventData) {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            public virtual void OnCancel(BaseEventData eventData) {
                var dropdown = GetComponentInParent<Dropdown>();
                if (dropdown)
                    dropdown.Hide();
            }
        }

        /// <summary>
        /// Class to store the text and/or image of a single option in the dropdown list.
        /// </summary>
        [Serializable]
        public class OptionData {
            [SerializeField]
            private string? m_Text;
            [SerializeField]
            private Sprite? m_Image;
            [SerializeField]
            private Color m_Color = Color.white;

            /// <summary>
            /// The text associated with the option.
            /// </summary>
            public string? Text { get { return m_Text; } set { m_Text = value; } }

            /// <summary>
            /// The image associated with the option.
            /// </summary>
            public Sprite? Image { get { return m_Image; } set { m_Image = value; } }

            /// <summary>
            /// The color associated with the option.
            /// </summary>
            public Color Color { get { return m_Color; } set { m_Color = value; } }

            public OptionData() { }

            public OptionData(string text) {
                Text = text;
            }

            public OptionData(Sprite image) {
                Image = image;
            }

            /// <summary>
            /// Create an object representing a single option for the dropdown list.
            /// </summary>
            /// <param name="text">Optional text for the option.</param>
            /// <param name="image">Optional image for the option.</param>
            /// <param name="color">Optional color for the option.</param>
            public OptionData(string text, Sprite image, Color color) {
                Text = text;
                Image = image;
                Color = color;
            }
        }

        /// <summary>
        /// Class used internally to store the list of options for the dropdown list.
        /// </summary>
        /// <remarks>
        /// The usage of this class is not exposed in the runtime API. It's only relevant for the PropertyDrawer drawing the list of options.
        /// </remarks>
        [Serializable]
        public class OptionDataList {
            [SerializeField]
            private List<OptionData> m_Options = [];
            public List<OptionData> Options { get { return m_Options; } set { m_Options = value; } }
        }

        private static readonly OptionData s_NothingOption = new() { Text = "Nothing" };
        private static readonly OptionData s_EverythingOption = new() { Text = "Everything" };
        private static readonly OptionData s_MixedOption = new() { Text = "Mixed..." };

        // Template used to create the dropdown.
        [SerializeField]
        private RectTransform m_Template = null!;

        public event Action<int>? ValueChanged;
        public event Action<int, DropdownItem>? ItemInitialized;

        /// <summary>
        /// The Rect Transform of the template for the dropdown list.
        /// </summary>
        public RectTransform Template {
            get { return m_Template; }
            set {
                m_Template = value;
                RefreshShownValue();
            }
        }

        // Text to be used as a caption for the current value. It's not required, but it's kept here for convenience.
        [SerializeField]
        private TMP_Text? m_CaptionText;

        /// <summary>
        /// The Text component to hold the text of the currently selected option.
        /// </summary>
        public TMP_Text? CaptionText {
            get { return m_CaptionText; }
            set {
                m_CaptionText = value;
                RefreshShownValue();
            }
        }

        [SerializeField]
        private Image? m_CaptionImage;

        /// <summary>
        /// The Image component to hold the image of the currently selected option.
        /// </summary>
        public Image? CaptionImage {
            get { return m_CaptionImage; }
            set {
                m_CaptionImage = value;
                RefreshShownValue();
            }
        }

        [SerializeField]
        private Graphic? m_Placeholder;

        /// <summary>
        /// The placeholder Graphic component. Shown when no option is selected.
        /// </summary>
        public Graphic? Placeholder {
            get { return m_Placeholder; }
            set {
                m_Placeholder = value;
                RefreshShownValue();
            }
        }

        [Space]
        [SerializeField]
        private TMP_Text? m_ItemText;

        /// <summary>
        /// The Text component to hold the text of the item.
        /// </summary>
        public TMP_Text? ItemText {
            get { return m_ItemText; }
            set {
                m_ItemText = value;
                RefreshShownValue();
            }
        }

        [SerializeField]
        private Image? m_ItemImage;

        /// <summary>
        /// The Image component to hold the image of the item
        /// </summary>
        public Image? ItemImage {
            get { return m_ItemImage; }
            set {
                m_ItemImage = value;
                RefreshShownValue();
            }
        }

        [Space]
        [SerializeField]
        private int m_Value;

        [SerializeField]
        private bool m_MultiSelect;

        [Space]

        // Items that will be visible when the dropdown is shown.
        // We box this into its own class so we can use a Property Drawer for it.
        [SerializeField]
        private OptionDataList m_Options = new();

        public List<OptionData> Options {
            get { return m_Options.Options; }
            set {
                m_Options.Options = value;
                RefreshShownValue();
            }
        }

        [Space]
        [SerializeField]
        private float m_AlphaFadeSpeed = 0.15f;

        /// <summary>
        /// The time interval at which a drop down will appear and disappear
        /// </summary>
        public float AlphaFadeSpeed { get { return m_AlphaFadeSpeed; } set { m_AlphaFadeSpeed = value; } }

        private GameObject? m_Dropdown;
        private IFadeable? m_DropdownFadeable;
        private GameObject? m_Blocker;
        private List<DropdownItem> m_Items = [];
        private bool m_ValidTemplate;
        private Coroutine? m_Coroutine;

        private static OptionData s_NoOptionData = new();

        /// <summary>
        /// The Value is the index number of the current selection in the Dropdown. 0 is the first option in the Dropdown, 1 is the second, and so on.
        /// </summary>
        public int Value {
            get {
                return m_Value;
            }
            set {
                SetValue(value);
            }
        }

        /// <summary>
        /// Set index number of the current selection in the Dropdown without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new index for the current selection.</param>
        public void SetValueWithoutNotify(int input) {
            SetValue(input, false);
        }

        private void SetValue(int value, bool sendCallback = true) {
            if (Application.isPlaying && (value == m_Value || Options.Count == 0))
                return;

            if (m_MultiSelect)
                m_Value = value;
            else
                m_Value = Mathf.Clamp(value, m_Placeholder ? -1 : 0, Options.Count - 1);

            RefreshShownValue();

            if (sendCallback) {
                // Notify all listeners
                UISystemProfilerApi.AddMarker("Dropdown.value", this);
                ValueChanged?.Invoke(m_Value);
            }
        }

        public bool IsExpanded { get { return m_Dropdown != null; } }

        public bool MultiSelect { get { return m_MultiSelect; } set { m_MultiSelect = value; } }

        protected Dropdown() { }

        protected override void Awake() {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            if (m_CaptionImage)
                m_CaptionImage.enabled = m_CaptionImage.sprite != null && m_CaptionImage.color.a > 0;

            if (m_Template)
                m_Template.gameObject.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            RefreshShownValue();
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            if (!IsActive())
                return;

            RefreshShownValue();
        }
#endif

        protected override void OnDisable() {
            //Destroy dropdown and blocker in case user deactivates the dropdown when they click an option (case 935649)
            ImmediateDestroyDropdownList();

            if (m_Blocker != null)
                DestroyBlocker(m_Blocker);

            m_Blocker = null;

            base.OnDisable();
        }

        /// <summary>
        /// Refreshes the text and image (if available) of the currently selected option.
        /// </summary>
        /// <remarks>
        /// If you have modified the list of options, you should call this method afterwards to ensure that the visual state of the dropdown corresponds to the updated options.
        /// </remarks>
        public void RefreshShownValue() {
            var data = s_NoOptionData;

            if (Options.Count > 0) {
                if (m_MultiSelect) {
                    var firstActiveFlag = FirstActiveFlagIndex(m_Value);
                    if (m_Value == 0 || firstActiveFlag >= Options.Count)
                        data = s_NothingOption;
                    else if (IsEverythingValue(Options.Count, m_Value))
                        data = s_EverythingOption;
                    else if (Mathf.IsPowerOfTwo(m_Value) && m_Value > 0)
                        data = Options[firstActiveFlag];
                    else
                        data = s_MixedOption;
                } else if (m_Value >= 0) {
                    data = Options[Mathf.Clamp(m_Value, 0, Options.Count - 1)];
                }
            }

            if (m_CaptionText) {
                if (data != null && data.Text != null)
                    m_CaptionText.text = data.Text;
                else
                    m_CaptionText.text = "";
            }

            if (m_CaptionImage) {
                m_CaptionImage.sprite = data!.Image;
                m_CaptionImage.color = data.Color;
                m_CaptionImage.enabled = m_CaptionImage.sprite != null && m_CaptionImage.color.a > 0;
            }

            if (m_Placeholder) {
                m_Placeholder.enabled = Options.Count == 0 || m_Value == -1;
            }
        }

        public void AddOptions(List<OptionData> options) {
            Options.AddRange(options);
            RefreshShownValue();
        }

        public void AddOptions(List<string> options) {
            for (var i = 0; i < options.Count; i++)
                Options.Add(new OptionData(options[i]));

            RefreshShownValue();
        }

        public void AddOptions(List<Sprite> options) {
            for (var i = 0; i < options.Count; i++)
                Options.Add(new OptionData(options[i]));

            RefreshShownValue();
        }

        /// <summary>
        /// Clear the list of options in the Dropdown.
        /// </summary>
        public void ClearOptions() {
            Options.Clear();
            m_Value = m_Placeholder ? -1 : 0;
            RefreshShownValue();
        }

        private void SetupTemplate() {
            m_ValidTemplate = false;

            if (!m_Template) {
                Debug.LogError("The dropdown template is not assigned. The template needs to be assigned and must have a child GameObject with a Toggle component serving as the item.", this);
                return;
            }

            var templateGo = m_Template.gameObject;
            templateGo.SetActive(true);
            var itemToggle = m_Template.GetComponentInChildren<Toggle>();

            m_ValidTemplate = true;
            if (!itemToggle || itemToggle.transform == Template) {
                m_ValidTemplate = false;
                Debug.LogError("The dropdown template is not valid. The template must have a child GameObject with a Toggle component serving as the item.", Template);
            } else if (!(itemToggle.transform.parent is RectTransform)) {
                m_ValidTemplate = false;
                Debug.LogError("The dropdown template is not valid. The child GameObject with a Toggle component (the item) must have a RectTransform on its parent.", Template);
            } else if (ItemText != null && !ItemText.transform.IsChildOf(itemToggle.transform)) {
                m_ValidTemplate = false;
                Debug.LogError("The dropdown template is not valid. The Item Text must be on the item GameObject or children of it.", Template);
            } else if (ItemImage != null && !ItemImage.transform.IsChildOf(itemToggle.transform)) {
                m_ValidTemplate = false;
                Debug.LogError("The dropdown template is not valid. The Item Image must be on the item GameObject or children of it.", Template);
            }

            if (!m_ValidTemplate) {
                templateGo.SetActive(false);
                return;
            }

            var item = itemToggle!.gameObject.AddComponent<DropdownItem>();
            item.Text = m_ItemText;
            item.Image = m_ItemImage;
            item.Toggle = itemToggle;
            item.RectTransform = (RectTransform)itemToggle.transform;

            // Find the Canvas that this dropdown is a part of
            Canvas? parentCanvas = null;
            var parentTransform = m_Template.parent;
            while (parentTransform != null) {
                parentCanvas = parentTransform.GetComponent<Canvas>();
                if (parentCanvas != null)
                    break;

                parentTransform = parentTransform.parent;
            }

            var popupCanvas = GetOrAddComponent<Canvas>(templateGo);
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 30000;

            // If we have a parent canvas, apply the same raycasters as the parent for consistency.
            if (parentCanvas != null) {
                var components = parentCanvas.GetComponents<BaseRaycaster>();
                for (var i = 0; i < components.Length; i++) {
                    var raycasterType = components[i].GetType();
                    if (templateGo.GetComponent(raycasterType) == null) {
                        templateGo.AddComponent(raycasterType);
                    }
                }
            } else {
                GetOrAddComponent<GraphicRaycaster>(templateGo);
            }

            GetOrAddComponent<CanvasGroup>(templateGo);
            templateGo.SetActive(false);

            m_ValidTemplate = true;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component {
            var comp = go.GetComponent<T>();
            if (!comp)
                comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>
        /// Handling for when the dropdown is initially 'clicked'. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnPointerClick(PointerEventData eventData) {
            Show();
        }

        /// <summary>
        /// Handling for when the dropdown is selected and a submit event is processed. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnSubmit(BaseEventData eventData) {
            Show();
        }

        /// <summary>
        /// This will hide the dropdown list.
        /// </summary>
        /// <remarks>
        /// Called by a BaseInputModule when a Cancel event occurs.
        /// </remarks>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnCancel(BaseEventData eventData) {
            Hide();
        }

        /// <summary>
        /// Show the dropdown.
        ///
        /// Plan for dropdown scrolling to ensure dropdown is contained within screen.
        ///
        /// We assume the Canvas is the screen that the dropdown must be kept inside.
        /// This is always valid for screen space canvas modes.
        /// For world space canvases we don't know how it's used, but it could be e.g. for an in-game monitor.
        /// We consider it a fair constraint that the canvas must be big enough to contain dropdowns.
        /// </summary>
        public void Show() {
            if (m_Coroutine != null) {
                StopCoroutine(m_Coroutine);
                ImmediateDestroyDropdownList();
            }

            if (!IsActive() || !IsInteractable() || m_Dropdown != null)
                return;

            // Get root Canvas.
            var list = new List<Canvas>();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count == 0)
                return;

            var rootCanvas = list[^1];
            for (var i = 0; i < list.Count; i++) {
                if (list[i].isRootCanvas) {
                    rootCanvas = list[i];
                    break;
                }
            }

            if (!m_ValidTemplate) {
                SetupTemplate();
                if (!m_ValidTemplate)
                    return;
            }

            m_Template.gameObject.SetActive(true);

            // popupCanvas used to assume the root canvas had the default sorting Layer, next line fixes (case 958281 - [UI] Dropdown list does not copy the parent canvas layer when the panel is opened)
            m_Template.GetComponent<Canvas>().sortingLayerID = rootCanvas.sortingLayerID;

            // Instantiate the drop-down template
            m_Dropdown = CreateDropdownList(m_Template.gameObject);
            m_DropdownFadeable = new FadeableBehaviour(m_Dropdown.GetOrAddComponent<CanvasGroup>());
            m_Dropdown.name = "Dropdown List";
            m_Dropdown.SetActive(true);

            // Make drop-down RectTransform have same values as original.
            var dropdownRectTransform = m_Dropdown.transform as RectTransform;
            dropdownRectTransform!.SetParent(m_Template.transform.parent, false);

            // Instantiate the drop-down list items

            // Find the dropdown item and disable it.
            var itemTemplate = m_Dropdown.GetComponentInChildren<DropdownItem>();

            var content = itemTemplate.RectTransform.parent.gameObject;
            var contentRectTransform = content.transform as RectTransform;
            itemTemplate.RectTransform.gameObject.SetActive(true);

            // Get the rects of the dropdown and item
            var dropdownContentRect = contentRectTransform!.rect;
            var itemTemplateRect = itemTemplate.RectTransform.rect;

            // Calculate the visual offset between the item's edges and the background's edges
            var offsetMin = itemTemplateRect.min - dropdownContentRect.min + (Vector2)itemTemplate.RectTransform.localPosition;
            var offsetMax = itemTemplateRect.max - dropdownContentRect.max + (Vector2)itemTemplate.RectTransform.localPosition;
            var itemSize = itemTemplateRect.size;

            m_Items.Clear();

            Toggle? prev = null;
            if (m_MultiSelect && Options.Count > 0) {
                var item = AddItem(-1, s_NothingOption, itemTemplate, m_Items);
                if (item.Image != null)
                    item.Image.gameObject.SetActive(false);

                var nothingToggle = item.Toggle;
                nothingToggle.isOn = Value == 0;
                nothingToggle.onValueChanged.AddListener(_ => OnSelectItem(nothingToggle));
                prev = nothingToggle;

                var isEverythingValue = IsEverythingValue(Options.Count, Value);
                item = AddItem(-1, s_EverythingOption, itemTemplate, m_Items);
                if (item.Image != null)
                    item.Image.gameObject.SetActive(false);

                var everythingToggle = item.Toggle;
                everythingToggle.isOn = isEverythingValue;
                everythingToggle.onValueChanged.AddListener(_ => OnSelectItem(everythingToggle));

                // Automatically set up explicit navigation
                if (prev != null) {
                    var prevNav = prev.navigation;
                    var toggleNav = item.Toggle.navigation;
                    prevNav.mode = Navigation.Mode.Explicit;
                    toggleNav.mode = Navigation.Mode.Explicit;

                    prevNav.selectOnDown = item.Toggle;
                    prevNav.selectOnRight = item.Toggle;
                    toggleNav.selectOnLeft = prev;
                    toggleNav.selectOnUp = prev;

                    prev.navigation = prevNav;
                    item.Toggle.navigation = toggleNav;
                }
            }

            for (var i = 0; i < Options.Count; ++i) {
                var data = Options[i];
                var item = AddItem(i, data, itemTemplate, m_Items);
                if (item == null)
                    continue;

                // Automatically set up a toggle state change listener
                if (m_MultiSelect)
                    item.Toggle.isOn = (Value & (1 << i)) != 0;
                else
                    item.Toggle.isOn = Value == i;

                item.Toggle.onValueChanged.AddListener(_ => OnSelectItem(item.Toggle));

                // Select current option
                if (item.Toggle.isOn)
                    item.Toggle.Select();

                // Automatically set up explicit navigation
                if (prev != null) {
                    var prevNav = prev.navigation;
                    var toggleNav = item.Toggle.navigation;
                    prevNav.mode = Navigation.Mode.Explicit;
                    toggleNav.mode = Navigation.Mode.Explicit;

                    prevNav.selectOnDown = item.Toggle;
                    prevNav.selectOnRight = item.Toggle;
                    toggleNav.selectOnLeft = prev;
                    toggleNav.selectOnUp = prev;

                    prev.navigation = prevNav;
                    item.Toggle.navigation = toggleNav;
                }

                prev = item.Toggle;
            }

            // Reposition all items now that all of them have been added
            var sizeDelta = contentRectTransform.sizeDelta;
            sizeDelta.y = itemSize.y * m_Items.Count + offsetMin.y - offsetMax.y;
            contentRectTransform.sizeDelta = sizeDelta;

            var extraSpace = dropdownRectTransform.rect.height - contentRectTransform.rect.height;
            if (extraSpace > 0)
                dropdownRectTransform.sizeDelta = new Vector2(dropdownRectTransform.sizeDelta.x, dropdownRectTransform.sizeDelta.y - extraSpace);

            // Invert anchoring and position if dropdown is partially or fully outside of canvas rect.
            // Typically this will have the effect of placing the dropdown above the button instead of below,
            // but it works as inversion regardless of initial setup.
            var corners = new Vector3[4];
            dropdownRectTransform.GetWorldCorners(corners);

            var rootCanvasRectTransform = rootCanvas.transform as RectTransform;
            var rootCanvasRect = rootCanvasRectTransform!.rect;
            for (var axis = 0; axis < 2; axis++) {
                var outside = false;
                for (var i = 0; i < 4; i++) {
                    var corner = rootCanvasRectTransform.InverseTransformPoint(corners[i]);
                    if ((corner[axis] < rootCanvasRect.min[axis] && !Mathf.Approximately(corner[axis], rootCanvasRect.min[axis])) ||
                        (corner[axis] > rootCanvasRect.max[axis] && !Mathf.Approximately(corner[axis], rootCanvasRect.max[axis]))) {
                        outside = true;
                        break;
                    }
                }

                if (outside)
                    RectTransformUtility.FlipLayoutOnAxis(dropdownRectTransform, axis, false, false);
            }

            for (var i = 0; i < m_Items.Count; i++) {
                var itemRect = m_Items[i].RectTransform;
                itemRect.anchorMin = new Vector2(itemRect.anchorMin.x, 0);
                itemRect.anchorMax = new Vector2(itemRect.anchorMax.x, 0);
                itemRect.anchoredPosition = new Vector2(itemRect.anchoredPosition.x, offsetMin.y + itemSize.y * (m_Items.Count - 1 - i) + itemSize.y * itemRect.pivot.y);
                itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemSize.y);
            }

            // Fade in the popup
            AlphaFadeList(m_AlphaFadeSpeed, 0f, 1f);

            // Make drop-down template and item template inactive
            m_Template.gameObject.SetActive(false);
            itemTemplate.gameObject.SetActive(false);

            m_Blocker = CreateBlocker(rootCanvas);
        }

        private static bool IsEverythingValue(int count, int value) {
            var result = true;
            for (var i = 0; i < count; i++) {
                if ((value & 1 << i) == 0)
                    result = false;
            }

            return result;
        }

        private static int EverythingValue(int count) {
            var result = 0;
            for (var i = 0; i < count; i++) {
                result |= 1 << i;
            }

            return result;
        }

        /// <summary>
        /// Create a blocker that blocks clicks to other controls while the dropdown list is open.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain a blocker GameObject.
        /// </remarks>
        /// <param name="rootCanvas">The root canvas the dropdown is under.</param>
        /// <returns>The created blocker object</returns>
        protected virtual GameObject CreateBlocker(Canvas rootCanvas) {
            // Create blocker GameObject.
            var blocker = new GameObject("Blocker");

            // Set the game object layer to match the Canvas' game object layer, as not doing this can lead to issues
            // especially in XR applications like PolySpatial on VisionOS (UUM-62470).
            blocker.layer = rootCanvas.gameObject.layer;

            // Setup blocker RectTransform to cover entire root canvas area.
            var blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.SetParent(rootCanvas.transform, false);
            blockerRect.anchorMin = Vector3.zero;
            blockerRect.anchorMax = Vector3.one;
            blockerRect.sizeDelta = Vector2.zero;

            // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
            var blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            var dropdownCanvas = m_Dropdown!.GetComponent<Canvas>();
            blockerCanvas.sortingLayerID = dropdownCanvas.sortingLayerID;
            blockerCanvas.sortingOrder = dropdownCanvas.sortingOrder - 1;

            // Find the Canvas that this dropdown is a part of
            Canvas? parentCanvas = null;
            var parentTransform = m_Template.parent;
            while (parentTransform != null) {
                parentCanvas = parentTransform.GetComponent<Canvas>();
                if (parentCanvas != null)
                    break;

                parentTransform = parentTransform.parent;
            }

            // If we have a parent canvas, apply the same raycasters as the parent for consistency.
            if (parentCanvas != null) {
                var components = parentCanvas.GetComponents<BaseRaycaster>();
                for (var i = 0; i < components.Length; i++) {
                    var raycasterType = components[i].GetType();
                    if (blocker.GetComponent(raycasterType) == null) {
                        blocker.AddComponent(raycasterType);
                    }
                }
            } else {
                // Add raycaster since it's needed to block.
                GetOrAddComponent<GraphicRaycaster>(blocker);
            }

            // Add image since it's needed to block, but make it clear.
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = Color.clear;

            // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
            var blockerButton = blocker.AddComponent<Button>();
            blockerButton.onClick.AddListener(Hide);

            //add canvas group to ensure clicking outside the dropdown will hide it (UUM-33691)
            var blockerCanvasGroup = blocker.AddComponent<CanvasGroup>();
            blockerCanvasGroup.ignoreParentGroups = true;

            return blocker;
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated blocker object
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a blocker GameObject that blocks clicks to other controls while the dropdown list is open.
        /// </remarks>
        /// <param name="blocker">The blocker object to destroy.</param>
        protected virtual void DestroyBlocker(GameObject blocker) {
            Destroy(blocker);
        }

        /// <summary>
        /// Create the dropdown list to be shown when the dropdown is clicked. The dropdown list should correspond to the provided template GameObject, equivalent to instantiating a copy of it.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain a dropdown list GameObject.
        /// </remarks>
        /// <param name="template">The template to create the dropdown list from.</param>
        /// <returns>The created drop down list gameobject.</returns>
        protected virtual GameObject CreateDropdownList(GameObject template) {
            return Instantiate(template);
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated dropdown list
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a dropdown list GameObject.
        /// </remarks>
        /// <param name="dropdownList">The dropdown list GameObject to destroy</param>
        protected virtual void DestroyDropdownList(GameObject dropdownList) {
            Destroy(dropdownList);
        }

        /// <summary>
        /// Create a dropdown item based upon the item template.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain an option item.
        /// The option item should correspond to the provided template DropdownItem and its GameObject, equivalent to instantiating a copy of it.
        /// </remarks>
        /// <param name="itemTemplate">e template to create the option item from.</param>
        /// <returns>The created dropdown item component</returns>
        protected virtual DropdownItem CreateItem(DropdownItem itemTemplate) {
            return Instantiate(itemTemplate);
        }

        /// <summary>
        ///  Convenience method to explicitly destroy the previously generated Items.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of an option item.
        /// Likely no action needed since destroying the dropdown list destroys all contained items as well.
        /// </remarks>
        /// <param name="item">The Item to destroy.</param>
        protected virtual void DestroyItem(DropdownItem item) { }

        // Add a new drop-down list item with the specified values.
        private DropdownItem AddItem(int itemIndex, OptionData data, DropdownItem itemTemplate, List<DropdownItem> items) {
            // Add a new item to the dropdown.
            var item = CreateItem(itemTemplate);
            item.RectTransform.SetParent(itemTemplate.RectTransform.parent, false);

            item.gameObject.SetActive(true);
            item.gameObject.name = "Item " + items.Count + (data.Text != null ? ": " + data.Text : "");

            if (item.Toggle != null) {
                item.Toggle.isOn = false;
            }

            // Set the item's data
            if (item.Text)
                item.Text.text = data.Text;

            if (item.Image) {
                item.Image.sprite = data.Image;
                item.Image.color = data.Color;
                item.Image.enabled = item.Image.sprite != null && data.Color.a > 0;
            }

            items.Add(item);
            ItemInitialized?.Invoke(itemIndex, item);
            return item;
        }

        private void AlphaFadeList(float duration, float alpha) {
            var group = m_Dropdown!.GetComponent<CanvasGroup>();
            AlphaFadeList(duration, group.alpha, alpha);
        }

        private void AlphaFadeList(float duration, float start, float end) {
            if (end.Equals(start))
                return;

            m_DropdownFadeable?.AnimateAlpha(start, end, duration);
        }

        private void SetAlpha(float alpha) {
            if (!m_Dropdown)
                return;

            var group = m_Dropdown.GetComponent<CanvasGroup>();
            group.alpha = alpha;
        }

        /// <summary>
        /// Hide the dropdown list. I.e. close it.
        /// </summary>
        public void Hide() {
            if (m_Coroutine == null) {
                if (m_Dropdown != null) {
                    AlphaFadeList(m_AlphaFadeSpeed, 0f);

                    // User could have disabled the dropdown during the OnValueChanged call.
                    if (IsActive())
                        m_Coroutine = StartCoroutine(DelayedDestroyDropdownList(m_AlphaFadeSpeed));
                }

                if (m_Blocker != null)
                    DestroyBlocker(m_Blocker);

                m_Blocker = null;
                Select();
            }
        }

        private IEnumerator DelayedDestroyDropdownList(float delay) {
            yield return new WaitForSecondsRealtime(delay);
            ImmediateDestroyDropdownList();
        }

        private void ImmediateDestroyDropdownList() {
            for (var i = 0; i < m_Items.Count; i++) {
                if (m_Items[i] != null)
                    DestroyItem(m_Items[i]);
            }

            m_Items.Clear();

            if (m_Dropdown != null)
                DestroyDropdownList(m_Dropdown);

            if (m_DropdownFadeable != null)
                m_DropdownFadeable.CancelAlphaAnimation();

            m_Dropdown = null;
            m_Coroutine = null;
        }

        // Change the value and hide the dropdown.
        private void OnSelectItem(Toggle toggle) {
            var selectedIndex = -1;
            var tr = toggle.transform;
            var parent = tr.parent;
            for (var i = 1; i < parent.childCount; i++) {
                if (parent.GetChild(i) == tr) {
                    // Subtract one to account for template child.
                    selectedIndex = i - 1;
                    break;
                }
            }

            if (selectedIndex < 0)
                return;

            if (m_MultiSelect) {
                switch (selectedIndex) {
                    case 0: // Nothing
                        Value = 0;
                        for (var i = 3; i < parent.childCount; i++) {
                            var toggleComponent = parent.GetChild(i).GetComponentInChildren<Toggle>();
                            if (toggleComponent)
                                toggleComponent.SetIsOnWithoutNotify(false);
                        }

                        toggle.isOn = true;
                        break;
                    case 1: // Everything
                        Value = EverythingValue(Options.Count);
                        for (var i = 3; i < parent.childCount; i++) {
                            var toggleComponent = parent.GetChild(i).GetComponentInChildren<Toggle>();
                            if (toggleComponent)
                                toggleComponent.SetIsOnWithoutNotify(i > 2);
                        }

                        break;
                    default:
                        var flagValue = 1 << (selectedIndex - 2);
                        var wasSelected = (Value & flagValue) != 0;
                        toggle.SetIsOnWithoutNotify(!wasSelected);

                        if (wasSelected)
                            Value &= ~flagValue;
                        else
                            Value |= flagValue;

                        break;
                }
            } else {
                if (!toggle.isOn)
                    toggle.SetIsOnWithoutNotify(true);

                Value = selectedIndex;
            }

            Hide();
        }

        private static int FirstActiveFlagIndex(int value) {
            if (value == 0)
                return 0;

            const int bits = sizeof(int) * 8;
            for (var i = 0; i < bits; i++)
                if ((value & 1 << i) != 0)
                    return i;

            return 0;
        }
    }
}
