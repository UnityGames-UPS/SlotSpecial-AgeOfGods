using UnityEngine;

public class WheelStopper : MonoBehaviour
{
  [SerializeField] private AudioController audioController;
  [SerializeField] private WheelController wheelController;

  void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("WheelValue") && wheelController.canPlayAudio)
    {
      audioController.Play("Wheel");
    }
  }
}
