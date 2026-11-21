/****************
20227234
Bùi Phạm Quang Huy
****************/

using System;
using System.Collections.Generic;

namespace CampusSafetyHub
{
    // =========================================================================
    // 1. HỢP ĐỒNG CẢM BIẾN & DỮ LIỆU CẢNH BÁO
    // =========================================================================

    /// <summary>
    /// Giao diện chuẩn mà module giám sát sử dụng để đọc giá trị cảm biến.
    /// </summary>
    public interface ISensor
    {
        double ReadValue();
        string GetUnit();
    }

    /// <summary>
    /// Đối tượng dữ liệu bất biến (immutable) mô tả một sự kiện cảnh báo.
    /// </summary>
    public sealed record AlertEvent(
        string SensorName,
        double Value,
        string Unit,
        string Severity,
        DateTime OccurredAt);

    // =========================================================================
    // 2. ADAPTER PATTERN: TÍCH HỢP CẢM BIẾN KHÓI CŨ
    // =========================================================================

    /// <summary>
    /// Lớp cảm biến khói có sẵn (0..1000), giả định không được sửa đổi.
    /// </summary>
    public sealed class LegacySmokeDetector
    {
        private int smokeLevel;

        public LegacySmokeDetector(int smokeLevel)
        {
            this.smokeLevel = Math.Clamp(smokeLevel, 0, 1000);
        }

        public int GetSmokeLevel() => smokeLevel; // Thang 0..1000
        public string GetDescription() => "Legacy smoke detector";

        public void SetSmokeLevel(int value)
        {
            smokeLevel = Math.Clamp(value, 0, 1000);
        }
    }

    /// <summary>
    /// Adapter chuyển đổi LegacySmokeDetector sang chuẩn ISensor (thang 0..100, đơn vị "% smoke").
    /// </summary>
    public sealed class SmokeSensorAdapter : ISensor
    {
        private readonly LegacySmokeDetector legacy;

        public SmokeSensorAdapter(LegacySmokeDetector legacy)
        {
            this.legacy = legacy ?? throw new ArgumentNullException(nameof(legacy));
        }

        public double ReadValue()
        {
            // Chuyển thang 0..1000 về phần trăm 0..100
            return legacy.GetSmokeLevel() / 10.0;
        }

        public string GetUnit() => "% smoke";
    }

    // =========================================================================
    // 3. OBSERVER PATTERN: PHÁT VÀ NHẬN CẢNH BÁO
    // =========================================================================

    /// <summary>
    /// Hợp đồng dành cho các thành phần muốn nhận thông báo cảnh báo.
    /// </summary>
    public interface IAlertObserver
    {
        void Update(AlertEvent alertEvent);
    }

    /// <summary>
    /// Subject giám sát cảm biến, kiểm tra ngưỡng và thông báo cho các Observer.
    /// </summary>
    public sealed class SensorMonitor
    {
        private readonly List<IAlertObserver> observers = new();

        public void Attach(IAlertObserver observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public void Detach(IAlertObserver observer)
        {
            if (observer is not null)
            {
                observers.Remove(observer);
            }
        }

        public void CheckSensor(string sensorName, ISensor sensor)
        {
            ArgumentNullException.ThrowIfNull(sensor);

            double value = sensor.ReadValue();
            string? severity = value switch
            {
                >= 70 => "CRITICAL",
                >= 50 => "WARNING",
                _ => null
            };

            if (severity is null)
            {
                return;
            }

            var alertEvent = new AlertEvent(
                sensorName,
                value,
                sensor.GetUnit(),
                severity,
                DateTime.Now);

            Notify(alertEvent);
        }

        private void Notify(AlertEvent alertEvent)
        {
            // Duyệt trên bản sao ToArray() để tránh lỗi khi Attach/Detach trong lúc Notify
            foreach (IAlertObserver observer in observers.ToArray())
            {
                try
                {
                    observer.Update(alertEvent);
                }
                catch (Exception ex)
                {
                    // Cô lập lỗi: một observer lỗi không làm ảnh hưởng các observer khác
                    Console.WriteLine($"[MONITOR ERROR] Observer '{observer.GetType().Name}' failed: {ex.Message}");
                }
            }
        }
    }

