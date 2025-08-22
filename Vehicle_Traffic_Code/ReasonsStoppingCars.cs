using UnityEngine;

public class ReasonsStoppingCars : MonoBehaviour
{
    public static void CarInView(GameObject go, Rigidbody rigbody, float distance, float startSpeed, ref float moveSpeed, ref bool tempStop, float distanceToCar = 15)
    {
        if (go == null) return;

        CarAIController car = go.GetComponent<CarAIController>();
        if (car == null) return;

        if (distance >= distanceToCar)
        {
            moveSpeed = car.TEMP_STOP ? startSpeed * 0.5f : startSpeed;
            tempStop = false;
        }
        else if (distance < distanceToCar)
        {
            if (car.GetComponent<Rigidbody>().velocity.magnitude < rigbody.velocity.magnitude)
            {
                tempStop = true;
            }
            else
            {
                tempStop = !car.TEMP_STOP ? false : tempStop;
            }
        }
    }

    public static void SemaphoreInView(SemaphorePeople semaphore, float distance, float startSpeed, bool insideSemaphore, ref float moveSpeed, ref bool tempStop, float stopDistance = 10)
    {
        if (semaphore == null) return;

        // 🔴 If the car is outside the intersection and the light is red
        if (!semaphore.CAR_CAN && !insideSemaphore && distance < stopDistance)
        {
            tempStop = true;
            moveSpeed = 0;
            return;
        }

        // 🟡 If light is flickering and car is approaching
        if (semaphore.FLICKER && !insideSemaphore && distance < stopDistance)
        {
            tempStop = true;
            moveSpeed = 0;
            return;
        }

        // 🟢 If CAR_CAN is true and car is inside or very close
        if (semaphore.CAR_CAN)
        {
            tempStop = false;
            moveSpeed = startSpeed;
            return;
        }

        // 🔒 Default: reduce speed if near but not allowed
        if (distance < stopDistance && !insideSemaphore)
        {
            moveSpeed = startSpeed * 0.5f;
        }
        else
        {
            moveSpeed = startSpeed;
            tempStop = false;
        }
    }

    public static void PlayerInView(Transform player, float distance, float startSpeed, ref float moveSpeed, ref bool tempStop)
    {
        if (distance >= 8.0f)
        {
            moveSpeed = startSpeed * 0.5f;
        }
        else
        {
            tempStop = true;
        }
    }

    public static void BcycleGyroInView(BcycleGyroController controller, Rigidbody rigbody, float distance, float startSpeed, ref float moveSpeed, ref bool tempStop)
    {
        if (controller == null) return;

        if (distance >= 9.0f)
        {
            moveSpeed = controller.tempStop ? startSpeed * 0.5f : startSpeed;
            tempStop = false;
        }
        else if (distance < 9.0f)
        {
            if (controller.GetComponent<Rigidbody>().velocity.magnitude < rigbody.velocity.magnitude)
            {
                tempStop = true;
            }
            else if (!controller.tempStop)
            {
                tempStop = false;
            }
        }
    }
}
