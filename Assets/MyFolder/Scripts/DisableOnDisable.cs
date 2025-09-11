using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugEx;

public class DisableOnDisable : MonoBehaviour
{
   private void OnDisable()
   {
      gameObject.SetActive(false);
   }
}
