using UnityEngine;
using DG.Tweening;

public class UITweener : MonoBehaviour
{
    public RectTransform firstBackground;  // Assign the 1st Background in the Inspector
    public RectTransform secondBackground; // Assign the 2nd Background in the Inspector

    private Vector3 minScaleFirst = new Vector3(3f, 3f, 3f); // Minimum scale for 1st Background
    private Vector3 maxScaleFirst = new Vector3(4.5f, 4.5f, 4.5f); // Maximum scale for 1st Background

    private Vector3 minScaleSecond = new Vector3(3f, 3f, 3f); // Minimum scale for 2nd Background
    private Vector3 maxScaleSecond = new Vector3(4.5f, 4.5f, 4.5f); // Maximum scale for 2nd Background

    private float duration = 2f; // Time to complete one cycle

    public RectTransform targetImage; // Assign the image RectTransform in the Inspector

    private Vector3 initialPosition;
    public float floatStrength = 10f; // Strength of floating movement
    public float floatDuration = 2f;  // Time for one float cycle

    public float circleRadius = 5f;   // Radius for circular motion
    public float circleDuration = 3f; // Time for one full circle
    void Start()
    {
        StartTweening();

        if (targetImage != null)
        {
            initialPosition = targetImage.anchoredPosition;
            StartFloating();
            //StartCircularMotion(); // Uncomment if you want a circular motion instead
        }
    }

    void StartTweening()
    {
        // Ensure initial scales
        firstBackground.localScale = minScaleFirst;
        secondBackground.localScale = maxScaleSecond;

        // Tween animations for infinite looping
        AnimateBackgrounds();
    }

    void AnimateBackgrounds()
    {
        Sequence sequence = DOTween.Sequence();

        sequence.Append(firstBackground.DOScale(maxScaleFirst, duration).SetEase(Ease.InOutSine));
        sequence.Join(secondBackground.DOScale(minScaleSecond, duration).SetEase(Ease.InOutSine));

        sequence.Append(firstBackground.DOScale(minScaleFirst, duration).SetEase(Ease.InOutSine));
        sequence.Join(secondBackground.DOScale(maxScaleSecond, duration).SetEase(Ease.InOutSine));

        sequence.SetLoops(-1, LoopType.Yoyo); // Infinite looping
    }

    public void StartFloating()
    {
        targetImage.DOAnchorPosY(initialPosition.y + floatStrength, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StartCircularMotion()
    {
        Sequence circleSequence = DOTween.Sequence();

        circleSequence.Append(targetImage.DOAnchorPos(initialPosition + new Vector3(circleRadius, 0, 0), circleDuration / 4).SetEase(Ease.Linear));
        circleSequence.Append(targetImage.DOAnchorPos(initialPosition + new Vector3(0, circleRadius, 0), circleDuration / 4).SetEase(Ease.Linear));
        circleSequence.Append(targetImage.DOAnchorPos(initialPosition + new Vector3(-circleRadius, 0, 0), circleDuration / 4).SetEase(Ease.Linear));
        circleSequence.Append(targetImage.DOAnchorPos(initialPosition + new Vector3(0, -circleRadius, 0), circleDuration / 4).SetEase(Ease.Linear));

        circleSequence.SetLoops(-1, LoopType.Restart);
    }
}
