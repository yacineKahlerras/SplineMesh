using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperPlane : MonoBehaviour
{
    public SplineComponent spline;
    public float speed = 1f;
    float moveAmmount;
    float maxMoveAmmount;
    public Vector3 rotationOffset;
    public float startPosition;

    private void Start()
    {
        maxMoveAmmount = spline.GetLength();
        moveAmmount = startPosition;
    }

    private void Update()
    {
        moveAmmount = (moveAmmount + (Time.deltaTime * speed)) % maxMoveAmmount;
        if (moveAmmount >= 1) moveAmmount = 0;
        transform.position = spline.Hermite(moveAmmount);
        transform.rotation = Quaternion.LookRotation(spline.GetTangent(moveAmmount));
        transform.Rotate(rotationOffset);
    }
}
