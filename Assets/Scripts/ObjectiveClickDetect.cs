using UnityEngine;
using UnityEngine.UI;

public class ObjectiveClickDetect : MonoBehaviour
{
    public Camera cam;
    int score = 0;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.CompareTag("ObjectivePOI"))
                {
                    score += 1;
                    Debug.Log("Score: "+score);
                }
            }
            else
            {
                Debug.Log("Nothing.");
            }
        }
    }
}
