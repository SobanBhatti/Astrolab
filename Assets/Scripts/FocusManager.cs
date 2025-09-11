using UnityEngine;

public class FocusManager : MonoBehaviour
{
    public CameraController cameraController;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Venstreklikk
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CelestialBody body = hit.collider.GetComponent<CelestialBody>();
                if (body != null)
                {
                    FocusOn(body);
                }
            }
        }
    }

    void FocusOn(CelestialBody body)
    {
        Debug.Log("Focusing on: " + body.bodyName);

        // Flytt kamera-rig til objektets posisjon
        cameraController.transform.position = body.transform.position;

        // Nullstill rotasjon slik at kamera peker mot objektet
        cameraController.ResetView();
    }
}