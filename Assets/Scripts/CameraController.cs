using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   [SerializeField] private GameObject target;

   private void LateUpdate()
   {
      Vector3 targetPos = target.transform.position;
      transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
   }
}
