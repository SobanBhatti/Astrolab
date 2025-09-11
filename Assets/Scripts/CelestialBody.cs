using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    public string bodyName = "Unnamed Star";
    public float radius = 1f; // i solradier
    public float luminosity = 1f; // i sol-luminositeter
    public Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }
}