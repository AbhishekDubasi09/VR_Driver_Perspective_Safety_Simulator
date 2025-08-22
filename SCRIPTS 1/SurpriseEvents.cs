using UnityEngine;

public static class SurpriseEvents
{
    public static void SpawnPedestrian(Vector3 near)
    {
        GameObject prefab = Resources.Load<GameObject>("PedestrianPrefab");
        Object.Instantiate(prefab, near + Vector3.left * 3, Quaternion.identity);
    }

    public static void SpawnCutInCar(Vector3 near)
    {
        GameObject prefab = Resources.Load<GameObject>("CutInCarPrefab");
        Object.Instantiate(prefab, near + Vector3.forward * 10, Quaternion.Euler(0, -45, 0));
    }

    public static void SpawnAnimal(Vector3 near)
    {
        GameObject prefab = Resources.Load<GameObject>("DogPrefab");
        Object.Instantiate(prefab, near + Vector3.right * 4, Quaternion.identity);
    }

    public static void PlayHornNearPlayer()
    {
        GameObject horn = new GameObject("Horn");
        AudioSource a = horn.AddComponent<AudioSource>();
        a.clip = Resources.Load<AudioClip>("HornSFX");
        a.Play();
        Object.Destroy(horn, 2f);
    }

    public static void BurstLightFlash(Vector3 pos)
    {
        GameObject lightGO = new GameObject("Flash");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = Color.white;
        light.intensity = 50f;
        light.range = 10f;
        light.spotAngle = 80f;
        lightGO.transform.position = pos + Vector3.up * 2;
        Object.Destroy(lightGO, 0.3f);
    }
}
