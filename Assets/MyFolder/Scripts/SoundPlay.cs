using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugEx;

public class SoundPlay : MonoBehaviour
{
   public void PlayShot()
   {
      AudioManager.Instance.PlayOneShotAudio(AudioName.Take);
   }
}
