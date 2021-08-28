using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Nigga : MonoBehaviour
{
    public SplineComponent spline;
    public float speed = 1f;
    public float moveAmmount;
    public float maxMoveAmmount;

    private void Start()
    {
        maxMoveAmmount = spline.GetLength();
    }

    private void Update()
    {
        moveAmmount = (moveAmmount + (Time.deltaTime * speed)) % maxMoveAmmount;
        if (moveAmmount >= 1) moveAmmount = 0;
        transform.position = (Vector2)spline.GetPoint(moveAmmount);
    }
}
