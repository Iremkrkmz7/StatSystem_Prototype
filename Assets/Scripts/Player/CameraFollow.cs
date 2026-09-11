using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 10, -8);
    [SerializeField] float smoothSpeed = 5f;

    // Diger scriptler (orn. giris dusme animasyonu) gecici olarak offset'i
    // degistirip sonra eski haline dondurebilsin diye disariya aciyoruz.
    public Vector3 Offset { get => offset; set => offset = value; }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    // Lerp beklemeden kamerayi aninda hedef+offset pozisyonuna tasir - ani
    // buyuk pozisyon degisikliklerinde (orn. karakter birden yukari isinlaninca)
    // kameranin "yetismeye calisip" sicramasini onlemek icin kullanilir.
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
