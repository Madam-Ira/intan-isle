using System.Collections;
using CesiumForUnity;
using UnityEngine;

/// <summary>
/// Waits one frame so CesiumGeoreference can finish its own Awake(),
/// then re-enables CesiumGlobeAnchor so it finds its parent georeference.
/// Fixes: "PlayerRig is not nested inside a game object with a CesiumGeoreference"
/// warning that fires when CesiumGlobeAnchor.OnEnable() runs before Cesium
/// has initialised its scene-level systems.
/// </summary>
public class CesiumAnchorRestarter : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null; // one frame — lets CesiumGeoreference complete Awake()

        var anchor = GetComponent<CesiumGlobeAnchor>();
        if (anchor != null)
        {
            anchor.enabled = false;
            anchor.enabled = true;  // triggers OnEnable → Restart() with Cesium ready
            Debug.Log("[CesiumAnchorRestarter] CesiumGlobeAnchor restarted successfully.");
        }
    }
}
