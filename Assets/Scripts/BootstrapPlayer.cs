using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class BootstrapPlayer : NetworkBehaviour
{
    private Rigidbody rb;

    public TextMeshPro textMesh;

    public LineRenderer lr;

    public float moveSpeed;

    public LayerMask layerMask;

    public Transform prefab;

    private NetworkVariable<float> hp = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //hp.Value -= 10;
            //SpawnServerRpc(transform.position);
        }
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");

        rb.velocity = new Vector3(h, 0, v) * moveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        hp.OnValueChanged += OnHpChanged;

        if (IsOwner)
        {
            FindFirstObjectByType<CinemachineCamera>().Follow = transform;
        }
    }

    private void Attack()
    {
        var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(mouseRay, out var enter))
        {
            var mousePoint = mouseRay.GetPoint(enter);
            mousePoint.y = transform.position.y;

            LineCast(mousePoint);

            var directionToMouse = (mousePoint - transform.position).normalized;
            var ray = new Ray(transform.position + directionToMouse, directionToMouse);

            CastRayServerRpc(ray, mousePoint);
        }
    }

    private async void LineCast(Vector3 position)
    {
        lr.SetPosition(0, transform.position);
        lr.enabled = true;
        lr.SetPosition(1, position);
        await Awaitable.WaitForSecondsAsync(0.1f);
        lr.enabled = false;
    }

    private void OnHpChanged(float prevValue, float newValue)
    {
        textMesh.text = newValue.ToString();
    }

    [ClientRpc]
    public void LineCastClientRpc(Vector3 position)
    {
        if (IsOwner) return;

        LineCast(position);
    }

    [ClientRpc]
    public void SetPositionClientRpc(Vector3 position)
    {
        rb.position = position;
    }

    [ServerRpc]
    public void CastRayServerRpc(Ray ray, Vector3 rayEnd)
    {
        if (Physics.Raycast(ray, out var hit, 100, layerMask))
        {
            LineCastClientRpc(hit.point);

            if (hit.collider.TryGetComponent<BootstrapPlayer>(out var player))
            {
                player.hp.Value -= 10;

                if (player.hp.Value <= 0)
                {
                    player.SetPositionClientRpc(Vector3.zero);
                    player.hp.Value = 100;
                }
            }
        }
        else
        {
            LineCastClientRpc(rayEnd);
        }
    }

    [ServerRpc]
    public void SpawnServerRpc(Vector3 position)
    {
        var obj = Instantiate(prefab);
        obj.transform.position = position;
        obj.GetComponent<NetworkObject>().Spawn(true);
    }
}