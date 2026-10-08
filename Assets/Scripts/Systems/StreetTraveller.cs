using UnityEngine;

// Sokakta bir hat boyunca ilerleyen görsel yolcu (araç ya da figüran): hedefe düz gider, varınca kendini yok eder.
// Görünüşü ayrı bileşenlerdedir (StreetCar, StreetPedestrian) ve hızı buradan okur.
public class StreetTraveller : MonoBehaviour
{
    public float Speed { get; private set; }

    private Vector3 _destination;
    private bool _moving;

    public void Begin(Vector3 destination, float speed)
    {
        _destination = destination;
        Speed = speed;
        _moving = true;
    }

    private void Update()
    {
        if (!_moving)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _destination, Speed * Time.deltaTime);
        if ((transform.position - _destination).sqrMagnitude < 0.0001f)
            Destroy(gameObject);
    }
}
