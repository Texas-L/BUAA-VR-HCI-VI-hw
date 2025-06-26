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
    [Header("�󶨶���")]
    public Text debugText;      // �����ı�����ѡ��
    public Text airdoortmp;     //
    public Text indoortmp;      //
    public Text biglighttext;
    public Text mixlighttext;
    public Light biglight;
    public Light mixlight;

    [Header("ASR����")]
    [Tooltip("�Զ�ֹͣASR������Ϊfalse��ǿ�Ƴ���������")]
    public bool autoStop = true;          // �����Ƿ��Զ�ֹͣASR
    public int maxDuration = 60000;       // ������ʱ�������룩

    private bool isAsrConfigured = false; // ASR�����Ƿ��ʼ���ɹ�
    private bool permissionRequested;     // �Ƿ����������˷�Ȩ��

    void Start()
    {
        // ��ʼȨ�޼��
        RequestMicrophonePermission();
        UpdateDebugText("��ʼȨ�޼�� ");
    }

    // ================== Ȩ�޹��� ==================
    #region Ȩ���߼�
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
            UpdateDebugText("Ȩ�޵�����");
            StartCoroutine(CheckPermissionAfterRequest());
            permissionRequested = true;
        }
#else
        // ��Androidƽֱ̨�ӳ�ʼ���������ã�
        InitializeAndStartAsrSystem();
#endif
    }

#if PLATFORM_ANDROID
    private IEnumerator CheckPermissionAfterRequest()
    {
        
        // �ȴ�Ȩ�޵����ر�
        yield return new WaitUntil(() => Application.isFocused);
        yield return new WaitForSeconds(0.1f);

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InitializeAndStartAsrSystem();
        }
        else
        {
            UpdateDebugText("��˷�Ȩ�ޱ��ܾ���");
        }
    }
#endif
    #endregion

    // ================== ASR�����߼� ==================
    #region ASR�������
    private void InitializeAndStartAsrSystem()
    {
        if (isAsrConfigured)
        {
            Debug.Log("ASR�ѳ�ʼ���������ظ�����");
            return;
        }

        // ע��ص�
        SpeechService.SetOnAsrResultCallback(HandleAsrResult);
        SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);

        // ��ʼ������
        AsrEngineInitResult result = SpeechService.InitAsrEngine();
        if (result == AsrEngineInitResult.Success)
        {
            isAsrConfigured = true;
            UpdateDebugText("��������˵��ָ��");
            StartAsrService();
        }
        else
        {
            UpdateDebugText($"��ʼ��ʧ��: {result}");
        }
    }

    private void StartAsrService()
    {
        SpeechService.StartAsr(autoStop, showPunctual: true, maxDuration);
        Debug.Log("ASR����������");
    }

    private void StopAsrService()
    {
        if (isAsrConfigured)
        {
            SpeechService.StopAsr();
            Debug.Log("ASR������ֹͣ");
        }
    }
    #endregion

    // ================== �����ص����� ==================
    #region �����¼�����
    private void HandleAsrResult(Message<AsrResult> msg)
    {
        if (!isActiveAndEnabled) return;

        if (msg.IsError)
        {
            UpdateDebugText($"ASR����: {msg.GetError().Message}");
            return;
        }

        AsrResult result = msg.Data;
        if (result.IsFinalResult && !string.IsNullOrEmpty(result.Text))
        {
            string command = result.Text.Trim().ToLower();
            UpdateDebugText($"ʶ��: {command}");
            ProcessVoiceCommand(command);

            // �ؼ��޸���������ɺ�����ASR����
            if (autoStop) StartAsrService();
        }
    }

    private void HandleSpeechError(Message<SpeechError> msg)
    {
        UpdateDebugText($"��������: {msg.Data.Message}");
        if (msg.Data.Code == -402) // Ȩ�޴���
        {
            isAsrConfigured = false;
            RequestMicrophonePermission();
        }
        // ��ʱ�����������󣨼���Code=-1001��
        else if (msg.Data.Code == 1014)
        {
            UpdateDebugText("δ��⵽������������������...");
            StopAsrService();
            StartAsrService();
        }
        // ����δ֪����
        else
        {
            UpdateDebugText("���������������");
            StopAsrService();
            StartAsrService();
        }
    }
    #endregion

    // ================== ָ��� ==================
    #region �����߼�
    private void ProcessVoiceCommand(string command)
    {
        UpdateDebugText(command);
        if (command.Contains("�龰ģʽ"))
        {
            if (command.Contains("�л�"))
            {
                if (command.Contains("���ģʽ"))
                {
                    UpdateDebugText("�ѽ��յ��ر�");
                    airdoortmp.text = "�ر�";
                }
                else if (command.Contains("�ڼ�ģʽ"))
                {
                    UpdateDebugText("�ѽ��յ���");
                    airdoortmp.text = "26��c";
                }
            }
        }
        if (command.Contains("�յ��¶�") || command.Contains("�����¶�"))
        {
            if (command.Contains("�յ��¶�"))
            {
                if (command.Contains("̫����")) 
                {
                    airdoortmp.text = "26��c";
                    UpdateDebugText("��Ϊ��������26���϶�");
                }
                else if (command.Contains("̫����"))
                {
                    airdoortmp.text = "32��c";
                    UpdateDebugText("��Ϊ��������32���϶�");
                }

            }
            if (command.Contains("�����¶�"))
            {
                if (command.Contains("���ٶ�"))
                {
                    UpdateDebugText(indoortmp.text);
                }
            }
        }
        if(command.Contains("���") || command.Contains("������"))
        {
            if (command.Contains("���"))
            {
                if (command.Contains("��"))
                {
                    biglight.intensity = 1f;
                    biglighttext.text = "��� ��";
                }
                else if(command.Contains("�ر�"))
                {
                    biglight.intensity = 0f;
                    biglighttext.text = "��� �ر�";
                }
            }
            if (command.Contains("������"))
            {
                if (command.Contains("��"))
                {
                    mixlight.intensity = 1f;
                    mixlighttext.text = "������ ��";
                }
                else if (command.Contains("�ر�"))
                {
                    biglight.intensity = 0f;
                    mixlighttext.text = "������ �ر�";
                }
            }
        }
    }
    #endregion

    // ================== �������ڹ��� ==================
    #region ����/���ô���
    private void OnEnable()
    {
        if (isAsrConfigured)
        {
            // ����ע��ص�����������
            SpeechService.SetOnAsrResultCallback(HandleAsrResult);
            SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);
            StartAsrService();
        }
    }

    private void OnDisable()
    {
        // ����ص���ֹͣ����
        SpeechService.SetOnAsrResultCallback(null);
        SpeechService.SetOnSpeechErrorCallback(null);
        StopAsrService();
    }

    private void OnDestroy()
    {
        StopAsrService();
    }
    #endregion

    // ================== �������� ==================
    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
        Debug.Log(message);
    }
}