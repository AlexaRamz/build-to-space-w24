using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Transform target;
    public float minValueY;

    private void Start()
    {
        target = GameObject.Find("Player").transform;
    }
    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = new Vector3(target.position.x, Mathf.Clamp(target.position.y, minValueY, Mathf.Infinity), transform.position.z);
        }
    }
}
