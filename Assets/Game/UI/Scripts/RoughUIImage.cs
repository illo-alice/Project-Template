using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

// Supplies atlas bounds and per-element hover state without copying the shared material.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/Rough UI Image")]
public class RoughUIImage : BaseMeshEffect, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool _overlayOnHover;

    private Selectable _selectable;
    private bool _pointerInside;
    private bool _overlayVisible;

    protected override void OnEnable()
    {
        _selectable = GetComponent<Selectable>();
        _pointerInside = false;
        _overlayVisible = false;
        base.OnEnable();
        EnableUVChannel();
    }

    protected override void OnDisable()
    {
        _pointerInside = false;
        _overlayVisible = false;
        base.OnDisable();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        RefreshHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        RefreshHover();
    }

    private void LateUpdate()
    {
        // Also handle interactability changes while the pointer stays over the button.
        if (_pointerInside || _overlayVisible)
            RefreshHover();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            return;

        _pointerInside = false;
        RefreshHover();
    }

    private void RefreshHover()
    {
        bool visible = isActiveAndEnabled && _pointerInside &&
            (_selectable == null || (_selectable.IsActive() && _selectable.IsInteractable()));
        if (_overlayVisible == visible)
            return;

        _overlayVisible = visible;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        EnableUVChannel();
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    private void EnableUVChannel()
    {
        if (graphic != null && graphic.canvas != null)
            graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 |
                                                       AdditionalCanvasShaderChannels.TexCoord2;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        // The Image may acquire its Canvas after this component's OnEnable callback.
        EnableUVChannel();
        var image = (Image)graphic;
        var sprite = image.overrideSprite;
        var bounds = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0, 0, 1, 1);
        var vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.uv1 = bounds;
            // A marker outside ordinary UVs distinguishes our data from default UI streams.
            vertex.uv2 = new Vector4(_overlayVisible ? 1 : 0, _overlayOnHover ? 2 : 0, 0, 0);
            vh.SetUIVertex(vertex, i);
        }
    }
}
