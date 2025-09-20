using UnityEngine;
using UnityEngine.UI;

namespace TrafficSimulation.UI;

public sealed partial class UIImage {
    /// <summary>
    /// Gets or sets the Sprite associated with the image component.
    /// </summary>
    public Sprite? Sprite {
        get => Image.sprite;
        set => Image.sprite = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="Image"/> can be a target for raycasting.
    /// Raycasting allows the <see cref="Image"/> to block input events, such as mouse clicks or touch events.
    /// When set to false, input events will pass through the image.
    /// </summary>
    public bool RaycastTarget {
        get => Image.raycastTarget;
        set => Image.raycastTarget = value;
    }

    /// <summary>
    /// Controls the padding applied for the raycasting area of the image.
    /// This property expands or contracts the clickable or interactable area
    /// of the image for UI interactions, adjusting by the specified padding values.
    /// </summary>
    public Vector4 RaycastPadding {
        get => Image.raycastPadding;
        set => Image.raycastPadding = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the image can be masked by a parent Mask component.
    /// </summary>
    public bool Maskable {
        get => Image.maskable;
        set => Image.maskable = value;
    }

    /// <summary>
    /// Gets or sets the type of the image, which defines how the image is drawn (e.g., Simple, Sliced, Tiled, or Filled).
    /// </summary>
    public Image.Type ImageType {
        get => Image.type;
        set => Image.type = value;
    }

    /// <summary>
    /// Gets or sets the method used to determine how the image is filled.
    /// </summary>
    public Image.FillMethod FillMethod {
        get => Image.fillMethod;
        set => Image.fillMethod = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the aspect ratio of the image will be preserved
    /// when the component is resized.
    /// </summary>
    public bool PreserveAspect {
        get => Image.preserveAspect;
        set => Image.preserveAspect = value;
    }

    /// <summary>
    /// Determines whether the center of the image should be filled when using the Sliced or Tiled image type in the UI.
    /// </summary>
    public bool FillCenter {
        get => Image.fillCenter;
        set => Image.fillCenter = value;
    }

    /// <summary>
    /// Gets or sets the fill amount of the image.
    /// Controls how much of the Image's sprite is visible, ranging from 0 (empty) to 1 (full).
    /// Commonly used in radial or horizontal fill types to represent progress or completion.
    /// </summary>
    public float FillAmount {
        get => Image.fillAmount;
        set => Image.fillAmount = value;
    }

    /// <summary>
    /// Determines the direction in which the fill of the image progresses when
    /// using a fill method such as radial or linear.
    /// </summary>
    public bool FillClockwise {
        get => Image.fillClockwise;
        set => Image.fillClockwise = value;
    }

    /// <summary>
    /// Gets or sets the origin for the image fill process when using a filled Image.Type.
    /// Determines the starting point of the fill, such as bottom, top, left, right, or corners.
    /// This is used in conjunction with the FillMethod to define the fill behavior of the image.
    /// </summary>
    public int FillOrigin {
        get => Image.fillOrigin;
        set => Image.fillOrigin = value;
    }

    /// Gets or sets a value indicating whether this image uses the sprite mesh for rendering.
    /// When set to true, the image will utilize the sprite's mesh data instead of generating
    /// a rectangular mesh for the sprite. This is useful for sprites with custom meshes that
    /// define unique shapes or boundaries. If the sprite does not define a custom mesh, this
    /// property may have no visible effect.
    public bool UseSpriteMesh {
        get => Image.useSpriteMesh;
        set => Image.useSpriteMesh = value;
    }

    /// <summary>
    /// Gets or sets the pixels-per-unit multiplier for the image.
    /// This value scales the pixels-per-unit value of the associated sprite, allowing for size
    /// adjustments without modifying the sprite itself.
    /// </summary>
    public float PixelsPerUnitMultiplier {
        get => Image.pixelsPerUnitMultiplier;
        set => Image.pixelsPerUnitMultiplier = value;
    }
}
