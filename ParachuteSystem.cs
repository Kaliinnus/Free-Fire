using UnityEngine;
using UnityEngine.UI;

public class ParachuteSystem : MonoBehaviour
{
    public enum FlightState { OnPlane, FreeFall, Parachuting, Landed }
    public FlightState currentState = FlightState.OnPlane;

    [Header("Cấu hình tốc độ")]
    public float planeSpeed = 50f;
    public float freeFallSpeed = 40f;      // Tốc độ rơi cực nhanh khi chưa bung dù
    public float parachuteSpeed = 10f;     // Tốc độ rơi chậm khi đã bung dù
    public float steerSpeed = 15f;         // Tốc độ lượn sang trái/phải

    [Header("Tham chiếu")]
    public GameObject parachuteModel;      // Model cái dù (ẩn/hiện)
    public Transform plane;                // Máy bay
    public Text altitudeText;              // Hiển thị độ cao

    private CharacterController controller;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        parachuteModel.SetActive(false);
        // Ban đầu người chơi đi theo máy bay
        transform.SetParent(plane); 
    }

    void Update()
    {
        switch (currentState)
        {
            case FlightState.OnPlane:
                HandlePlaneFlight();
                break;
            case FlightState.FreeFall:
                HandleFreeFall();
                break;
            case FlightState.Parachuting:
                HandleParachuting();
                break;
            case FlightState.Landed:
                // Chuyển sang script điều khiển nhân vật bình thường
                GetComponent<FreeFireMini>().enabled = true;
                this.enabled = false;
                break;
        }

        UpdateUI();
    }

    // 1. Giai đoạn trên máy bay
    void HandlePlaneFlight()
    {
        if (Input.GetKeyDown(KeyCode.F)) // Nhấn F để nhảy
        {
            JumpFromPlane();
        }
    }

    void JumpFromPlane()
    {
        transform.SetParent(null); // Rời khỏi máy bay
        currentState = FlightState.FreeFall;
        Debug.Log("Đang nhảy tự do!");
    }

    // 2. Giai đoạn rơi tự do
    void HandleFreeFall()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Di chuyển hướng rơi và lượn
        moveDirection = new Vector3(h * steerSpeed, -freeFallSpeed, v * steerSpeed);
        controller.Move(moveDirection * Time.deltaTime);

        // Nhấn Space hoặc xuống độ cao nhất định thì bung dù
        if (Input.GetKeyDown(KeyCode.Space) || transform.position.y < 100f)
        {
            OpenParachute();
        }
    }

    // 3. Giai đoạn bung dù
    void OpenParachute()
    {
        currentState = FlightState.Parachuting;
        parachuteModel.SetActive(true);
        Debug.Log("Đã bung dù!");
    }

    void HandleParachuting()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Rơi chậm hơn và có thể lượn xa hơn
        moveDirection = new Vector3(h * steerSpeed, -parachuteSpeed, v * steerSpeed);
        controller.Move(moveDirection * Time.deltaTime);

        // Kiểm tra chạm đất
        if (controller.isGrounded)
        {
            Land();
        }
    }

    void Land()
    {
        currentState = FlightState.Landed;
        parachuteModel.SetActive(false);
        Debug.Log("Đã chạm đất an toàn. Bắt đầu loot đồ!");
    }

    void UpdateUI()
    {
        if (altitudeText != null)
        {
            altitudeText.text = "Độ cao: " + (int)transform.position.y + "m";
        }
    }
}
