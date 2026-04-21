using UnityEngine;

public class TrainingTarget : MonoBehaviour
{
    public float moveRange = 5f;
    public float moveSpeed = 2f;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    void Start()
    {
        startPosition = transform.localPosition;
        SetRandomTarget();
    }

    void Update()
    {
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.localPosition, targetPosition) < 0.1f)
        {
            SetRandomTarget();
        }
    }

    private void SetRandomTarget()
    {
        targetPosition = startPosition + new Vector3(
            Random.Range(-moveRange, moveRange),
            Random.Range(-moveRange, moveRange),
            0
        );
    }

    public void ResetPosition()
    {
        transform.localPosition = startPosition;
        SetRandomTarget();
    }
}
