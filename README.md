# Free-Fire
Việc lập trình một trò chơi Battle Royale quy mô như Free Fire là một dự án khổng lồ, đòi hỏi sự kết hợp của nhiều hệ thống: nhân vật, vũ khí, bản đồ, mạng (multiplayer), và đồ họa. ​Dưới đây là một bản hướng dẫn chi tiết và mã nguồn mẫu mô phỏng các hệ thống cốt lõi (Máu, Sát thương, Vũ khí)
Chi tiết về các thành phần UI cần thiết (Giao diện)
Để game trông giống Free Fire, bạn cần thiết lập các Layer UI sau trong Canvas của Lobby:
Thành phần Chức năng
Start Button Kích hoạt hàm StartMatch().
Character Preview Một vùng trống có Camera riêng để soi model nhân vật.
Currency UI Hiển thị Vàng/Kim cương (Dùng PlayerPrefs để lưu số dư).
Weapon Tab Nơi chọn skin súng (tương tự như cách chọn nhân vật).
