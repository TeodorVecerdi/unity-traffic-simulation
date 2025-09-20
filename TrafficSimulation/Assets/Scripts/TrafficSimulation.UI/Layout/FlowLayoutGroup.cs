using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI.Layout;

/// <summary>
/// Layout Group controller that arranges children in bars, fitting as many on a line until total size exceeds parent bounds
/// </summary>
public class FlowLayoutGroup : LayoutGroup {
    public enum Axis {
        Horizontal = 0,
        Vertical = 1,
    }

    private float m_LayoutHeight;
    private float m_LayoutWidth;

    [SerializeField] private float m_SpacingX;
    [SerializeField] private float m_SpacingY;
    [SerializeField] private bool m_ExpandHorizontalSpacing;
    [SerializeField] private bool m_ChildForceExpandWidth;
    [SerializeField] private bool m_ChildForceExpandHeight;
    [SerializeField] private bool m_InvertOrder;

    [SerializeField] private Axis m_StartAxis = Axis.Horizontal;

    public Axis StartAxis {
        get => m_StartAxis;
        set => SetProperty(ref m_StartAxis, value);
    }

    public override void CalculateLayoutInputHorizontal() {
        if (StartAxis == Axis.Horizontal) {
            base.CalculateLayoutInputHorizontal();
            var minimumWidth = GetGreatestMinimumChildWidth() + padding.left + padding.right;
            SetLayoutInputForAxis(minimumWidth, -1, -1, 0);
        } else {
            m_LayoutWidth = SetLayout(0, true);
        }
    }

    public override void SetLayoutHorizontal() {
        SetLayout(0, false);
    }

    public override void SetLayoutVertical() {
        SetLayout(1, false);
    }

    public override void CalculateLayoutInputVertical() {
        if (StartAxis == Axis.Horizontal) {
            m_LayoutHeight = SetLayout(1, true);
        } else {
            base.CalculateLayoutInputHorizontal();
            var minimumHeight = GetGreatestMinimumChildHeight() + padding.bottom + padding.top;
            SetLayoutInputForAxis(minimumHeight, -1, -1, 1);
        }
    }

    private bool IsCenterAlign => childAlignment is TextAnchor.LowerCenter or TextAnchor.MiddleCenter or TextAnchor.UpperCenter;

    private bool IsRightAlign => childAlignment is TextAnchor.LowerRight or TextAnchor.MiddleRight or TextAnchor.UpperRight;

    private bool IsMiddleAlign => childAlignment is TextAnchor.MiddleLeft or TextAnchor.MiddleRight or TextAnchor.MiddleCenter;

    private bool IsLowerAlign => childAlignment is TextAnchor.LowerLeft or TextAnchor.LowerRight or TextAnchor.LowerCenter;

    /// <summary>
    /// Holds the rects that will make up the current bar being processed
    /// </summary>
    private readonly IList<RectTransform> m_ItemList = new List<RectTransform>();

