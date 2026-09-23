namespace HTools.Windows.Screenshot;

/// <summary>The annotation tools available on the capture overlay's toolbar, matching the tool set
/// (minus the reference project's "line" being oddly ordered last — grouped sensibly with the other
/// shape tools here instead) from the H_Assistant reference screenshot module.</summary>
public enum AnnotationTool
{
    None,
    Rectangle,
    RectangleFilled,
    Ellipse,
    EllipseFilled,
    Line,
    Arrow,
    Freehand,
    Text,
    Mosaic,
}
