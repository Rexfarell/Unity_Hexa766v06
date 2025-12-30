using UnityEngine;
using System.Collections;

public class UniversalClickDebugger : MonoBehaviour
{
    [Header("DEBUG SETTINGS")]
    public bool showAllHits = true;
    public bool flashOnHit = true;
    public float flashDuration = 0.3f;
    public Color flashColor = Color.red;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DebugClick();
        }
    }

    void DebugClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);

        // Sort by distance (closest first)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (hits.Length > 0)
        {
            Debug.Log($"✦ RAYCAST HIT: {hits.Length} objects | Mouse: {Input.mousePosition:F0}");

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                Renderer rend = hit.collider.GetComponent<Renderer>();
                PlayerClickFeedback feedback = hit.collider.GetComponent<PlayerClickFeedback>();
                ClickableSphere sphereFeedback = hit.collider.GetComponent<ClickableSphere>();

                string feedbackType = "None";
                if (feedback != null) feedbackType = "PlayerClickFeedback";
                else if (sphereFeedback != null) feedbackType = "ClickableSphere";

                Debug.Log($"  [{i:00}] {hit.collider.gameObject.name,-20} | Dist: {hit.distance:F2}m | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer),-12} | Trigger: {hit.collider.isTrigger,-5} | Renderer: {rend != null,-7} | Feedback: {feedbackType,-20}");

                
            }
        }
        else
        {
            Debug.Log($"✗ NO HIT - Raycast missed everything");
            Debug.Log($"   Mouse: {Input.mousePosition:F0} | Cam Pos: {Camera.main.transform.position:F1} | Cam Fwd: {Camera.main.transform.forward:F2}");
        }
    }

    IEnumerator FlashObject(Renderer renderer, Color flashColor, float duration)
    {
        if (renderer == null || renderer.material == null) yield break;

        Color originalColor = renderer.material.color;
        renderer.material.color = flashColor;

        yield return new WaitForSeconds(duration);

        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = originalColor;
        }
    }

    // BONUS: Hover debug (press H to toggle)
    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.H)
        {
            showAllHits = !showAllHits;
            Debug.Log($"[DEBUG] All hits: {showAllHits}");
            Event.current.Use();
        }
    }
}