    /// <summary>
    /// Main layout method
    /// </summary>
    /// <param name="axis">0 for horizontal axis, 1 for vertical</param>
    /// <param name="layoutInput">If true, sets the layout input for the axis. If false, sets child position for axis</param>
    public float SetLayout(int axis, bool layoutInput) {
        //container height and width
        var groupHeight = rectTransform.rect.height;
        var groupWidth = rectTransform.rect.width;

        float spacingBetweenBars = 0;
        float spacingBetweenElements = 0;
        float offset = 0;
        float counterOffset = 0;
        float groupSize = 0;
        float workingSize = 0;
        if (StartAxis == Axis.Horizontal) {
            groupSize = groupHeight;
            workingSize = groupWidth - padding.left - padding.right;
            if (IsLowerAlign) {
                offset = padding.bottom;
                counterOffset = padding.top;
            } else {
                offset = padding.top;
                counterOffset = padding.bottom;
            }

            spacingBetweenBars = m_SpacingY;
            spacingBetweenElements = m_SpacingX;
        } else if (StartAxis == Axis.Vertical) {
            groupSize = groupWidth;
            workingSize = groupHeight - padding.top - padding.bottom;
            if (IsRightAlign) {
                offset = padding.right;
                counterOffset = padding.left;
            } else {
                offset = padding.left;
                counterOffset = padding.right;
            }

            spacingBetweenBars = m_SpacingX;
            spacingBetweenElements = m_SpacingY;
        }

        var currentBarSize = 0f;
        var currentBarSpace = 0f;

        for (var i = 0; i < rectChildren.Count; i++) {
            var index = i;
            var child = rectChildren[index];
            float childSize = 0;
            float childOtherSize = 0;
            //get height and width of elements.
            if (StartAxis == Axis.Horizontal) {
                if (m_InvertOrder) {
                    index = IsLowerAlign ? rectChildren.Count - 1 - i : i;
                }

                child = rectChildren[index];
                childSize = LayoutUtility.GetPreferredSize(child, 0);
                childSize = Mathf.Min(childSize, workingSize);
                childOtherSize = LayoutUtility.GetPreferredSize(child, 1);
            } else if (StartAxis == Axis.Vertical) {
                if (m_InvertOrder) {
                    index = IsRightAlign ? rectChildren.Count - 1 - i : i;
                }

                child = rectChildren[index];
                childSize = LayoutUtility.GetPreferredSize(child, 1);
                childSize = Mathf.Min(childSize, workingSize);
                childOtherSize = LayoutUtility.GetPreferredSize(child, 0);
            }

            // If adding this element would exceed the bounds of the container,
            // go to a new bar after processing the current bar
            if (currentBarSize + childSize > workingSize) {
                currentBarSize -= spacingBetweenElements;

                // Process current bar elements positioning
                if (!layoutInput) {
                    if (StartAxis == Axis.Horizontal) {
                        var newOffset = CalculateRowVerticalOffset(groupSize, offset, currentBarSpace);
                        LayoutRow(m_ItemList, currentBarSize, currentBarSpace, workingSize, padding.left, newOffset, axis);
                    } else if (StartAxis == Axis.Vertical) {
                        var newOffset = CalculateColHorizontalOffset(groupSize, offset, currentBarSpace);
                        LayoutCol(m_ItemList, currentBarSpace, currentBarSize, workingSize, newOffset, padding.top, axis);
                    }
                }

                // Clear existing bar
                m_ItemList.Clear();

                // Add the current bar space to total barSpace accumulator, and reset to 0 for the next row
                offset += currentBarSpace;
                offset += spacingBetweenBars;

                currentBarSpace = 0;
                currentBarSize = 0;
            }

            currentBarSize += childSize;
            m_ItemList.Add(child);

            // We need the largest element height to determine the starting position of the next line
            currentBarSpace = childOtherSize > currentBarSpace ? childOtherSize : currentBarSpace;

            currentBarSize += spacingBetweenElements;
        }

        // Layout the final bar
        if (!layoutInput) {
            if (StartAxis == Axis.Horizontal) {
                var newOffset = CalculateRowVerticalOffset(groupHeight, offset, currentBarSpace);
                currentBarSize -= spacingBetweenElements;
                LayoutRow(m_ItemList, currentBarSize, currentBarSpace, workingSize, padding.left, newOffset, axis);
            } else if (StartAxis == Axis.Vertical) {
                var newOffset = CalculateColHorizontalOffset(groupWidth, offset, currentBarSpace);
                currentBarSize -= spacingBetweenElements;
                LayoutCol(m_ItemList, currentBarSpace, currentBarSize, workingSize, newOffset, padding.top, axis);
            }
        }

        m_ItemList.Clear();

        // Add the last bar space to the barSpace accumulator
        offset += currentBarSpace;
        offset += counterOffset;

        if (layoutInput) {
            SetLayoutInputForAxis(offset, offset, -1, axis);
        }

        return offset;
    }

    private float CalculateRowVerticalOffset(float groupHeight, float yOffset, float currentRowHeight) {
        if (IsLowerAlign) {
            return groupHeight - yOffset - currentRowHeight;
        }

        if (IsMiddleAlign) {
            return groupHeight * 0.5f - m_LayoutHeight * 0.5f + yOffset;
        }

        return yOffset;
    }

    private float CalculateColHorizontalOffset(float groupWidth, float xOffset, float currentColWidth) {
        if (IsRightAlign) {
            return groupWidth - xOffset - currentColWidth;
        }

        if (IsCenterAlign) {
            return groupWidth * 0.5f - m_LayoutWidth * 0.5f + xOffset;
        }

        return xOffset;
    }

