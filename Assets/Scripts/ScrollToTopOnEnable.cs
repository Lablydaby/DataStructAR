using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Resets a scroll view to the top each time it is shown, after any same-frame button actions have run
[RequireComponent(typeof(ScrollRect))]
public class ScrollToTopOnEnable : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(ScrollToTop());
    }

    private IEnumerator ScrollToTop()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
    }
}
