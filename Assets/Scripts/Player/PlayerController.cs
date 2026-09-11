using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(SCharacterStats))]
public class PlayerController : MonoBehaviour
{
  [Header("Kamera")]
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask groundLayerMask;

    CharacterController _cc;
    SCharacterStats _stats;
    Vector3 _velocity;
    const float Gravity = -18f;
    Animator _animator;


    void Awake()
    {
        _cc    = GetComponent<CharacterController>();
        _stats = GetComponent<SCharacterStats>();
        _animator = GetComponentInChildren<Animator>();
        // Bu iki metot tanimliydi ama hicbir yere abone edilmemisti - ne hasar
        // alma animasyonu ne olum animasyonu hic tetiklenmiyordu.
        _stats.OnHealthChanged += OnHealthChanged;
        _stats.OnDied += OnDied;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (_stats.IsDead) return;
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        float speed = _stats.GetStat(StatType.Speed);
        _cc.Move(dir * speed * Time.deltaTime);

       _animator?.SetBool("isRunning", dir.magnitude > 0.1f);


        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += Gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }
    void OnHealthChanged(float current, float max)
{
    _animator?.SetTrigger("TakeDamage");
}

void OnDied()
{
    _animator?.SetTrigger("Death");
}

    void HandleRotation()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}