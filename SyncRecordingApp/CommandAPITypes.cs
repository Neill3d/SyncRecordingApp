
/// <summary>
/// CommandAPITypes.cs
/// Declaration of command API input packets which have to be serialized into json and transferred as a byte array
/// github repository - https://github.com/Neill3d/SyncRecordingApp
/// Developed by Sergei <Neill3d> Solokhin 2022
/// </summary>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace SyncRecordingApp
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BalancedNewtonPose
    {
        [EnumMember(Value = "tpose")]
        TPose = 0,

        [EnumMember(Value = "straight-arms-down")]
        StraightArmsDown = 1,

        [EnumMember(Value = "straight-arms-forward")]
        StraightArmsForward = 2,
    }


    [Serializable]
    public class CommandAPIRecordingInput
    {
        [JsonProperty(PropertyName = "filename")]
        public string filename = "";    // actor clip name or filename for recording

        [JsonProperty(PropertyName = "time")]
        public string time = "00:00:00:00"; // smart suit operation time in SMPTE format

        [JsonProperty(PropertyName = "frame_rate")]
        public float frameRate = 30.0f;

        [JsonProperty(PropertyName = "back_to_live")]
        public bool backToLive = false;

        public override string ToString()
        {
            return $"{filename}, {time}, {frameRate}, {backToLive}";
        }
    }


    /// <summary>
    /// These are options we pass within a command, to do some remote control over device, like calibration
    /// </summary>
    [Serializable]
    public class CommandAPICalibrationInput
    {
        [JsonProperty(PropertyName = "device_id")]
        public string deviceID; // the live input device hubName that the command should target
        
        [JsonProperty(PropertyName = "countdown_delay")]
        public int countdownDelay = -1;  // countdown in seconds

        [JsonProperty(PropertyName = "skip_suit")]
        public bool skipSuit = false; // should we skip suit from a processing (calibration)

        [JsonProperty(PropertyName = "skip_gloves")]
        public bool skipGloves = false; // should we skip gloves from a processing (calibration)

        [JsonProperty(PropertyName = "use_custom_pose")]
        public bool useCustomPose = false;

        public BalancedNewtonPose pose = BalancedNewtonPose.StraightArmsDown;

        public override string ToString()
        {
            return $"{deviceID}, {countdownDelay}";
        }
    }
}
