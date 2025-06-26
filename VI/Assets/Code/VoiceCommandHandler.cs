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
    public Text[] shoppingItems = new Text[12];
    public Text debugText;      // �����ı�����ѡ��

    [Header("ASR����")]
    [Tooltip("�Զ�ֹͣASR������Ϊfalse��ǿ�Ƴ���������")]
    public bool autoStop = true;          // �����Ƿ��Զ�ֹͣASR
    public int maxDuration = 60000;       // ������ʱ�������룩

    private bool isAsrConfigured = false; // ASR�����Ƿ��ʼ���ɹ�
    private bool permissionRequested;     // �Ƿ����������˷�Ȩ��

    private static readonly Regex ChineseNumberPattern =
        new Regex(@"([һ�����������߰˾�ʮ]+)([��ֻ����ƿ�д�Ͱ��]+)");
    private static readonly Regex ChineseNumber =
        new Regex(@"([һ�����������߰˾�ʮ]+)");

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
        if (command.Contains("ţ���ɿ���") ||
            command.Contains("����") ||
            command.Contains("��Ȫˮ") ||
            command.Contains("�ͽ�ֽ") ||
            command.Contains("����") ||
            command.Contains("������") ||
            command.Contains("�ɿ�����") ||
            command.Contains("��Ƭ") ||
            command.Contains("����")
            )
        {
            string commodity = "";
            if (command.Contains("ţ���ɿ���")) commodity = "ţ���ɿ���";
            else if (command.Contains("����")) commodity = "����";
            else if (command.Contains("��Ȫˮ")) commodity = "��Ȫˮ";
            else if (command.Contains("�ͽ�ֽ")) commodity = "�ͽ�ֽ";
            else if (command.Contains("����")) commodity = "����";
            else if (command.Contains("������")) commodity = "������";
            else if (command.Contains("�ɿ�����")) commodity = "�ɿ�����";
            else if (command.Contains("��Ƭ")) commodity = "��Ƭ";
            else if (command.Contains("����")) commodity = "����";

            MatchCollection matches = ChineseNumberPattern.Matches(command);
            if (matches.Count > 0)
            {

                for (int i = 0; i < 12; i++)
                {
                    if (shoppingItems[i].text == "")
                    {
                        string chineseNumber = matches[0].Groups[1].Value; // �������ֲ���
                        string unit = matches[0].Groups[2].Value;          // ���ʲ���
                        switch (chineseNumber)
                        {
                            case "һ": chineseNumber = "1"; break;
                            case "��": chineseNumber = "2"; break;
                            case "��": chineseNumber = "3"; break;
                            case "��": chineseNumber = "4"; break;
                            case "��": chineseNumber = "5"; break;
                            case "��": chineseNumber = "6"; break;
                            case "��": chineseNumber = "7"; break;
                            case "��": chineseNumber = "8"; break;
                            case "��": chineseNumber = "9"; break;
                        }
                        shoppingItems[i].text = $"{commodity}  {chineseNumber}{unit}";
                        break;
                    }
                }
            }
        }
        else if (command.Contains("ɾ��"))
        {
            MatchCollection matches = ChineseNumber.Matches(command);
            if (command.Contains("ȫ��"))
            {
                for (int i = 0; i < 12; i++)
                {
                    shoppingItems[i].text = "";
                }
            }
            else if (matches.Count > 0)
            {
                int index = 0;
                string chineseNumber = matches[0].Groups[1].Value;
                switch (chineseNumber)
                {
                    case "һ": index = 0; break;
                    case "��": index = 1; break;
                    case "��": index = 2; break;
                    case "��": index = 3; break;
                    case "��": index = 4; break;
                    case "��": index = 5; break;
                    case "��": index = 6; break;
                    case "��": index = 7; break;
                }
                shoppingItems[index].text = "";
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