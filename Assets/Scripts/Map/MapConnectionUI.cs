using UnityEngine;
using UnityEngine.UI;

public class MapConnectionUI : MonoBehaviour
{
    [SerializeField] private Image connectionLine;
    private CosmicMapNodeArt sourceArt;
    private Image routeDiamond;
    private bool lastHighlighted;

    private void Update()
    {
        if (sourceArt == null || connectionLine == null) return;
        bool highlighted = sourceArt.IsCurrent;
        if (highlighted == lastHighlighted) return;
        lastHighlighted = highlighted;
        connectionLine.color = highlighted
            ? new Color(.69f, .40f, .90f, .95f)
            : new Color(.30f, .17f, .40f, .70f);
        RectTransform line = connectionLine.rectTransform;
        line.sizeDelta = new Vector2(line.sizeDelta.x, highlighted ? 2f : 1.3f);
        if (routeDiamond != null)
        {
            routeDiamond.color = connectionLine.color;
            routeDiamond.rectTransform.sizeDelta = Vector2.one * (highlighted ? 6f : 4f);
        }
    }

    public void Setup(
        RectTransform startNode,
        RectTransform endNode)
    {
        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("Map Connection Node is null");
            return;
        }

        if (connectionLine == null)
        {
            Debug.LogError("Connection Line Image is not assigned");
            return;
        }

        connectionLine.raycastTarget = false;
        CosmicMapNodeArt startArt = startNode.GetComponent<CosmicMapNodeArt>();
        bool highlighted = startArt != null && startArt.IsCurrent;
        sourceArt = startArt;
        lastHighlighted = highlighted;
        connectionLine.color = highlighted
            ? new Color(.69f, .40f, .90f, .95f)
            : new Color(.30f, .17f, .40f, .70f);
        Outline glow = connectionLine.GetComponent<Outline>();
        if (glow == null)
        {
            glow = connectionLine.gameObject.AddComponent<Outline>();
        }
        glow.effectColor = new Color(0.52f, 0.30f, 0.72f, 0.30f);
        glow.effectDistance = new Vector2(1.5f, -1.5f);
        glow.useGraphicAlpha = true;

        RectTransform connectionRect = connectionLine.rectTransform;
        Transform lineParent = connectionRect.parent;
        Vector3 startPosition = lineParent.InverseTransformPoint(startNode.position);
        Vector3 endPosition = lineParent.InverseTransformPoint(endNode.position);
        Vector3 axis = (endPosition - startPosition).normalized;
        startPosition += axis * startNode.rect.width * .39f;
        endPosition -= axis * endNode.rect.width * .39f;

        Vector3 direction =
            endPosition - startPosition;

        float distance =
            direction.magnitude;

        connectionRect.anchorMin = connectionRect.anchorMax = new Vector2(.5f, .5f);
        connectionRect.pivot = new Vector2(0f, .5f);
        connectionRect.anchoredPosition = startPosition;
        connectionRect.localRotation =
            Quaternion.FromToRotation(
                Vector3.right,
                direction
            );

        connectionRect.sizeDelta =
            new Vector2(
                distance,
                highlighted ? 2f : 1.3f
            );

        GameObject waypoint = new GameObject("RouteDiamond", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        waypoint.transform.SetParent(connectionRect, false);
        RectTransform diamond = waypoint.GetComponent<RectTransform>();
        diamond.anchorMin = diamond.anchorMax = new Vector2(.5f, .5f);
        diamond.sizeDelta = new Vector2(highlighted ? 6f : 4f, highlighted ? 6f : 4f);
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
        waypoint.GetComponent<Image>().color = connectionLine.color;
        waypoint.GetComponent<Image>().raycastTarget = false;
        routeDiamond = waypoint.GetComponent<Image>();
    }
}
