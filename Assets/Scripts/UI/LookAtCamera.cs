using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;;

    }
    void LateUpdate()
    {
        transform.rotation = Camera.main.transform.rotation;
    }
}