    public sealed class DashboardObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            Console.WriteLine(
                $"[DASHBOARD] {alertEvent.SensorName}: {alertEvent.Value:F1}{alertEvent.Unit} [{alertEvent.Severity}]");
        }
    }

    public sealed class EmailAlertObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            Console.WriteLine(
                $"[EMAIL ALERT] Gửi cảnh báo {alertEvent.Severity} cho {alertEvent.SensorName} ({alertEvent.Value:F1}{alertEvent.Unit}) lúc {alertEvent.OccurredAt:HH:mm:ss}");
        }
    }

    public sealed class AuditLogObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            Console.WriteLine(
                $"[AUDIT LOG] {alertEvent.OccurredAt:yyyy-MM-dd HH:mm:ss} | {alertEvent.SensorName} | {alertEvent.Value:F1} {alertEvent.Unit} | {alertEvent.Severity}");
        }
    }

    // =========================================================================
    // 4. MEDIATOR PATTERN + BRIDGE OBSERVER: ĐIỀU PHỐI ỨNG PHÓ KHẨN CẤP
    // =========================================================================

    /// <summary>
    /// Hợp đồng điều phối các thiết bị ứng phó khẩn cấp.
    /// </summary>
    public interface IEmergencyMediator
    {
        void Notify(object sender, string eventCode, AlertEvent? alertEvent = null);
    }

    public sealed class Alarm
    {
        public void Activate(string reason)
        {
            Console.WriteLine($"[ALARM] Bật còi báo động khẩn cấp! Lý do: {reason}");
        }

        public void Deactivate()
        {
            Console.WriteLine("[ALARM] Tắt còi báo động.");
        }
    }

    public sealed class DoorController
    {
        public void UnlockEmergencyExits()
        {
            Console.WriteLine("[DOOR] Mở khóa toàn bộ cửa thoát hiểm phòng máy.");
        }

        public void LockFireDoors()
        {
            Console.WriteLine("[DOOR] Đóng/khóa cửa ngăn cháy khu vực.");
        }
    }

    public sealed class VentilationController
    {
        public void ShutdownHvac()
        {
            Console.WriteLine("[VENTILATION] Ngắt hệ thống thông gió/điều hòa để tránh lan khói.");
        }

        public void EnableSmokeExhaust()
        {
            Console.WriteLine("[VENTILATION] Kích hoạt chế độ hút khói khẩn cấp.");
        }
    }

    /// <summary>
    /// Mediator điều phối tập trung giữa Alarm, DoorController và VentilationController.
    /// </summary>
    public sealed class EmergencyMediator : IEmergencyMediator
    {
        private readonly Alarm alarm;
        private readonly DoorController doorController;
        private readonly VentilationController ventilationController;

        public EmergencyMediator(
            Alarm alarm,
            DoorController doorController,
            VentilationController ventilationController)
        {
            this.alarm = alarm ?? throw new ArgumentNullException(nameof(alarm));
            this.doorController = doorController ?? throw new ArgumentNullException(nameof(doorController));
            this.ventilationController = ventilationController ?? throw new ArgumentNullException(nameof(ventilationController));
        }

        public void Notify(object sender, string eventCode, AlertEvent? alertEvent = null)
        {
            if (eventCode == "FIRE_DETECTED")
            {
                string detail = alertEvent is not null
                    ? $"{alertEvent.SensorName} đạt {alertEvent.Value:F1}{alertEvent.Unit} ({alertEvent.Severity})"
                    : "Phát hiện nguy cơ hỏa hoạn";

                alarm.Activate(detail);
                doorController.UnlockEmergencyExits();
                ventilationController.ShutdownHvac();
            }
        }
    }

    /// <summary>
    /// Cầu nối từ Observer sang Mediator: khi nhận cảnh báo phù hợp (ví dụ CRITICAL), gửi sự kiện FIRE_DETECTED.
    /// </summary>
    public sealed class EmergencyBridgeObserver : IAlertObserver
    {
        private readonly IEmergencyMediator mediator;

        public EmergencyBridgeObserver(IEmergencyMediator mediator)
        {
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public void Update(AlertEvent alertEvent)
        {
            if (alertEvent.Severity == "CRITICAL")
            {
                mediator.Notify(this, "FIRE_DETECTED", alertEvent);
            }
        }
    }

    // =========================================================================
    // 5. CHƯƠNG TRÌNH CHÍNH & KIỂM THỬ KỊCH BẢN
    // =========================================================================

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Khởi tạo thiết bị ứng phó & Mediator
            var alarm = new Alarm();
            var doors = new DoorController();
            var ventilation = new VentilationController();
            IEmergencyMediator mediator = new EmergencyMediator(alarm, doors, ventilation);

            // Khởi tạo Monitor & các Observer
            var monitor = new SensorMonitor();
            var dashboard = new DashboardObserver();
            var email = new EmailAlertObserver();
            var audit = new AuditLogObserver();
            var bridge = new EmergencyBridgeObserver(mediator);

            monitor.Attach(dashboard);
            monitor.Attach(email);
            monitor.Attach(audit);
            monitor.Attach(bridge);
            monitor.Attach(dashboard); // Test không đăng ký trùng lặp

            // Tích hợp cảm biến cũ qua Adapter
            var legacySmoke = new LegacySmokeDetector(250);
            ISensor smokeSensor = new SmokeSensorAdapter(legacySmoke);

            Console.WriteLine("--- Kịch bản 1: Mức khói bình thường (250/1000 -> 25.0% smoke) ---");
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine("\n--- Kịch bản 2: Mức WARNING (580/1000 -> 58.0% smoke) ---");
            legacySmoke.SetSmokeLevel(580);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine("\n--- Kịch bản 3: Mức CRITICAL (820/1000 -> 82.0% smoke) ---");
            legacySmoke.SetSmokeLevel(820);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine("\n--- Kịch bản 4: Hủy đăng ký EmailAlertObserver & kiểm tra lại ---");
            monitor.Detach(email);
            legacySmoke.SetSmokeLevel(750);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);
        }
    }
}