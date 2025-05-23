using Player;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] Vector3 offset;

    // Update is called once per frame
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, PlayerController.Instance.transform.position + offset, followSpeed);
    }
}
