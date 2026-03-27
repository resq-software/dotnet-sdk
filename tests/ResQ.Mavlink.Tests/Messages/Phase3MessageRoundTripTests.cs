/**
 * Copyright 2026 ResQ
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using FluentAssertions;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Tests.Messages;

/// <summary>
/// Parameterized round-trip tests for all Phase 3 messages: serialize then deserialize via MessageRegistry.
/// </summary>
public sealed class Phase3MessageRoundTripTests
{
    private static void RoundTrip(IMavlinkMessage message, int payloadSize)
    {
        var buf = new byte[payloadSize];
        message.Serialize(buf);
        var ok = MessageRegistry.TryDeserialize(message.MessageId, buf, out var result);
        ok.Should().BeTrue($"message ID {message.MessageId} must be registered");
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(message.MessageId);
    }

    [Fact]
    public void MissionRequestList_RoundTrips()
    {
        var msg = new MissionRequestList { TargetSystem = 1, TargetComponent = 1 };
        RoundTrip(msg, MissionRequestList.PayloadSize);
    }

    [Fact]
    public void MissionClearAll_RoundTrips()
    {
        var msg = new MissionClearAll { TargetSystem = 1, TargetComponent = 1 };
        RoundTrip(msg, MissionClearAll.PayloadSize);
    }

    [Fact]
    public void MissionSetCurrent_RoundTrips()
    {
        var msg = new MissionSetCurrent { Seq = 3, TargetSystem = 1, TargetComponent = 1 };
        var buf = new byte[MissionSetCurrent.PayloadSize];
        msg.Serialize(buf);
        var parsed = MissionSetCurrent.Deserialize(buf);
        parsed.Seq.Should().Be(3);
        parsed.TargetSystem.Should().Be(1);
    }

    [Fact]
    public void ScaledImu_RoundTrips()
    {
        var msg = new ScaledImu { TimeBootMs = 1000, Xacc = 100, Yacc = -50, Zacc = 980, Xgyro = 1, Ygyro = 2, Zgyro = 3, Xmag = 100, Ymag = 200, Zmag = 50 };
        var buf = new byte[ScaledImu.PayloadSize];
        msg.Serialize(buf);
        var parsed = ScaledImu.Deserialize(buf);
        parsed.TimeBootMs.Should().Be(1000);
        parsed.Xacc.Should().Be(100);
        parsed.Zmag.Should().Be(50);
    }

    [Fact]
    public void ScaledImu2_RoundTrips()
    {
        var msg = new ScaledImu2 { TimeBootMs = 2000, Xacc = 200, Zacc = 970 };
        var buf = new byte[ScaledImu2.PayloadSize];
        msg.Serialize(buf);
        var parsed = ScaledImu2.Deserialize(buf);
        parsed.Xacc.Should().Be(200);
        parsed.Zacc.Should().Be(970);
    }

    [Fact]
    public void ScaledPressure_RoundTrips()
    {
        var msg = new ScaledPressure { TimeBootMs = 5000, PressAbs = 1013.25f, PressDiff = 0.5f, Temperature = 2500 };
        var buf = new byte[ScaledPressure.PayloadSize];
        msg.Serialize(buf);
        var parsed = ScaledPressure.Deserialize(buf);
        parsed.PressAbs.Should().BeApproximately(1013.25f, 1e-3f);
        parsed.Temperature.Should().Be(2500);
    }

    [Fact]
    public void RawImu_RoundTrips()
    {
        var msg = new RawImu { TimeUsec = 123456789, Xacc = 100, Yacc = 200, Zacc = 980 };
        var buf = new byte[RawImu.PayloadSize];
        msg.Serialize(buf);
        var parsed = RawImu.Deserialize(buf);
        parsed.TimeUsec.Should().Be(123456789);
        parsed.Zacc.Should().Be(980);
    }

    [Fact]
    public void GpsGlobalOrigin_RoundTrips()
    {
        var msg = new GpsGlobalOrigin { Latitude = 473977418, Longitude = 85255792, Altitude = 408000 };
        var buf = new byte[GpsGlobalOrigin.PayloadSize];
        msg.Serialize(buf);
        var parsed = GpsGlobalOrigin.Deserialize(buf);
        parsed.Latitude.Should().Be(473977418);
        parsed.Altitude.Should().Be(408000);
    }

    [Fact]
    public void Ping_RoundTrips()
    {
        var msg = new Ping { TimeUsec = 9999999, Seq = 42, TargetSystem = 1, TargetComponent = 0 };
        var buf = new byte[Ping.PayloadSize];
        msg.Serialize(buf);
        var parsed = Ping.Deserialize(buf);
        parsed.Seq.Should().Be(42);
        parsed.TimeUsec.Should().Be(9999999);
    }

    [Fact]
    public void SystemTime_RoundTrips()
    {
        var msg = new SystemTime { TimeUnixUsec = 1700000000000000, TimeBootMs = 60000 };
        var buf = new byte[SystemTime.PayloadSize];
        msg.Serialize(buf);
        var parsed = SystemTime.Deserialize(buf);
        parsed.TimeUnixUsec.Should().Be(1700000000000000);
        parsed.TimeBootMs.Should().Be(60000);
    }

    [Fact]
    public void Timesync_RoundTrips()
    {
        var msg = new Timesync { Tc1 = 123456789012345, Ts1 = 987654321098765 };
        var buf = new byte[Timesync.PayloadSize];
        msg.Serialize(buf);
        var parsed = Timesync.Deserialize(buf);
        parsed.Tc1.Should().Be(123456789012345);
        parsed.Ts1.Should().Be(987654321098765);
    }

    [Fact]
    public void BatteryStatus_RoundTrips()
    {
        var msg = new BatteryStatus
        {
            Id = 0,
            BatteryFunction = 1,
            Type = 0,
            Temperature = 2800,
            Voltages0 = 16800,
            CurrentBattery = 1500,
            CurrentConsumed = 350,
            EnergyConsumed = 1260,
            BatteryRemaining = 80,
        };
        var buf = new byte[BatteryStatus.PayloadSize];
        msg.Serialize(buf);
        var parsed = BatteryStatus.Deserialize(buf);
        parsed.Voltages0.Should().Be(16800);
        parsed.BatteryRemaining.Should().Be(80);
        parsed.CurrentConsumed.Should().Be(350);
    }

    [Fact]
    public void RadioStatus_RoundTrips()
    {
        var msg = new RadioStatus { Rssi = 200, Remrssi = 190, Txbuf = 100, Noise = 50, Remnoise = 45, Rxerrors = 0, Fixed = 0 };
        var buf = new byte[RadioStatus.PayloadSize];
        msg.Serialize(buf);
        var parsed = RadioStatus.Deserialize(buf);
        parsed.Rssi.Should().Be(200);
        parsed.Txbuf.Should().Be(100);
    }

    [Fact]
    public void LocalPositionNed_RoundTrips()
    {
        var msg = new LocalPositionNed { TimeBootMs = 100, X = 1.5f, Y = 2.5f, Z = -30.0f, Vx = 0.1f, Vy = 0.0f, Vz = -0.5f };
        var buf = new byte[LocalPositionNed.PayloadSize];
        msg.Serialize(buf);
        var parsed = LocalPositionNed.Deserialize(buf);
        parsed.Z.Should().BeApproximately(-30.0f, 1e-5f);
        parsed.Vz.Should().BeApproximately(-0.5f, 1e-5f);
    }

    [Fact]
    public void NavControllerOutput_RoundTrips()
    {
        var msg = new NavControllerOutput { NavRoll = 5f, NavPitch = -3f, AltError = 2f, AspdError = 0.5f, XtrackError = 10f, NavBearing = 270, TargetBearing = 260, WpDist = 150 };
        var buf = new byte[NavControllerOutput.PayloadSize];
        msg.Serialize(buf);
        var parsed = NavControllerOutput.Deserialize(buf);
        parsed.NavBearing.Should().Be(270);
        parsed.WpDist.Should().Be(150);
    }

    [Fact]
    public void AttitudeQuaternion_RoundTrips()
    {
        var msg = new AttitudeQuaternion { TimeBootMs = 200, Q1 = 1f, Q2 = 0f, Q3 = 0f, Q4 = 0f, Rollspeed = 0.01f, Pitchspeed = 0.02f, Yawspeed = 0.0f };
        var buf = new byte[AttitudeQuaternion.PayloadSize];
        msg.Serialize(buf);
        var parsed = AttitudeQuaternion.Deserialize(buf);
        parsed.Q1.Should().BeApproximately(1f, 1e-6f);
        parsed.Rollspeed.Should().BeApproximately(0.01f, 1e-6f);
    }

    [Fact]
    public void AttitudeTarget_RoundTrips()
    {
        var msg = new AttitudeTarget { TimeBootMs = 300, Q1 = 1f, Q2 = 0f, Q3 = 0f, Q4 = 0f, BodyRollRate = 0f, BodyPitchRate = 0f, BodyYawRate = 0.1f, Thrust = 0.7f, TypeMask = 0 };
        var buf = new byte[AttitudeTarget.PayloadSize];
        msg.Serialize(buf);
        var parsed = AttitudeTarget.Deserialize(buf);
        parsed.Thrust.Should().BeApproximately(0.7f, 1e-6f);
        parsed.BodyYawRate.Should().BeApproximately(0.1f, 1e-6f);
    }

    [Fact]
    public void Vibration_RoundTrips()
    {
        var msg = new Vibration { TimeUsec = 99999, VibrationX = 0.1f, VibrationY = 0.2f, VibrationZ = 0.3f, Clipping0 = 0, Clipping1 = 0, Clipping2 = 0 };
        var buf = new byte[Vibration.PayloadSize];
        msg.Serialize(buf);
        var parsed = Vibration.Deserialize(buf);
        parsed.VibrationZ.Should().BeApproximately(0.3f, 1e-6f);
    }

    [Fact]
    public void EstimatorStatus_RoundTrips()
    {
        var msg = new EstimatorStatus { TimeUsec = 111111, Flags = 0b11111111, PosHorizAccuracy = 1.5f, PosVertAccuracy = 2.0f };
        var buf = new byte[EstimatorStatus.PayloadSize];
        msg.Serialize(buf);
        var parsed = EstimatorStatus.Deserialize(buf);
        parsed.Flags.Should().Be(0b11111111);
        parsed.PosHorizAccuracy.Should().BeApproximately(1.5f, 1e-5f);
    }

    [Fact]
    public void WindCov_RoundTrips()
    {
        var msg = new WindCov { TimeUsec = 55555, WindX = 3f, WindY = 1.5f, WindZ = 0f, VarHoriz = 0.5f, VarVert = 0.1f, WindAlt = 100f, HorizAccuracy = 0.5f, VertAccuracy = 0.2f };
        var buf = new byte[WindCov.PayloadSize];
        msg.Serialize(buf);
        var parsed = WindCov.Deserialize(buf);
        parsed.WindX.Should().BeApproximately(3f, 1e-5f);
        parsed.WindAlt.Should().BeApproximately(100f, 1e-5f);
    }

    [Fact]
    public void TerrainRequest_RoundTrips()
    {
        var msg = new TerrainRequest { Lat = 473977418, Lon = 85255792, GridSpacing = 100, Mask = 0xFFFFFFFF };
        var buf = new byte[TerrainRequest.PayloadSize];
        msg.Serialize(buf);
        var parsed = TerrainRequest.Deserialize(buf);
        parsed.Lat.Should().Be(473977418);
        parsed.GridSpacing.Should().Be(100);
    }

    [Fact]
    public void TerrainReport_RoundTrips()
    {
        var msg = new TerrainReport { Lat = 473977418, Lon = 85255792, TerrainHeight = 408f, CurrentHeight = 50f, Spacing = 100, Pending = 0, Loaded = 10 };
        var buf = new byte[TerrainReport.PayloadSize];
        msg.Serialize(buf);
        var parsed = TerrainReport.Deserialize(buf);
        parsed.TerrainHeight.Should().BeApproximately(408f, 1e-3f);
        parsed.Loaded.Should().Be(10);
    }

    [Fact]
    public void TerrainCheck_RoundTrips()
    {
        var msg = new TerrainCheck { Lat = 473977418, Lon = 85255792 };
        var buf = new byte[TerrainCheck.PayloadSize];
        msg.Serialize(buf);
        var parsed = TerrainCheck.Deserialize(buf);
        parsed.Lat.Should().Be(473977418);
        parsed.Lon.Should().Be(85255792);
    }

    [Fact]
    public void TerrainData_RoundTrips()
    {
        var msg = new TerrainData { Lat = 473977418, Lon = 85255792, GridSpacing = 100, Gridbit = 0, Data0 = 4080, Data1 = 4090 };
        var buf = new byte[TerrainData.PayloadSize];
        msg.Serialize(buf);
        var parsed = TerrainData.Deserialize(buf);
        parsed.Gridbit.Should().Be(0);
        parsed.Data0.Should().Be(4080);
    }

    [Fact]
    public void ServoOutputRaw_RoundTrips()
    {
        var msg = new ServoOutputRaw { TimeUsec = 12345, Port = 0, Servo1Raw = 1500, Servo2Raw = 1500, Servo3Raw = 1000, Servo4Raw = 1000 };
        var buf = new byte[ServoOutputRaw.PayloadSize];
        msg.Serialize(buf);
        var parsed = ServoOutputRaw.Deserialize(buf);
        parsed.Servo1Raw.Should().Be(1500);
        parsed.Servo3Raw.Should().Be(1000);
    }

    [Fact]
    public void ActuatorControlTarget_RoundTrips()
    {
        var msg = new ActuatorControlTarget { TimeUsec = 99999, GroupMlx = 0, Controls0 = 0.5f, Controls1 = -0.5f, Controls7 = 0.8f };
        var buf = new byte[ActuatorControlTarget.PayloadSize];
        msg.Serialize(buf);
        var parsed = ActuatorControlTarget.Deserialize(buf);
        parsed.Controls0.Should().BeApproximately(0.5f, 1e-6f);
        parsed.Controls7.Should().BeApproximately(0.8f, 1e-6f);
    }

    [Fact]
    public void PowerStatus_RoundTrips()
    {
        var msg = new PowerStatus { Vcc = 5000, Vservo = 5100, Flags = 0b11 };
        var buf = new byte[PowerStatus.PayloadSize];
        msg.Serialize(buf);
        var parsed = PowerStatus.Deserialize(buf);
        parsed.Vcc.Should().Be(5000);
        parsed.Flags.Should().Be(0b11);
    }

    [Fact]
    public void AutopilotVersion_RoundTrips()
    {
        var msg = new AutopilotVersion { Capabilities = 0xFFFF, FlightSwVersion = 0x040300, VendorId = 0x26AC, ProductId = 0x0011, Uid = 12345678 };
        var buf = new byte[AutopilotVersion.PayloadSize];
        msg.Serialize(buf);
        var parsed = AutopilotVersion.Deserialize(buf);
        parsed.VendorId.Should().Be(0x26AC);
        parsed.Uid.Should().Be(12345678);
    }

    [Fact]
    public void HighresImu_RoundTrips()
    {
        var msg = new HighresImu { TimeUsec = 1234567890, Xacc = 0.1f, Yacc = 0.2f, Zacc = 9.81f, Temperature = 25.5f, FieldsUpdated = 0xFF };
        var buf = new byte[HighresImu.PayloadSize];
        msg.Serialize(buf);
        var parsed = HighresImu.Deserialize(buf);
        parsed.Zacc.Should().BeApproximately(9.81f, 1e-5f);
        parsed.Temperature.Should().BeApproximately(25.5f, 1e-5f);
        parsed.FieldsUpdated.Should().Be(0xFF);
    }

    [Fact]
    public void MountOrientation_RoundTrips()
    {
        var msg = new MountOrientation { TimeBootMs = 500, Roll = 0f, Pitch = -15f, Yaw = 90f };
        var buf = new byte[MountOrientation.PayloadSize];
        msg.Serialize(buf);
        var parsed = MountOrientation.Deserialize(buf);
        parsed.Pitch.Should().BeApproximately(-15f, 1e-5f);
        parsed.Yaw.Should().BeApproximately(90f, 1e-5f);
    }

    [Fact]
    public void CameraImageCaptured_RoundTrips()
    {
        var msg = new CameraImageCaptured
        {
            TimeUtc = 1700000000000000,
            TimeBootMs = 60000,
            Lat = 473977418,
            Lon = 85255792,
            Alt = 408000,
            RelativeAlt = 50000,
            ImageIndex = 7,
            CameraId = 1,
            CaptureResult = 1,
        };
        var buf = new byte[CameraImageCaptured.PayloadSize];
        msg.Serialize(buf);
        var parsed = CameraImageCaptured.Deserialize(buf);
        parsed.Lat.Should().Be(473977418);
        parsed.ImageIndex.Should().Be(7);
        parsed.CaptureResult.Should().Be(1);
    }

    [Fact]
    public void GpsRtcmData_RoundTrips()
    {
        var msg = new GpsRtcmData { Flags = 0x01, Len = 10 };
        var buf = new byte[GpsRtcmData.PayloadSize];
        msg.Serialize(buf);
        var parsed = GpsRtcmData.Deserialize(buf);
        parsed.Flags.Should().Be(0x01);
        parsed.Len.Should().Be(10);
    }

    [Fact]
    public void GlobalPositionIntCov_RoundTrips()
    {
        var msg = new GlobalPositionIntCov { TimeUsec = 9876543, Lat = 473977418, Lon = 85255792, Alt = 408000, RelativeAlt = 50000, Vx = 1.5f, Vz = 0f };
        var buf = new byte[GlobalPositionIntCov.PayloadSize];
        msg.Serialize(buf);
        var parsed = GlobalPositionIntCov.Deserialize(buf);
        parsed.Lat.Should().Be(473977418);
        parsed.Vx.Should().BeApproximately(1.5f, 1e-5f);
    }

    [Fact]
    public void SetPositionTargetLocalNed_RoundTrips()
    {
        var msg = new SetPositionTargetLocalNed { TimeBootMs = 100, X = 10f, Y = 5f, Z = -20f, TargetSystem = 1, TargetComponent = 1, CoordinateFrame = MavFrame.LocalNed };
        var buf = new byte[SetPositionTargetLocalNed.PayloadSize];
        msg.Serialize(buf);
        var parsed = SetPositionTargetLocalNed.Deserialize(buf);
        parsed.Z.Should().BeApproximately(-20f, 1e-5f);
        parsed.CoordinateFrame.Should().Be(MavFrame.LocalNed);
    }

    [Fact]
    public void PositionTargetLocalNed_RoundTrips()
    {
        var msg = new PositionTargetLocalNed { TimeBootMs = 100, X = 10f, Y = 5f, Z = -20f, CoordinateFrame = MavFrame.LocalNed };
        var buf = new byte[PositionTargetLocalNed.PayloadSize];
        msg.Serialize(buf);
        var parsed = PositionTargetLocalNed.Deserialize(buf);
        parsed.Z.Should().BeApproximately(-20f, 1e-5f);
    }
}
