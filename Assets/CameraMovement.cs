using UnityEngine;
using DG.Tweening;
using UnityEngine.TextCore.Text;
public class CameraMovement : MonoBehaviour
{
    public Transform character; // Reference to your character
    public Transform finalLookTarget; // The point in front of the character where the camera should stop
    public float moveDuration = 3f; // Total time for the camera motion
    public float rotateDuration = 3f; // Time for smooth rotation

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TweenCamera();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [ContextMenu("Tween")]
    public void TweenCamera()
    {
        Vector3 startPosition = transform.position;
        Vector3 midPoint = (startPosition + character.position) / 2 + Vector3.up * 3; // Raise it a bit for a curve
        Vector3 endPosition = finalLookTarget.position; // Camera stops in front of character

        Vector3[] path = new Vector3[] { startPosition, midPoint, endPosition };

        // Move along path with a curve
        transform.DOPath(path, moveDuration, PathType.CatmullRom)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                // Ensure the camera perfectly faces the finalLookTarget at the end
                transform.DOLookAt(character.position, 3f);
            });

        // Rotate camera gradually WHILE moving
        transform.DORotateQuaternion(Quaternion.LookRotation(character.position/* - transform.position*//* - transform.position*/), rotateDuration)
            .SetEase(Ease.InOutSine);
    }
}
