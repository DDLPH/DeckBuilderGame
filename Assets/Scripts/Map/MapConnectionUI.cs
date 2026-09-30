using UnityEngine;
using UnityEngine.UI;

public class MapConnectionUI : MonoBehaviour
{
    [SerializeField] private Image connectionLine;

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

        Vector3 startPosition = startNode.position;
        Vector3 endPosition = endNode.position;

        Vector3 direction =
            endPosition - startPosition;

        float distance =
            direction.magnitude;

        RectTransform connectionRect =
            connectionLine.rectTransform;

        connectionRect.position =
            startPosition;

        connectionRect.rotation =
            Quaternion.FromToRotation(
                Vector3.right,
                direction
            );

        connectionRect.sizeDelta =
            new Vector2(
                distance,
                connectionRect.sizeDelta.y
            );
    }
}