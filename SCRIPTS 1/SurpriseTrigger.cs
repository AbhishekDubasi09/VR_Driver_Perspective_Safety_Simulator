using UnityEngine;

public class SurpriseTrigger : MonoBehaviour
{
    public enum SurpriseType { Pedestrian, Car, Animal, Horn, Flashlight }

    public SurpriseType surprise;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (surprise)
        {
            case SurpriseType.Pedestrian:
                SurpriseEvents.SpawnPedestrian(transform.position);
                break;
            case SurpriseType.Car:
                SurpriseEvents.SpawnCutInCar(transform.position);
                break;
            case SurpriseType.Animal:
                SurpriseEvents.SpawnAnimal(transform.position);
                break;
            case SurpriseType.Horn:
                SurpriseEvents.PlayHornNearPlayer();
                break;
            case SurpriseType.Flashlight:
                SurpriseEvents.BurstLightFlash(transform.position);
                break;
        }

        Destroy(gameObject); // One-time use
    }
}
