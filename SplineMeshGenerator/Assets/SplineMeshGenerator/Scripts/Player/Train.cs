using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Train : MonoBehaviour
{
    public SplineComponent spline;
    public float speed = 1f;
    float moveAmmount;
    float maxMoveAmmount;
    public Transform head;

    private void Start()
    {
        maxMoveAmmount = spline.GetLength();
    }

    private void Update()
    {
        moveAmmount = (moveAmmount + (Time.deltaTime * speed)) % maxMoveAmmount;
        if (moveAmmount >= 1) moveAmmount = 0;
        transform.position = spline.Hermite(moveAmmount);
        head.rotation = Quaternion.LookRotation(spline.GetTangent(moveAmmount));
    }
}
