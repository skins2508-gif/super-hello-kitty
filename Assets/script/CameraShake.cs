using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Coroutine shakeRoutine;
    private Vector3 restPosition;

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            transform.localPosition = restPosition;
        }

        restPosition = transform.localPosition;
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float fade = 1f - elapsed / Mathf.Max(0.01f, duration);
            Vector2 offset = Random.insideUnitCircle * strength * fade;
            transform.localPosition = restPosition + new Vector3(offset.x, offset.y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = restPosition;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
            transform.localPosition = restPosition;
    }
}
