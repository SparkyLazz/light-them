using UnityEngine;

public class MainMenuCameraDrift : MonoBehaviour
{
    [Header("Drift Settings")]
    [SerializeField] private float driftAmount = 0.3f;
    [SerializeField] private float driftSpeed = 0.3f;

    private Vector3 _basePosition;
    private float _noiseOffsetX;
    private float _noiseOffsetY;

    private void Start()
    {
        _basePosition = transform.position;

        // Random offsets so each axis drifts independently
        _noiseOffsetX = Random.Range(0f, 100f);
        _noiseOffsetY = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time * driftSpeed;

        float x = (Mathf.PerlinNoise(t, _noiseOffsetX) - 0.5f) * 2f * driftAmount;
        float y = (Mathf.PerlinNoise(_noiseOffsetY, t) - 0.5f) * 2f * driftAmount;

        transform.position = _basePosition + new Vector3(x, y, 0f);
    }
}