    private void LayoutRow(IList<RectTransform> contents, float rowWidth, float rowHeight, float maxWidth, float xOffset, float yOffset, int axis) {
        var xPos = xOffset;

        if (!m_ChildForceExpandWidth && IsCenterAlign) {
            xPos += (maxWidth - rowWidth) * 0.5f;
        } else if (!m_ChildForceExpandWidth && IsRightAlign) {
            xPos += maxWidth - rowWidth;
        }

        var extraWidth = 0f;
        var extraSpacing = 0f;

        if (m_ChildForceExpandWidth) {
            extraWidth = (maxWidth - rowWidth) / contents.Count;
        } else if (m_ExpandHorizontalSpacing) {
            extraSpacing = (maxWidth - rowWidth) / (contents.Count - 1);
            if (contents.Count > 1) {
                if (IsCenterAlign) {
                    xPos -= extraSpacing * 0.5f * (contents.Count - 1);
                } else if (IsRightAlign) {
                    xPos -= extraSpacing * (contents.Count - 1);
                }
            }
        }

        for (var j = 0; j < contents.Count; j++) {
            var index = IsLowerAlign ? contents.Count - 1 - j : j;

            var rowChild = contents[index];

            var rowChildWidth = LayoutUtility.GetPreferredSize(rowChild, 0) + extraWidth;
            var rowChildHeight = LayoutUtility.GetPreferredSize(rowChild, 1);

            if (m_ChildForceExpandHeight) {
                rowChildHeight = rowHeight;
            }

            rowChildWidth = Mathf.Min(rowChildWidth, maxWidth);
            var yPos = yOffset;

            if (IsMiddleAlign) {
                yPos += (rowHeight - rowChildHeight) * 0.5f;
            } else if (IsLowerAlign) {
                yPos += rowHeight - rowChildHeight;
            }

            if (m_ExpandHorizontalSpacing && j > 0) {
                xPos += extraSpacing;
            }

            if (axis == 0) {
                SetChildAlongAxis(rowChild, 0, xPos, rowChildWidth);
            } else {
                SetChildAlongAxis(rowChild, 1, yPos, rowChildHeight);
            }

            // Don't do horizontal spacing for the last one
            if (j < contents.Count - 1) {
                xPos += rowChildWidth + m_SpacingX;
            }
        }
    }

    private void LayoutCol(IList<RectTransform> contents, float colWidth, float colHeight, float maxHeight, float xOffset, float yOffset, int axis) {
        var yPos = yOffset;

        if (!m_ChildForceExpandHeight && IsMiddleAlign) {
            yPos += (maxHeight - colHeight) * 0.5f;
        } else if (!m_ChildForceExpandHeight && IsLowerAlign) {
            yPos += maxHeight - colHeight;
        }

        var extraHeight = 0f;
        var extraSpacing = 0f;

        if (m_ChildForceExpandHeight) {
            extraHeight = (maxHeight - colHeight) / contents.Count;
        } else if (m_ExpandHorizontalSpacing) {
            extraSpacing = (maxHeight - colHeight) / (contents.Count - 1);
            if (contents.Count > 1) {
                if (IsMiddleAlign) {
                    yPos -= extraSpacing * 0.5f * (contents.Count - 1);
                } else if (IsLowerAlign) {
                    yPos -= extraSpacing * (contents.Count - 1);
                }
            }
        }

        for (var j = 0; j < contents.Count; j++) {
            var index = IsRightAlign ? contents.Count - 1 - j : j;

            var rowChild = contents[index];

            var rowChildWidth = LayoutUtility.GetPreferredSize(rowChild, 0);
            var rowChildHeight = LayoutUtility.GetPreferredSize(rowChild, 1) + extraHeight;

            if (m_ChildForceExpandWidth) {
                rowChildWidth = colWidth;
            }

            rowChildHeight = Mathf.Min(rowChildHeight, maxHeight);

            var xPos = xOffset;

            if (IsCenterAlign) {
                xPos += (colWidth - rowChildWidth) * 0.5f;
            } else if (IsRightAlign) {
                xPos += colWidth - rowChildWidth;
            }

            if (m_ExpandHorizontalSpacing && j > 0) {
                yPos += extraSpacing;
            }

            if (axis == 0) {
                SetChildAlongAxis(rowChild, 0, xPos, rowChildWidth);
            } else {
                SetChildAlongAxis(rowChild, 1, yPos, rowChildHeight);
            }

            // Don't do vertical spacing for the last one
            if (j < contents.Count - 1) {
                yPos += rowChildHeight + m_SpacingY;
            }
        }
    }

    public float GetGreatestMinimumChildWidth() {
        var max = 0f;
        for (var i = 0; i < rectChildren.Count; i++) {
            var w = LayoutUtility.GetMinWidth(rectChildren[i]);
            max = Mathf.Max(w, max);
        }

        return max;
    }

    public float GetGreatestMinimumChildHeight() {
        var max = 0f;
        foreach (var child in rectChildren) {
            max = Mathf.Max(LayoutUtility.GetMinHeight(child), max);
        }

        return max;
    }

    protected override void OnDisable() {
        m_Tracker.Clear();
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}
