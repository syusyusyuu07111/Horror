using UnityEngine;

public class LightSway : MonoBehaviour
{
    public float amplitude = 2f; // “x
    public float speed = 0.5f;

    Quaternion baseRot; float t;

    void Start() { baseRot = transform.rotation; }

    void Update()
    {
        t += Time.deltaTime * speed;
        float ax = Mathf.Sin(t) * amplitude;
        float az = Mathf.Cos(t * 0.7f) * amplitude * 0.7f;
        transform.rotation = baseRot * Quaternion.Euler(ax, 0f, az);
    }
}
