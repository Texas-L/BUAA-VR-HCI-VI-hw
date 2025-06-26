using UnityEngine;
using UnityEngine.UI;
using Pico.Platform;
using Pico.Platform.Models;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.Windows;
using static UnityEngine.InputSystem.InputControlScheme.MatchResult;



#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
using System.Collections;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("绑定对象")]
    public Text debugText;      // 调试文本（可选）
    public Text airdoortmp;     //
    public Text indoortmp;      //
    public Text biglighttext;
    public Text mixlighttext;
    public Light biglight;
    public Light mixlight;

    [Header("ASR配置")]
    [Tooltip("自动停止ASR服务（设为false可强制持续监听）")]
    public bool autoStop = true;          // 控制是否自动停止ASR
    public int maxDuration = 60000;       // 最大监听时长（毫秒）

    private bool isAsrConfigured = false; // ASR引擎是否初始化成功
    private bool permissionRequested;     // 是否已请求过麦克风权限

    void Start()
    {
        // 初始权限检查
        RequestMicrophonePermission();
        UpdateDebugText("初始权限检查 ");
    }

    // ================== 权限管理 ==================
    #region 权限逻辑
    private void RequestMicrophonePermission()
    {
#if PLATFORM_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InitializeAndStartAsrSystem();
        }
        else
        {
            Permission.RequestUserPermission(Permission.Microphone);
            UpdateDebugText("权限弹窗！");
            StartCoroutine(CheckPermissionAfterRequest());
            permissionRequested = true;
        }
#else
        // 非Android平台直接初始化（测试用）
        InitializeAndStartAsrSystem();
#endif
    }

#if PLATFORM_ANDROID
    private IEnumerator CheckPermissionAfterRequest()
    {
        
        // 等待权限弹窗关闭
        yield return new WaitUntil(() => Application.isFocused);
        yield return new WaitForSeconds(0.1f);

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InitializeAndStartAsrSystem();
        }
        else
        {
            UpdateDebugText("麦克风权限被拒绝！");
        }
    }
#endif
    #endregion

    // ================== ASR核心逻辑 ==================
    #region ASR服务管理
    private void InitializeAndStartAsrSystem()
    {
        if (isAsrConfigured)
        {
            Debug.Log("ASR已初始化，跳过重复操作");
            return;
        }

        // 注册回调
        SpeechService.SetOnAsrResultCallback(HandleAsrResult);
        SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);

        // 初始化引擎
        AsrEngineInitResult result = SpeechService.InitAsrEngine();
        if (result == AsrEngineInitResult.Success)
        {
            isAsrConfigured = true;
            UpdateDebugText("就绪：请说出指令");
            StartAsrService();
        }
        else
        {
            UpdateDebugText($"初始化失败: {result}");
        }
    }

    private void StartAsrService()
    {
        SpeechService.StartAsr(autoStop, showPunctual: true, maxDuration);
        Debug.Log("ASR服务已启动");
    }

    private void StopAsrService()
    {
        if (isAsrConfigured)
        {
            SpeechService.StopAsr();
            Debug.Log("ASR服务已停止");
        }
    }
    #endregion

    // ================== 语音回调处理 ==================
    #region 语音事件处理
    private void HandleAsrResult(Message<AsrResult> msg)
    {
        if (!isActiveAndEnabled) return;

        if (msg.IsError)
        {
            UpdateDebugText($"ASR错误: {msg.GetError().Message}");
            return;
        }

        AsrResult result = msg.Data;
        if (result.IsFinalResult && !string.IsNullOrEmpty(result.Text))
        {
            string command = result.Text.Trim().ToLower();
            UpdateDebugText($"识别到: {command}");
            ProcessVoiceCommand(command);

            // 关键修复：处理完成后重启ASR服务
            if (autoStop) StartAsrService();
        }
    }

    private void HandleSpeechError(Message<SpeechError> msg)
    {
        UpdateDebugText($"语音错误: {msg.Data.Message}");
        if (msg.Data.Code == -402) // 权限错误
        {
            isAsrConfigured = false;
            RequestMicrophonePermission();
        }
        // 超时或无语音错误（假设Code=-1001）
        else if (msg.Data.Code == 1014)
        {
            UpdateDebugText("未检测到语音，重新启动监听...");
            StopAsrService();
            StartAsrService();
        }
        // 其他未知错误
        else
        {
            UpdateDebugText("请继续，我在听。");
            StopAsrService();
            StartAsrService();
        }
    }
    #endregion

    // ================== 指令处理 ==================
    #region 交互逻辑
    private void ProcessVoiceCommand(string command)
    {
        UpdateDebugText(command);
        if (command.Contains("情景模式"))
        {
            if (command.Contains("切换"))
            {
                if (command.Contains("离家模式"))
                {
                    UpdateDebugText("已将空调关闭");
                    airdoortmp.text = "关闭";
                }
                else if (command.Contains("在家模式"))
                {
                    UpdateDebugText("已将空调打开");
                    airdoortmp.text = "26°c";
                }
            }
        }
        if (command.Contains("空调温度") || command.Contains("室内温度"))
        {
            if (command.Contains("空调温度"))
            {
                if (command.Contains("太高了")) 
                {
                    airdoortmp.text = "26°c";
                    UpdateDebugText("已为您调整至26摄氏度");
                }
                else if (command.Contains("太低了"))
                {
                    airdoortmp.text = "32°c";
                    UpdateDebugText("已为您调整至32摄氏度");
                }

            }
            if (command.Contains("室内温度"))
            {
                if (command.Contains("多少度"))
                {
                    UpdateDebugText(indoortmp.text);
                }
            }
        }
        if(command.Contains("大灯") || command.Contains("环境灯"))
        {
            if (command.Contains("大灯"))
            {
                if (command.Contains("打开"))
                {
                    biglight.intensity = 1f;
                    biglighttext.text = "大灯 打开";
                }
                else if(command.Contains("关闭"))
                {
                    biglight.intensity = 0f;
                    biglighttext.text = "大灯 关闭";
                }
            }
            if (command.Contains("环境灯"))
            {
                if (command.Contains("打开"))
                {
                    mixlight.intensity = 1f;
                    mixlighttext.text = "环境灯 打开";
                }
                else if (command.Contains("关闭"))
                {
                    biglight.intensity = 0f;
                    mixlighttext.text = "环境灯 关闭";
                }
            }
        }
    }
    #endregion

    // ================== 生命周期管理 ==================
    #region 启用/禁用处理
    private void OnEnable()
    {
        if (isAsrConfigured)
        {
            // 重新注册回调并启动服务
            SpeechService.SetOnAsrResultCallback(HandleAsrResult);
            SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);
            StartAsrService();
        }
    }

    private void OnDisable()
    {
        // 清理回调并停止服务
        SpeechService.SetOnAsrResultCallback(null);
        SpeechService.SetOnSpeechErrorCallback(null);
        StopAsrService();
    }

    private void OnDestroy()
    {
        StopAsrService();
    }
    #endregion

    // ================== 辅助方法 ==================
    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
        Debug.Log(message);
    }
}